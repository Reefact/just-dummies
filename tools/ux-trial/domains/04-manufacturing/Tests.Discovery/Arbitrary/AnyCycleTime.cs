// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CycleTime --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using Manufacturing.Domain;

namespace Manufacturing.Tests.Discovery.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="CycleTime" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCycleTime : IAny<CycleTime> {

    private IAny<TimeSpan> _duration;

    private AnyTimeSpan _durationRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCycleTime()
        : this(duration: AnyValidDuration()) { }

    private static AnyTimeSpan AnyValidDuration() {

        return Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromHours(1)).WithGranularity(TimeSpan.FromSeconds(1));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCycleTime(AnyTimeSpan duration) {
        _duration       = duration;
        _durationRecipe = duration;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCycleTime(AnyCycleTime from) {
        _duration       = from._duration;
        _durationRecipe = from._durationRecipe;
    }

    /// <summary>Pins <c>duration</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCycleTime WithDuration(TimeSpan value) {
        AnyCycleTime copy = new AnyCycleTime(this);

        copy._duration = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>duration</c> is drawn from — <c>AnyValidDuration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCycleTime WithDuration(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCycleTime copy = new AnyCycleTime(this);

        copy._durationRecipe = configure(_durationRecipe);
        copy._duration       = copy._durationRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CycleTime" />.</summary>
    public CycleTime Generate() {
        return new CycleTime(_duration.Generate());
    }

}
