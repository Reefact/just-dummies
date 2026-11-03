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
using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="CalibrationCurve" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCalibrationCurve : IAny<CalibrationCurve> {

    private IAny<string>                          _name;
    private IAny<IReadOnlyList<CalibrationPoint>> _points;

    private AnyString                 _nameRecipe;
    private AnyList<CalibrationPoint> _pointsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCalibrationCurve()
        : this(name:   AnyValidName(),
               points: AnyValidPoints()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    private static AnyList<CalibrationPoint> AnyValidPoints() {
        return Any.ListOf(new AnyCalibrationPoint())
                  .WithCountBetween(2, 20)
                  .OrderedBy(point => point.CalendarYear)
                  .DistinctBy(point => point.CalendarYear);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCalibrationCurve(AnyString                 name,
                                AnyList<CalibrationPoint> points) {
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
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;CalibrationPoint&gt;, AnyList&lt;CalibrationPoint&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalibrationCurve WithPoints(Func<AnyList<CalibrationPoint>, AnyList<CalibrationPoint>> configure) {
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
