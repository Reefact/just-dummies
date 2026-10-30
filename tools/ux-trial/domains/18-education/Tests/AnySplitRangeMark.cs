// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SplitRangeMark --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Education.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SplitRangeMark" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySplitRangeMark : IAny<SplitRangeMark> {

    private readonly IAny<decimal> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySplitRangeMark()
        : this(value: AnyValidValue()) { }

    private static AnyDecimal AnyValidValue() {
        return Any.Decimal().Between(0m, 20m);
    }

    private AnySplitRangeMark(IAny<decimal> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnySplitRangeMark WithValue(decimal value) {
        return new AnySplitRangeMark(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnySplitRangeMark WithValue(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnySplitRangeMark(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="SplitRangeMark" />.</summary>
    public SplitRangeMark Generate() {
        return new SplitRangeMark(_value.Generate());
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
