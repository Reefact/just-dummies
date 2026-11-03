// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeBucket --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace LiquidityGap.Domain;

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

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTimeBucket()
        : this(label:    AnyValidLabel(),
               startDay: AnyValidStartDay(),
               endDay:   AnyValidEndDay()) { }

    private static AnyString AnyValidLabel() {
        return Any.String().NotBlank();
    }

    private static AnyInt32 AnyValidStartDay() {
        // An end day must come after the start day: the two ranges do not overlap, so it holds by construction.
        return Any.Int32().Between(0, 1_000);
    }

    private static AnyInt32 AnyValidEndDay() {
        return Any.Int32().Between(1_001, 2_000);
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

        copy._endDay = Any.Fixed(value);

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

        copy._endDay = Any.Fixed<int?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TimeBucket" />.</summary>
    public TimeBucket Generate() {
        return new TimeBucket(_label.Generate(),
                              _startDay.Generate(),
                              _endDay.Generate());
    }

}
