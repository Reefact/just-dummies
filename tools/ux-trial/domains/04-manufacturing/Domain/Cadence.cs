namespace Manufacturing.Domain;

/// <summary>A line's nominal throughput, in units per hour.</summary>
public sealed class Cadence : IEquatable<Cadence> {

    public const int MaximumUnitsPerHour = 10_000;

    public Cadence(int unitsPerHour) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitsPerHour);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(unitsPerHour, MaximumUnitsPerHour);
        UnitsPerHour = unitsPerHour;
    }

    public int UnitsPerHour { get; }

    /// <summary>Time needed to produce <paramref name="quantity"/> units at this cadence, rounded up to the whole minute.</summary>
    public TimeSpan TimeToProduce(int quantity) {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);
        long minutes = ((long)quantity * 60 + UnitsPerHour - 1) / UnitsPerHour;

        return TimeSpan.FromMinutes(minutes);
    }

    public bool Equals(Cadence? other) => other is not null && UnitsPerHour == other.UnitsPerHour;
    public override bool Equals(object? obj) => Equals(obj as Cadence);
    public override int GetHashCode() => UnitsPerHour;

}
