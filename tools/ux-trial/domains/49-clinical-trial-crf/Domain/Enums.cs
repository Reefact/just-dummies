namespace ClinicalTrial.Domain;

/// <summary>Why a field carries no value (CDISC-style "reason not done").</summary>
public enum MissingReason
{
    NotDone,
    NotApplicable,
    Unknown,
    LostToFollowUp,
    Refused,
}

public enum TreatmentArm
{
    Placebo,
    LowDose,
    HighDose,
}

/// <summary>The lifecycle of a case report form.</summary>
public enum FormStatus
{
    Draft,
    Submitted,
    Queried,
    Signed,
    Locked,
}

public enum QueryStatus
{
    Open,
    Answered,
    Closed,
}

/// <summary>The investigator's assessment of the relationship between an adverse event and the study drug.</summary>
public enum Causality
{
    NotRelated,
    Unlikely,
    Possible,
    Probable,
    Definite,
}

public enum AdverseEventOutcome
{
    Recovered,
    RecoveredWithSequelae,
    Recovering,
    NotRecovered,
    Fatal,
    Unknown,
}

public enum LabFlag
{
    NotEvaluable,
    Low,
    Normal,
    High,
}

public enum CasebookRefusal
{
    CasebookLocked,
    AlreadyRandomised,
    RandomisationBeforeConsent,
    ScreeningPeriodExceeded,
    NotRandomised,
    UnknownVisit,
    VisitNotRecorded,
    VisitBeforeRandomisation,
    VisitsOutOfOrder,
    SubjectLostToFollowUp,
    FormNotEditable,
    InvalidTransition,
    OpenQueries,
    UnclosedQueries,
    UnresolvedFields,
    UnknownQuery,
    QueryNotOpen,
    QueryNotAnswered,
    EventBeforeConsent,
    EventAfterDeath,
    UnknownAdverseEvent,
    FormsNotSigned,
}
