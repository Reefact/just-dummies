namespace MusicScore.Domain;

/// <summary>A sounding note to perform: where it starts and how long it sounds, exactly and in seconds.</summary>
public sealed class PerformedEvent
{
    public PerformedEvent(string staffName, int voiceNumber, Pitch pitch, int velocity, Duration onset, Duration length, double startSeconds, double durationSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(staffName);
        if (voiceNumber is < 1 or > Voice.MaxVoices)
        {
            throw new ArgumentOutOfRangeException(nameof(voiceNumber), voiceNumber, $"A voice is numbered from 1 to {Voice.MaxVoices}.");
        }

        ArgumentNullException.ThrowIfNull(pitch);
        if (velocity is < 1 or > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(velocity), velocity, "A velocity lies between 1 and 127.");
        }

        ArgumentNullException.ThrowIfNull(onset);
        ArgumentNullException.ThrowIfNull(length);
        if (length.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "A performed note sounds for a positive duration.");
        }

        if (!double.IsFinite(startSeconds) || startSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startSeconds), startSeconds, "The start time is a finite, non-negative number of seconds.");
        }

        if (!double.IsFinite(durationSeconds) || durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "The duration is a finite, positive number of seconds.");
        }

        StaffName = staffName;
        VoiceNumber = voiceNumber;
        Pitch = pitch;
        Velocity = velocity;
        Onset = onset;
        Length = length;
        StartSeconds = startSeconds;
        DurationSeconds = durationSeconds;
    }

    public string StaffName { get; }

    public int VoiceNumber { get; }

    public Pitch Pitch { get; }

    public int Velocity { get; }

    public Duration Onset { get; }

    public Duration Length { get; }

    public double StartSeconds { get; }

    public double DurationSeconds { get; }

    public Duration End => Onset + Length;
}
