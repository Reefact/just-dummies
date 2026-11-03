// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Tempo --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Tempo" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTempo : IAny<Tempo> {

    private IAny<double>   _beatsPerMinute;
    private IAny<Duration> _beatUnit;

    private AnyDouble   _beatsPerMinuteRecipe;
    private AnyDuration _beatUnitRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTempo()
        : this(beatsPerMinute: AnyValidBeatsPerMinute(),
               beatUnit:       AnyValidBeatUnit()) { }

    private static AnyDouble AnyValidBeatsPerMinute() {
        return Any.Double().Between(20d, 400d);
    }

    private static AnyDuration AnyValidBeatUnit() {
        // A unit fraction 1/1 to 1/64: positive and at most a whole note by construction.
        return new AnyDuration().WithNumerator(1).WithDenominator(d => d.Between(1, 64));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTempo(AnyDouble   beatsPerMinute,
                     AnyDuration beatUnit) {
        _beatsPerMinute       = beatsPerMinute;
        _beatsPerMinuteRecipe = beatsPerMinute;
        _beatUnit             = beatUnit;
        _beatUnitRecipe       = beatUnit;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTempo(AnyTempo from) {
        _beatsPerMinute       = from._beatsPerMinute;
        _beatsPerMinuteRecipe = from._beatsPerMinuteRecipe;
        _beatUnit             = from._beatUnit;
        _beatUnitRecipe       = from._beatUnitRecipe;
    }

    /// <summary>Pins <c>beatsPerMinute</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTempo WithBeatsPerMinute(double value) {
        AnyTempo copy = new AnyTempo(this);

        copy._beatsPerMinute = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>beatsPerMinute</c> is drawn from — <c>AnyValidBeatsPerMinute()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTempo WithBeatsPerMinute(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTempo copy = new AnyTempo(this);

        copy._beatsPerMinuteRecipe = configure(_beatsPerMinuteRecipe);
        copy._beatsPerMinute       = copy._beatsPerMinuteRecipe;

        return copy;
    }

    /// <summary>Pins <c>beatUnit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTempo WithBeatUnit(Duration value) {
        AnyTempo copy = new AnyTempo(this);

        copy._beatUnit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>beatUnit</c> is drawn from — <c>AnyValidBeatUnit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDuration, AnyDuration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTempo WithBeatUnit(Func<AnyDuration, AnyDuration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTempo copy = new AnyTempo(this);

        copy._beatUnitRecipe = configure(_beatUnitRecipe);
        copy._beatUnit       = copy._beatUnitRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Tempo" />.</summary>
    public Tempo Generate() {
        return new Tempo(_beatsPerMinute.Generate(),
                         _beatUnit.Generate());
    }

}
