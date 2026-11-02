namespace ClinicalTrial.Domain;

/// <summary>
/// What the site enters for one scheduled visit: the visit date (or why the visit did not take place), the vital
/// signs and the laboratory results. A visit that did not take place carries no data, and a scheduled visit is
/// never "not applicable".
/// </summary>
public sealed class VisitEntry
{
    private readonly List<LabResult> _labs;

    public VisitEntry(Observed<DateOnly> visitDate, VitalSigns? vitals, IEnumerable<LabResult> labs)
    {
        ArgumentNullException.ThrowIfNull(visitDate);
        ArgumentNullException.ThrowIfNull(labs);
        List<LabResult> list = labs.ToList();
        if (list.Any(l => l is null))
        {
            throw new ArgumentException("A visit cannot list a null laboratory result.", nameof(labs));
        }

        if (list.Select(l => l.AnalyteCode).Distinct(StringComparer.Ordinal).Count() != list.Count)
        {
            throw new ArgumentException("Each analyte is reported once per visit.", nameof(labs));
        }

        if (visitDate.IsMissing(MissingReason.NotApplicable))
        {
            throw new ArgumentException("A scheduled visit is never 'not applicable': it took place or it did not, for a reason.", nameof(visitDate));
        }

        if (!visitDate.IsObserved && (vitals is not null || list.Count > 0))
        {
            throw new ArgumentException($"A visit missing as {visitDate.Reason} carries no vital signs and no laboratory results.", nameof(visitDate));
        }

        VisitDate = visitDate;
        Vitals = vitals;
        _labs = list;
    }

    public Observed<DateOnly> VisitDate { get; }

    public bool TookPlace => VisitDate.IsObserved;

    public VitalSigns? Vitals { get; }

    public IReadOnlyList<LabResult> Labs => _labs;

    public LabResult? Lab(string analyteCode)
    {
        ArgumentNullException.ThrowIfNull(analyteCode);
        return _labs.Find(l => l.AnalyteCode == analyteCode);
    }
}
