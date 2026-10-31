// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VoiceEntry --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VoiceEntry" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVoiceEntry : IAny<VoiceEntry> {

    private readonly IAny<Duration> _duration;
    private readonly IAny<Pitch?>   _pitch;
    private readonly IAny<bool>     _tiedToNext;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVoiceEntry()
        : this(duration:   AnyValidDuration(),
               pitch:      AnyValidPitch(),
               tiedToNext: AnyValidTiedToNext()) { }

    private static AnyDuration AnyValidDuration() {
        return new AnyDuration().WithNumerator(n => n.Positive());
    }

    private static AnyPitch AnyValidPitch() {
        return new AnyPitch();
    }

    private static AnyBoolean AnyValidTiedToNext() {
        return Any.Boolean();
    }

    private AnyVoiceEntry(IAny<Duration> duration,
                          IAny<Pitch?>   pitch,
                          IAny<bool>     tiedToNext) {
        _duration   = duration;
        _pitch      = pitch;
        _tiedToNext = tiedToNext;
    }

    /// <summary>Pins <c>duration</c> to a fixed value.</summary>
    public AnyVoiceEntry WithDuration(Duration value) {
        return new AnyVoiceEntry(new FixedValue<Duration>(value), _pitch, _tiedToNext);
    }

    /// <summary>Draws <c>duration</c> from <c>AnyValidDuration()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoiceEntry WithDuration(Func<AnyDuration, AnyDuration> configure) {
        return new AnyVoiceEntry(configure(AnyValidDuration()), _pitch, _tiedToNext);
    }

    /// <summary>Pins <c>pitch</c> to a fixed value.</summary>
    public AnyVoiceEntry WithPitch(Pitch? value) {
        return new AnyVoiceEntry(_duration, new FixedValue<Pitch?>(value), _tiedToNext);
    }

    /// <summary>Draws <c>pitch</c> from <c>AnyValidPitch()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoiceEntry WithPitch(Func<AnyPitch, AnyPitch> configure) {
        return new AnyVoiceEntry(_duration, configure(AnyValidPitch()), _tiedToNext);
    }

    /// <summary>Pins <c>tiedToNext</c> to a fixed value.</summary>
    public AnyVoiceEntry WithTiedToNext(bool value) {
        return new AnyVoiceEntry(_duration, _pitch, new FixedValue<bool>(value));
    }

    /// <summary>Draws <c>tiedToNext</c> from <c>AnyValidTiedToNext()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoiceEntry WithTiedToNext(Func<AnyBoolean, AnyBoolean> configure) {
        return new AnyVoiceEntry(_duration, _pitch, configure(AnyValidTiedToNext()));
    }

    /// <summary>Produces one arbitrary <see cref="VoiceEntry" />.</summary>
    public VoiceEntry Generate() {
        return new VoiceEntry(_duration.Generate(),
                              _pitch.Generate(),
                              _tiedToNext.Generate());
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
