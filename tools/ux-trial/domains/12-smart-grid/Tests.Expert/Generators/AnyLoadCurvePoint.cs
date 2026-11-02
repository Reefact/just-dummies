// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LoadCurvePoint --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LoadCurvePoint" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLoadCurvePoint : IAny<LoadCurvePoint> {

    private IAny<TimeOnly> _slotStart;
    private IAny<decimal>  _averagePowerKw;

    private AnyTimeOnly _slotStartRecipe;
    private AnyDecimal  _averagePowerKwRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLoadCurvePoint()
        : this(slotStart:      AnyValidSlotStart(),
               averagePowerKw: AnyValidAveragePowerKw()) { }

    private static AnyTimeOnly AnyValidSlotStart() {
        return Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30));
    }

    private static AnyDecimal AnyValidAveragePowerKw() {
        return Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLoadCurvePoint(AnyTimeOnly slotStart,
                              AnyDecimal  averagePowerKw) {
        _slotStart            = slotStart;
        _slotStartRecipe      = slotStart;
        _averagePowerKw       = averagePowerKw;
        _averagePowerKwRecipe = averagePowerKw;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLoadCurvePoint(AnyLoadCurvePoint from) {
        _slotStart            = from._slotStart;
        _slotStartRecipe      = from._slotStartRecipe;
        _averagePowerKw       = from._averagePowerKw;
        _averagePowerKwRecipe = from._averagePowerKwRecipe;
    }

    /// <summary>Pins <c>slotStart</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoadCurvePoint WithSlotStart(TimeOnly value) {
        AnyLoadCurvePoint copy = new AnyLoadCurvePoint(this);

        copy._slotStart = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>slotStart</c> is drawn from — <c>AnyValidSlotStart()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeOnly, AnyTimeOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoadCurvePoint WithSlotStart(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoadCurvePoint copy = new AnyLoadCurvePoint(this);

        copy._slotStartRecipe = configure(_slotStartRecipe);
        copy._slotStart       = copy._slotStartRecipe;

        return copy;
    }

    /// <summary>Pins <c>averagePowerKw</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoadCurvePoint WithAveragePowerKw(decimal value) {
        AnyLoadCurvePoint copy = new AnyLoadCurvePoint(this);

        copy._averagePowerKw = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>averagePowerKw</c> is drawn from — <c>AnyValidAveragePowerKw()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoadCurvePoint WithAveragePowerKw(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoadCurvePoint copy = new AnyLoadCurvePoint(this);

        copy._averagePowerKwRecipe = configure(_averagePowerKwRecipe);
        copy._averagePowerKw       = copy._averagePowerKwRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LoadCurvePoint" />.</summary>
    public LoadCurvePoint Generate() {
        return new LoadCurvePoint(_slotStart.Generate(),
                                  _averagePowerKw.Generate());
    }

}
