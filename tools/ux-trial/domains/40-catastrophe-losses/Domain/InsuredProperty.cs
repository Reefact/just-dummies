using System.Text.RegularExpressions;

namespace CatastropheLosses.Domain;

/// <summary>A building on the books: where it stands, what it is worth, how it breaks.</summary>
public sealed partial class InsuredProperty
{
    public InsuredProperty(string id, GeoPoint location, decimal insuredValue, VulnerabilityCurve vulnerability)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(vulnerability);

        if (!IdFormat().IsMatch(id))
        {
            throw new FormatException($"Property id '{id}' must look like 'PRP-' followed by six digits, e.g. PRP-004217.");
        }

        if (insuredValue <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(insuredValue), insuredValue, "Insured value must be strictly positive.");
        }

        Id = id;
        Location = location;
        InsuredValue = insuredValue;
        Vulnerability = vulnerability;
    }

    public string Id { get; }

    public GeoPoint Location { get; }

    public decimal InsuredValue { get; }

    public VulnerabilityCurve Vulnerability { get; }

    /// <summary>The loss before any contract term applies, for a given hazard intensity.</summary>
    public decimal GroundUpLoss(double intensity) =>
        decimal.Round(InsuredValue * (decimal)Vulnerability.DamageRatioAt(intensity), 2);

    [GeneratedRegex(@"^PRP-\d{6}$")]
    private static partial Regex IdFormat();
}
