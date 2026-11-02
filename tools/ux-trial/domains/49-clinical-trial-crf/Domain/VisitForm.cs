namespace ClinicalTrial.Domain;

/// <summary>
/// The case report form of one scheduled visit inside a casebook: the current entry, its lifecycle status and
/// the queries raised on it. Created and changed only through <see cref="SubjectCasebook"/>.
/// </summary>
public sealed class VisitForm
{
    private readonly List<Query> _queries = new();

    internal VisitForm(VisitDefinition definition, VisitEntry entry, int? studyDay)
    {
        Definition = definition;
        Entry = entry;
        StudyDay = studyDay;
        Status = FormStatus.Draft;
    }

    public VisitDefinition Definition { get; }

    public VisitEntry Entry { get; private set; }

    /// <summary>Study day of the visit date; absent when the visit did not take place.</summary>
    public int? StudyDay { get; private set; }

    public FormStatus Status { get; internal set; }

    public IReadOnlyList<Query> Queries => _queries;

    public bool IsOutOfWindow => StudyDay is int day && !Definition.Contains(day);

    public bool HasOpenQuery => _queries.Exists(q => q.Status == QueryStatus.Open);

    public bool HasUnclosedQuery => _queries.Exists(q => q.Status != QueryStatus.Closed);

    /// <summary>
    /// Fields that block signing, as paths: <c>Vitals</c> or <c>Vitals.X</c> for a visit that took place, and
    /// <c>Lab.CODE</c> (required analyte not reported) or <c>Lab.CODE.Result</c> (result not entered).
    /// A visit that did not take place has nothing left to resolve.
    /// </summary>
    public IReadOnlyList<string> UnresolvedFields()
    {
        List<string> fields = new();
        if (!Entry.TookPlace)
        {
            return fields;
        }

        if (Entry.Vitals is null)
        {
            fields.Add("Vitals");
        }
        else
        {
            fields.AddRange(Entry.Vitals.UnresolvedFields().Select(f => "Vitals." + f));
        }

        foreach (string analyte in Definition.RequiredAnalytes)
        {
            if (Entry.Lab(analyte) is null)
            {
                fields.Add("Lab." + analyte);
            }
        }

        foreach (LabResult lab in Entry.Labs.Where(l => !l.IsResolved))
        {
            fields.Add($"Lab.{lab.AnalyteCode}.Result");
        }

        return fields;
    }

    internal void Replace(VisitEntry entry, int? studyDay)
    {
        Entry = entry;
        StudyDay = studyDay;
    }

    internal Query AddQuery(string fieldName, string text, DateOnly raisedOn)
    {
        Query query = new(_queries.Count + 1, fieldName, text, raisedOn);
        _queries.Add(query);
        return query;
    }

    internal Query? FindQuery(int number) => _queries.Find(q => q.Number == number);
}
