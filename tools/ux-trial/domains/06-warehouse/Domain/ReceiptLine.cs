namespace Warehouse.Domain;

/// <summary>One trade item on a pallet: what the advance shipping notice announced, and what was counted.</summary>
public sealed class ReceiptLine {

    public ReceiptLine(Ean13 gtin, Quantity expected, Quantity received, LotNumber lot, DateOnly bestBefore, decimal unitWeightKg) {
        ArgumentNullException.ThrowIfNull(gtin);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(received);
        ArgumentNullException.ThrowIfNull(lot);
        if (unitWeightKg <= 0m) {
            throw new ArgumentOutOfRangeException(nameof(unitWeightKg), unitWeightKg, "A unit weighs something.");
        }

        Gtin         = gtin;
        Expected     = expected;
        Received     = received;
        Lot          = lot;
        BestBefore   = bestBefore;
        UnitWeightKg = unitWeightKg;
    }

    public Ean13     Gtin         { get; }
    public Quantity  Expected     { get; }
    public Quantity  Received     { get; }
    public LotNumber Lot          { get; }
    public DateOnly  BestBefore   { get; }
    public decimal   UnitWeightKg { get; }

    /// <summary>Positive when more was received than announced, negative when short.</summary>
    public int Discrepancy => Received.Units - Expected.Units;

    public decimal WeightKg => Received.Units * UnitWeightKg;

}
