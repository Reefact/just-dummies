using System.Text.RegularExpressions;

namespace CatastropheLosses.Domain;

/// <summary>A stochastic scenario of the event catalogue, with the yearly rate at which it occurs.</summary>
public sealed partial class CatastropheEvent
{
    public const double MaxAnnualRate = 5.0;

    public CatastropheEvent(string id, Peril peril, Footprint footprint, double annualRate)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(footprint);

        if (!IdFormat().IsMatch(id))
        {
            throw new FormatException($"Event id '{id}' must look like 'EV-' followed by four to eight digits, e.g. EV-0042.");
        }

        if (!Enum.IsDefined(peril))
        {
            throw new ArgumentOutOfRangeException(nameof(peril), peril, "Unknown peril.");
        }

        if (double.IsNaN(annualRate) || annualRate <= 0.0 || annualRate > MaxAnnualRate)
        {
            throw new ArgumentOutOfRangeException(nameof(annualRate), annualRate, $"Annual rate must lie within (0, {MaxAnnualRate}].");
        }

        Id = id;
        Peril = peril;
        Footprint = footprint;
        AnnualRate = annualRate;
    }

    public string Id { get; }

    public Peril Peril { get; }

    public Footprint Footprint { get; }

    public double AnnualRate { get; }

    /// <summary>Mean return period in years.</summary>
    public double ReturnPeriodYears => 1.0 / AnnualRate;

    [GeneratedRegex(@"^EV-\d{4,8}$")]
    private static partial Regex IdFormat();
}
