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

    private IAny<int>    _calendarYear;
    private IAny<double> _radiocarbonYearsBp;
    private IAny<double> _uncertainty;

    private AnyInt32  _calendarYearRecipe;
    private AnyDouble _radiocarbonYearsBpRecipe;
    private AnyDouble _uncertaintyRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCalibrationPoint()
        : this(calendarYear:       AnyValidCalendarYear(),
               radiocarbonYearsBp: AnyValidRadiocarbonYearsBp(),
               uncertainty:        AnyValidUncertainty()) { }

    private static AnyInt32 AnyValidCalendarYear() {
        return Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear);
    }

    private static AnyDouble AnyValidRadiocarbonYearsBp() {
        return Any.Double().Between(0d, 55000d);
    }

    private static AnyDouble AnyValidUncertainty() {
        return Any.Double().Positive().LessThanOrEqualTo(5000d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCalibrationPoint(AnyInt32  calendarYear,
                                AnyDouble radiocarbonYearsBp,
                                AnyDouble uncertainty) {
        _calendarYear             = calendarYear;
        _calendarYearRecipe       = calendarYear;
        _radiocarbonYearsBp       = radiocarbonYearsBp;
        _radiocarbonYearsBpRecipe = radiocarbonYearsBp;
        _uncertainty              = uncertainty;
        _uncertaintyRecipe        = uncertainty;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCalibrationPoint(AnyCalibrationPoint from) {
        _calendarYear             = from._calendarYear;
        _calendarYearRecipe       = from._calendarYearRecipe;
        _radiocarbonYearsBp       = from._radiocarbonYearsBp;
        _radiocarbonYearsBpRecipe = from._radiocarbonYearsBpRecipe;
        _uncertainty              = from._uncertainty;
        _uncertaintyRecipe        = from._uncertaintyRecipe;
    }

    /// <summary>Pins <c>calendarYear</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCalibrationPoint WithCalendarYear(int value) {
        AnyCalibrationPoint copy = new AnyCalibrationPoint(this);

        copy._calendarYear = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>calendarYear</c> is drawn from — <c>AnyValidCalendarYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalibrationPoint WithCalendarYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalibrationPoint copy = new AnyCalibrationPoint(this);

        copy._calendarYearRecipe = configure(_calendarYearRecipe);
        copy._calendarYear       = copy._calendarYearRecipe;

        return copy;
    }

    /// <summary>Pins <c>radiocarbonYearsBp</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCalibrationPoint WithRadiocarbonYearsBp(double value) {
        AnyCalibrationPoint copy = new AnyCalibrationPoint(this);

        copy._radiocarbonYearsBp = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>radiocarbonYearsBp</c> is drawn from — <c>AnyValidRadiocarbonYearsBp()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalibrationPoint WithRadiocarbonYearsBp(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalibrationPoint copy = new AnyCalibrationPoint(this);

        copy._radiocarbonYearsBpRecipe = configure(_radiocarbonYearsBpRecipe);
        copy._radiocarbonYearsBp       = copy._radiocarbonYearsBpRecipe;

        return copy;
    }

    /// <summary>Pins <c>uncertainty</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCalibrationPoint WithUncertainty(double value) {
        AnyCalibrationPoint copy = new AnyCalibrationPoint(this);

        copy._uncertainty = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>uncertainty</c> is drawn from — <c>AnyValidUncertainty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalibrationPoint WithUncertainty(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalibrationPoint copy = new AnyCalibrationPoint(this);

        copy._uncertaintyRecipe = configure(_uncertaintyRecipe);
        copy._uncertainty       = copy._uncertaintyRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CalibrationPoint" />.</summary>
    public CalibrationPoint Generate() {
        return new CalibrationPoint(_calendarYear.Generate(),
                                    _radiocarbonYearsBp.Generate(),
                                    _uncertainty.Generate());
    }

}
