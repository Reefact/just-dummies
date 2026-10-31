namespace MusicScore.Domain;

/// <summary>One written note or rest in a voice. A <c>null</c> pitch is a rest.</summary>
public sealed class VoiceEntry
{
    public VoiceEntry(Duration duration, Pitch? pitch, bool tiedToNext)
    {
        ArgumentNullException.ThrowIfNull(duration);
        if (duration.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "A note or rest must last a positive duration.");
        }

        if (pitch is null && tiedToNext)
        {
            throw new ArgumentException("A rest cannot be tied.", nameof(tiedToNext));
        }

        Duration = duration;
        Pitch = pitch;
        TiedToNext = tiedToNext;
    }

    public Duration Duration { get; }

    public Pitch? Pitch { get; }

    public bool TiedToNext { get; }

    public bool IsRest => Pitch is null;

    public static VoiceEntry Rest(Duration duration) => new(duration, null, false);
}
