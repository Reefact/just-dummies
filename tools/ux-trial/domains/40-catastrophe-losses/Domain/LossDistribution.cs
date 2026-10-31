namespace CatastropheLosses.Domain;

/// <summary>Simulated annual insured losses, one per simulated year, and the risk measures read from them.</summary>
public sealed class LossDistribution
{
    private readonly decimal[] _sorted;

    public LossDistribution(IReadOnlyList<decimal> annualLosses)
    {
        ArgumentNullException.ThrowIfNull(annualLosses);

        if (annualLosses.Count == 0)
        {
            throw new ArgumentException("A loss distribution needs at least one simulated year.", nameof(annualLosses));
        }

        if (annualLosses.Any(loss => loss < 0m))
        {
            throw new ArgumentException("An annual loss cannot be negative.", nameof(annualLosses));
        }

        AnnualLosses = annualLosses.ToArray();
        _sorted = annualLosses.Order().ToArray();
    }

    /// <summary>The losses in simulation order.</summary>
    public IReadOnlyList<decimal> AnnualLosses { get; }

    public int Years => _sorted.Length;

    /// <summary>The average annual loss (AAL).</summary>
    public decimal Mean => _sorted.Sum() / _sorted.Length;

    public decimal Maximum => _sorted[^1];

    public double StandardDeviation
    {
        get
        {
            double mean = (double)Mean;
            double variance = _sorted.Sum(loss => Math.Pow((double)loss - mean, 2)) / _sorted.Length;
            return Math.Sqrt(variance);
        }
    }

    /// <summary>Value at risk: the smallest loss not exceeded with the given probability (empirical quantile).</summary>
    public decimal Quantile(double probability)
    {
        RequireProbability(probability);
        int index = (int)Math.Ceiling(probability * _sorted.Length) - 1;
        return _sorted[Math.Clamp(index, 0, _sorted.Length - 1)];
    }

    /// <summary>Tail value at risk: the mean of the losses at or above the quantile.</summary>
    public decimal TailValueAtRisk(double probability)
    {
        decimal threshold = Quantile(probability);
        decimal[] tail = _sorted.Where(loss => loss >= threshold).ToArray();
        return tail.Sum() / tail.Length;
    }

    /// <summary>The loss exceeded on average once every <paramref name="years"/> years (OEP-style reading of the AEP curve).</summary>
    public decimal ReturnPeriodLoss(double years)
    {
        if (double.IsNaN(years) || years <= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(years), years, "A return period must exceed one year.");
        }

        return Quantile(1.0 - (1.0 / years));
    }

    /// <summary>Share of simulated years whose loss strictly exceeds the threshold.</summary>
    public double ExceedanceProbability(decimal threshold)
    {
        if (threshold < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "A loss threshold cannot be negative.");
        }

        return (double)_sorted.Count(loss => loss > threshold) / _sorted.Length;
    }

    private static void RequireProbability(double probability)
    {
        if (double.IsNaN(probability) || probability <= 0.0 || probability >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), probability, "A probability must lie strictly within (0, 1).");
        }
    }
}
