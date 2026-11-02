namespace ShiftPlanning.Domain;

/// <summary>
/// How a local wall-clock time is turned into an instant when it occurs twice (the fall-back overlap).
/// <see cref="Earlier"/> keeps the offset in force before the transition (the first occurrence, the earlier instant);
/// <see cref="Later"/> keeps the offset in force after it (the second occurrence). Ignored for a time that occurs once.
/// </summary>
public enum OffsetChoice
{
    Unspecified,
    Earlier,
    Later,
}

/// <summary>What a local wall-clock time is in a given zone on a given date.</summary>
public enum LocalTimeKind
{
    /// <summary>The time occurs exactly once.</summary>
    Unique,

    /// <summary>The time falls in the spring-forward gap and never occurs.</summary>
    Skipped,

    /// <summary>The time falls in the fall-back overlap and occurs twice.</summary>
    Ambiguous,
}

public enum RosterStatus
{
    Draft,
    Published,
}

/// <summary>Why the roster refused an operation.</summary>
public enum RefusalReason
{
    None,
    RosterPublished,
    UnknownSite,
    DuplicateSite,
    UnknownEmployee,
    DuplicateEmployee,
    UnknownShift,
    OutsidePlanningPeriod,
    StartTimeDoesNotExist,
    StartTimeAmbiguous,
    EndTimeDoesNotExist,
    EndTimeAmbiguous,
    EndNotAfterStart,
    ShiftAlreadyAssigned,
    ShiftNotAssigned,
    OverlapsAnotherShift,
    InsufficientRest,
    WeeklyLimitExceeded,
    OutsideWorkingHours,
    UnstaffedShift,
    NothingToPublish,
}
