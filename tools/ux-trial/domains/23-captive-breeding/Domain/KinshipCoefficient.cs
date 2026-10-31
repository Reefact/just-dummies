namespace CaptiveBreeding.Domain;

/// <summary>
/// The probability that two alleles drawn at random, one from each individual, are identical
/// by descent. 0 for unrelated founders, 0.25 for full siblings or parent and offspring,
/// 0.5 for an individual with itself (more if it is inbred), never above 1.
/// </summary>
public sealed class KinshipCoefficient : IEquatable<KinshipCoefficient>, IComparable<KinshipCoefficient>
{
    public static readonly KinshipCoefficient Unrelated = new(0.0);

    public KinshipCoefficient(double value)
    {
        if (double.IsNaN(value) || value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A kinship coefficient lies in [0, 1].");
        }

        Value = value;
    }

    public double Value { get; }

    /// <summary>The inbreeding coefficient an offspring of this pair would have: it equals the parents' kinship.</summary>
    public double OffspringInbreeding => Value;

    public bool Exceeds(KinshipCoefficient limit)
    {
        ArgumentNullException.ThrowIfNull(limit);
        return Value > limit.Value;
    }

    public int CompareTo(KinshipCoefficient? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public bool Equals(KinshipCoefficient? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => Equals(obj as KinshipCoefficient);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
}
