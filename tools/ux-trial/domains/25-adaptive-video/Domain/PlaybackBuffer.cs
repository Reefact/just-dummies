namespace AdaptiveVideo.Domain;

/// <summary>Seconds of media downloaded but not yet played.</summary>
public sealed class PlaybackBuffer
{
    public PlaybackBuffer(double levelSeconds, double capacitySeconds)
    {
        if (double.IsNaN(capacitySeconds) || capacitySeconds < 4 || capacitySeconds > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(capacitySeconds), capacitySeconds, "Buffer capacity must be between 4 and 300 seconds.");
        }

        if (double.IsNaN(levelSeconds) || levelSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(levelSeconds), levelSeconds, "Buffer level cannot be negative.");
        }

        if (levelSeconds > capacitySeconds)
        {
            throw new ArgumentException($"Buffer level {levelSeconds}s exceeds its capacity {capacitySeconds}s.", nameof(levelSeconds));
        }

        LevelSeconds = levelSeconds;
        CapacitySeconds = capacitySeconds;
    }

    public double LevelSeconds { get; private set; }

    public double CapacitySeconds { get; }

    public bool IsEmpty => LevelSeconds == 0;

    public double FreeSeconds => CapacitySeconds - LevelSeconds;

    /// <summary>Plays <paramref name="seconds"/> of wall-clock time; returns the stall time when the buffer runs dry.</summary>
    public double Drain(double seconds)
    {
        if (double.IsNaN(seconds) || seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Elapsed time cannot be negative.");
        }

        double stall = Math.Max(0, seconds - LevelSeconds);
        LevelSeconds = Math.Max(0, LevelSeconds - seconds);
        return stall;
    }

    /// <summary>Appends a downloaded segment; refuses when it does not fit.</summary>
    public void Append(Segment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        if (segment.DurationSeconds > FreeSeconds)
        {
            throw new InvalidOperationException(
                $"Segment {segment.Index} ({segment.DurationSeconds}s) does not fit: only {FreeSeconds}s free.");
        }

        LevelSeconds += segment.DurationSeconds;
    }
}
