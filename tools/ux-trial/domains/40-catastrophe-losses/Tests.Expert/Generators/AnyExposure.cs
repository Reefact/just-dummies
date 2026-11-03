// Scaffolded by `any` (JustDummies), then completed by hand: the terms are drawn AFTER the property,
// because the Exposure constructor ties them to its insured value (deductible < value, limit <= value).
// `any generate Exposure --force` overwrites it.

using System;

using CatastropheLosses.Domain;
using JustDummies;

namespace CatastropheLosses.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Exposure" /> values. The terms always fit the property they cover:
///     pin the terms (<see cref="WithTerms(PolicyTerms)" />) and you own that fit.
/// </summary>
public sealed partial class AnyExposure : IAny<Exposure> {

    private IAny<InsuredProperty> _property;
    private PolicyTerms?          _pinnedTerms;

    private AnyInsuredProperty _propertyRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyExposure()
        : this(property: new AnyInsuredProperty()) { }

    private AnyExposure(AnyInsuredProperty property) {
        _property       = property;
        _propertyRecipe = property;
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

    public AnyExposure WithTerms(PolicyTerms value) {
        AnyExposure copy = new AnyExposure(this);

        copy._pinnedTerms = value;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Exposure" />.</summary>
    public Exposure Generate() {
        InsuredProperty property = _property.Generate();
        PolicyTerms     terms    = _pinnedTerms ?? TermsFitting(property.InsuredValue).Generate();

        return new Exposure(property, terms);
    }

    private static IAny<PolicyTerms> TermsFitting(decimal insuredValue) {
        return Any.Combine(Any.Decimal().GreaterThanOrEqualTo(0m).LessThan(insuredValue),
                           Any.Decimal().Positive().LessThanOrEqualTo(insuredValue),
                           (deductible, limit) => new PolicyTerms(deductible, limit));
    }

}
