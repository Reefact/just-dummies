// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeSignature --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TimeSignature" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTimeSignature : IAny<TimeSignature> {

    private readonly IAny<int> _beats;
    private readonly IAny<int> _beatValue;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTimeSignature()
        : this(beats:     AnyValidBeats(),
               beatValue: AnyValidBeatValue()) { }

    private static AnyInt32 AnyValidBeats() {
        return Any.Int32().Between(1, 32);
    }

    private static AnyInt32 AnyValidBeatValue() {
        return Any.Int32().OneOf(1, 2, 4, 8, 16, 32, 64);
    }

    private AnyTimeSignature(IAny<int> beats,
                             IAny<int> beatValue) {
        _beats     = beats;
        _beatValue = beatValue;
    }

    /// <summary>Pins <c>beats</c> to a fixed value.</summary>
    public AnyTimeSignature WithBeats(int value) {
        return new AnyTimeSignature(new FixedValue<int>(value), _beatValue);
    }

    /// <summary>Draws <c>beats</c> from <c>AnyValidBeats()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTimeSignature WithBeats(Func<AnyInt32, AnyInt32> configure) {
        return new AnyTimeSignature(configure(AnyValidBeats()), _beatValue);
    }

    /// <summary>Pins <c>beatValue</c> to a fixed value.</summary>
    public AnyTimeSignature WithBeatValue(int value) {
        return new AnyTimeSignature(_beats, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>beatValue</c> from <c>AnyValidBeatValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTimeSignature WithBeatValue(Func<AnyInt32, AnyInt32> configure) {
        return new AnyTimeSignature(_beats, configure(AnyValidBeatValue()));
    }

    /// <summary>Produces one arbitrary <see cref="TimeSignature" />.</summary>
    public TimeSignature Generate() {
        return new TimeSignature(_beats.Generate(),
                                 _beatValue.Generate());
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
