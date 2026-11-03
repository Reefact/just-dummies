// Scaffolded by `any` (JustDummies), then completed by hand: Exposure ties its terms to its property
// (deductible < insured value, limit <= insured value), which no per-parameter recipe can express.
// The terms are therefore derived from the drawn property inside Generate().

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

public sealed partial class AnyExposure : IAny<Exposure> {

    private IAny<InsuredProperty> _property;
    private PolicyTerms?          _pinnedTerms;

    private AnyInsuredProperty _propertyRecipe;

    public AnyExposure() {
        _propertyRecipe = new AnyInsuredProperty();
        _property       = _propertyRecipe;
    }

    private AnyExposure(AnyExposure from) {
        _property       = from._property;
        _propertyRecipe = from._propertyRecipe;
        _pinnedTerms    = from._pinnedTerms;
    }

    public AnyExposure WithProperty(InsuredProperty value) {
        AnyExposure copy = new AnyExposure(this);

        copy._property = Any.Fixed(value);

        return copy;
    }

    public AnyExposure WithProperty(Func<AnyInsuredProperty, AnyInsuredProperty> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExposure copy = new AnyExposure(this);

        copy._propertyRecipe = configure(_propertyRecipe);
        copy._property       = copy._propertyRecipe;

        return copy;
    }

    /// <summary>Pins the terms; the property must then be compatible with them (a test's own responsibility).</summary>
    public AnyExposure WithTerms(PolicyTerms value) {
        AnyExposure copy = new AnyExposure(this);

        copy._pinnedTerms = value;

        return copy;
    }

    public Exposure Generate() {
        InsuredProperty property = _property.Generate();
        PolicyTerms     terms    = _pinnedTerms ?? AnyTermsFor(property);

        return new Exposure(property, terms);
    }

    private static PolicyTerms AnyTermsFor(InsuredProperty property) {
        decimal value = property.InsuredValue;

        decimal deductible = Any.Decimal().Between(0m, value - 0.01m).WithScale(2).Generate();
        decimal limit      = Any.Decimal().Between(0.01m, value).WithScale(2).Generate();

        return new PolicyTerms(deductible, limit);
    }

}
