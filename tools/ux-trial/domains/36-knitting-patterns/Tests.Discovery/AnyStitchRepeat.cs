// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StitchRepeat --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StitchRepeat" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStitchRepeat : IAny<StitchRepeat> {

    private IAny<IReadOnlyList<StitchRun>> _runs;
    private IAny<int>                      _times;

    private AnyList<StitchRun> _runsRecipe;
    private AnyInt32           _timesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStitchRepeat()
        : this(runs:  AnyValidRuns(),
               times: AnyValidTimes()) { }

    private static AnyList<StitchRun> AnyValidRuns() {
        // A repeat expands to at most StitchRepeat.MaxOperations operations. At most 3 runs of at most 10
        // stitches, repeated at most 300 times, stay under 9 000 by construction.
        return Any.ListOf(new AnyStitchRun().WithCount(c => c.LessThanOrEqualTo(10))).WithCountBetween(1, 3);
    }

    private static AnyInt32 AnyValidTimes() {
        return Any.Int32().Between(2, 300);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStitchRepeat(AnyList<StitchRun> runs,
                            AnyInt32           times) {
        _runs        = runs;
        _runsRecipe  = runs;
        _times       = times;
        _timesRecipe = times;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStitchRepeat(AnyStitchRepeat from) {
        _runs        = from._runs;
        _runsRecipe  = from._runsRecipe;
        _times       = from._times;
        _timesRecipe = from._timesRecipe;
    }

    /// <summary>Pins <c>runs</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStitchRepeat WithRuns(IReadOnlyList<StitchRun> value) {
        AnyStitchRepeat copy = new AnyStitchRepeat(this);

        copy._runs = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>runs</c> is drawn from — <c>AnyValidRuns()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;StitchRun&gt;, AnyList&lt;StitchRun&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStitchRepeat WithRuns(Func<AnyList<StitchRun>, AnyList<StitchRun>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStitchRepeat copy = new AnyStitchRepeat(this);

        copy._runsRecipe = configure(_runsRecipe);
        copy._runs       = copy._runsRecipe;

        return copy;
    }

    /// <summary>Pins <c>times</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStitchRepeat WithTimes(int value) {
        AnyStitchRepeat copy = new AnyStitchRepeat(this);

        copy._times = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>times</c> is drawn from — <c>AnyValidTimes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStitchRepeat WithTimes(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStitchRepeat copy = new AnyStitchRepeat(this);

        copy._timesRecipe = configure(_timesRecipe);
        copy._times       = copy._timesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="StitchRepeat" />.</summary>
    public StitchRepeat Generate() {
        return new StitchRepeat(_runs.Generate(),
                                _times.Generate());
    }

}
