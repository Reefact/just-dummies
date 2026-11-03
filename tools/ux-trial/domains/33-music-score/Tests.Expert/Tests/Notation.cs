namespace MusicScore.Domain.Tests.Expert;

/// <summary>Literal-friendly spellings, so that a test reads in musical terms.</summary>
internal static class Notation {

    public static readonly TimeSignature FourFour = new(4, 4);
    public static readonly TimeSignature ThreeFour = new(3, 4);

    public static readonly Tempo QuarterEquals60 = new(60, new Duration(1, 4));

    /// <summary>"C4", "F#3", "Bb2", "G##9".</summary>
    public static Pitch P(string text) {
        char step   = text[0];
        int  alter  = text.Count(character => character == '#') - text.Count(character => character == 'b');
        int  octave = int.Parse(text[^1].ToString());

        return new Pitch(step, alter, octave);
    }

    public static Duration D(long numerator, long denominator) {
        return new Duration(numerator, denominator);
    }

    public static VoiceEntry Note(string pitch, long numerator, long denominator, bool tied = false) {
        return new VoiceEntry(D(numerator, denominator), P(pitch), tied);
    }

    public static VoiceEntry Rest(long numerator, long denominator) {
        return VoiceEntry.Rest(D(numerator, denominator));
    }

    public static Voice VoiceOf(int number, params VoiceEntry[] entries) {
        return new Voice(number, entries);
    }

    /// <summary>
    ///     A measure whose number and meter are what the test is about; its dynamic, if the test says nothing,
    ///     is arbitrary, and so are its voices when none are given (they then fill the meter).
    /// </summary>
    public static AnyMeasure Bar(int number, TimeSignature meter, params Voice[] voices) {
        AnyMeasure bar = Arbitrary.Measure().WithNumber(number).WithTimeSignature(meter);

        return voices.Length == 0 ? bar : bar.WithVoices(voices);
    }

    /// <summary>`count` numbered measures in one meter, with arbitrary voices that fill it and no repeat barline.</summary>
    public static List<Measure> Bars(int count, TimeSignature meter) {
        return Enumerable.Range(1, count).Select(number => Arbitrary.Measure().WithNumber(number).WithTimeSignature(meter).Generate()).ToList();
    }

    public static Staff StaffOf(string name, params Measure[] measures) {
        return Arbitrary.Staff().WithName(name).WithMeasures(measures).Generate();
    }

}
