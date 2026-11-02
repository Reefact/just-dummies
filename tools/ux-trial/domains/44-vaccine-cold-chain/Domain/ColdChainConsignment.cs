using Freight.Domain;
using Serialisation.Domain;
using Warehouse.Domain;
using FreightShipment = Freight.Domain.Shipment;

namespace ColdChain.Domain;

/// <summary>
/// A consignment of vaccine on SSCC pallets, carried in a reefer over the legs of a freight shipment, each pallet
/// watched by its own data logger. It tracks who holds it on each leg, charges every excursion outside the label to
/// the product's stability budget, writes a pallet off at once when it breaches a hard limit, and gives the
/// receiving warehouse its release, quarantine or rejection decision per pallet.
/// </summary>
public sealed class ColdChainConsignment
{
    /// <summary>A 40 ft reefer takes twenty pallets.</summary>
    public const int MaxPallets = 20;

    private readonly ColdChainPallet[] _pallets;
    private readonly Dictionary<Sscc, TemperatureHistory> _histories = new();
    private readonly Dictionary<Sscc, ReleaseDecision> _decisions = new();
    private readonly List<LegCustody> _custody = new();

    public ColdChainConsignment(ConsignmentId id, VaccineProduct product, FreightShipment shipment, IReadOnlyList<ColdChainPallet> pallets)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(pallets);
        if (shipment.Status != ShipmentStatus.Booked)
        {
            throw new ArgumentException($"A consignment is planned on a booked shipment, not a {shipment.Status} one.", nameof(shipment));
        }

        if (shipment.ContainerType != ContainerType.Reefer40)
        {
            throw new ArgumentException($"Vaccines travel in a temperature-controlled reefer, not a {shipment.ContainerType}.", nameof(shipment));
        }

        if (pallets.Count is 0 or > MaxPallets)
        {
            throw new ArgumentException($"A consignment holds 1 to {MaxPallets} pallets, not {pallets.Count}.", nameof(pallets));
        }

        if (pallets.Any(p => p is null))
        {
            throw new ArgumentException("A consignment cannot list a missing pallet.", nameof(pallets));
        }

        if (pallets.Select(p => p.Sscc).Distinct().Count() != pallets.Count)
        {
            throw new ArgumentException("An SSCC appears twice in the consignment.", nameof(pallets));
        }

        if (pallets.Select(p => p.Logger.Id).Distinct().Count() != pallets.Count)
        {
            throw new ArgumentException("Each pallet travels with its own logger.", nameof(pallets));
        }

        int packCount = pallets.Sum(p => p.PackCount);
        if (pallets.SelectMany(p => p.Packs).Select(p => p.Serial).Distinct().Count() != packCount)
        {
            throw new ArgumentException("A serialised pack cannot be on two pallets.", nameof(pallets));
        }

        foreach (ColdChainPallet pallet in pallets)
        {
            if (!pallet.Gtin.Equals(product.Gtin))
            {
                throw new ArgumentException($"Pallet {pallet.Sscc} carries {pallet.Gtin}, not {product.Gtin}.", nameof(pallets));
            }

            int allowance = product.Budget.MaxSingleExcursionMinutes;
            if (allowance > 0 && pallet.Logger.RecordingIntervalMinutes > allowance)
            {
                throw new ArgumentException(
                    $"Logger {pallet.Logger.Id} records every {pallet.Logger.RecordingIntervalMinutes} min and cannot resolve a {allowance}-min excursion allowance.",
                    nameof(pallets));
            }

            _histories.Add(pallet.Sscc, new TemperatureHistory(pallet.Logger, product.LabelledRange));
        }

