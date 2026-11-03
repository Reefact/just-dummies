// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CalibrationCurve --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CalibrationCurve" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCalibrationCurve : IAny<CalibrationCurve> {

    private IAny<string>                          _name;
    private IAny<IReadOnlyList<CalibrationPoint>> _points;

    private AnyString                             _nameRecipe;
    private IAny<IReadOnlyList<CalibrationPoint>> _pointsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCalibrationCurve()
        : this(name:   AnyValidName(),
               points: AnyValidPoints()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    // Strictly increasing calendar years, by construction: a first year, then positive steps from one point to the next.
    // Steps total at most 30 * 200 years, so the last point stays under the calendar's latest year.
    private static IAny<IReadOnlyList<CalibrationPoint>> AnyValidPoints() {
        return Any.Combine(Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear - 30 * 200),
                           Any.ListOf(Any.Int32().Between(1, 200)).WithCountBetween(1, 30),
                           PointsFrom);
    }

    private static IReadOnlyList<CalibrationPoint> PointsFrom(int firstYear, List<int> steps) {
        List<CalibrationPoint> points = new List<CalibrationPoint>();
        int                    year   = firstYear;

        points.Add(new AnyCalibrationPoint().WithCalendarYear(year).Generate());
        foreach (int step in steps) {
            year += step;
            points.Add(new AnyCalibrationPoint().WithCalendarYear(year).Generate());
        }

        return points;
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCalibrationCurve(AnyString                             name,
                                IAny<IReadOnlyList<CalibrationPoint>> points) {
        _name         = name;
        _nameRecipe   = name;
        _points       = points;
        _pointsRecipe = points;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCalibrationCurve(AnyCalibrationCurve from) {
        _name         = from._name;
        _nameRecipe   = from._nameRecipe;
        _points       = from._points;
        _pointsRecipe = from._pointsRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCalibrationCurve WithName(string value) {
        AnyCalibrationCurve copy = new AnyCalibrationCurve(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalibrationCurve WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalibrationCurve copy = new AnyCalibrationCurve(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>points</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCalibrationCurve WithPoints(IReadOnlyList<CalibrationPoint> value) {
        AnyCalibrationCurve copy = new AnyCalibrationCurve(this);

        copy._points = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>points</c> is drawn from — <c>AnyValidPoints()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;IAny&lt;IReadOnlyList&lt;CalibrationPoint&gt;&gt;, IAny&lt;IReadOnlyList&lt;CalibrationPoint&gt;&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalibrationCurve WithPoints(Func<IAny<IReadOnlyList<CalibrationPoint>>, IAny<IReadOnlyList<CalibrationPoint>>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalibrationCurve copy = new AnyCalibrationCurve(this);

        copy._pointsRecipe = configure(_pointsRecipe);
        copy._points       = copy._pointsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CalibrationCurve" />.</summary>
    public CalibrationCurve Generate() {
        return new CalibrationCurve(_name.Generate(),
                                    _points.Generate());
    }

}
