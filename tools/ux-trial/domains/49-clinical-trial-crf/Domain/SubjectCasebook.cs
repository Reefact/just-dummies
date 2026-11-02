using System.Text.RegularExpressions;

namespace ClinicalTrial.Domain;

/// <summary>
/// The casebook of one trial subject: consent, randomisation, one case report form per scheduled visit, the
/// adverse event log, and the monitors' queries. Every refusal throws <see cref="CasebookRefusedException"/>.
/// </summary>
public sealed partial class SubjectCasebook
{
    /// <summary>Longest screening period: randomisation happens at most this many days after consent.</summary>
    public const int MaxScreeningDays = 42;

    private readonly Dictionary<string, VisitForm> _forms = new(StringComparer.Ordinal);
    private readonly List<AdverseEvent> _adverseEvents = new();

    public SubjectCasebook(ScreeningNumber screeningNumber, string? initials, DateOnly consentDate, VisitSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(screeningNumber);
        ArgumentNullException.ThrowIfNull(schedule);
        if (initials is not null && !InitialsPattern().IsMatch(initials))
        {
            throw new FormatException($"'{initials}' are not initials (2 or 3 upper-case letters); leave them absent where they are not collected.");
        }

        ScreeningNumber = screeningNumber;
        Initials = initials;
        ConsentDate = consentDate;
        Schedule = schedule;
    }

    public ScreeningNumber ScreeningNumber { get; }

    public string? Initials { get; }

    public DateOnly ConsentDate { get; }

    public VisitSchedule Schedule { get; }

    public Randomisation? Randomisation { get; private set; }

    public bool IsRandomised => Randomisation is not null;

    public bool IsLocked { get; private set; }

    /// <summary>The recorded visit forms, in schedule order.</summary>
    public IReadOnlyList<VisitForm> Forms =>
        Schedule.Visits.Where(v => _forms.ContainsKey(v.Code)).Select(v => _forms[v.Code]).ToList();

    /// <summary>The adverse event log; event number n is at index n - 1.</summary>
    public IReadOnlyList<AdverseEvent> AdverseEvents => _adverseEvents;

    /// <summary>True once a visit has been recorded as missing for loss to follow-up.</summary>
    public bool IsLostToFollowUp => _forms.Values.Any(f => f.Entry.VisitDate.IsMissing(MissingReason.LostToFollowUp));

    /// <summary>The end date of the earliest fatal adverse event, if any.</summary>
    public DateOnly? DeathDate => DeathDateOf(_adverseEvents);

    public int OpenQueryCount => _forms.Values.Sum(f => f.Queries.Count(q => q.Status == QueryStatus.Open));

    public VisitForm? Form(string visitCode)
    {
        ArgumentNullException.ThrowIfNull(visitCode);
        return _forms.GetValueOrDefault(visitCode);
    }

    public void Randomise(Randomisation randomisation)
    {
        ArgumentNullException.ThrowIfNull(randomisation);
        EnsureNotLocked();
        if (Randomisation is not null)
        {
            throw Refuse(CasebookRefusal.AlreadyRandomised, $"{ScreeningNumber} was already randomised on {Randomisation.Date:yyyy-MM-dd}.");
        }

        if (randomisation.Date < ConsentDate)
        {
            throw Refuse(CasebookRefusal.RandomisationBeforeConsent, $"Randomisation ({randomisation.Date:yyyy-MM-dd}) cannot precede consent ({ConsentDate:yyyy-MM-dd}).");
        }

        if (randomisation.Date.DayNumber - ConsentDate.DayNumber > MaxScreeningDays)
        {
            throw Refuse(CasebookRefusal.ScreeningPeriodExceeded, $"Randomisation must happen within {MaxScreeningDays} days of consent.");
        }

        Randomisation = randomisation;
    }

