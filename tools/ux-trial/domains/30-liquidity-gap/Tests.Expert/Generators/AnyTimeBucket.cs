// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeBucket --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using LiquidityGap.Domain;

namespace LiquidityGap.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="TimeBucket" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTimeBucket : IAny<TimeBucket> {

    private IAny<string> _label;
    private IAny<int>    _startDay;
    private IAny<int?>   _endDay;

    private AnyString _labelRecipe;
    private AnyInt32  _startDayRecipe;
    private AnyInt32  _endDayRecipe;
    private bool      _endDayPinned;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTimeBucket()
        : this(label:    AnyValidLabel(),
               startDay: AnyValidStartDay(),
               endDay:   AnyValidEndDay()) { }

    private static AnyString AnyValidLabel() {
        return Any.String().NotBlank();
    }

    private static AnyInt32 AnyValidStartDay() {
        // A century of days: ample, and it leaves room above the start for an end.
        return Any.Int32().Between(0, 36_500);
    }

    private static AnyInt32 AnyValidEndDay() {
        return Any.Int32();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTimeBucket(AnyString label,
                          AnyInt32  startDay,
                          AnyInt32  endDay) {
        _label          = label;
        _labelRecipe    = label;
        _startDay       = startDay;
        _startDayRecipe = startDay;
        _endDay         = endDay.OrNull();
        _endDayRecipe   = endDay;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTimeBucket(AnyTimeBucket from) {
        _label          = from._label;
        _labelRecipe    = from._labelRecipe;
        _startDay       = from._startDay;
        _startDayRecipe = from._startDayRecipe;
        _endDay         = from._endDay;
        _endDayRecipe   = from._endDayRecipe;
        _endDayPinned   = from._endDayPinned;
    }

    /// <summary>Pins <c>label</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTimeBucket WithLabel(string value) {
        AnyTimeBucket copy = new AnyTimeBucket(this);

        copy._label = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>label</c> is drawn from — <c>AnyValidLabel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTimeBucket WithLabel(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTimeBucket copy = new AnyTimeBucket(this);

        copy._labelRecipe = configure(_labelRecipe);
        copy._label       = copy._labelRecipe;

        return copy;
    }

    /// <summary>Pins <c>startDay</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTimeBucket WithStartDay(int value) {
        AnyTimeBucket copy = new AnyTimeBucket(this);

        copy._startDay = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>startDay</c> is drawn from — <c>AnyValidStartDay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTimeBucket WithStartDay(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTimeBucket copy = new AnyTimeBucket(this);

        copy._startDayRecipe = configure(_startDayRecipe);
        copy._startDay       = copy._startDayRecipe;

        return copy;
    }

    /// <summary>Pins <c>endDay</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTimeBucket WithEndDay(int? value) {
        AnyTimeBucket copy = new AnyTimeBucket(this);

        copy._endDay       = Any.Fixed(value);
        copy._endDayPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>endDay</c> is drawn from — <c>AnyValidEndDay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTimeBucket WithEndDay(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTimeBucket copy = new AnyTimeBucket(this);

        copy._endDayRecipe = configure(_endDayRecipe);
        copy._endDay       = copy._endDayRecipe.AsNullable();

        return copy;
    }

    /// <summary>Pins <c>endDay</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyTimeBucket WithoutEndDay() {
        AnyTimeBucket copy = new AnyTimeBucket(this);

        copy._endDay       = Any.Fixed<int?>(null);
        copy._endDayPinned = true;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TimeBucket" />.</summary>
    public TimeBucket Generate() {
        // The tie: endDay, when present, is after startDay, unless the end is pinned.
        // Written by hand: the scaffolder drew the two independently.
        int  startDay = _startDay.Generate();
        int? endDay   = _endDayPinned ? _endDay.Generate() : _endDayRecipe.GreaterThan(startDay).OrNull().Generate();

        return new TimeBucket(_label.Generate(),
                              startDay,
                              endDay);
    }

}
