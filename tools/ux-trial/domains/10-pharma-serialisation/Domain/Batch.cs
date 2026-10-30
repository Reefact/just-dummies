namespace Serialisation.Domain;

/// <summary>A manufactured lot of one product, and the register of the serials commissioned in it.</summary>
public sealed class Batch {

    private readonly Dictionary<SerialNumber, SerialisedPack> _packs = new Dictionary<SerialNumber, SerialisedPack>();

    public Batch(Gtin gtin, BatchNumber number, DateOnly manufacturedOn, DateOnly expiresOn, int plannedQuantity, ReimbursementPrice price) {
        ArgumentNullException.ThrowIfNull(gtin);
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(price);
        if (expiresOn <= manufacturedOn) {
            throw new ArgumentException($"A batch must expire after it is made ({manufacturedOn} -> {expiresOn}).", nameof(expiresOn));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(plannedQuantity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(plannedQuantity, 500_000);

        Gtin            = gtin;
        Number          = number;
        ManufacturedOn  = manufacturedOn;
        ExpiresOn       = expiresOn;
        PlannedQuantity = plannedQuantity;
        Price           = price;
    }

    public Gtin               Gtin            { get; }
    public BatchNumber        Number          { get; }
    public DateOnly           ManufacturedOn  { get; }
    public DateOnly           ExpiresOn       { get; }
    public int                PlannedQuantity { get; }
    public ReimbursementPrice Price           { get; }

    public IReadOnlyCollection<SerialisedPack> Packs => _packs.Values;

    public SerialisedPack Commission(SerialNumber serial) {
        ArgumentNullException.ThrowIfNull(serial);
        if (_packs.ContainsKey(serial)) {
            throw new InvalidOperationException($"Serial {serial} is already commissioned in batch {Number}.");
        }
        if (_packs.Count >= PlannedQuantity) {
            throw new InvalidOperationException($"Batch {Number} is full ({PlannedQuantity} packs).");
        }

        SerialisedPack pack = new SerialisedPack(Gtin, Number, ExpiresOn, serial);
        _packs.Add(serial, pack);
        return pack;
    }

    public IReadOnlyList<SerialisedPack> CommissionAll(IEnumerable<SerialNumber> serials) {
        return serials.Select(Commission).ToList();
    }

    public VerificationOutcome Verify(SerialNumber serial, DateOnly today) {
        if (!_packs.TryGetValue(serial, out SerialisedPack? pack)) {
            return VerificationOutcome.UnknownSerial;
        }
        if (pack.Status != PackStatus.Active) {
            return VerificationOutcome.AlreadyDecommissioned;
        }

        return today > ExpiresOn ? VerificationOutcome.Expired : VerificationOutcome.Authentic;
    }

    public void Dispense(SerialNumber serial, DateOnly today) {
        VerificationOutcome outcome = Verify(serial, today);
        if (outcome != VerificationOutcome.Authentic) {
            throw new InvalidOperationException($"Pack {serial} cannot be dispensed: {outcome}.");
        }

        _packs[serial].Decommission(PackStatus.Dispensed);
    }

    /// <summary>Recalls every pack still in the supply chain and returns how many were recalled.</summary>
    public int Recall() {
        List<SerialisedPack> active = _packs.Values.Where(pack => pack.Status == PackStatus.Active).ToList();
        foreach (SerialisedPack pack in active) {
            pack.Decommission(PackStatus.Recalled);
        }

        return active.Count;
    }

    /// <summary>The share of the shelf life left on <paramref name="today"/>, between 0 and 1, rounded to 2 places.</summary>
    public decimal RemainingShelfLifeRatio(DateOnly today) {
        int total = ExpiresOn.DayNumber - ManufacturedOn.DayNumber;
        int left  = Math.Clamp(ExpiresOn.DayNumber - today.DayNumber, 0, total);
        return Math.Round((decimal)left / total, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>The value still reimbursable: active packs times the unit price.</summary>
    public decimal ActiveStockValue() {
        return _packs.Values.Count(pack => pack.Status == PackStatus.Active) * Price.Amount;
    }

}
