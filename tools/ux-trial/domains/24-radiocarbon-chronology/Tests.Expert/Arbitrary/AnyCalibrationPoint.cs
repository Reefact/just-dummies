// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CalibrationPoint --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CalibrationPoint" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCalibrationPoint : IAny<CalibrationPoint> {

    private readonly IAny<int>    _calendarYear;
    private readonly IAny<double> _radiocarbonYearsBp;
    private readonly IAny<double> _uncertainty;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCalibrationPoint()
        : this(calendarYear:       AnyValidCalendarYear(),
               radiocarbonYearsBp: AnyValidRadiocarbonYearsBp(),
               uncertainty:        AnyValidUncertainty()) { }

    private static AnyInt32 AnyValidCalendarYear() {
        return Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear);
    }

    private static AnyDouble AnyValidRadiocarbonYearsBp() {
        return Any.Double().Between(0, RadiocarbonAge.MaximumYearsBp);
    }

    private static AnyDouble AnyValidUncertainty() {
        return Any.Double().Positive().LessThanOrEqualTo(RadiocarbonAge.MaximumUncertainty);
    }

    private AnyCalibrationPoint(IAny<int>    calendarYear,
                                IAny<double> radiocarbonYearsBp,
                                IAny<double> uncertainty) {
        _calendarYear       = calendarYear;
        _radiocarbonYearsBp = radiocarbonYearsBp;
        _uncertainty        = uncertainty;
    }

    /// <summary>Pins <c>calendarYear</c> to a fixed value.</summary>
    public AnyCalibrationPoint WithCalendarYear(int value) {
        return new AnyCalibrationPoint(new FixedValue<int>(value), _radiocarbonYearsBp, _uncertainty);
    }

    /// <summary>Draws <c>calendarYear</c> from <c>AnyValidCalendarYear()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCalibrationPoint WithCalendarYear(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCalibrationPoint(configure(AnyValidCalendarYear()), _radiocarbonYearsBp, _uncertainty);
    }

    /// <summary>Pins <c>radiocarbonYearsBp</c> to a fixed value.</summary>
    public AnyCalibrationPoint WithRadiocarbonYearsBp(double value) {
        return new AnyCalibrationPoint(_calendarYear, new FixedValue<double>(value), _uncertainty);
    }

    /// <summary>Draws <c>radiocarbonYearsBp</c> from <c>AnyValidRadiocarbonYearsBp()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCalibrationPoint WithRadiocarbonYearsBp(Func<AnyDouble, AnyDouble> configure) {
        return new AnyCalibrationPoint(_calendarYear, configure(AnyValidRadiocarbonYearsBp()), _uncertainty);
    }

    /// <summary>Pins <c>uncertainty</c> to a fixed value.</summary>
    public AnyCalibrationPoint WithUncertainty(double value) {
        return new AnyCalibrationPoint(_calendarYear, _radiocarbonYearsBp, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>uncertainty</c> from <c>AnyValidUncertainty()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCalibrationPoint WithUncertainty(Func<AnyDouble, AnyDouble> configure) {
        return new AnyCalibrationPoint(_calendarYear, _radiocarbonYearsBp, configure(AnyValidUncertainty()));
    }

    /// <summary>Produces one arbitrary <see cref="CalibrationPoint" />.</summary>
    public CalibrationPoint Generate() {
        return new CalibrationPoint(_calendarYear.Generate(),
                                    _radiocarbonYearsBp.Generate(),
                                    _uncertainty.Generate());
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
