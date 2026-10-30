namespace Laboratory.Domain;

/// <summary>The results released together for one specimen.</summary>
public sealed class LabReport {

    private readonly List<LabResult> _results;

    public LabReport(SampleId sampleId, IReadOnlyList<LabResult> results) {
        ArgumentNullException.ThrowIfNull(sampleId);
        ArgumentNullException.ThrowIfNull(results);
        if (results.Count == 0) {
            throw new ArgumentException("A report holds at least one result.", nameof(results));
        }
        if (results.Any(result => !result.Specimen.Id.Equals(sampleId))) {
            throw new ArgumentException("Every result of a report comes from its specimen.", nameof(results));
        }
        if (results.Select(result => result.Analyte).Distinct().Count() != results.Count) {
            throw new ArgumentException("An analyte appears at most once in a report.", nameof(results));
        }

        SampleId = sampleId;
        _results = results.ToList();
    }

    public SampleId                 SampleId { get; }
    public IReadOnlyList<LabResult> Results  => _results;

    public IReadOnlyList<LabResult> Abnormal() => _results.Where(result => result.IsOutOfRange).ToList();

    /// <summary>From the earliest collection to the latest report.</summary>
    public TimeSpan TurnaroundTime() {
        DateTimeOffset collected = _results.Min(result => result.Specimen.CollectedAt);
        DateTimeOffset reported  = _results.Max(result => result.ReportedAt);

        return reported - collected;
    }

}
