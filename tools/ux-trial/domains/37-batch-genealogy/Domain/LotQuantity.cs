namespace BatchGenealogy.Domain;

/// <summary>A strictly positive quantity of one lot taking part in a transformation.</summary>
public sealed class LotQuantity
{
    public LotQuantity(LotNumber lot, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(lot);
        ArgumentNullException.ThrowIfNull(quantity);
        if (quantity.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "A lot takes part in a transformation with a positive quantity.");
        }

        Lot = lot;
        Quantity = quantity;
    }

    public LotNumber Lot { get; }

    public Quantity Quantity { get; }

    public override string ToString() => $"{Quantity} of {Lot}";
}
