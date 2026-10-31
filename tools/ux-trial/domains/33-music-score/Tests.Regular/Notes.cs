using JustDummies;

using MusicScore.Domain;

namespace MusicScore.Tests.Regular;

/// <summary>Shorthands for the entries a test writes out by hand: their durations matter, their pitches do not.</summary>
internal static class Notes {

    public static readonly Duration Whole   = new(1, 1);
    public static readonly Duration HalfNote    = new(1, 2);
    public static readonly Duration Quarter = new(1, 4);
    public static readonly Duration Eighth  = new(1, 8);

    public static readonly TimeSignature FourFour  = new(4, 4);
    public static readonly TimeSignature ThreeFour = new(3, 4);

    public static VoiceEntry AnyNote(Duration duration) {
        return Arbitrary.VoiceEntry().WithDuration(duration).WithTiedToNext(false).Generate();
    }

    public static VoiceEntry Note(Duration duration, Pitch pitch, bool tiedToNext = false) {
        return new VoiceEntry(duration, pitch, tiedToNext);
    }

    public static Staff AnyStaffOf(params AnyMeasure[] measures) {
        return Arbitrary.Staff().WithMeasures(measures.Select(measure => measure.Generate()).ToArray()).Generate();
    }

    public static Score AnyScoreOf(params Staff[] staves) {
        return Arbitrary.Score().WithStaves(staves).Generate();
    }

    /// <summary>A measure in 4/4 without repeats, whose content the test does not care about.</summary>
    public static AnyMeasure AnyBar(int number) {
        return Arbitrary.Measure().WithNumber(number).WithTimeSignature(FourFour).WithoutRepeats();
    }

}
