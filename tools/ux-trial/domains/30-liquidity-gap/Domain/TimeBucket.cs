namespace LiquidityGap.Domain;

/// <summary>A maturity bucket, in days from the as-of date: [StartDay, EndDay), or open-ended when EndDay is null.</summary>
public sealed class TimeBucket
{
    public TimeBucket(string label, int startDay, int? endDay)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("A bucket needs a label.", nameof(label));
        }

        if (startDay < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startDay), startDay, "A bucket cannot start before the as-of date.");
        }

        if (endDay is not null && endDay <= startDay)
        {
            throw new ArgumentException($"A bucket ends after it starts: [{startDay}, {endDay}) is empty.", nameof(endDay));
        }

        Label = label;
        StartDay = startDay;
        EndDay = endDay;
    }

    public string Label { get; }

    public int StartDay { get; }

    public int? EndDay { get; }

    public bool IsOpenEnded => EndDay is null;

    public bool Contains(int day) => day >= StartDay && (EndDay is null || day < EndDay);

    /// <summary>
    /// Checks that buckets form a ladder: non-empty, starting at day 0, contiguous, and ending with
    /// the only open-ended bucket, so every future day falls in exactly one bucket.
    /// </summary>
    public static void EnsureLadder(IReadOnlyList<TimeBucket> buckets)
    {
        ArgumentNullException.ThrowIfNull(buckets);
        if (buckets.Count == 0)
        {
            throw new ArgumentException("A bucket ladder has at least one bucket.", nameof(buckets));
        }

        if (buckets[0].StartDay != 0)
        {
            throw new ArgumentException("A bucket ladder starts at day 0.", nameof(buckets));
        }

        for (int i = 0; i < buckets.Count - 1; i++)
        {
            if (buckets[i].EndDay != buckets[i + 1].StartDay)
            {
                throw new ArgumentException($"Buckets '{buckets[i].Label}' and '{buckets[i + 1].Label}' are not contiguous.", nameof(buckets));
            }
        }

        if (!buckets[^1].IsOpenEnded)
        {
            throw new ArgumentException("The last bucket of a ladder is open-ended.", nameof(buckets));
        }
    }
}
