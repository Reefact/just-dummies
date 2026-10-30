// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MeterIndex --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MeterIndex" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeterIndex : IAny<MeterIndex> {

    private readonly IAny<decimal> _kwh;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeterIndex()
        : this(kwh: AnyValidKwh()) { }

    private static AnyDecimal AnyValidKwh() {
        // The constructor also refuses more than three decimal places.
        return Any.Decimal().Between(0m, MeterIndex.MaxKwh).WithScale(3);
    }

    private AnyMeterIndex(IAny<decimal> kwh) {
        _kwh = kwh;
    }

    /// <summary>Pins <c>kwh</c> to a fixed value.</summary>
    public AnyMeterIndex WithKwh(decimal value) {
        return new AnyMeterIndex(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>kwh</c> from <c>AnyValidKwh()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeterIndex WithKwh(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyMeterIndex(configure(AnyValidKwh()));
    }

    /// <summary>Produces one arbitrary <see cref="MeterIndex" />.</summary>
    public MeterIndex Generate() {
        return new MeterIndex(_kwh.Generate());
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
