// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Exposure --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Exposure" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyExposure : IAny<Exposure> {

    private readonly IAny<InsuredProperty> _property;
    private readonly IAny<PolicyTerms>     _terms;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyExposure()
        : this(property: AnyValidProperty(),
               terms:    AnyValidTerms()) { }

    private static AnyInsuredProperty AnyValidProperty() {
        // An exposure ties its terms to the property (deductible < value, limit <= value).
        // Rather than draw one from the other, the two bases sit in disjoint bands: every property
        // is worth more than any limit or deductible this generator writes.
        return new AnyInsuredProperty().WithInsuredValue(v => v.Between(250_000m, 5_000_000m));
    }

    private static AnyPolicyTerms AnyValidTerms() {
        return new AnyPolicyTerms().WithDeductible(d => d.LessThanOrEqualTo(25_000m))
                                   .WithLimit(l => l.Between(1_000m, 250_000m));
    }

    private AnyExposure(IAny<InsuredProperty> property,
                        IAny<PolicyTerms>     terms) {
        _property = property;
        _terms    = terms;
    }

    /// <summary>Pins <c>property</c> to a fixed value.</summary>
    public AnyExposure WithProperty(InsuredProperty value) {
        return new AnyExposure(new FixedValue<InsuredProperty>(value), _terms);
    }

    /// <summary>Draws <c>property</c> from <c>AnyValidProperty()</c>, refined by <paramref name="configure" />.</summary>
    public AnyExposure WithProperty(Func<AnyInsuredProperty, AnyInsuredProperty> configure) {
        return new AnyExposure(configure(AnyValidProperty()), _terms);
    }

    /// <summary>Pins <c>terms</c> to a fixed value.</summary>
    public AnyExposure WithTerms(PolicyTerms value) {
        return new AnyExposure(_property, new FixedValue<PolicyTerms>(value));
    }

    /// <summary>Draws <c>terms</c> from <c>AnyValidTerms()</c>, refined by <paramref name="configure" />.</summary>
    public AnyExposure WithTerms(Func<AnyPolicyTerms, AnyPolicyTerms> configure) {
        return new AnyExposure(_property, configure(AnyValidTerms()));
    }

    /// <summary>Produces one arbitrary <see cref="Exposure" />.</summary>
    public Exposure Generate() {
        return new Exposure(_property.Generate(),
                            _terms.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
