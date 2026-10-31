// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Measure --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;
using MusicScore.Domain;

namespace MusicScore.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Measure" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasure : IAny<Measure> {

    private readonly IAny<int>                  _number;
    private readonly IAny<TimeSignature>        _timeSignature;
    private readonly IAny<Dynamic>              _dynamic;
    private readonly Func<TimeSignature, IAny<IReadOnlyList<Voice>>> _voices;
    private readonly IAny<bool>                 _repeatStart;
    private readonly IAny<int>                  _repeatEndTimes;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasure()
        : this(number:         AnyValidNumber(),
               timeSignature:  AnyValidTimeSignature(),
               dynamic:        AnyValidDynamic(),
               voices:         meter => AnyValidVoices(meter),
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

    private static AnyList<Voice> AnyValidVoices(TimeSignature meter) {
        // Every voice fills exactly the meter's length, and no two voices share a number.
        return Any.ListOf(new AnyVoice().Filling(meter.MeasureLength))
                  .WithCountBetween(1, Voice.MaxVoices)
                  .Distinct(VoiceNumberComparer.Instance);
    }

    private static AnyBoolean AnyValidRepeatStart() {
        // Repeat barlines shape the played order of a whole score: a test that wants one writes it.
        return Any.Boolean().False();
    }

    private static AnyInt32 AnyValidRepeatEndTimes() {
        // No end repeat by default, for the same reason as the start repeat.
        return Any.Int32().Zero();
    }

    private AnyMeasure(IAny<int>                  number,
                       IAny<TimeSignature>        timeSignature,
                       IAny<Dynamic>              dynamic,
                       Func<TimeSignature, IAny<IReadOnlyList<Voice>>> voices,
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
        return new AnyMeasure(_number, _timeSignature, _dynamic, _ => new FixedValue<IReadOnlyList<Voice>>(value), _repeatStart, _repeatEndTimes);
    }

    /// <summary>Draws <c>voices</c> from <c>AnyValidVoices()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasure WithVoices(Func<AnyList<Voice>, AnyList<Voice>> configure) {
        return new AnyMeasure(_number, _timeSignature, _dynamic, meter => configure(AnyValidVoices(meter)), _repeatStart, _repeatEndTimes);
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
        // The voices depend on the meter: draw it first, then fill it.
        TimeSignature meter = _timeSignature.Generate();

        return new Measure(_number.Generate(),
                           meter,
                           _dynamic.Generate(),
                           _voices(meter).Generate(),
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
