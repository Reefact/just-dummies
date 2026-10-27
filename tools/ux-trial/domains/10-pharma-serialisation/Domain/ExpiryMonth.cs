namespace Serialisation.Domain;

/// <summary>An expiry expressed to the month, as printed on a carton ("EXP 2027-03").</summary>
public sealed class ExpiryMonth {

    private ExpiryMonth(int year, int month) {
        Year  = year;
        Month = month;
    }

    public int Year  { get; }
    public int Month { get; }

    public static ExpiryMonth Of(int year, int month) {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 2000);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 2099);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        return new ExpiryMonth(year, month);
    }

    public DateOnly LastDay => new DateOnly(Year, Month, DateTime.DaysInMonth(Year, Month));

    public override bool Equals(object? obj) => obj is ExpiryMonth other && other.Year == Year && other.Month == Month;
    public override int GetHashCode() => HashCode.Combine(Year, Month);

}
