namespace RoboticMilking.Domain;

/// <summary>
/// The progress of one quarter within a session. Its transitions are driven by <see cref="MilkingSession"/>,
/// which keeps the ordering rules across quarters.
/// </summary>
public sealed class QuarterMilking
{
    private TimeSpan? _lastReadingAt;
    private decimal _lastFlow;

    /// <param name="quarter">The quarter followed.</param>
    /// <param name="maxAttachAttempts">Attempts allowed before the quarter is given up, 1 to 5.</param>
    public QuarterMilking(UdderQuarter quarter, int maxAttachAttempts)
    {
        if (!Enum.IsDefined(quarter))
        {
            throw new ArgumentOutOfRangeException(nameof(quarter), quarter, "Unknown udder quarter.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttachAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxAttachAttempts, 5);

        Quarter = quarter;
        MaxAttachAttempts = maxAttachAttempts;
        State = QuarterState.Pending;
    }

    public UdderQuarter Quarter { get; }

    public int MaxAttachAttempts { get; }

    public QuarterState State { get; private set; }

    public int AttachAttempts { get; private set; }

    public TimeSpan? AttachedAt { get; private set; }

    public decimal YieldKg { get; private set; }

    public decimal PeakFlowKgPerMinute { get; private set; }

    /// <summary>Whether this quarter's milk goes to the discard line.</summary>
    public bool IsDiverted { get; private set; }

    public bool IsCupOn => State is QuarterState.Attached or QuarterState.Milking;

    public bool IsFinished => State is QuarterState.Completed or QuarterState.AttachmentFailed or QuarterState.Aborted;

    internal void MarkPrepared()
    {
        Require(QuarterState.Pending, "prepare");
        State = QuarterState.Prepared;
    }

    internal void RecordAttachAttempt(bool succeeded, TimeSpan elapsed)
    {
        Require(QuarterState.Prepared, "attach a cup to");
        AttachAttempts++;
        if (succeeded)
        {
            State = QuarterState.Attached;
            AttachedAt = elapsed;
            _lastReadingAt = elapsed;
            _lastFlow = 0m;
        }
        else if (AttachAttempts >= MaxAttachAttempts)
        {
            State = QuarterState.AttachmentFailed;
        }
    }

    internal void RecordFlow(MilkFlowReading reading, decimal detachFlowThreshold)
    {
        if (!IsCupOn)
        {
            throw new MilkingRefusedException($"Quarter {Quarter} has no cup on (state {State}); it cannot yield a reading.");
        }

        if (reading.Elapsed <= _lastReadingAt)
        {
            throw new MilkingRefusedException(
                $"Reading at {reading.Elapsed} for quarter {Quarter} is not after the previous one at {_lastReadingAt}.");
        }

        // Trapezoidal integration of the flow between two samples.
        decimal minutes = (decimal)(reading.Elapsed - _lastReadingAt!.Value).TotalMinutes;
        YieldKg += (_lastFlow + reading.FlowKgPerMinute) / 2m * minutes;
        _lastFlow = reading.FlowKgPerMinute;
        _lastReadingAt = reading.Elapsed;
        PeakFlowKgPerMinute = Math.Max(PeakFlowKgPerMinute, reading.FlowKgPerMinute);
        State = QuarterState.Milking;

        // The cup comes off once the flow has risen above the threshold and fallen back under it.
        if (PeakFlowKgPerMinute >= detachFlowThreshold && reading.FlowKgPerMinute < detachFlowThreshold)
        {
            State = QuarterState.Completed;
        }
    }

    internal void Divert() => IsDiverted = true;

    internal void Abort()
    {
        if (!IsFinished)
        {
            State = QuarterState.Aborted;
        }
    }

    private void Require(QuarterState expected, string operation)
    {
        if (State != expected)
        {
            throw new MilkingRefusedException($"Cannot {operation} quarter {Quarter} in state {State}; it must be {expected}.");
        }
    }
}
