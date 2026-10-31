// Written by hand, never touched by `any generate Measure --force`.

using MusicScore.Domain;

namespace MusicScore.Tests.Expert.Arbitrary;

public sealed partial class AnyMeasure {

    /// <summary>Starts a repeated section at this measure.</summary>
    public AnyMeasure OpeningRepeat() {
        return WithRepeatStart(true);
    }

    /// <summary>Ends a repeated section at this measure, played <paramref name="times" /> times in all.</summary>
    public AnyMeasure ClosingRepeat(int times) {
        return WithRepeatEndTimes(times);
    }

    /// <summary>A single voice holding exactly <paramref name="entries" />, in a meter they fill.</summary>
    public AnyMeasure WithSingleVoice(TimeSignature meter, params VoiceEntry[] entries) {
        return WithTimeSignature(meter).WithVoices([new Voice(1, entries)]);
    }

}

/// <summary>Voices are entities numbered within their measure: two voices are the same voice when their numbers are.</summary>
internal sealed class VoiceNumberComparer : IEqualityComparer<Voice> {

    public static readonly VoiceNumberComparer Instance = new();

    public bool Equals(Voice? x, Voice? y) {
        return x?.Number == y?.Number;
    }

    public int GetHashCode(Voice obj) {
        return obj.Number;
    }

}
