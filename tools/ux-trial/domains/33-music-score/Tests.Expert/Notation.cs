using MusicScore.Domain;

namespace MusicScore.Tests.Expert;

/// <summary>The written values a test is about: they stay literals, spelled the way a musician reads them.</summary>
internal static class Notation {

    public static readonly TimeSignature CommonTime = new(4, 4);
    public static readonly TimeSignature ThreeFour  = new(3, 4);
    public static readonly TimeSignature TwoFour    = new(2, 4);

    public static Duration Whole         => new(1, 1);
    public static Duration HalfNote          => new(1, 2);
    public static Duration DottedHalf    => new(3, 4);
    public static Duration Quarter       => new(1, 4);
    public static Duration DottedQuarter => new(3, 8);
    public static Duration Eighth        => new(1, 8);

    public static VoiceEntry Note(Duration duration, Pitch pitch) {
        return new VoiceEntry(duration, pitch, tiedToNext: false);
    }

    public static VoiceEntry TiedNote(Duration duration, Pitch pitch) {
        return new VoiceEntry(duration, pitch, tiedToNext: true);
    }

    public static VoiceEntry Rest(Duration duration) {
        return VoiceEntry.Rest(duration);
    }

}
