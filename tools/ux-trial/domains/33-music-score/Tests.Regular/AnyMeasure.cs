// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Measure --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Measure" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasure : IAny<Measure> {

    private readonly IAny<int>                  _number;
    private readonly IAny<TimeSignature>        _timeSignature;
    private readonly IAny<Dynamic>              _dynamic;
    private readonly IAny<IReadOnlyList<Voice>>? _voices;
    private readonly IAny<bool>                 _repeatStart;
    private readonly IAny<int>                  _repeatEndTimes;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasure()
        : this(number:         AnyValidNumber(),
               timeSignature:  AnyValidTimeSignature(),
               dynamic:        AnyValidDynamic(),
               voices:         null,
               repeatStart:    AnyValidRepeatStart(),
               repeatEndTimes: AnyValidRepeatEndTimes()) { }

    private static AnyInt32 AnyValidNumber() {
        return Any.Int32().Positive();
    }

    private static AnyTimeSignature AnyValidTimeSignature() {
        return new AnyTimeSignature();
    }

    private static AnyEnum<Dynamic> AnyValidDynamic() {
        return Any.Enum<Dynamic>();
    }

    // Every voice must fill the meter exactly, so the voices are not drawn on their own: unless a test
    // pins them, Generate() derives one voice of untied beat-long notes from the drawn time signature.
    private static IReadOnlyList<Voice> VoicesFilling(TimeSignature timeSignature) {
        Duration beat = new Duration(1, timeSignature.BeatValue);
        List<VoiceEntry> entries = Any.ListOf(new AnyVoiceEntry().WithDuration(beat).WithTiedToNext(false))
                                      .WithCount(timeSignature.Beats)
                                      .Generate();

        return [new Voice(1, entries)];
    }

    private static AnyBoolean AnyValidRepeatStart() {
        return Any.Boolean();
    }

    private static AnyInt32 AnyValidRepeatEndTimes() {
        return Any.Int32().OneOf(0, 2, 3, 4, 5, 6, 7, 8);
    }

    private AnyMeasure(IAny<int>                  number,
                       IAny<TimeSignature>        timeSignature,
                       IAny<Dynamic>              dynamic,
                       IAny<IReadOnlyList<Voice>>? voices,
                       IAny<bool>                 repeatStart,
                       IAny<int>                  repeatEndTimes) {
        _number         = number;
        _timeSignature  = timeSignature;
        _dynamic        = dynamic;
        _voices         = voices;
        _repeatStart    = repeatStart;
        _repeatEndTimes = repeatEndTimes;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyMeasure WithNumber(int value) {
        return new AnyMeasure(new FixedValue<int>(value), _timeSignature, _dynamic, _voices, _repeatStart, _repeatEndTimes);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasure WithNumber(Func<AnyInt32, AnyInt32> configure) {
        return new AnyMeasure(configure(AnyValidNumber()), _timeSignature, _dynamic, _voices, _repeatStart, _repeatEndTimes);
    }

    /// <summary>Pins <c>timeSignature</c> to a fixed value.</summary>
    public AnyMeasure WithTimeSignature(TimeSignature value) {
        return new AnyMeasure(_number, new FixedValue<TimeSignature>(value), _dynamic, _voices, _repeatStart, _repeatEndTimes);
    }

    /// <summary>Draws <c>timeSignature</c> from <c>AnyValidTimeSignature()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasure WithTimeSignature(Func<AnyTimeSignature, AnyTimeSignature> configure) {
        return new AnyMeasure(_number, configure(AnyValidTimeSignature()), _dynamic, _voices, _repeatStart, _repeatEndTimes);
    }

    /// <summary>Pins <c>dynamic</c> to a fixed value.</summary>
    public AnyMeasure WithDynamic(Dynamic value) {
        return new AnyMeasure(_number, _timeSignature, new FixedValue<Dynamic>(value), _voices, _repeatStart, _repeatEndTimes);
    }

    /// <summary>Draws <c>dynamic</c> from <c>AnyValidDynamic()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasure WithDynamic(Func<AnyEnum<Dynamic>, AnyEnum<Dynamic>> configure) {
        return new AnyMeasure(_number, _timeSignature, configure(AnyValidDynamic()), _voices, _repeatStart, _repeatEndTimes);
    }

    /// <summary>Pins <c>voices</c> to a fixed value.</summary>
    public AnyMeasure WithVoices(IReadOnlyList<Voice> value) {
        return new AnyMeasure(_number, _timeSignature, _dynamic, new FixedValue<IReadOnlyList<Voice>>(value), _repeatStart, _repeatEndTimes);
    }

    /// <summary>Pins <c>repeatStart</c> to a fixed value.</summary>
    public AnyMeasure WithRepeatStart(bool value) {
        return new AnyMeasure(_number, _timeSignature, _dynamic, _voices, new FixedValue<bool>(value), _repeatEndTimes);
    }

    /// <summary>Draws <c>repeatStart</c> from <c>AnyValidRepeatStart()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasure WithRepeatStart(Func<AnyBoolean, AnyBoolean> configure) {
        return new AnyMeasure(_number, _timeSignature, _dynamic, _voices, configure(AnyValidRepeatStart()), _repeatEndTimes);
    }

    /// <summary>Pins <c>repeatEndTimes</c> to a fixed value.</summary>
    public AnyMeasure WithRepeatEndTimes(int value) {
        return new AnyMeasure(_number, _timeSignature, _dynamic, _voices, _repeatStart, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>repeatEndTimes</c> from <c>AnyValidRepeatEndTimes()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasure WithRepeatEndTimes(Func<AnyInt32, AnyInt32> configure) {
        return new AnyMeasure(_number, _timeSignature, _dynamic, _voices, _repeatStart, configure(AnyValidRepeatEndTimes()));
    }

    /// <summary>Produces one arbitrary <see cref="Measure" />.</summary>
    public Measure Generate() {
        TimeSignature timeSignature = _timeSignature.Generate();

        return new Measure(_number.Generate(),
                           timeSignature,
                           _dynamic.Generate(),
                           _voices?.Generate() ?? VoicesFilling(timeSignature),
                           _repeatStart.Generate(),
                           _repeatEndTimes.Generate());
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
