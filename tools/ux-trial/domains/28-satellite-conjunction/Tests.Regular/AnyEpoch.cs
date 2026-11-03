// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Epoch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Epoch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEpoch : IAny<Epoch> {

    private IAny<DateTimeOffset> _utc;

    private AnyDateTimeOffset _utcRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEpoch()
        : this(utc: AnyValidUtc()) { }

    private static AnyDateTimeOffset AnyValidUtc() {
        // UTC (the default offset of Any.DateTimeOffset()), a day after the space age so a trajectory can start before it; capped so that Add stays far from the type's end.
        return Any.DateTimeOffset().Between(Epoch.SpaceAgeStart.AddDays(1), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEpoch(AnyDateTimeOffset utc) {
        _utc       = utc;
        _utcRecipe = utc;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEpoch(AnyEpoch from) {
        _utc       = from._utc;
        _utcRecipe = from._utcRecipe;
    }

    /// <summary>Pins <c>utc</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEpoch WithUtc(DateTimeOffset value) {
        AnyEpoch copy = new AnyEpoch(this);

        copy._utc = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>utc</c> is drawn from — <c>AnyValidUtc()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEpoch WithUtc(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEpoch copy = new AnyEpoch(this);

        copy._utcRecipe = configure(_utcRecipe);
        copy._utc       = copy._utcRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Epoch" />.</summary>
    public Epoch Generate() {
        return new Epoch(_utc.Generate());
    }

}
