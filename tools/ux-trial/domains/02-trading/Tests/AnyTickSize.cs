// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TickSize --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TickSize" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTickSize : IAny<TickSize> {

    private readonly IAny<decimal> _increment;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTickSize()
        : this(increment: AnyValidIncrement()) { }

    private static AnyDecimal AnyValidIncrement() {
        // TickSize: (0, 1] and at most four decimal places.
        return Any.Decimal().Positive().LessThanOrEqualTo(TickSize.Largest).WithScale(4);
    }

    private AnyTickSize(IAny<decimal> increment) {
        _increment = increment;
    }

    /// <summary>Pins <c>increment</c> to a fixed value.</summary>
    public AnyTickSize WithIncrement(decimal value) {
        return new AnyTickSize(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>increment</c> from <c>AnyValidIncrement()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTickSize WithIncrement(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyTickSize(configure(AnyValidIncrement()));
    }

    /// <summary>Produces one arbitrary <see cref="TickSize" />.</summary>
    public TickSize Generate() {
        return new TickSize(_increment.Generate());
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