    /// <summary>The calendar dates bounding a visit's tolerance window.</summary>
    public (DateOnly Earliest, DateOnly Latest) WindowOf(string visitCode)
    {
        Randomisation randomisation = RequireRandomisation();
        VisitDefinition definition = RequireVisit(visitCode);
        return (randomisation.DateOfStudyDay(definition.EarliestDay), randomisation.DateOfStudyDay(definition.LatestDay));
    }

    /// <summary>
    /// Records (or re-enters) the data of a scheduled visit. Allowed while the form is a draft or queried; a visit
    /// outside its window is accepted and reported by <see cref="OutOfWindowVisits"/>.
    /// </summary>
    public VisitForm RecordVisit(string visitCode, VisitEntry entry)
    {
        ArgumentNullException.ThrowIfNull(visitCode);
        ArgumentNullException.ThrowIfNull(entry);
        EnsureNotLocked();
        Randomisation randomisation = RequireRandomisation();
        VisitDefinition definition = RequireVisit(visitCode);
        VisitForm? existing = _forms.GetValueOrDefault(visitCode);
        if (existing is not null && existing.Status is not (FormStatus.Draft or FormStatus.Queried))
        {
            throw Refuse(CasebookRefusal.FormNotEditable, $"The {visitCode} form is {existing.Status} and cannot be edited.");
        }

        DateOnly? date = entry.VisitDate.Value;
        if (date < randomisation.Date)
        {
            throw Refuse(CasebookRefusal.VisitBeforeRandomisation, $"Visit {visitCode} ({date:yyyy-MM-dd}) cannot precede randomisation ({randomisation.Date:yyyy-MM-dd}).");
        }

        int index = Schedule.IndexOf(visitCode);
        bool lostHere = entry.VisitDate.IsMissing(MissingReason.LostToFollowUp);
        foreach (VisitForm other in _forms.Values.Where(f => f.Definition.Code != visitCode))
        {
            int otherIndex = Schedule.IndexOf(other.Definition.Code);
            bool otherLost = other.Entry.VisitDate.IsMissing(MissingReason.LostToFollowUp);
            if (otherIndex < index && otherLost && !lostHere)
            {
                throw Refuse(CasebookRefusal.SubjectLostToFollowUp, $"The subject was lost to follow-up at {other.Definition.Code}; {visitCode} can only be recorded as lost to follow-up.");
            }

            if (otherIndex > index && lostHere && other.Entry.TookPlace)
            {
                throw Refuse(CasebookRefusal.SubjectLostToFollowUp, $"The subject cannot be lost to follow-up at {visitCode}: {other.Definition.Code} took place later.");
            }

            if (date is DateOnly d && other.Entry.VisitDate.Value is DateOnly otherDate
                && ((otherIndex < index && otherDate >= d) || (otherIndex > index && otherDate <= d)))
            {
                throw Refuse(CasebookRefusal.VisitsOutOfOrder, $"Visit {visitCode} ({d:yyyy-MM-dd}) is out of order with {other.Definition.Code} ({otherDate:yyyy-MM-dd}).");
            }
        }

        int? studyDay = date is DateOnly visitDate ? randomisation.StudyDayOf(visitDate) : null;
        if (existing is null)
        {
            existing = new VisitForm(definition, entry, studyDay);
            _forms.Add(visitCode, existing);
        }
        else
        {
            existing.Replace(entry, studyDay);
        }

        return existing;
    }

    /// <summary>Draft → Submitted, or Queried → Submitted once every query is answered.</summary>
    public void SubmitVisit(string visitCode)
    {
        EnsureNotLocked();
        VisitForm form = RequireForm(visitCode);
        if (form.Status == FormStatus.Queried && form.HasOpenQuery)
        {
            throw Refuse(CasebookRefusal.OpenQueries, $"The {visitCode} form still has open queries.");
        }

        if (form.Status is not (FormStatus.Draft or FormStatus.Queried))
        {
            throw Refuse(CasebookRefusal.InvalidTransition, $"The {visitCode} form is {form.Status} and cannot be submitted.");
        }

        form.Status = FormStatus.Submitted;
    }

