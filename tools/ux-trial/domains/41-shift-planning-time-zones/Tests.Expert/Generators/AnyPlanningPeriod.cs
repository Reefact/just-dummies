// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PlanningPeriod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using ShiftPlanning.Domain;

namespace ShiftPlanning.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="PlanningPeriod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPlanningPeriod : IAny<PlanningPeriod> {

    private IAny<IsoWeek> _firstWeek;
    private IAny<int>     _weekCount;

    private AnyIsoWeek _firstWeekRecipe;
    private AnyInt32   _weekCountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPlanningPeriod()
        : this(firstWeek: AnyValidFirstWeek(),
               weekCount: AnyValidWeekCount()) { }

    private static AnyIsoWeek AnyValidFirstWeek() {
        return new AnyIsoWeek().WithYear(year => year.LessThanOrEqualTo(9998));   // 13 weeks from 9998 still end in 9999
    }

    private static AnyInt32 AnyValidWeekCount() {
        return Any.Int32().Positive().LessThanOrEqualTo(PlanningPeriod.MaxWeeks);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPlanningPeriod(AnyIsoWeek firstWeek,
                              AnyInt32   weekCount) {
        _firstWeek       = firstWeek;
        _firstWeekRecipe = firstWeek;
        _weekCount       = weekCount;
        _weekCountRecipe = weekCount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPlanningPeriod(AnyPlanningPeriod from) {
        _firstWeek       = from._firstWeek;
        _firstWeekRecipe = from._firstWeekRecipe;
        _weekCount       = from._weekCount;
        _weekCountRecipe = from._weekCountRecipe;
    }

    /// <summary>Pins <c>firstWeek</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlanningPeriod WithFirstWeek(IsoWeek value) {
        AnyPlanningPeriod copy = new AnyPlanningPeriod(this);

        copy._firstWeek = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>firstWeek</c> is drawn from — <c>AnyValidFirstWeek()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyIsoWeek, AnyIsoWeek&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlanningPeriod WithFirstWeek(Func<AnyIsoWeek, AnyIsoWeek> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlanningPeriod copy = new AnyPlanningPeriod(this);

        copy._firstWeekRecipe = configure(_firstWeekRecipe);
        copy._firstWeek       = copy._firstWeekRecipe;

        return copy;
    }

    /// <summary>Pins <c>weekCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlanningPeriod WithWeekCount(int value) {
        AnyPlanningPeriod copy = new AnyPlanningPeriod(this);

        copy._weekCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>weekCount</c> is drawn from — <c>AnyValidWeekCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlanningPeriod WithWeekCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlanningPeriod copy = new AnyPlanningPeriod(this);

        copy._weekCountRecipe = configure(_weekCountRecipe);
        copy._weekCount       = copy._weekCountRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PlanningPeriod" />.</summary>
    public PlanningPeriod Generate() {
        return new PlanningPeriod(_firstWeek.Generate(),
                                  _weekCount.Generate());
    }

}