        Id = id;
        Product = product;
        Shipment = shipment;
        _pallets = pallets.ToArray();
        Status = ConsignmentStatus.Planned;
    }

    public ConsignmentId Id { get; }

    public VaccineProduct Product { get; }

    public FreightShipment Shipment { get; }

    public IReadOnlyList<ColdChainPallet> Pallets => _pallets;

    public ConsignmentStatus Status { get; private set; }

    public DateTimeOffset? DispatchedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public ReceivingWarehouse? ReceivedBy { get; private set; }

    public IReadOnlyList<LegCustody> Custody => _custody;

    public IReadOnlyCollection<ReleaseDecision> Decisions => _decisions.Values;

    public Leg? CurrentLeg => Status == ConsignmentStatus.InTransit ? _custody[^1].Leg : null;

    public int TotalPacks => _pallets.Sum(p => p.PackCount);

    /// <summary>Hands the consignment to the carrier of the first leg: every logger calibrated and every pack in date that day.</summary>
    public void Dispatch(CustodianId carrier, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(carrier);
        if (Status != ConsignmentStatus.Planned)
        {
            throw Refuse(ColdChainRefusal.NotPlanned, $"Consignment {Id} is {Status}, it cannot be dispatched again.");
        }

        if (Shipment.Status != ShipmentStatus.Booked)
        {
            throw Refuse(ColdChainRefusal.ShipmentNotBooked, $"Shipment {Shipment.Id} is {Shipment.Status}.");
        }

        DateOnly day = DateOnly.FromDateTime(at.UtcDateTime);
        foreach (ColdChainPallet pallet in _pallets)
        {
            if (!pallet.Logger.IsCalibratedOn(day))
            {
                throw Refuse(ColdChainRefusal.LoggerCalibrationExpired, $"Logger {pallet.Logger.Id} on {pallet.Sscc} is not calibrated on {day}.");
            }

            if (pallet.EarliestExpiry < day)
            {
                throw Refuse(ColdChainRefusal.PackExpiredBeforeDispatch, $"Pallet {pallet.Sscc} carries packs expired on {pallet.EarliestExpiry}.");
            }
        }

        Shipment.Depart(at);
        _custody.Add(new LegCustody(0, Shipment.Legs[0], carrier, at));
        DispatchedAt = at;
        Status = ConsignmentStatus.InTransit;
    }

    /// <summary>
    /// Records a reading of a pallet's logger, taken between dispatch and delivery, after the previous one.
    /// A reading beyond a hard limit writes the pallet off at once: it is rejected there and then.
    /// </summary>
    public void RecordReading(Sscc pallet, LoggerReading reading)
    {
        ArgumentNullException.ThrowIfNull(pallet);
        ArgumentNullException.ThrowIfNull(reading);
        if (Status == ConsignmentStatus.Planned)
        {
            throw Refuse(ColdChainRefusal.NotDispatched, $"Consignment {Id} has not been dispatched.");
        }

        TemperatureHistory history = HistoryFor(pallet);
        if (_decisions.ContainsKey(pallet))
        {
            throw Refuse(ColdChainRefusal.PalletAlreadyDecided, $"Pallet {pallet} is already {_decisions[pallet].Outcome}.");
        }

        if (!reading.Logger.Equals(history.Logger.Id))
        {
            throw Refuse(ColdChainRefusal.LoggerMismatch, $"Pallet {pallet} travels with logger {history.Logger.Id}, not {reading.Logger}.");
        }

        if (reading.TakenAt < DispatchedAt || reading.TakenAt > DeliveredAt)
        {
            throw Refuse(ColdChainRefusal.ReadingOutsideTransit, $"Reading at {reading.TakenAt:u} falls outside the consignment's transit.");
        }

        if (history.Last is LoggerReading last && reading.TakenAt <= last.TakenAt)
        {
            throw Refuse(ColdChainRefusal.ReadingOutOfOrder, $"Reading at {reading.TakenAt:u} does not follow the last one at {last.TakenAt:u}.");
        }

        history.Record(reading);
        if (reading.BreachesHardLimit)
        {
            _decisions.Add(pallet, new ReleaseDecision(pallet, ReleaseOutcome.Rejected, [DecisionReason.HardLimitBreached], history.MinutesOutsideRange, null));
            CloseIfComplete();
        }
    }

    /// <summary>Ends the current leg and gives the next one to <paramref name="custodian"/>, who may be the same carrier.</summary>
    public void HandOverToNextLeg(CustodianId custodian, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(custodian);
        if (Status != ConsignmentStatus.InTransit)
        {
            throw Refuse(ColdChainRefusal.NotInTransit, $"Consignment {Id} is {Status}.");
        }

        LegCustody current = _custody[^1];
        if (current.LegIndex == Shipment.Legs.Count - 1)
        {
            throw Refuse(ColdChainRefusal.NoFurtherLeg, $"Leg {current.LegIndex} is the last leg of shipment {Shipment.Id}.");
        }

        if (at < current.AcceptedAt)
        {
            throw Refuse(ColdChainRefusal.BeforeCurrentCustody, $"{current.Custodian} only took custody at {current.AcceptedAt:u}.");
        }

        current.Release(at);
        int next = current.LegIndex + 1;
        _custody.Add(new LegCustody(next, Shipment.Legs[next], custodian, at));
    }

    /// <summary>Delivers to the warehouse at the end of the last leg, whose cold room must keep the product within its label.</summary>
    public void Deliver(ReceivingWarehouse warehouse, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        if (Status != ConsignmentStatus.InTransit)
        {
            throw Refuse(ColdChainRefusal.NotInTransit, $"Consignment {Id} is {Status}.");
        }

        LegCustody current = _custody[^1];
        if (current.LegIndex != Shipment.Legs.Count - 1)
        {
            throw Refuse(ColdChainRefusal.ItineraryIncomplete, $"The consignment is still on leg {current.LegIndex} of {Shipment.Legs.Count}.");
        }

        UnLocode destination = Shipment.Legs[^1].Destination;
        if (!warehouse.Location.Equals(destination))
        {
            throw Refuse(ColdChainRefusal.WrongDestination, $"The shipment ends at {destination}, not at {warehouse.Location}.");
        }

        if (!Product.CanBeStoredAt(warehouse.ColdRoom))
        {
            throw Refuse(
                ColdChainRefusal.ColdRoomOutOfSpecification,
                $"A cold room at {warehouse.ColdRoom.MinCelsius}-{warehouse.ColdRoom.MaxCelsius} °C does not keep {Product.Name} within {Product.LabelledRange.MinCelsius}-{Product.LabelledRange.MaxCelsius} °C.");
        }

        if (at < current.AcceptedAt)
        {
            throw Refuse(ColdChainRefusal.BeforeCurrentCustody, $"{current.Custodian} only took custody at {current.AcceptedAt:u}.");
        }

        current.Release(at);
        Shipment.Deliver(at);
        ReceivedBy = warehouse;
        DeliveredAt = at;
        Status = ConsignmentStatus.Delivered;
        CloseIfComplete();
    }

    /// <summary>
    /// The receiving warehouse's decision on one delivered pallet, from its temperature record, its packs' expiry
    /// and its logger's calibration. The consignment closes once every pallet is decided.
    /// </summary>
    public ReleaseDecision Decide(Sscc pallet)
    {
        ArgumentNullException.ThrowIfNull(pallet);
        TemperatureHistory history = HistoryFor(pallet);
        if (_decisions.ContainsKey(pallet))
        {
            throw Refuse(ColdChainRefusal.PalletAlreadyDecided, $"Pallet {pallet} is already {_decisions[pallet].Outcome}.");
        }

        if (Status != ConsignmentStatus.Delivered || DispatchedAt is not DateTimeOffset dispatchedAt || DeliveredAt is not DateTimeOffset deliveredAt || ReceivedBy is null)
        {
            throw Refuse(ColdChainRefusal.NotDelivered, $"Consignment {Id} is {Status}; pallets are decided at the receiving warehouse.");
        }

        ColdChainPallet palletOnBoard = _pallets.First(p => p.Sscc.Equals(pallet));
        DateOnly receivedOn = DateOnly.FromDateTime(deliveredAt.UtcDateTime);
        StabilityBudget budget = Product.Budget;
        int consumed = history.MinutesOutsideRange;
        List<DecisionReason> reasons = new();

        if (consumed > budget.CumulativeMinutes)
        {
            reasons.Add(DecisionReason.StabilityBudgetExhausted);
        }

        if (palletOnBoard.EarliestExpiry < receivedOn)
        {
            reasons.Add(DecisionReason.PackExpired);
        }
        else if (palletOnBoard.EarliestExpiry.DayNumber - receivedOn.DayNumber < Product.MinimumShelfLifeOnReceiptDays)
        {
            reasons.Add(DecisionReason.ShortShelfLife);
        }

        if (history.LongestExcursionMinutes > budget.MaxSingleExcursionMinutes)
        {
            reasons.Add(DecisionReason.SingleExcursionTooLong);
        }

        if (history.Excursions.Any(e => e.IsOpen))
        {
            reasons.Add(DecisionReason.ArrivedOutOfRange);
        }

        if (!history.Covers(dispatchedAt, deliveredAt))
        {
            reasons.Add(DecisionReason.IncompleteTemperatureRecord);
        }

        if (!palletOnBoard.Logger.IsCalibratedOn(receivedOn))
        {
            reasons.Add(DecisionReason.LoggerCalibrationLapsed);
        }

        ReleaseOutcome outcome = ReleaseDecision.OutcomeFor(reasons);
        ReleaseDecision decision = new(pallet, outcome, reasons, consumed, outcome == ReleaseOutcome.Quarantined ? ReceivedBy.QuarantineBay : null);
        _decisions.Add(pallet, decision);
        CloseIfComplete();
        return decision;
    }

    /// <summary>Decides every pallet not decided yet, in loading order.</summary>
    public IReadOnlyList<ReleaseDecision> DecideRemaining()
    {
        if (Status != ConsignmentStatus.Delivered)
        {
            throw Refuse(ColdChainRefusal.NotDelivered, $"Consignment {Id} is {Status}; pallets are decided at the receiving warehouse.");
        }

        return _pallets.Where(p => !_decisions.ContainsKey(p.Sscc)).Select(p => Decide(p.Sscc)).ToList();
    }

    public TemperatureHistory HistoryOf(Sscc pallet)
    {
        ArgumentNullException.ThrowIfNull(pallet);
        return _histories.TryGetValue(pallet, out TemperatureHistory? history)
            ? history
            : throw new ArgumentException($"Pallet {pallet} is not part of consignment {Id}.", nameof(pallet));
    }

    public ReleaseDecision? DecisionFor(Sscc pallet)
    {
        ArgumentNullException.ThrowIfNull(pallet);
        return _decisions.GetValueOrDefault(pallet);
    }

    /// <summary>Written off in transit: a reading beyond a hard limit destroyed the pallet's contents.</summary>
    public bool IsWrittenOff(Sscc pallet) =>
        DecisionFor(pallet) is { Outcome: ReleaseOutcome.Rejected } decision && decision.Reasons.Contains(DecisionReason.HardLimitBreached);

    /// <summary>The stability budget the pallet's packs still carry forward.</summary>
    public int RemainingBudgetMinutes(Sscc pallet) => Product.Budget.RemainingAfter(HistoryOf(pallet).MinutesOutsideRange);

    /// <summary>Who held the consignment at <paramref name="instant"/>, if anyone along the itinerary did.</summary>
    public CustodianId? CustodianAt(DateTimeOffset instant) => _custody.FirstOrDefault(c => c.Covers(instant))?.Custodian;

    /// <summary>Every excursion, on any pallet, that started while <paramref name="custodian"/> held the consignment.</summary>
    public IReadOnlyList<Excursion> ExcursionsAttributedTo(CustodianId custodian)
    {
        ArgumentNullException.ThrowIfNull(custodian);
        List<LegCustody> periods = _custody.Where(c => c.Custodian.Equals(custodian)).ToList();
        return _histories.Values
            .SelectMany(h => h.Excursions)
            .Where(e => periods.Any(c => c.Covers(e.StartedAt)))
            .OrderBy(e => e.StartedAt)
            .ToList();
    }

    private TemperatureHistory HistoryFor(Sscc pallet) =>
        _histories.TryGetValue(pallet, out TemperatureHistory? history)
            ? history
            : throw Refuse(ColdChainRefusal.UnknownPallet, $"Pallet {pallet} is not part of consignment {Id}.");

    private void CloseIfComplete()
    {
        if (Status == ConsignmentStatus.Delivered && _decisions.Count == _pallets.Length)
        {
            Status = ConsignmentStatus.Closed;
        }
    }

    private static ColdChainRefusedException Refuse(ColdChainRefusal refusal, string message) => new(refusal, message);
}
