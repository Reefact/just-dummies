namespace ClinicalTrial.Domain;

/// <summary>
/// An adverse event as recorded on the AE log: verbatim term, onset, end (absent while ongoing), CTCAE grade 1-5,
/// seriousness, relationship to the study drug (absent until the investigator assesses it) and outcome.
/// </summary>
public sealed class AdverseEvent : IEquatable<AdverseEvent>
{
    public const int MaxTermLength = 200;
    public const int MaxCommentLength = 500;

    public AdverseEvent(
        string term,
        DateOnly onsetDate,
        TimeOnly? onsetTime,
        DateOnly? endDate,
        int grade,
        bool isSerious,
        Causality? relationship,
        AdverseEventOutcome? outcome,
        string? comment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);
        if (term.Length > MaxTermLength)
        {
            throw new ArgumentException($"An adverse event term has at most {MaxTermLength} characters.", nameof(term));
        }

        if (grade < 1 || grade > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(grade), grade, "A CTCAE grade is between 1 and 5.");
        }

        if (endDate is DateOnly end && end < onsetDate)
        {
            throw new ArgumentException($"An adverse event cannot end ({end:yyyy-MM-dd}) before its onset ({onsetDate:yyyy-MM-dd}).", nameof(endDate));
        }

        if (relationship.HasValue && !Enum.IsDefined(relationship.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(relationship), relationship, "Unknown causality.");
        }

        if (outcome.HasValue && !Enum.IsDefined(outcome.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown outcome.");
        }

        if (grade == 5 && !isSerious)
        {
            throw new ArgumentException("A grade 5 (fatal) adverse event is always serious.", nameof(isSerious));
        }

        if (grade == 5 && outcome.HasValue && outcome != AdverseEventOutcome.Fatal)
        {
            throw new ArgumentException($"A grade 5 adverse event has a fatal outcome, not {outcome}.", nameof(outcome));
        }

        if (outcome == AdverseEventOutcome.Fatal && grade != 5)
        {
            throw new ArgumentException($"A fatal outcome is graded 5, not {grade}.", nameof(grade));
        }

        switch (outcome)
        {
            case AdverseEventOutcome.Recovered or AdverseEventOutcome.RecoveredWithSequelae or AdverseEventOutcome.Fatal when endDate is null:
                throw new ArgumentException($"An adverse event with outcome {outcome} has ended and needs its end date.", nameof(endDate));
            case AdverseEventOutcome.Recovering or AdverseEventOutcome.NotRecovered when endDate is not null:
                throw new ArgumentException($"An adverse event with outcome {outcome} is ongoing and has no end date.", nameof(endDate));
        }

        if (comment is not null && (string.IsNullOrWhiteSpace(comment) || comment.Length > MaxCommentLength))
        {
            throw new ArgumentException($"A comment is absent, or 1 to {MaxCommentLength} characters of text.", nameof(comment));
        }

        Term = term;
        OnsetDate = onsetDate;
        OnsetTime = onsetTime;
        EndDate = endDate;
        Grade = grade;
        IsSerious = isSerious;
        Relationship = relationship;
        Outcome = outcome;
        Comment = comment;
    }

    public string Term { get; }

    public DateOnly OnsetDate { get; }

    public TimeOnly? OnsetTime { get; }

    public DateOnly? EndDate { get; }

    public int Grade { get; }

    public bool IsSerious { get; }

    public Causality? Relationship { get; }

    public AdverseEventOutcome? Outcome { get; }

    public string? Comment { get; }

    public bool IsOngoing => EndDate is null;

    /// <summary>Related when the investigator judged the relationship possible, probable or definite.</summary>
    public bool IsRelated => Relationship is Causality.Possible or Causality.Probable or Causality.Definite;

    /// <summary>Duration in days, both ends included; absent while ongoing.</summary>
    public int? DurationDays => EndDate is DateOnly end ? end.DayNumber - OnsetDate.DayNumber + 1 : null;

    /// <summary>
    /// The fields that still block signing: the relationship until assessed, the outcome of a serious event,
    /// and the end date (date of death) of a grade 5 event.
    /// </summary>
    public IReadOnlyList<string> UnresolvedFields()
    {
        List<string> fields = new();
        if (Relationship is null)
        {
            fields.Add(nameof(Relationship));
        }

        if (IsSerious && Outcome is null)
        {
            fields.Add(nameof(Outcome));
        }

        if (Grade == 5 && EndDate is null)
        {
            fields.Add(nameof(EndDate));
        }

        return fields;
    }

    public bool Equals(AdverseEvent? other) =>
        other is not null
        && other.Term == Term
        && other.OnsetDate == OnsetDate
        && other.OnsetTime == OnsetTime
        && other.EndDate == EndDate
        && other.Grade == Grade
        && other.IsSerious == IsSerious
        && other.Relationship == Relationship
        && other.Outcome == Outcome
        && other.Comment == Comment;

    public override bool Equals(object? obj) => Equals(obj as AdverseEvent);

    public override int GetHashCode() => HashCode.Combine(Term, OnsetDate, EndDate, Grade, IsSerious, Relationship, Outcome);
}
