namespace Serialisation.Domain;

/// <summary>A product's approved shelf life, in whole months.</summary>
public sealed class ShelfLife {

    public ShelfLife(int months) {
        if (months is < 1 or > 60) {
            throw new ArgumentOutOfRangeException(nameof(months), months, "A shelf life lasts 1 to 60 months.");
        }

        Months = months;
    }

    public int Months { get; }

    /// <summary>Expiry is printed to the month: the last day of the month the shelf life ends in.</summary>
    public DateOnly ExpiryFor(DateOnly manufacturedOn) {
        DateOnly end = manufacturedOn.AddMonths(Months);
        return new DateOnly(end.Year, end.Month, DateTime.DaysInMonth(end.Year, end.Month));
    }

    public override bool Equals(object? obj) => obj is ShelfLife other && other.Months == Months;
    public override int GetHashCode() => Months;

}
