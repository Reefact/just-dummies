namespace ColdChain.Domain;

/// <summary>
/// The time a product may spend outside its labelled range without losing potency, from its stability data:
/// a cumulative allowance over its whole life in the chain, and the longest single excursion it tolerates.
/// </summary>
public sealed class StabilityBudget : IEquatable<StabilityBudget>
{
    /// <summary>One week: no vaccine's stability file supports more.</summary>
    public const int MaxCumulativeMinutes = 10_080;

    public StabilityBudget(int cumulativeMinutes, int maxSingleExcursionMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cumulativeMinutes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cumulativeMinutes, MaxCumulativeMinutes);
        if (maxSingleExcursionMinutes < 0 || maxSingleExcursionMinutes > cumulativeMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSingleExcursionMinutes),
                maxSingleExcursionMinutes,
                $"A single excursion allowance lies between 0 and the cumulative budget ({cumulativeMinutes} min).");
        }

        CumulativeMinutes = cumulativeMinutes;
        MaxSingleExcursionMinutes = maxSingleExcursionMinutes;
    }

    public int CumulativeMinutes { get; }

    public int MaxSingleExcursionMinutes { get; }

    /// <summary>A product with no allowance at all: any excursion exhausts it.</summary>
    public bool IsZeroTolerance => CumulativeMinutes == 0;

    public int RemainingAfter(int consumedMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(consumedMinutes);
        return Math.Max(0, CumulativeMinutes - consumedMinutes);
    }

    public bool Equals(StabilityBudget? other) =>
        other is not null && other.CumulativeMinutes == CumulativeMinutes && other.MaxSingleExcursionMinutes == MaxSingleExcursionMinutes;

    public override bool Equals(object? obj) => Equals(obj as StabilityBudget);

    public override int GetHashCode() => HashCode.Combine(CumulativeMinutes, MaxSingleExcursionMinutes);

    public override string ToString() => $"{CumulativeMinutes} min cumulative, {MaxSingleExcursionMinutes} min per excursion";
}
