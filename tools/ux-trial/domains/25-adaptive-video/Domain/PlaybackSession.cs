namespace AdaptiveVideo.Domain;

/// <summary>The adaptation feedback loop for one viewer: decide, download, play, repeat.</summary>
public sealed class PlaybackSession
{
    private readonly List<AdaptationDecision> _history = [];

    public PlaybackSession(BitrateLadder ladder, PlaybackBuffer buffer, AdaptationPolicy policy, BandwidthEstimate bandwidth,
                           double segmentDurationSeconds)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(bandwidth);
        if (policy.ComfortBufferSeconds > buffer.CapacitySeconds)
        {
            throw new ArgumentException(
                $"The comfort threshold ({policy.ComfortBufferSeconds}s) cannot exceed the buffer capacity ({buffer.CapacitySeconds}s).",
                nameof(policy));
        }

        NextSegment = new Segment(0, segmentDurationSeconds);
        if (NextSegment.DurationSeconds > buffer.CapacitySeconds)
        {
            throw new ArgumentException("A segment cannot be longer than the buffer capacity.", nameof(segmentDurationSeconds));
        }

        Ladder = ladder;
        Buffer = buffer;
        Policy = policy;
        Bandwidth = bandwidth;
        CurrentVariant = ladder.Lowest;
    }

    public BitrateLadder Ladder { get; }

    public PlaybackBuffer Buffer { get; }

    public AdaptationPolicy Policy { get; }

    public BandwidthEstimate Bandwidth { get; private set; }

    public EncodingVariant CurrentVariant { get; private set; }

    public Segment NextSegment { get; private set; }

    public AdaptationDecision? PendingDecision { get; private set; }

    public int StallCount { get; private set; }

    public double TotalStallSeconds { get; private set; }

    public IReadOnlyList<AdaptationDecision> History => _history;

    /// <summary>Chooses the variant for the next segment from the buffer level and the bandwidth estimate.</summary>
    public AdaptationDecision DecideNext(Latency latency)
    {
        ArgumentNullException.ThrowIfNull(latency);
        if (PendingDecision is not null)
        {
            throw new InvalidOperationException($"Segment {PendingDecision.Segment.Index} is still being downloaded.");
        }

        if (NextSegment.DurationSeconds > Buffer.FreeSeconds)
        {
            throw new InvalidOperationException("The buffer is full: wait for playback before requesting another segment.");
        }

        double budget = Bandwidth.ThroughputKbps * Policy.SafetyFactor;
        EncodingVariant previous = CurrentVariant;
        EncodingVariant chosen;
        AdaptationReason reason;

        if (_history.Count == 0 || Bandwidth.IsWarmingUp)
        {
            chosen = Ladder.HighestWithin(budget);
            if (Ladder.IndexOf(chosen) > Ladder.IndexOf(previous) + 1)
            {
                chosen = Ladder.StepUp(previous);
            }

            reason = _history.Count == 0 ? AdaptationReason.Startup : Reason(previous, chosen);
        }
        else if (Buffer.LevelSeconds < Policy.PanicBufferSeconds)
        {
            chosen = Ladder.Lowest;
            reason = AdaptationReason.Panic;
        }
        else
        {
            EncodingVariant sustainable = Ladder.HighestWithin(budget);
            int target = Ladder.IndexOf(sustainable);
            int current = Ladder.IndexOf(previous);
            if (target < current)
            {
                chosen = sustainable;
            }
            else if (target > current && Buffer.LevelSeconds >= Policy.ComfortBufferSeconds)
            {
                chosen = Ladder.StepUp(previous);
            }
            else
            {
                chosen = previous;
            }

            reason = Reason(previous, chosen);
        }

        double predicted = latency.Seconds + chosen.Bitrate.DownloadSeconds(NextSegment.DurationSeconds, Bandwidth.ThroughputKbps);
        AdaptationDecision decision = new(NextSegment, previous, chosen, reason, predicted);
        PendingDecision = decision;
        _history.Add(decision);
        return decision;
    }

    /// <summary>
    /// Completes the pending download: playback drains the buffer for the elapsed time (stalling if it runs dry),
    /// the segment is appended, and the bandwidth estimate learns from the observed throughput.
    /// </summary>
    public double CompleteDownload(double elapsedSeconds)
    {
        if (PendingDecision is null)
        {
            throw new InvalidOperationException("No segment download is pending.");
        }

        if (double.IsNaN(elapsedSeconds) || elapsedSeconds <= 0 || elapsedSeconds > 600)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), elapsedSeconds, "A download takes between 0 and 600 seconds.");
        }

        AdaptationDecision decision = PendingDecision;
        double stall = Buffer.Drain(elapsedSeconds);
        if (stall > 0)
        {
            StallCount++;
            TotalStallSeconds += stall;
        }

        Buffer.Append(decision.Segment);
        Bandwidth = Bandwidth.WithSample(decision.Segment.SizeKilobits(decision.Chosen), elapsedSeconds);
        CurrentVariant = decision.Chosen;
        NextSegment = decision.Segment.Next();
        PendingDecision = null;
        return stall;
    }

    /// <summary>Plays media while no download is in flight (for example when the buffer is full).</summary>
    public double Idle(double seconds)
    {
        if (PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot idle while a download is pending.");
        }

        double stall = Buffer.Drain(seconds);
        if (stall > 0)
        {
            StallCount++;
            TotalStallSeconds += stall;
        }

        return stall;
    }

    private static AdaptationReason Reason(EncodingVariant previous, EncodingVariant chosen)
    {
        int direction = chosen.Bitrate.CompareTo(previous.Bitrate);
        return direction > 0 ? AdaptationReason.SwitchUp : direction < 0 ? AdaptationReason.SwitchDown : AdaptationReason.Hold;
    }
}
