namespace CatastropheLosses.Domain;

/// <summary>
/// Monte Carlo loss model. Each simulated year draws, per catalogue event, a Poisson number of
/// occurrences; each occurrence draws one lognormal severity factor (mean 1) shared by every
/// exposure it touches, which is what keeps the losses of one event correlated. Contract terms
/// apply per exposure and per occurrence; occurrences sum into the annual loss.
/// </summary>
public sealed class CatastropheModel
{
    public const int MaxYears = 1_000_000;
    public const double MaxSeverityVolatility = 2.0;

    private readonly CatastropheEvent[] _events;

    public CatastropheModel(IReadOnlyList<CatastropheEvent> catalogue, double severityVolatility)
    {
        ArgumentNullException.ThrowIfNull(catalogue);

        if (catalogue.Count == 0)
        {
            throw new ArgumentException("An event catalogue needs at least one event.", nameof(catalogue));
        }

        if (catalogue.Any(e => e is null))
        {
            throw new ArgumentException("An event catalogue cannot hold a null event.", nameof(catalogue));
        }

        string? duplicate = catalogue.GroupBy(e => e.Id, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1)?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException($"Event {duplicate} appears more than once in the catalogue.", nameof(catalogue));
        }

        if (double.IsNaN(severityVolatility) || severityVolatility < 0.0 || severityVolatility > MaxSeverityVolatility)
        {
            throw new ArgumentOutOfRangeException(nameof(severityVolatility), severityVolatility, $"Severity volatility must lie within [0, {MaxSeverityVolatility}].");
        }

        _events = catalogue.ToArray();
        SeverityVolatility = severityVolatility;
    }

    public IReadOnlyList<CatastropheEvent> Catalogue => _events;

    /// <summary>Standard deviation of the log of the severity factor; 0 makes every occurrence exactly the catalogued footprint.</summary>
    public double SeverityVolatility { get; }

    public LossDistribution Simulate(Portfolio portfolio, int years, int seed)
    {
        ArgumentNullException.ThrowIfNull(portfolio);

        if (!portfolio.IsFrozen)
        {
            throw new InvalidOperationException($"Portfolio '{portfolio.Name}' must be frozen before it is simulated.");
        }

        if (years < 1 || years > MaxYears)
        {
            throw new ArgumentOutOfRangeException(nameof(years), years, $"Simulated years must lie within [1, {MaxYears}].");
        }

        Random random = new(seed);
        decimal[] annualLosses = new decimal[years];
        for (int year = 0; year < years; year++)
        {
            decimal annualLoss = 0m;
            foreach (CatastropheEvent catastrophe in _events)
            {
                int occurrences = DrawPoisson(random, catastrophe.AnnualRate);
                for (int k = 0; k < occurrences; k++)
                {
                    annualLoss += portfolio.EventLoss(catastrophe, DrawSeverityFactor(random));
                }
            }

            annualLosses[year] = annualLoss;
        }

        return new LossDistribution(annualLosses);
    }

    private static int DrawPoisson(Random random, double rate)
    {
        double limit = Math.Exp(-rate);
        double product = random.NextDouble();
        int count = 0;
        while (product > limit)
        {
            count++;
            product *= random.NextDouble();
        }

        return count;
    }

    private double DrawSeverityFactor(Random random)
    {
        if (SeverityVolatility == 0.0)
        {
            return 1.0;
        }

        double u1 = 1.0 - random.NextDouble();
        double u2 = random.NextDouble();
        double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        double sigma = SeverityVolatility;
        return Math.Exp((sigma * normal) - (sigma * sigma / 2.0));
    }
}
