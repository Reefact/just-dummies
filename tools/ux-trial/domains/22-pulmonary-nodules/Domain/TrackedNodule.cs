namespace PulmonaryNodules.Domain;

/// <summary>Where a tracked nodule stands in its follow-up.</summary>
public enum NoduleStatus
{
    UnderSurveillance,
    Resolved,
}

/// <summary>
/// The aggregate: one pulmonary nodule followed across successive CT exams of the same patient.
/// Each new observation must be recognised as the same nodule once both exams are registered.
/// </summary>
public sealed class TrackedNodule
{
    public const double MinMatchToleranceMm = 1.0;
    public const double MaxMatchToleranceMm = 20.0;

    private readonly List<NoduleObservation> _observations;

    public TrackedNodule(Guid id, string patientId, NoduleObservation baseline, double matchToleranceMm)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A nodule identifier cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(patientId);
        if (patientId.Length > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(patientId), patientId.Length, "A patient identifier holds at most 64 characters.");
        }

        ArgumentNullException.ThrowIfNull(baseline);
        if (double.IsNaN(matchToleranceMm) || matchToleranceMm < MinMatchToleranceMm || matchToleranceMm > MaxMatchToleranceMm)
        {
            throw new ArgumentOutOfRangeException(nameof(matchToleranceMm), matchToleranceMm, $"A match tolerance must lie within [{MinMatchToleranceMm}, {MaxMatchToleranceMm}] mm.");
        }

        Id = id;
        PatientId = patientId;
        MatchToleranceMm = matchToleranceMm;
        _observations = [baseline];
        Status = NoduleStatus.UnderSurveillance;
    }

    public Guid Id { get; }

    public string PatientId { get; }

    public double MatchToleranceMm { get; }

    public NoduleStatus Status { get; private set; }

    public DateOnly? ResolvedOn { get; private set; }

    public IReadOnlyList<NoduleObservation> Observations => _observations;

    public NoduleObservation Baseline => _observations[0];

    public NoduleObservation Latest => _observations[^1];

    /// <summary>
    /// Registers the latest exam onto the follow-up, maps the last known centroid and accepts the
    /// observation when the follow-up centroid lies within the match tolerance plus the registration residual.
    /// Returns the distance between the predicted and the observed centroid.
    /// </summary>
    public double Track(NoduleObservation followUp)
    {
        ArgumentNullException.ThrowIfNull(followUp);
        EnsureUnderSurveillance();
        EnsureLaterThanLatest(followUp.Exam);

        TranslationTransform transform = Latest.Exam.RegisterTo(followUp.Exam);
        SpatialPoint predicted = transform.Apply(Latest.Segmentation.Centroid);
        double distance = predicted.DistanceTo(followUp.Segmentation.Centroid);
        double allowed = MatchToleranceMm + transform.ResidualErrorMm;
        if (distance > allowed)
        {
            throw new NoduleMismatchException(
                $"The follow-up segmentation lies {distance:0.0} mm from the predicted position (allowed {allowed:0.0} mm): it is not nodule {Id}.");
        }

        _observations.Add(followUp);
        return distance;
    }

    /// <summary>Records that the nodule is no longer visible on <paramref name="exam"/>; no later observation is accepted.</summary>
    public void MarkResolved(CtExam exam)
    {
        ArgumentNullException.ThrowIfNull(exam);
        EnsureUnderSurveillance();
        EnsureLaterThanLatest(exam);
        Status = NoduleStatus.Resolved;
        ResolvedOn = exam.AcquiredOn;
    }

    /// <summary>The volume change between the two most recent observations.</summary>
    public VolumeChange CompareLatest()
    {
        if (_observations.Count < 2)
        {
            throw new InvalidOperationException("A comparison needs at least two observations.");
        }

        return Compare(_observations[^2], Latest);
    }

    /// <summary>The volume change between the baseline and the most recent observation.</summary>
    public VolumeChange CompareWithBaseline()
    {
        if (_observations.Count < 2)
        {
            throw new InvalidOperationException("A comparison needs at least two observations.");
        }

        return Compare(Baseline, Latest);
    }

    private static VolumeChange Compare(NoduleObservation before, NoduleObservation after) =>
        new(before.Volume, after.Volume, after.Exam.AcquiredOn.DayNumber - before.Exam.AcquiredOn.DayNumber);

    private void EnsureUnderSurveillance()
    {
        if (Status != NoduleStatus.UnderSurveillance)
        {
            throw new InvalidOperationException($"Nodule {Id} was resolved on {ResolvedOn}; it no longer accepts exams.");
        }
    }

    private void EnsureLaterThanLatest(CtExam exam)
    {
        if (exam.AccessionNumber == Latest.Exam.AccessionNumber)
        {
            throw new InvalidOperationException($"Exam {exam.AccessionNumber} is already the latest exam of nodule {Id}.");
        }

        if (exam.AcquiredOn <= Latest.Exam.AcquiredOn)
        {
            throw new InvalidOperationException(
                $"Exam {exam.AccessionNumber} ({exam.AcquiredOn}) is not later than the latest exam ({Latest.Exam.AcquiredOn}).");
        }
    }
}
