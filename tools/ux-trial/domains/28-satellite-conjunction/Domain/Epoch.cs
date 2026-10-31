namespace SatelliteConjunction.Domain;

/// <summary>An instant of orbital mechanics, always expressed in UTC.</summary>
public sealed class Epoch : IEquatable<Epoch>, IComparable<Epoch>
{
    /// <summary>The launch of Sputnik 1: no catalogued object predates it.</summary>
    public static readonly DateTimeOffset SpaceAgeStart = new(1957, 10, 4, 0, 0, 0, TimeSpan.Zero);

    public Epoch(DateTimeOffset utc)
    {
        if (utc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("An epoch must be expressed in UTC (offset zero).", nameof(utc));
        }

        if (utc < SpaceAgeStart)
        {
            throw new ArgumentOutOfRangeException(nameof(utc), utc, "An epoch cannot predate the space age (1957-10-04).");
        }

        Utc = utc;
    }

    public DateTimeOffset Utc { get; }

    public Epoch Add(TimeSpan offset) => new(Utc + offset);

    public double SecondsSince(Epoch other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return (Utc - other.Utc).TotalSeconds;
    }

    public int CompareTo(Epoch? other) => other is null ? 1 : Utc.CompareTo(other.Utc);

    public bool Equals(Epoch? other) => other is not null && Utc == other.Utc;

    public override bool Equals(object? obj) => Equals(obj as Epoch);

    public override int GetHashCode() => Utc.GetHashCode();

    public override string ToString() => Utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);
}
