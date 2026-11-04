// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CoveragePeriod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CoveragePeriod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCoveragePeriod : IAny<CoveragePeriod> {


    private AnyDateOnly _startRecipe;
    private AnyDateOnly _endRecipe;

    private DateOnly? _startPin;
    private DateOnly? _endPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCoveragePeriod()
        : this(start: AnyValidStart(),
               end:   AnyValidEnd()) { }

    private static AnyDateOnly AnyValidStart() {

        // Both ends in one calendar year: a period of at most a year, whichever the draw (CoveragePeriod's other tie).
        return Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
    }

    private static AnyDateOnly AnyValidEnd() {

        // Both ends in one calendar year: a period of at most a year, whichever the draw (CoveragePeriod's other tie).
        return Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCoveragePeriod(AnyDateOnly start,
                              AnyDateOnly end) {
        _startRecipe = start;
        _endRecipe   = end;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCoveragePeriod(AnyCoveragePeriod from) {
        _startRecipe = from._startRecipe;
        _startPin    = from._startPin;
        _endRecipe   = from._endRecipe;
        _endPin      = from._endPin;
    }

    /// <summary>Draws <c>start</c> and <c>end</c>, <c>start</c> always before <c>end</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (DateOnly Start, DateOnly End) StartAndEnd() {
        if (_startPin is { } start) { return (start, _endPin ?? _endRecipe.After(start).Generate()); }
        if (_endPin is { } end) { return (_startRecipe.Before(end).Generate(), end); }

        return Any.Pair(_startRecipe, _endRecipe).Having.Item1BeforeItem2().Generate();
    }

    /// <summary>Pins <c>start</c> to a fixed value, which bounds <c>end</c>; its recipe is kept for a later refinement.</summary>
    public AnyCoveragePeriod WithStart(DateOnly value) {
        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._startPin    = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>start</c> is drawn from — <c>AnyValidStart()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>end</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCoveragePeriod WithStart(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._startRecipe = configure(_startRecipe);
        copy._startPin    = null;

        return copy;
    }

    /// <summary>Pins <c>end</c> to a fixed value, which bounds <c>start</c>; its recipe is kept for a later refinement.</summary>
    public AnyCoveragePeriod WithEnd(DateOnly value) {
        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._endPin      = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>end</c> is drawn from — <c>AnyValidEnd()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>start</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCoveragePeriod WithEnd(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._endRecipe   = configure(_endRecipe);
        copy._endPin      = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CoveragePeriod" />.</summary>
    public CoveragePeriod Generate() {
        (DateOnly start, DateOnly end) = StartAndEnd();

        return new CoveragePeriod(start,
                                  end);
    }

}
