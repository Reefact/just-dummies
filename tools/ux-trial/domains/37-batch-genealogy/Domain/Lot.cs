namespace BatchGenealogy.Domain;

/// <summary>
/// A lot (or sub-lot) of one material: a quantity produced on a date, with a shelf life,
/// a quality status and the quantity still available for consumption.
/// </summary>
public sealed class Lot
{
    /// <summary>The longest shelf life the quality system accepts.</summary>
    public const int MaximumShelfLifeDays = 3653;

    public Lot(LotNumber number, MaterialCode material, Quantity initialQuantity, DateOnly manufacturedOn, DateOnly expiresOn)
    {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(initialQuantity);
        if (initialQuantity.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialQuantity), initialQuantity, "A lot is created with a positive quantity.");
        }

        if (expiresOn <= manufacturedOn)
        {
            throw new ArgumentException($"A lot expires after it is manufactured ({expiresOn:yyyy-MM-dd} is not after {manufacturedOn:yyyy-MM-dd}).", nameof(expiresOn));
        }

        if (expiresOn.DayNumber - manufacturedOn.DayNumber > MaximumShelfLifeDays)
        {
            throw new ArgumentException($"A shelf life cannot exceed {MaximumShelfLifeDays} days.", nameof(expiresOn));
        }

        Number = number;
        Material = material;
        InitialQuantity = initialQuantity;
        RemainingQuantity = initialQuantity;
        ManufacturedOn = manufacturedOn;
        ExpiresOn = expiresOn;
        Status = LotStatus.Quarantined;
    }

    public LotNumber Number { get; }

    public MaterialCode Material { get; }

    public Quantity InitialQuantity { get; }

    public Quantity RemainingQuantity { get; private set; }

    public DateOnly ManufacturedOn { get; }

    public DateOnly ExpiresOn { get; }

    public LotStatus Status { get; private set; }

    public bool IsDepleted => RemainingQuantity.IsZero;

    public bool IsExpiredOn(DateOnly date) => date >= ExpiresOn;

    /// <summary>Quality release: only a quarantined lot can be released.</summary>
    public void Release()
    {
        if (Status != LotStatus.Quarantined)
        {
            throw new InvalidOperationException($"Lot {Number} is {Status}: only a quarantined lot can be released.");
        }

        Status = LotStatus.Released;
    }

    /// <summary>Blocks the lot pending investigation. Idempotent; a rejected lot stays rejected.</summary>
    public void Quarantine()
    {
        if (Status == LotStatus.Rejected)
        {
            throw new InvalidOperationException($"Lot {Number} is rejected: it cannot return to quarantine.");
        }

        Status = LotStatus.Quarantined;
    }

    /// <summary>Declares the lot non-conforming. Final.</summary>
    public void Reject() => Status = LotStatus.Rejected;

    /// <summary>Withdraws <paramref name="quantity"/> from the lot for a transformation performed on <paramref name="on"/>.</summary>
    public void Consume(Quantity quantity, DateOnly on)
    {
        EnsureConsumable(quantity, on);
        RemainingQuantity = RemainingQuantity.Subtract(quantity);
    }

    internal void EnsureConsumable(Quantity quantity, DateOnly on)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        if (Status != LotStatus.Released)
        {
            throw new InvalidOperationException($"Lot {Number} is {Status}: only a released lot can be consumed.");
        }

        if (on < ManufacturedOn)
        {
            throw new InvalidOperationException($"Lot {Number} cannot be consumed on {on:yyyy-MM-dd}, before it was manufactured.");
        }

        if (IsExpiredOn(on))
        {
            throw new InvalidOperationException($"Lot {Number} expired on {ExpiresOn:yyyy-MM-dd}.");
        }

        if (!quantity.IsCommensurableWith(RemainingQuantity))
        {
            throw new InvalidOperationException($"Lot {Number} is measured in {RemainingQuantity.Unit}: {quantity} does not apply.");
        }

        if (quantity.CompareTo(RemainingQuantity) > 0)
        {
            throw new InvalidOperationException($"Lot {Number} holds {RemainingQuantity}: {quantity} cannot be consumed.");
        }
    }
}
