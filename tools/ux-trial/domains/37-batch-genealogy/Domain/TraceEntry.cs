namespace BatchGenealogy.Domain;

/// <summary>
/// One lot reached by a trace. Upstream, <see cref="Share"/> is the fraction of the traced product that
/// comes from this lot; downstream, it is the fraction of this lot that comes from the traced material,
/// and <see cref="ExposedQuantity"/> is how much of that material this lot carries.
/// </summary>
public sealed class TraceEntry
{
    public TraceEntry(LotNumber lot, int depth, decimal share, Quantity exposedQuantity)
    {
        ArgumentNullException.ThrowIfNull(lot);
        ArgumentNullException.ThrowIfNull(exposedQuantity);
        ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1);
        if (share <= 0m || share > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(share), share, "A share lies in (0, 1].");
        }

        Lot = lot;
        Depth = depth;
        Share = share;
        ExposedQuantity = exposedQuantity;
    }

    public LotNumber Lot { get; }

    /// <summary>The fewest transformations between the traced lot and this one (at least one).</summary>
    public int Depth { get; }

    public decimal Share { get; }

    public Quantity ExposedQuantity { get; }

    public override string ToString() => $"{Lot} (depth {Depth}, {Share:P2}, {ExposedQuantity})";
}
