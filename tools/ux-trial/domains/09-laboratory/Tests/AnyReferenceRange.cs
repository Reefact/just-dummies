// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReferenceRange --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReferenceRange" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReferenceRange : IAny<ReferenceRange> {

    private readonly IAny<double>          _lower;
    private readonly IAny<double>          _upper;
    private readonly IAny<MeasurementUnit> _unit;
    private readonly IAny<double?>         _criticalLow;
    private readonly IAny<double?>         _criticalHigh;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReferenceRange()
        : this(lower:        AnyValidLower(),
               upper:        AnyValidUpper(),
               unit:         AnyValidUnit(),
               criticalLow:  AnyValidCriticalLow().AsNullable(),
               criticalHigh: AnyValidCriticalHigh().AsNullable()) { }

    private static AnyDouble AnyValidLower() {
        // lower < upper and criticalLow <= lower <= upper <= criticalHigh are cross-field rules: each bound
        // gets its own band so the four independent draws always order themselves.
        return Any.Double().Between(1d, 100d);
    }

    private static AnyDouble AnyValidUpper() {
        return Any.Double().GreaterThan(100d).LessThanOrEqualTo(1_000d);
    }

    private static AnyMeasurementUnit AnyValidUnit() {
        return new AnyMeasurementUnit();
    }

    private static AnyDouble AnyValidCriticalLow() {
        return Any.Double().Between(0d, 1d);
    }

    private static AnyDouble AnyValidCriticalHigh() {
        return Any.Double().Between(1_000d, 5_000d);
    }

    private AnyReferenceRange(IAny<double>          lower,
                              IAny<double>          upper,
                              IAny<MeasurementUnit> unit,
                              IAny<double?>         criticalLow,
                              IAny<double?>         criticalHigh) {
        _lower        = lower;
        _upper        = upper;
        _unit         = unit;
        _criticalLow  = criticalLow;
        _criticalHigh = criticalHigh;
    }

    /// <summary>Pins <c>lower</c> to a fixed value.</summary>
    public AnyReferenceRange WithLower(double value) {
        return new AnyReferenceRange(new FixedValue<double>(value), _upper, _unit, _criticalLow, _criticalHigh);
    }

    /// <summary>Draws <c>lower</c> from <c>AnyValidLower()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReferenceRange WithLower(Func<AnyDouble, AnyDouble> configure) {
        return new AnyReferenceRange(configure(AnyValidLower()), _upper, _unit, _criticalLow, _criticalHigh);
    }

    /// <summary>Pins <c>upper</c> to a fixed value.</summary>
    public AnyReferenceRange WithUpper(double value) {
        return new AnyReferenceRange(_lower, new FixedValue<double>(value), _unit, _criticalLow, _criticalHigh);
    }

    /// <summary>Draws <c>upper</c> from <c>AnyValidUpper()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReferenceRange WithUpper(Func<AnyDouble, AnyDouble> configure) {
        return new AnyReferenceRange(_lower, configure(AnyValidUpper()), _unit, _criticalLow, _criticalHigh);
    }

    /// <summary>Pins <c>unit</c> to a fixed value.</summary>
    public AnyReferenceRange WithUnit(MeasurementUnit value) {
        return new AnyReferenceRange(_lower, _upper, new FixedValue<MeasurementUnit>(value), _criticalLow, _criticalHigh);
    }

    /// <summary>Draws <c>unit</c> from <c>AnyValidUnit()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReferenceRange WithUnit(Func<AnyMeasurementUnit, AnyMeasurementUnit> configure) {
        return new AnyReferenceRange(_lower, _upper, configure(AnyValidUnit()), _criticalLow, _criticalHigh);
    }

    /// <summary>Pins <c>criticalLow</c> to a fixed value.</summary>
    public AnyReferenceRange WithCriticalLow(double? value) {
        return new AnyReferenceRange(_lower, _upper, _unit, new FixedValue<double?>(value), _criticalHigh);
    }

    /// <summary>Draws <c>criticalLow</c> from <c>AnyValidCriticalLow()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReferenceRange WithCriticalLow(Func<AnyDouble, AnyDouble> configure) {
        return new AnyReferenceRange(_lower, _upper, _unit, configure(AnyValidCriticalLow()).AsNullable(), _criticalHigh);
    }

    /// <summary>Pins <c>criticalHigh</c> to a fixed value.</summary>
    public AnyReferenceRange WithCriticalHigh(double? value) {
        return new AnyReferenceRange(_lower, _upper, _unit, _criticalLow, new FixedValue<double?>(value));
    }

    /// <summary>Draws <c>criticalHigh</c> from <c>AnyValidCriticalHigh()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReferenceRange WithCriticalHigh(Func<AnyDouble, AnyDouble> configure) {
        return new AnyReferenceRange(_lower, _upper, _unit, _criticalLow, configure(AnyValidCriticalHigh()).AsNullable());
    }

    /// <summary>Produces one arbitrary <see cref="ReferenceRange" />.</summary>
    public ReferenceRange Generate() {
        return new ReferenceRange(_lower.Generate(),
                                  _upper.Generate(),
                                  _unit.Generate(),
                                  _criticalLow.Generate(),
                                  _criticalHigh.Generate());
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
