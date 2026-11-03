// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeSignature --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TimeSignature" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTimeSignature : IAny<TimeSignature> {

    private IAny<int> _beats;
    private IAny<int> _beatValue;

    private AnyInt32 _beatsRecipe;
    private AnyInt32 _beatValueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTimeSignature()
        : this(beats:     AnyValidBeats(),
               beatValue: AnyValidBeatValue()) { }

    private static AnyInt32 AnyValidBeats() {
        return Any.Int32().Positive().LessThanOrEqualTo(32);
    }

    private static AnyInt32 AnyValidBeatValue() {
        return Any.Int32().OneOf(1, 2, 4, 8, 16, 32, 64);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTimeSignature(AnyInt32 beats,
                             AnyInt32 beatValue) {
        _beats           = beats;
        _beatsRecipe     = beats;
        _beatValue       = beatValue;
        _beatValueRecipe = beatValue;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTimeSignature(AnyTimeSignature from) {
        _beats           = from._beats;
        _beatsRecipe     = from._beatsRecipe;
        _beatValue       = from._beatValue;
        _beatValueRecipe = from._beatValueRecipe;
    }

    /// <summary>Pins <c>beats</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTimeSignature WithBeats(int value) {
        AnyTimeSignature copy = new AnyTimeSignature(this);

        copy._beats = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>beats</c> is drawn from — <c>AnyValidBeats()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTimeSignature WithBeats(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTimeSignature copy = new AnyTimeSignature(this);

        copy._beatsRecipe = configure(_beatsRecipe);
        copy._beats       = copy._beatsRecipe;

        return copy;
    }

    /// <summary>Pins <c>beatValue</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTimeSignature WithBeatValue(int value) {
        AnyTimeSignature copy = new AnyTimeSignature(this);

        copy._beatValue = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>beatValue</c> is drawn from — <c>AnyValidBeatValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTimeSignature WithBeatValue(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTimeSignature copy = new AnyTimeSignature(this);

        copy._beatValueRecipe = configure(_beatValueRecipe);
        copy._beatValue       = copy._beatValueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TimeSignature" />.</summary>
    public TimeSignature Generate() {
        return new TimeSignature(_beats.Generate(),
                                 _beatValue.Generate());
    }

}
