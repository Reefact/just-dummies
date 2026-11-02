namespace ClinicalTrial.Domain;

/// <summary>
/// The protocol's schedule of visits after randomisation, in chronological order. Codes are unique and
/// the tolerance windows neither overlap nor touch, so a study day belongs to at most one visit.
/// </summary>
public sealed class VisitSchedule
{
    private readonly List<VisitDefinition> _visits;

    public VisitSchedule(IEnumerable<VisitDefinition> visits)
    {
        ArgumentNullException.ThrowIfNull(visits);
        List<VisitDefinition> list = visits.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("A schedule has at least one visit.", nameof(visits));
        }

        if (list.Any(v => v is null))
        {
            throw new ArgumentException("A schedule cannot contain a null visit.", nameof(visits));
        }

        if (list.Select(v => v.Code).Distinct(StringComparer.Ordinal).Count() != list.Count)
        {
            throw new ArgumentException("Visit codes are unique within a schedule.", nameof(visits));
        }

        for (int i = 1; i < list.Count; i++)
        {
            VisitDefinition previous = list[i - 1];
            VisitDefinition next = list[i];
            if (next.TargetDay <= previous.TargetDay)
            {
                throw new ArgumentException($"Visits are listed in chronological order: {next.Code} comes before {previous.Code}.", nameof(visits));
            }

            if (next.EarliestDay <= previous.LatestDay)
            {
                throw new ArgumentException(
                    $"The window of {next.Code} (from day {next.EarliestDay}) overlaps the window of {previous.Code} (to day {previous.LatestDay}).",
                    nameof(visits));
            }
        }

        _visits = list;
    }

    public IReadOnlyList<VisitDefinition> Visits => _visits;

    public VisitDefinition? Find(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return _visits.Find(v => v.Code == code);
    }

    /// <summary>Zero-based position of a visit in the schedule, or -1.</summary>
    public int IndexOf(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return _visits.FindIndex(v => v.Code == code);
    }

    /// <summary>The visit whose window contains the study day, if any.</summary>
    public VisitDefinition? VisitOn(int studyDay) => _visits.Find(v => v.Contains(studyDay));
}