    /// <summary>A monitor queries a field of a submitted, queried or signed form; a signed form loses its signature.</summary>
    public Query RaiseQuery(string visitCode, string fieldName, string text, DateOnly raisedOn)
    {
        EnsureNotLocked();
        VisitForm form = RequireForm(visitCode);
        if (form.Status == FormStatus.Draft)
        {
            throw Refuse(CasebookRefusal.InvalidTransition, $"The {visitCode} form is a draft: there is nothing to query yet.");
        }

        Query query = form.AddQuery(fieldName, text, raisedOn);
        form.Status = FormStatus.Queried;
        return query;
    }

    public void AnswerQuery(string visitCode, int queryNumber, string response, DateOnly answeredOn)
    {
        EnsureNotLocked();
        Query query = RequireQuery(visitCode, queryNumber);
        if (query.Status != QueryStatus.Open)
        {
            throw Refuse(CasebookRefusal.QueryNotOpen, $"Query {queryNumber} on {visitCode} is {query.Status}.");
        }

        query.Answer(response, answeredOn);
    }

    public void CloseQuery(string visitCode, int queryNumber, DateOnly closedOn)
    {
        EnsureNotLocked();
        Query query = RequireQuery(visitCode, queryNumber);
        if (query.Status != QueryStatus.Answered)
        {
            throw Refuse(CasebookRefusal.QueryNotAnswered, $"Query {queryNumber} on {visitCode} is {query.Status}: only an answered query is closed.");
        }

        query.Close(closedOn);
    }

    /// <summary>The investigator signs a submitted form whose queries are all closed and whose fields are all resolved.</summary>
    public void SignVisit(string visitCode)
    {
        EnsureNotLocked();
        VisitForm form = RequireForm(visitCode);
        if (form.Status != FormStatus.Submitted)
        {
            throw Refuse(CasebookRefusal.InvalidTransition, $"The {visitCode} form is {form.Status}: only a submitted form is signed.");
        }

        if (form.HasUnclosedQuery)
        {
            throw Refuse(CasebookRefusal.UnclosedQueries, $"The {visitCode} form has queries that are not closed.");
        }

        IReadOnlyList<string> unresolved = form.UnresolvedFields();
        if (unresolved.Count > 0)
        {
            throw Refuse(CasebookRefusal.UnresolvedFields, $"The {visitCode} form has unresolved fields: {string.Join(", ", unresolved)}.");
        }

        form.Status = FormStatus.Signed;
    }

    /// <summary>Adds an event to the log and returns its number (from 1).</summary>
    public int ReportAdverseEvent(AdverseEvent adverseEvent)
    {
        ArgumentNullException.ThrowIfNull(adverseEvent);
        EnsureNotLocked();
        List<AdverseEvent> candidate = new(_adverseEvents) { adverseEvent };
        CheckAdverseEvents(adverseEvent, candidate);
        _adverseEvents.Add(adverseEvent);
        return _adverseEvents.Count;
    }

    /// <summary>Replaces an event of the log with its revised recording (end date, outcome, assessment…).</summary>
    public void ReviseAdverseEvent(int number, AdverseEvent revised)
    {
        ArgumentNullException.ThrowIfNull(revised);
        EnsureNotLocked();
        if (number < 1 || number > _adverseEvents.Count)
        {
            throw Refuse(CasebookRefusal.UnknownAdverseEvent, $"There is no adverse event number {number}.");
        }

        List<AdverseEvent> candidate = new(_adverseEvents) { [number - 1] = revised };
        CheckAdverseEvents(revised, candidate);
        _adverseEvents[number - 1] = revised;
    }

    /// <summary>Codes of the visits that took place outside their tolerance window (protocol deviations).</summary>
    public IReadOnlyList<string> OutOfWindowVisits() =>
        Forms.Where(f => f.IsOutOfWindow).Select(f => f.Definition.Code).ToList();

