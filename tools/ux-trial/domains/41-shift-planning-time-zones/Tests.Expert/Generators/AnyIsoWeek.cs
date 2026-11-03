// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IsoWeek --force` overwrites it. This type is partial, so members you add in a
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
///     A generator of arbitrary <see cref="IsoWeek" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIsoWeek : IAny<IsoWeek> {

    private IAny<int> _year;
    private IAny<int> _week;

    private AnyInt32 _yearRecipe;
    private AnyInt32 _weekRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIsoWeek()
        : this(year: AnyValidYear(),
               week: AnyValidWeek()) { }

    private static AnyInt32 AnyValidYear() {
        return Any.Int32().Between(1, 9999);
    }

    private static AnyInt32 AnyValidWeek() {
        return Any.Int32().Between(1, 52);   // every ISO year has weeks 1 to 52; a 53rd week is a literal in the tests that need one
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyIsoWeek(AnyInt32 year,
                       AnyInt32 week) {
        _year       = year;
        _yearRecipe = year;
        _week       = week;
        _weekRecipe = week;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyIsoWeek(AnyIsoWeek from) {
        _year       = from._year;
        _yearRecipe = from._yearRecipe;
        _week       = from._week;
        _weekRecipe = from._weekRecipe;
    }

    /// <summary>Pins <c>year</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIsoWeek WithYear(int value) {
        AnyIsoWeek copy = new AnyIsoWeek(this);

        copy._year = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>year</c> is drawn from — <c>AnyValidYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIsoWeek WithYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIsoWeek copy = new AnyIsoWeek(this);

        copy._yearRecipe = configure(_yearRecipe);
        copy._year       = copy._yearRecipe;

        return copy;
    }

    /// <summary>Pins <c>week</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIsoWeek WithWeek(int value) {
        AnyIsoWeek copy = new AnyIsoWeek(this);

        copy._week = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>week</c> is drawn from — <c>AnyValidWeek()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIsoWeek WithWeek(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIsoWeek copy = new AnyIsoWeek(this);

        copy._weekRecipe = configure(_weekRecipe);
        copy._week       = copy._weekRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="IsoWeek" />.</summary>
    public IsoWeek Generate() {
        return new IsoWeek(_year.Generate(),
                           _week.Generate());
    }

}
