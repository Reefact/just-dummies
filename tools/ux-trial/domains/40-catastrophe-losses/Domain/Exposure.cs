namespace CatastropheLosses.Domain;

/// <summary>A property covered under given terms: the unit the portfolio is made of.</summary>
public sealed class Exposure
{
    public Exposure(InsuredProperty property, PolicyTerms terms)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(terms);

        if (terms.Deductible >= property.InsuredValue)
        {
            throw new ArgumentException(
                $"Deductible {terms.Deductible} must be below the insured value {property.InsuredValue} of {property.Id}: such a policy could never pay.",
                nameof(terms));
        }

        if (terms.Limit > property.InsuredValue)
        {
            throw new ArgumentException(
                $"Limit {terms.Limit} exceeds the insured value {property.InsuredValue} of {property.Id}: over-insurance is not written.",
                nameof(terms));
        }

        Property = property;
        Terms = terms;
    }

    public InsuredProperty Property { get; }

    public PolicyTerms Terms { get; }

    /// <summary>The insured loss this exposure suffers under a footprint scaled by a severity factor.</summary>
    public decimal InsuredLoss(Footprint footprint, double severityFactor)
    {
        ArgumentNullException.ThrowIfNull(footprint);
        if (!double.IsFinite(severityFactor) || severityFactor < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(severityFactor), severityFactor, "Severity factor must be finite and non-negative.");
        }

        double intensity = footprint.IntensityAt(Property.Location) * severityFactor;
        return Terms.Apply(Property.GroundUpLoss(intensity));
    }
}
