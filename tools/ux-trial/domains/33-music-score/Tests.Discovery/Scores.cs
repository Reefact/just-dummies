using JustDummies;

using MusicScore.Domain;

namespace MusicScore.Tests.Discovery;

/// <summary>Small helpers that spell hand-built scores, where the test is about the structure written.</summary>
internal static class Scores {

    public static readonly TimeSignature FourFour = new TimeSignature(4, 4);

    public static Duration Quarter { get; } = new Duration(1, 4);

    public static VoiceEntry Note(Pitch pitch, Duration duration, bool tiedToNext = false) {
        return new VoiceEntry(duration, pitch, tiedToNext);
    }

    public static VoiceEntry Rest(Duration duration) {
        return VoiceEntry.Rest(duration);
    }

    /// <summary>Four quarter notes of one pitch: fills a 4/4 measure.</summary>
    public static Voice FourQuarters(int number, Pitch pitch) {
        return new Voice(number, [Note(pitch, Quarter), Note(pitch, Quarter), Note(pitch, Quarter), Note(pitch, Quarter)]);
    }

    /// <summary>A whole-note rest: fills a 4/4 measure with silence.</summary>
    public static Voice WholeRest(int number) {
        return new Voice(number, [Rest(new Duration(1, 1))]);
    }

    /// <summary>A measure whose dynamic is of no interest to the test.</summary>
    public static Measure Bar(int number, TimeSignature timeSignature, params Voice[] voices) {
        return new Measure(number, timeSignature, Any.Enum<Dynamic>().Generate(), voices, repeatStart: false, repeatEndTimes: 0);
    }

    public static Measure Bar(int number, params Voice[] voices) {
        return Bar(number, FourFour, voices);
    }

    public static Measure BarWithDynamic(int number, Dynamic dynamic, params Voice[] voices) {
        return new Measure(number, FourFour, dynamic, voices, repeatStart: false, repeatEndTimes: 0);
    }

    /// <summary>A measure whose content does not matter, carrying the repeat barlines the test is about.</summary>
    public static Measure RepeatBar(int number, bool repeatStart = false, int repeatEndTimes = 0) {
        return Any.Measure().WithNumber(number).WithTimeSignature(FourFour).WithRepeatStart(repeatStart).WithRepeatEndTimes(repeatEndTimes).Generate();
    }

    public static Staff StaffOf(string name, params Measure[] measures) {
        return new Staff(name, measures);
    }

    public static Tempo QuarterEquals(double beatsPerMinute) {
        return new Tempo(beatsPerMinute, Quarter);
    }

    public static Score ScoreOf(Tempo tempo, params Staff[] staves) {
        return new Score(Any.String().NotBlank().Generate(), tempo, staves);
    }

}
