// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DiscountPercentage --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Shop.Domain;

namespace Shop.Tests;

/// <summary>
///     A generator of arbitrary <see cref="DiscountPercentage" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDiscountPercentage : IAny<DiscountPercentage> {

    private readonly IAny<decimal> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDiscountPercentage()
        : this(value: AnyValidValue()) { }

    private static AnyDecimal AnyValidValue() {
        // A percentage, 0 to 100, at most one decimal place.
        return Any.Decimal().Between(0m, 100m).WithScale(1);
    }

    private AnyDiscountPercentage(IAny<decimal> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyDiscountPercentage WithValue(decimal value) {
        return new AnyDiscountPercentage(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDiscountPercentage WithValue(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyDiscountPercentage(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="DiscountPercentage" />.</summary>
    public DiscountPercentage Generate() {
        return new DiscountPercentage(_value.Generate());
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
