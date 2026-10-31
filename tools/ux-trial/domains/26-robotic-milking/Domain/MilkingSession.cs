namespace RoboticMilking.Domain;

/// <summary>
/// One visit of a cow to a milking box, from identification to release. Teat preparation runs in strict order,
/// one teat at a time, and must cover every milkable quarter before any cup is attached; once attached, quarters
/// are milked in parallel and finish independently. A critical anomaly forces the safe state at once.
/// </summary>
public sealed class MilkingSession
{
    public const int BoxCount = 8;

    private readonly Dictionary<UdderQuarter, QuarterMilking> _quarters;
    private readonly List<MilkingAnomaly> _anomalies = new();
    private TimeSpan _lastPreparationEnd = TimeSpan.Zero;
    private bool _inSafeState;

    /// <param name="animal">The cow identified at the box entrance.</param>
    /// <param name="boxNumber">The box, 1 to <see cref="BoxCount"/>.</param>
    /// <param name="startedAt">When the ear tag was read.</param>
    /// <param name="settings">The robot's milking settings.</param>
    /// <exception cref="MilkingRefusedException">The cow was milked less than the minimum interval ago.</exception>
    public MilkingSession(Animal animal, int boxNumber, DateTimeOffset startedAt, MilkingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(animal);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(boxNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(boxNumber, BoxCount);

        if (animal.LastMilkedAt is { } last)
        {
            if (last > startedAt)
            {
                throw new ArgumentException(
                    $"Cow {animal.Id} was last milked at {last:O}, after this session starts at {startedAt:O}.", nameof(startedAt));
            }

            if (startedAt - last < settings.MinimumInterval)
            {
                throw new MilkingRefusedException(
                    $"Cow {animal.Id} was milked {(startedAt - last).TotalHours:0.#} h ago; the minimum interval is "
                    + $"{settings.MinimumInterval.TotalHours:0.#} h. She is sent out without milking.");
            }
        }

        Animal = animal;
        BoxNumber = boxNumber;
        StartedAt = startedAt;
        Settings = settings;
        _quarters = animal.MilkableQuarters.ToDictionary(q => q, q => new QuarterMilking(q, settings.MaxAttachAttempts));
    }

    public Animal Animal { get; }

    public int BoxNumber { get; }

    public DateTimeOffset StartedAt { get; }

    public MilkingSettings Settings { get; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public IReadOnlyCollection<QuarterMilking> Quarters => _quarters.Values;

    public IReadOnlyList<MilkingAnomaly> Anomalies => _anomalies;

    public SessionPhase Phase
    {
        get
        {
            if (ReleasedAt is not null)
            {
                return SessionPhase.Released;
            }

            if (_inSafeState)
            {
                return SessionPhase.SafeState;
            }

            if (_quarters.Values.All(q => q.IsFinished))
            {
                return SessionPhase.Completed;
            }

            if (_quarters.Values.Any(q => q.State == QuarterState.Milking))
            {
                return SessionPhase.Milking;
            }

            if (_quarters.Values.All(q => q.State != QuarterState.Pending))
            {
                return SessionPhase.Attaching;
            }

            return _quarters.Values.Any(q => q.State != QuarterState.Pending) ? SessionPhase.Preparing : SessionPhase.Identified;
        }
    }

    public decimal TotalYieldKg => _quarters.Values.Sum(q => q.YieldKg);

    /// <summary>The milk that went to the tank: every quarter not diverted.</summary>
    public decimal SaleableYieldKg => _quarters.Values.Where(q => !q.IsDiverted).Sum(q => q.YieldKg);

    /// <summary>An incomplete milking leaves at least one milkable quarter unmilked to the end; the cow is called back sooner.</summary>
    public bool IsIncomplete => _quarters.Values.Any(q => q.State is QuarterState.AttachmentFailed or QuarterState.Aborted);

    public QuarterMilking QuarterOf(UdderQuarter quarter) =>
        _quarters.TryGetValue(quarter, out QuarterMilking? milking)
            ? milking
            : throw new MilkingRefusedException($"Quarter {quarter} of cow {Animal.Id} is blind; it is never milked.");

    /// <summary>Brushes one teat. Teats are prepared one at a time: a brushing cannot overlap the previous one.</summary>
    /// <param name="quarter">The teat brushed.</param>
    /// <param name="brushingTime">How long it was brushed; at least <see cref="MilkingSettings.CleaningTime"/>.</param>
    /// <param name="endedAt">When the brushing ended, as time since the session started.</param>
    public void PrepareTeat(UdderQuarter quarter, TimeSpan brushingTime, TimeSpan endedAt)
    {
        RequireActive();
        QuarterMilking milking = QuarterOf(quarter);

        if (brushingTime < Settings.CleaningTime)
        {
            throw new MilkingRefusedException(
                $"Teat {quarter} was brushed {brushingTime.TotalSeconds} s; the protocol requires {Settings.CleaningTime.TotalSeconds} s.");
        }

        if (endedAt - brushingTime < _lastPreparationEnd)
        {
            throw new MilkingRefusedException(
                $"Brushing teat {quarter} from {endedAt - brushingTime} overlaps the previous brushing that ended at {_lastPreparationEnd}.");
        }

        milking.MarkPrepared();
        _lastPreparationEnd = endedAt;
    }

    /// <summary>Records one attempt of the robot arm to attach a cup. Allowed only once every milkable teat is prepared.</summary>
    public void AttachCup(UdderQuarter quarter, bool succeeded, TimeSpan elapsed)
    {
        RequireActive();
        QuarterMilking milking = QuarterOf(quarter);

        if (_quarters.Values.Any(q => q.State == QuarterState.Pending))
        {
            throw new MilkingRefusedException("Every milkable teat is prepared before the first cup is attached.");
        }

        TimeSpan earliest = _lastPreparationEnd + Settings.LetDownDelay;
        if (elapsed < earliest)
        {
            throw new MilkingRefusedException(
                $"Attaching at {elapsed} is before milk let-down; the earliest attachment is {earliest}.");
        }

        milking.RecordAttachAttempt(succeeded, elapsed);
    }

    /// <summary>Feeds a sensor sample. A mastitis or blood signal diverts the quarter; a fallen flow detaches its cup.</summary>
    public void RecordFlow(MilkFlowReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        RequireActive();
        QuarterMilking milking = QuarterOf(reading.Quarter);

        milking.RecordFlow(reading, Settings.DetachFlowThreshold);

        if (reading.SuggestsMastitis && !milking.IsDiverted)
        {
            Apply(new MilkingAnomaly(AnomalyKind.HighConductivity, reading.Quarter, reading.Elapsed));
        }

        if (reading.BloodDetected)
        {
            Apply(new MilkingAnomaly(AnomalyKind.BloodInMilk, reading.Quarter, reading.Elapsed));
        }
    }

    /// <summary>Reports an anomaly. A critical one detaches every cup and puts the session in the safe state at once.</summary>
    public void ReportAnomaly(MilkingAnomaly anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);
        if (ReleasedAt is not null)
        {
            throw new MilkingRefusedException("The cow has left the box; the session takes no more anomalies.");
        }

        if (anomaly.Quarter is { } quarter)
        {
            QuarterOf(quarter);
        }

        Apply(anomaly);
    }

    /// <summary>Opens the exit gate. Refused while any cup may still be on.</summary>
    public void Release(DateTimeOffset at)
    {
        SessionPhase phase = Phase;
        if (phase is not (SessionPhase.Completed or SessionPhase.SafeState))
        {
            throw new MilkingRefusedException($"A cow is released only once milking is completed or the box is safe; the phase is {phase}.");
        }

        if (at < StartedAt)
        {
            throw new ArgumentException($"Release at {at:O} precedes the session start at {StartedAt:O}.", nameof(at));
        }

        ReleasedAt = at;
    }

    private void Apply(MilkingAnomaly anomaly)
    {
        _anomalies.Add(anomaly);

        if (anomaly.IsCritical)
        {
            foreach (QuarterMilking milking in _quarters.Values)
            {
                milking.Abort();
            }

            _inSafeState = true;
            return;
        }

        QuarterMilking concerned = _quarters[anomaly.Quarter!.Value];
        if (anomaly.DivertsMilk)
        {
            concerned.Divert();
        }
        else if (anomaly.Kind == AnomalyKind.CupKickOff)
        {
            if (!concerned.IsCupOn)
            {
                throw new MilkingRefusedException($"Quarter {concerned.Quarter} has no cup on; it cannot be kicked off.");
            }

            concerned.Abort();
        }
    }

    private void RequireActive()
    {
        SessionPhase phase = Phase;
        if (phase is SessionPhase.SafeState or SessionPhase.Released)
        {
            throw new MilkingRefusedException($"The session is in phase {phase}; no milking operation is allowed.");
        }
    }
}
