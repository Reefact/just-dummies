namespace AdaptiveVideo.Domain;

/// <summary>A fixed-duration slice of the content timeline, fetched in one request.</summary>
public sealed class Segment
{
    public const double MinDurationSeconds = 0.5;
    public const double MaxDurationSeconds = 10.0;

    public Segment(int index, double durationSeconds)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "A segment index cannot be negative.");
        }

        if (double.IsNaN(durationSeconds) || durationSeconds < MinDurationSeconds || durationSeconds > MaxDurationSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds,
                $"A segment lasts between {MinDurationSeconds} and {MaxDurationSeconds} seconds.");
        }

        Index = index;
        DurationSeconds = durationSeconds;
    }

    public int Index { get; }

    public double DurationSeconds { get; }

    /// <summary>Size in kilobits of this segment encoded in <paramref name="variant"/>.</summary>
    public double SizeKilobits(EncodingVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        return variant.Bitrate.Kbps * DurationSeconds;
    }

    public Segment Next() => new(Index + 1, DurationSeconds);
}
