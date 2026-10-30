// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DilutionFactor --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DilutionFactor" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDilutionFactor : IAny<DilutionFactor> {

    private readonly IAny<decimal> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDilutionFactor()
        : this(value: AnyValidValue()) { }

    private static AnyDecimal AnyValidValue() {
        // At most two decimal places: WithScale(2) is the subset 'exactly two', which every such value is.
        return Any.Decimal().Between(1m, 1000m).WithScale(2);
    }

    private AnyDilutionFactor(IAny<decimal> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyDilutionFactor WithValue(decimal value) {
        return new AnyDilutionFactor(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDilutionFactor WithValue(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyDilutionFactor(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="DilutionFactor" />.</summary>
    public DilutionFactor Generate() {
        return new DilutionFactor(_value.Generate());
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
