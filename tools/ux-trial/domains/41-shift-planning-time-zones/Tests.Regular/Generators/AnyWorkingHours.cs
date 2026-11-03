// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate WorkingHours --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace ShiftPlanning.Domain;

/// <summary>
///     A generator of arbitrary <see cref="WorkingHours" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWorkingHours : IAny<WorkingHours> {

    private IAny<TimeOnly>               _opens;
    private IAny<TimeOnly>               _closes;
    private IAny<IEnumerable<DayOfWeek>> _days;

    private AnyTimeOnly            _opensRecipe;
    private AnyTimeOnly            _closesRecipe;
    private AnySequence<DayOfWeek> _daysRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWorkingHours()
        : this(opens:  AnyValidOpens(),
               closes: AnyValidCloses(),
               days:   AnyValidDays()) { }

    private static AnyTimeOnly AnyValidOpens() {
        return Any.TimeOnly().Between(new TimeOnly(6, 0), new TimeOnly(11, 45)).WithGranularity(TimeSpan.FromMinutes(15));
    }

    private static AnyTimeOnly AnyValidCloses() {
        return Any.TimeOnly().Between(new TimeOnly(13, 0), new TimeOnly(20, 0)).WithGranularity(TimeSpan.FromMinutes(15));
    }

    private static AnySequence<DayOfWeek> AnyValidDays() {
        return Any.SequenceOf(Any.Enum<DayOfWeek>()).WithCountBetween(1, 7).Distinct();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyWorkingHours(AnyTimeOnly            opens,
                            AnyTimeOnly            closes,
                            AnySequence<DayOfWeek> days) {
        _opens        = opens;
        _opensRecipe  = opens;
        _closes       = closes;
        _closesRecipe = closes;
        _days         = days;
        _daysRecipe   = days;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyWorkingHours(AnyWorkingHours from) {
        _opens        = from._opens;
        _opensRecipe  = from._opensRecipe;
        _closes       = from._closes;
        _closesRecipe = from._closesRecipe;
        _days         = from._days;
        _daysRecipe   = from._daysRecipe;
    }

    /// <summary>Pins <c>opens</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkingHours WithOpens(TimeOnly value) {
        AnyWorkingHours copy = new AnyWorkingHours(this);

        copy._opens = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>opens</c> is drawn from — <c>AnyValidOpens()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeOnly, AnyTimeOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkingHours WithOpens(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkingHours copy = new AnyWorkingHours(this);

        copy._opensRecipe = configure(_opensRecipe);
        copy._opens       = copy._opensRecipe;

        return copy;
    }

    /// <summary>Pins <c>closes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkingHours WithCloses(TimeOnly value) {
        AnyWorkingHours copy = new AnyWorkingHours(this);

        copy._closes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>closes</c> is drawn from — <c>AnyValidCloses()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeOnly, AnyTimeOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkingHours WithCloses(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkingHours copy = new AnyWorkingHours(this);

        copy._closesRecipe = configure(_closesRecipe);
        copy._closes       = copy._closesRecipe;

        return copy;
    }

    /// <summary>Pins <c>days</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkingHours WithDays(IEnumerable<DayOfWeek> value) {
        AnyWorkingHours copy = new AnyWorkingHours(this);

        copy._days = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>days</c> is drawn from — <c>AnyValidDays()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySequence&lt;DayOfWeek&gt;, AnySequence&lt;DayOfWeek&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkingHours WithDays(Func<AnySequence<DayOfWeek>, AnySequence<DayOfWeek>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkingHours copy = new AnyWorkingHours(this);

        copy._daysRecipe = configure(_daysRecipe);
        copy._days       = copy._daysRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="WorkingHours" />.</summary>
    public WorkingHours Generate() {
        return new WorkingHours(_opens.Generate(),
                                _closes.Generate(),
                                _days.Generate());
    }

}
