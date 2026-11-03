using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace MusicScore.Domain.Tests;

/// <summary>Short spellings for the literals the tests are about: pitches, notes, rests, voices, measures, scores.</summary>
internal static class Notation {

    /// <summary>"C4", "F#3", "Bb2", "G##9": step, accidental, octave.</summary>
    public static Pitch P(string spelling) {
        char step   = spelling[0];
        int  octave = spelling[^1] - '0';
        string accidental = spelling.Substring(1, spelling.Length - 2);
        int alter = accidental switch {
            "bb" => -2,
            "b"  => -1,
            ""   => 0,
            "#"  => 1,
            "##" => 2,
            _    => throw new System.ArgumentException($"Unknown accidental '{accidental}'.")
        };

        return new Pitch(step, alter, octave);
    }

    public static Duration D(long numerator, long denominator) { return new Duration(numerator, denominator); }

    public static VoiceEntry Note(string pitch, long numerator, long denominator) {
        return new VoiceEntry(D(numerator, denominator), P(pitch), tiedToNext: false);
    }

    public static VoiceEntry Tied(string pitch, long numerator, long denominator) {
        return new VoiceEntry(D(numerator, denominator), P(pitch), tiedToNext: true);
    }

    public static VoiceEntry Rest(long numerator, long denominator) {
        return VoiceEntry.Rest(D(numerator, denominator));
    }

    public static Voice V(int number, params VoiceEntry[] entries) { return new Voice(number, entries); }

    public static TimeSignature Meter(string meter) {
        string[] parts = meter.Split('/');

        return new TimeSignature(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    /// <summary>A measure; its dynamic is a dummy unless the test is about velocities.</summary>
    public static Measure Bar(int number, string meter, IReadOnlyList<Voice> voices, Dynamic? dynamic = null, bool repeatStart = false, int repeatEndTimes = 0) {
        return new Measure(number, Meter(meter), dynamic ?? Any.Enum<Dynamic>().Generate(), voices, repeatStart, repeatEndTimes);
    }

    /// <summary>A measure of 4/4 holding a single voice made of the given entries.</summary>
    public static Measure Bar(int number, params VoiceEntry[] entries) {
        return Bar(number, "4/4", new[] { V(1, entries) });
    }

    /// <summary>A staff whose name is a dummy.</summary>
    public static Staff StaffOf(params Measure[] measures) {
        return Arbitrary.Staff().WithMeasures(measures).Generate();
    }

    public static Staff StaffOf(string name, params Measure[] measures) {
        return Arbitrary.Staff().WithName(name).WithMeasures(measures).Generate();
    }

    /// <summary>A score whose title and tempo are dummies.</summary>
    public static Score ScoreOf(params Staff[] staves) {
        return Arbitrary.Score().WithStaves(staves).Generate();
    }

    /// <summary>A score at "<paramref name="beatsPerMinute"/> beats of <paramref name="beatUnit"/> per minute".</summary>
    public static Score ScoreAt(double beatsPerMinute, Duration beatUnit, params Staff[] staves) {
        return Arbitrary.Score().WithTempo(new Tempo(beatsPerMinute, beatUnit)).WithStaves(staves).Generate();
    }

    public static string Spell(IEnumerable<PerformedEvent> events) {
        return string.Join(" ", events.Select(performed => $"{performed.Pitch}@{performed.Onset}+{performed.Length}"));
    }

}
