// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Measurement --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Measurement" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasurement : IAny<Measurement> {

    private readonly IAny<double>          _value;
    private readonly IAny<MeasurementUnit> _unit;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasurement()
        : this(value: AnyValidValue(),
               unit:  AnyValidUnit()) { }

    private static AnyDouble AnyValidValue() {
        // Finite (never drawn otherwise) and non-negative.
        return Any.Double().GreaterThanOrEqualTo(0d);
    }

    private static AnyMeasurementUnit AnyValidUnit() {
        return new AnyMeasurementUnit();
    }

    private AnyMeasurement(IAny<double>          value,
                           IAny<MeasurementUnit> unit) {
        _value = value;
        _unit  = unit;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyMeasurement WithValue(double value) {
        return new AnyMeasurement(new FixedValue<double>(value), _unit);
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasurement WithValue(Func<AnyDouble, AnyDouble> configure) {
        return new AnyMeasurement(configure(AnyValidValue()), _unit);
    }

    /// <summary>Pins <c>unit</c> to a fixed value.</summary>
    public AnyMeasurement WithUnit(MeasurementUnit value) {
        return new AnyMeasurement(_value, new FixedValue<MeasurementUnit>(value));
    }

    /// <summary>Draws <c>unit</c> from <c>AnyValidUnit()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasurement WithUnit(Func<AnyMeasurementUnit, AnyMeasurementUnit> configure) {
        return new AnyMeasurement(_value, configure(AnyValidUnit()));
    }

    /// <summary>Produces one arbitrary <see cref="Measurement" />.</summary>
    public Measurement Generate() {
        return new Measurement(_value.Generate(),
                               _unit.Generate());
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
