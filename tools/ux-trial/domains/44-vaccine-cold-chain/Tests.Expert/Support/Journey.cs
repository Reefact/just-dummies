using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using Freight.Domain;
using JustDummies;
using Warehouse.Domain;

namespace ColdChain.Tests.Expert.Support;

/// <summary>
///     A consignment of one pallet on a single-leg itinerary, with the frame the behaviour tests share: it leaves at
///     <see cref="Start" />, its product is labelled <see cref="Label" />, its logger's certificate stays valid for
///     three days. Its budget is drawn, but never finer than the logger can resolve (the domain demands it): a test that
///     leans on the budget says which one.
///     Everything else about it is drawn.
/// </summary>
internal sealed class Journey {

    public static readonly DateTimeOffset   Start = new DateTimeOffset(2026, 3, 9, 8, 0, 0, TimeSpan.Zero);
    public static readonly TemperatureRange Label = new TemperatureRange(2m, 8m);

    private readonly int _loggerIntervalMinutes;

    private Journey(ColdChainConsignment consignment, CustodianId carrier, int loggerIntervalMinutes) {
        Consignment            = consignment;
        Carrier                = carrier;
        _loggerIntervalMinutes = loggerIntervalMinutes;
    }

    public ColdChainConsignment Consignment { get; }

    public CustodianId Carrier { get; }

    public ColdChainPallet Pallet => Consignment.Pallets[0];

    public Sscc PalletId => Pallet.Sscc;

    /// <summary>The generator of such consignments, for a test that refines what it is about.</summary>
    /// <param name="loggerIntervalMinutes">How often the logger records: it decides which gaps in the record are tolerated.</param>
    public static AnyColdChainConsignment Planned(int loggerIntervalMinutes = 5) {
        return Any.ColdChainConsignment()
                  .WithProduct(product => product.WithLabelledRange(Label).WithBudget(budget => budget.WithMaxSingleExcursionMinutes(allowance => allowance.GreaterThanOrEqualTo(loggerIntervalMinutes))))
                  .WithShipment(shipment => shipment.WithLegCount(1))
                  .WithPalletCount(1)
                  .WithEachPallet(pallet => pallet.WithLogger(logger => logger.WithRecordingIntervalMinutes(loggerIntervalMinutes)))
                  .ForTransit(Start, Start.AddDays(3));
    }

    public static Journey Dispatched() {
        return Dispatched(planned => planned);
    }

    public static Journey Dispatched(Func<AnyColdChainConsignment, AnyColdChainConsignment> refine, int loggerIntervalMinutes = 5) {
        ColdChainConsignment consignment = refine(Planned(loggerIntervalMinutes)).Generate();
        CustodianId          carrier     = Any.CustodianId().Generate();

        consignment.Dispatch(carrier, Start);

        return new Journey(consignment, carrier, loggerIntervalMinutes);
    }

    public static LoggerReading ReadingOf(ColdChainPallet pallet, int minutesAfterStart, decimal celsius) {
        return Any.LoggerReading().WithLogger(pallet.Logger.Id).WithTakenAt(Start.AddMinutes(minutesAfterStart)).WithCelsius(celsius).Generate();
    }

    public LoggerReading ReadingAt(int minutesAfterStart, decimal celsius) {
        return ReadingOf(Pallet, minutesAfterStart, celsius);
    }

    public void Record(int minutesAfterStart, decimal celsius) {
        Consignment.RecordReading(PalletId, ReadingAt(minutesAfterStart, celsius));
    }

    public void Record(ColdChainPallet pallet, int minutesAfterStart, decimal celsius) {
        Consignment.RecordReading(pallet.Sscc, ReadingOf(pallet, minutesAfterStart, celsius));
    }

    public void RecordAll(params (int Minute, decimal Celsius)[] readings) {
        foreach ((int minute, decimal celsius) in readings) {
            Record(minute, celsius);
        }
    }

    /// <summary>One reading per logger interval between the two instants, both included.</summary>
    public void RecordSteady(int fromMinute, int toMinute, decimal celsius = 5m) {
        for (int minute = fromMinute; minute <= toMinute; minute += _loggerIntervalMinutes) {
            Record(minute, celsius);
        }
    }

    /// <summary>A warehouse at the end of the itinerary whose cold room keeps the product within its label.</summary>
    public AnyReceivingWarehouse Warehouse() {
        return WarehouseFor(Consignment);
    }

    public static AnyReceivingWarehouse WarehouseFor(ColdChainConsignment consignment) {
        return Any.ReceivingWarehouse()
                  .WithLocation(consignment.Shipment.Legs[^1].Destination)
                  .WithColdRoom(coldRoom => coldRoom.WithMinCelsius(min => min.GreaterThanOrEqualTo(consignment.Product.LabelledRange.MinCelsius).WithScale(1))
                                                    .WithMaxCelsius(max => max.LessThanOrEqualTo(consignment.Product.LabelledRange.MaxCelsius).WithScale(1)));
    }

    public ReceivingWarehouse Deliver(int minutesAfterStart) {
        ReceivingWarehouse warehouse = Warehouse().Generate();

        Consignment.Deliver(warehouse, Start.AddMinutes(minutesAfterStart));

        return warehouse;
    }

}
