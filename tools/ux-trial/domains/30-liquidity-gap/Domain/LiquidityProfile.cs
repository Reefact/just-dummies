namespace LiquidityGap.Domain;

/// <summary>The projection of one portfolio under one scenario: a gap per maturity bucket.</summary>
public sealed class LiquidityProfile
{
    public LiquidityProfile(string scenarioName, Currency currency, IReadOnlyList<BucketGap> gaps)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
        {
            throw new ArgumentException("A profile names its scenario.", nameof(scenarioName));
        }

        ArgumentNullException.ThrowIfNull(gaps);
        TimeBucket.EnsureLadder(gaps.Select(static g => g.Bucket).ToList());

        decimal running = 0m;
        foreach (BucketGap gap in gaps)
        {
            running += gap.Gap;
            if (gap.CumulativeGap != running)
            {
                throw new ArgumentException(
                    $"The cumulative gap of '{gap.Bucket.Label}' is {gap.CumulativeGap}, but the gaps so far sum to {running}.",
                    nameof(gaps));
            }
        }

        ScenarioName = scenarioName;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Gaps = gaps.ToList();
    }

    public string ScenarioName { get; }

    public Currency Currency { get; }

    public IReadOnlyList<BucketGap> Gaps { get; }

    public decimal TotalInflows => Gaps.Sum(static g => g.Inflows);

    public decimal TotalOutflows => Gaps.Sum(static g => g.Outflows);

    public decimal NetPosition => Gaps[^1].CumulativeGap;

    /// <summary>The first bucket whose cumulative gap is negative, or null when the portfolio never runs short.</summary>
    public BucketGap? FirstShortfall => Gaps.FirstOrDefault(static g => g.CumulativeGap < 0m);

    /// <summary>
    /// The number of days the portfolio survives on its own flows: the start of the first shortfall
    /// bucket, or null when no bucket runs short.
    /// </summary>
    public int? SurvivalDays => FirstShortfall?.Bucket.StartDay;

    /// <summary>Refuses the profile when the cumulative gap of any bucket ending within the horizon is below the limit.</summary>
    public void EnsureWithinLimit(int horizonDays, decimal minimumCumulativeGap)
    {
        if (horizonDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(horizonDays), horizonDays, "A limit horizon is at least one day.");
        }

        foreach (BucketGap gap in Gaps)
        {
            if (gap.Bucket.StartDay >= horizonDays)
            {
                break;
            }

            if (gap.CumulativeGap < minimumCumulativeGap)
            {
                throw new InvalidOperationException(
                    $"Scenario '{ScenarioName}': cumulative gap {gap.CumulativeGap} in bucket '{gap.Bucket.Label}' is below the limit {minimumCumulativeGap}.");
            }
        }
    }
}
