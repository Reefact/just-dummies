// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DailyLoadCurve --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DailyLoadCurve" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDailyLoadCurve : IAny<DailyLoadCurve> {

    private IAny<DeliveryPointId>               _deliveryPoint;
    private IAny<DateOnly>                      _day;
    private IAny<IReadOnlyList<LoadCurvePoint>> _points;

    private AnyDeliveryPointId      _deliveryPointRecipe;
    private AnyDateOnly             _dayRecipe;
    private AnyList<LoadCurvePoint> _pointsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDailyLoadCurve()
        : this(deliveryPoint: AnyValidDeliveryPoint(),
               day:           AnyValidDay(),
               points:        AnyValidPoints()) { }

    private static AnyDeliveryPointId AnyValidDeliveryPoint() {
        return new AnyDeliveryPointId();
    }

    private static AnyDateOnly AnyValidDay() {
        return Any.DateOnly();
    }

    private static AnyList<LoadCurvePoint> AnyValidPoints() {
        return Any.ListOf(new AnyLoadCurvePoint()).WithCount(DailyLoadCurve.SlotsPerDay);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDailyLoadCurve(AnyDeliveryPointId      deliveryPoint,
                              AnyDateOnly             day,
                              AnyList<LoadCurvePoint> points) {
        _deliveryPoint       = deliveryPoint;
        _deliveryPointRecipe = deliveryPoint;
        _day                 = day;
        _dayRecipe           = day;
        _points              = points;
        _pointsRecipe        = points;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDailyLoadCurve(AnyDailyLoadCurve from) {
        _deliveryPoint       = from._deliveryPoint;
        _deliveryPointRecipe = from._deliveryPointRecipe;
        _day                 = from._day;
        _dayRecipe           = from._dayRecipe;
        _points              = from._points;
        _pointsRecipe        = from._pointsRecipe;
    }

    /// <summary>Pins <c>deliveryPoint</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDailyLoadCurve WithDeliveryPoint(DeliveryPointId value) {
        AnyDailyLoadCurve copy = new AnyDailyLoadCurve(this);

        copy._deliveryPoint = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deliveryPoint</c> is drawn from — <c>AnyValidDeliveryPoint()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDeliveryPointId, AnyDeliveryPointId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDailyLoadCurve WithDeliveryPoint(Func<AnyDeliveryPointId, AnyDeliveryPointId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDailyLoadCurve copy = new AnyDailyLoadCurve(this);

        copy._deliveryPointRecipe = configure(_deliveryPointRecipe);
        copy._deliveryPoint       = copy._deliveryPointRecipe;

        return copy;
    }

    /// <summary>Pins <c>day</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDailyLoadCurve WithDay(DateOnly value) {
        AnyDailyLoadCurve copy = new AnyDailyLoadCurve(this);

        copy._day = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>day</c> is drawn from — <c>AnyValidDay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDailyLoadCurve WithDay(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDailyLoadCurve copy = new AnyDailyLoadCurve(this);

        copy._dayRecipe = configure(_dayRecipe);
        copy._day       = copy._dayRecipe;

        return copy;
    }

    /// <summary>Pins <c>points</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDailyLoadCurve WithPoints(IReadOnlyList<LoadCurvePoint> value) {
        AnyDailyLoadCurve copy = new AnyDailyLoadCurve(this);

        copy._points = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>points</c> is drawn from — <c>AnyValidPoints()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;LoadCurvePoint&gt;, AnyList&lt;LoadCurvePoint&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDailyLoadCurve WithPoints(Func<AnyList<LoadCurvePoint>, AnyList<LoadCurvePoint>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDailyLoadCurve copy = new AnyDailyLoadCurve(this);

        copy._pointsRecipe = configure(_pointsRecipe);
        copy._points       = copy._pointsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="DailyLoadCurve" />.</summary>
    public DailyLoadCurve Generate() {
        // The curve requires point i to start at 00:00 + 30 min x i: the slots are laid out here, over the powers drawn.
        IReadOnlyList<LoadCurvePoint> drawn = _points.Generate();
        List<LoadCurvePoint>          laidOut = new List<LoadCurvePoint>();
        for (int slot = 0; slot < drawn.Count; slot++) {
            laidOut.Add(new LoadCurvePoint(TimeOnly.MinValue.AddMinutes(30 * slot), drawn[slot].AveragePowerKw));
        }

        return new DailyLoadCurve(_deliveryPoint.Generate(),
                                  _day.Generate(),
                                  laidOut);
    }

}
