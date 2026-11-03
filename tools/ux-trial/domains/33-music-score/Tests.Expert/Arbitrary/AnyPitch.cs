// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Pitch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Pitch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPitch : IAny<Pitch> {

    private IAny<char> _step;
    private IAny<int>  _alter;
    private IAny<int>  _octave;

    private AnyChar  _stepRecipe;
    private AnyInt32 _alterRecipe;
    private AnyInt32 _octaveRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPitch()
        : this(step:   AnyValidStep(),
               alter:  AnyValidAlter(),
               octave: AnyValidOctave()) { }

    private static AnyChar AnyValidStep() {
        return Any.Char().OneOf('A', 'B', 'C', 'D', 'E', 'F', 'G');
    }

    private static AnyInt32 AnyValidAlter() {
        return Any.Int32().Between(-2, 2);
    }

    private static AnyInt32 AnyValidOctave() {
        // The MIDI range is the only cross-field rule, and it can only fail in octave 9 (G##9 = 129
        // is out; octave 0 bottoms out at MIDI 10). An arbitrary pitch stays in 0..8 so that every
        // step and alteration is valid with it; a test about octave 9 pins the three parameters.
        return Any.Int32().Between(0, 8);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPitch(AnyChar  step,
                     AnyInt32 alter,
                     AnyInt32 octave) {
        _step         = step;
        _stepRecipe   = step;
        _alter        = alter;
        _alterRecipe  = alter;
        _octave       = octave;
        _octaveRecipe = octave;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPitch(AnyPitch from) {
        _step         = from._step;
        _stepRecipe   = from._stepRecipe;
        _alter        = from._alter;
        _alterRecipe  = from._alterRecipe;
        _octave       = from._octave;
        _octaveRecipe = from._octaveRecipe;
    }

    /// <summary>Pins <c>step</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPitch WithStep(char value) {
        AnyPitch copy = new AnyPitch(this);

        copy._step = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>step</c> is drawn from — <c>AnyValidStep()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyChar, AnyChar&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPitch WithStep(Func<AnyChar, AnyChar> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPitch copy = new AnyPitch(this);

        copy._stepRecipe = configure(_stepRecipe);
        copy._step       = copy._stepRecipe;

        return copy;
    }

    /// <summary>Pins <c>alter</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPitch WithAlter(int value) {
        AnyPitch copy = new AnyPitch(this);

        copy._alter = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>alter</c> is drawn from — <c>AnyValidAlter()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPitch WithAlter(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPitch copy = new AnyPitch(this);

        copy._alterRecipe = configure(_alterRecipe);
        copy._alter       = copy._alterRecipe;

        return copy;
    }

    /// <summary>Pins <c>octave</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPitch WithOctave(int value) {
        AnyPitch copy = new AnyPitch(this);

        copy._octave = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>octave</c> is drawn from — <c>AnyValidOctave()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPitch WithOctave(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPitch copy = new AnyPitch(this);

        copy._octaveRecipe = configure(_octaveRecipe);
        copy._octave       = copy._octaveRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Pitch" />.</summary>
    public Pitch Generate() {
        return new Pitch(_step.Generate(),
                         _alter.Generate(),
                         _octave.Generate());
    }

}
