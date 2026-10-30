// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LoadCurvePoint --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LoadCurvePoint" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLoadCurvePoint : IAny<LoadCurvePoint> {

    private readonly IAny<TimeOnly> _slotStart;
    private readonly IAny<decimal>  _averagePowerKw;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLoadCurvePoint()
        : this(slotStart:      AnyValidSlotStart(),
               averagePowerKw: AnyValidAveragePowerKw()) { }

    private static AnyTimeOnly AnyValidSlotStart() {
        // A slot starts on the hour or on the half hour.
        return Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30));
    }

    private static AnyDecimal AnyValidAveragePowerKw() {
        // Within the meter rating, metered to the watt.
        return Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).WithScale(3);
    }

    private AnyLoadCurvePoint(IAny<TimeOnly> slotStart,
                              IAny<decimal>  averagePowerKw) {
        _slotStart      = slotStart;
        _averagePowerKw = averagePowerKw;
    }

    /// <summary>Pins <c>slotStart</c> to a fixed value.</summary>
    public AnyLoadCurvePoint WithSlotStart(TimeOnly value) {
        return new AnyLoadCurvePoint(new FixedValue<TimeOnly>(value), _averagePowerKw);
    }

    /// <summary>Draws <c>slotStart</c> from <c>AnyValidSlotStart()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLoadCurvePoint WithSlotStart(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        return new AnyLoadCurvePoint(configure(AnyValidSlotStart()), _averagePowerKw);
    }

    /// <summary>Pins <c>averagePowerKw</c> to a fixed value.</summary>
    public AnyLoadCurvePoint WithAveragePowerKw(decimal value) {
        return new AnyLoadCurvePoint(_slotStart, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>averagePowerKw</c> from <c>AnyValidAveragePowerKw()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLoadCurvePoint WithAveragePowerKw(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyLoadCurvePoint(_slotStart, configure(AnyValidAveragePowerKw()));
    }

    /// <summary>Produces one arbitrary <see cref="LoadCurvePoint" />.</summary>
    public LoadCurvePoint Generate() {
        return new LoadCurvePoint(_slotStart.Generate(),
                                  _averagePowerKw.Generate());
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
