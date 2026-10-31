// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Pitch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Pitch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPitch : IAny<Pitch> {

    private readonly IAny<char> _step;
    private readonly IAny<int>  _alter;
    private readonly IAny<int>  _octave;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPitch()
        : this(step:   AnyValidStep(),
               alter:  AnyValidAlter(),
               octave: AnyValidOctave()) { }

    private static AnyChar AnyValidStep() {
        return Any.Char().OneOf("ABCDEFG");
    }

    private static AnyInt32 AnyValidAlter() {
        return Any.Int32().Between(-2, 2);
    }

    private static AnyInt32 AnyValidOctave() {
        // 0..9 is the constructor's range, but B##9 leaves MIDI 0-127; any alteration of
        // any step in octaves 0..8 stays inside it, so the cross-field rule holds by construction.
        return Any.Int32().Between(0, 8);
    }

    private AnyPitch(IAny<char> step,
                     IAny<int>  alter,
                     IAny<int>  octave) {
        _step   = step;
        _alter  = alter;
        _octave = octave;
    }

    /// <summary>Pins <c>step</c> to a fixed value.</summary>
    public AnyPitch WithStep(char value) {
        return new AnyPitch(new FixedValue<char>(value), _alter, _octave);
    }

    /// <summary>Draws <c>step</c> from <c>AnyValidStep()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPitch WithStep(Func<AnyChar, AnyChar> configure) {
        return new AnyPitch(configure(AnyValidStep()), _alter, _octave);
    }

    /// <summary>Pins <c>alter</c> to a fixed value.</summary>
    public AnyPitch WithAlter(int value) {
        return new AnyPitch(_step, new FixedValue<int>(value), _octave);
    }

    /// <summary>Draws <c>alter</c> from <c>AnyValidAlter()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPitch WithAlter(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPitch(_step, configure(AnyValidAlter()), _octave);
    }

    /// <summary>Pins <c>octave</c> to a fixed value.</summary>
    public AnyPitch WithOctave(int value) {
        return new AnyPitch(_step, _alter, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>octave</c> from <c>AnyValidOctave()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPitch WithOctave(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPitch(_step, _alter, configure(AnyValidOctave()));
    }

    /// <summary>Produces one arbitrary <see cref="Pitch" />.</summary>
    public Pitch Generate() {
        return new Pitch(_step.Generate(),
                         _alter.Generate(),
                         _octave.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
