// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OffPeakWindow --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="OffPeakWindow" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOffPeakWindow : IAny<OffPeakWindow> {

    private IAny<TimeOnly> _start;
    private IAny<TimeOnly> _end;

    private AnyTimeOnly _startRecipe;
    private AnyTimeOnly _endRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOffPeakWindow()
        : this(start: AnyValidStart(),
               end:   AnyValidEnd()) { }

    private static AnyTimeOnly AnyValidStart() {
        return Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30));
    }

    private static AnyTimeOnly AnyValidEnd() {
        return Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyOffPeakWindow(AnyTimeOnly start,
                             AnyTimeOnly end) {
        _start       = start;
        _startRecipe = start;
        _end         = end;
        _endRecipe   = end;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyOffPeakWindow(AnyOffPeakWindow from) {
        _start       = from._start;
        _startRecipe = from._startRecipe;
        _end         = from._end;
        _endRecipe   = from._endRecipe;
    }

    /// <summary>Pins <c>start</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOffPeakWindow WithStart(TimeOnly value) {
        AnyOffPeakWindow copy = new AnyOffPeakWindow(this);

        copy._start = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>start</c> is drawn from — <c>AnyValidStart()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeOnly, AnyTimeOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOffPeakWindow WithStart(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOffPeakWindow copy = new AnyOffPeakWindow(this);

        copy._startRecipe = configure(_startRecipe);
        copy._start       = copy._startRecipe;

        return copy;
    }

    /// <summary>Pins <c>end</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOffPeakWindow WithEnd(TimeOnly value) {
        AnyOffPeakWindow copy = new AnyOffPeakWindow(this);

        copy._end = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>end</c> is drawn from — <c>AnyValidEnd()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeOnly, AnyTimeOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOffPeakWindow WithEnd(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOffPeakWindow copy = new AnyOffPeakWindow(this);

        copy._endRecipe = configure(_endRecipe);
        copy._end       = copy._endRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="OffPeakWindow" />.</summary>
    public OffPeakWindow Generate() {
        TimeOnly start = _start.Generate();
        TimeOnly end   = _end.DifferentFrom(start).Generate();

        return new OffPeakWindow(start, end);
    }

}
