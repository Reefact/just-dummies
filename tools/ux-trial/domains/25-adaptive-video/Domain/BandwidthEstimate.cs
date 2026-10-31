namespace AdaptiveVideo.Domain;

/// <summary>A smoothed throughput estimate built from download samples.</summary>
public sealed class BandwidthEstimate
{
    public BandwidthEstimate(double throughputKbps, int sampleCount, double smoothingFactor)
    {
        if (double.IsNaN(throughputKbps) || throughputKbps <= 0 || throughputKbps > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(throughputKbps), throughputKbps, "Throughput must be in (0, 1 000 000] kbps.");
        }

        if (sampleCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount), sampleCount, "An estimate rests on at least one sample.");
        }

        if (double.IsNaN(smoothingFactor) || smoothingFactor <= 0 || smoothingFactor > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(smoothingFactor), smoothingFactor, "The smoothing factor must be in (0, 1].");
        }

        ThroughputKbps = throughputKbps;
        SampleCount = sampleCount;
        SmoothingFactor = smoothingFactor;
    }

    public double ThroughputKbps { get; }

    public int SampleCount { get; }

    /// <summary>Weight of a new sample in the exponentially weighted moving average.</summary>
    public double SmoothingFactor { get; }

    /// <summary>Fewer than three samples: the estimate is not yet trusted.</summary>
    public bool IsWarmingUp => SampleCount < 3;

    /// <summary>Folds the throughput observed while downloading <paramref name="kilobits"/> in <paramref name="seconds"/>.</summary>
    public BandwidthEstimate WithSample(double kilobits, double seconds)
    {
        if (kilobits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kilobits), kilobits, "A sample must carry data.");
        }

        if (seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A sample must take time.");
        }

        double observed = Math.Min(kilobits / seconds, 1_000_000);
        double smoothed = (SmoothingFactor * observed) + ((1 - SmoothingFactor) * ThroughputKbps);
        return new BandwidthEstimate(smoothed, SampleCount + 1, SmoothingFactor);
    }
}