    /// <summary>Every field of the casebook that blocks signing or locking, as <c>CODE.path</c> or <c>AEn.Field</c>.</summary>
    public IReadOnlyList<string> UnresolvedFields()
    {
        List<string> fields = new();
        foreach (VisitForm form in Forms)
        {
            fields.AddRange(form.UnresolvedFields().Select(f => $"{form.Definition.Code}.{f}"));
        }

        for (int i = 0; i < _adverseEvents.Count; i++)
        {
            int number = i + 1;
            fields.AddRange(_adverseEvents[i].UnresolvedFields().Select(f => $"AE{number}.{f}"));
        }

        return fields;
    }

    /// <summary>Locks the casebook: every scheduled visit is recorded and signed, and every adverse event is resolved.</summary>
    public void Lock()
    {
        EnsureNotLocked();
        RequireRandomisation();
        List<string> unsigned = Schedule.Visits
            .Where(v => !_forms.TryGetValue(v.Code, out VisitForm? form) || form.Status != FormStatus.Signed)
            .Select(v => v.Code)
            .ToList();
        if (unsigned.Count > 0)
        {
            throw Refuse(CasebookRefusal.FormsNotSigned, $"Visits not recorded and signed: {string.Join(", ", unsigned)}.");
        }

        IReadOnlyList<string> unresolved = UnresolvedFields();
        if (unresolved.Count > 0)
        {
            throw Refuse(CasebookRefusal.UnresolvedFields, $"Unresolved fields: {string.Join(", ", unresolved)}.");
        }

        foreach (VisitForm form in _forms.Values)
        {
            form.Status = FormStatus.Locked;
        }

        IsLocked = true;
    }

    private void CheckAdverseEvents(AdverseEvent adverseEvent, List<AdverseEvent> candidate)
    {
        if (adverseEvent.OnsetDate < ConsentDate)
        {
            throw Refuse(CasebookRefusal.EventBeforeConsent, $"Adverse events are collected from consent ({ConsentDate:yyyy-MM-dd}); onset {adverseEvent.OnsetDate:yyyy-MM-dd} is earlier.");
        }

        if (DeathDateOf(candidate) is DateOnly death && candidate.Any(e => e.OnsetDate > death))
        {
            throw Refuse(CasebookRefusal.EventAfterDeath, $"No adverse event can start after the subject's death ({death:yyyy-MM-dd}).");
        }
    }

    private static DateOnly? DeathDateOf(IEnumerable<AdverseEvent> events) =>
        events.Where(e => e.Outcome == AdverseEventOutcome.Fatal).Select(e => e.EndDate).Min();

    private void EnsureNotLocked()
    {
        if (IsLocked)
        {
            throw Refuse(CasebookRefusal.CasebookLocked, $"The casebook of {ScreeningNumber} is locked.");
        }
    }

    private Randomisation RequireRandomisation() =>
        Randomisation ?? throw Refuse(CasebookRefusal.NotRandomised, $"{ScreeningNumber} is not randomised.");

    private VisitDefinition RequireVisit(string visitCode)
    {
        ArgumentNullException.ThrowIfNull(visitCode);
        return Schedule.Find(visitCode) ?? throw Refuse(CasebookRefusal.UnknownVisit, $"The schedule has no visit {visitCode}.");
    }

    private VisitForm RequireForm(string visitCode)
    {
        RequireVisit(visitCode);
        return _forms.GetValueOrDefault(visitCode) ?? throw Refuse(CasebookRefusal.VisitNotRecorded, $"Visit {visitCode} has not been recorded.");
    }

    private Query RequireQuery(string visitCode, int queryNumber) =>
        RequireForm(visitCode).FindQuery(queryNumber)
        ?? throw Refuse(CasebookRefusal.UnknownQuery, $"Visit {visitCode} has no query number {queryNumber}.");

    private static CasebookRefusedException Refuse(CasebookRefusal reason, string message) => new(reason, message);

    [GeneratedRegex("^[A-Z]{2,3}$")]
    private static partial Regex InitialsPattern();
}
