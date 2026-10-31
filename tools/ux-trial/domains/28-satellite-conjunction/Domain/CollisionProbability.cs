namespace SatelliteConjunction.Domain;

public enum RiskLevel
{
    Low,
    Elevated,
    High,
}

/// <summary>A probability of collision in [0, 1], classified against the usual operational thresholds.</summary>
public sealed class CollisionProbability : IComparable<CollisionProbability>
{
    /// <summary>At or above this, a conjunction is watched closely.</summary>
    public const double ElevatedThreshold = 1e-5;

    /// <summary>At or above this, a conjunction calls for a manoeuvre.</summary>
    public const double HighThreshold = 1e-4;

    public CollisionProbability(double value)
    {
        if (double.IsNaN(value) || value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A probability lies between 0 and 1.");
        }

        Value = value;
    }

    public double Value { get; }

    public RiskLevel Level => Value >= HighThreshold ? RiskLevel.High
        : Value >= ElevatedThreshold ? RiskLevel.Elevated
        : RiskLevel.Low;

    public int CompareTo(CollisionProbability? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString("E2", System.Globalization.CultureInfo.InvariantCulture);
}
