// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate HourlyRate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payroll.Domain;

/// <summary>
///     A generator of arbitrary <see cref="HourlyRate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyHourlyRate : IAny<HourlyRate> {

    private readonly IAny<decimal> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyHourlyRate()
        : this(value: AnyValidValue()) { }

    private static AnyDecimal AnyValidValue() {
        // HourlyRate also requires a rate to the cent.
        return Any.Decimal().Positive().LessThanOrEqualTo(500m).WithScale(2);
    }

    private AnyHourlyRate(IAny<decimal> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyHourlyRate WithValue(decimal value) {
        return new AnyHourlyRate(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyHourlyRate WithValue(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyHourlyRate(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="HourlyRate" />.</summary>
    public HourlyRate Generate() {
        return new HourlyRate(_value.Generate());
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
