namespace GridLoadFlow.Domain;

/// <summary>Operating band of a bus voltage magnitude, in per-unit. The band must contain the nominal 1.0 pu.</summary>
public sealed class VoltageLimits : IEquatable<VoltageLimits>
{
    public const double AbsoluteMinimumPu = 0.5;

    public const double AbsoluteMaximumPu = 1.5;

    public static readonly VoltageLimits Standard = new(0.95, 1.05);

    public VoltageLimits(double minimumPu, double maximumPu)
    {
        if (!double.IsFinite(minimumPu) || minimumPu < AbsoluteMinimumPu || minimumPu > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumPu), minimumPu, $"Minimum voltage must be within [{AbsoluteMinimumPu}, 1.0] pu.");
        }

        if (!double.IsFinite(maximumPu) || maximumPu < 1.0 || maximumPu > AbsoluteMaximumPu)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPu), maximumPu, $"Maximum voltage must be within [1.0, {AbsoluteMaximumPu}] pu.");
        }

        if (minimumPu >= maximumPu)
        {
            throw new ArgumentException($"Minimum voltage {minimumPu} pu must be below maximum voltage {maximumPu} pu.", nameof(minimumPu));
        }

        MinimumPu = minimumPu;
        MaximumPu = maximumPu;
    }

    public double MinimumPu { get; }

    public double MaximumPu { get; }

    public bool Contains(double magnitudePu) => magnitudePu >= MinimumPu && magnitudePu <= MaximumPu;

    public bool Equals(VoltageLimits? other) => other is not null && MinimumPu == other.MinimumPu && MaximumPu == other.MaximumPu;

    public override bool Equals(object? obj) => obj is VoltageLimits other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(MinimumPu, MaximumPu);

    public override string ToString() => $"[{MinimumPu}, {MaximumPu}] pu";
}
