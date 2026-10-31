namespace RailwayInterlocking.Domain;

/// <summary>Why the interlocking refused a request.</summary>
public enum RefusalReason
{
    UnknownRoute,
    SectionOccupied,
    SectionLocked,
    PointLocked,
    ConflictingRoute,
    TrainAlreadyHasRoute,
    RouteNotSet,
    ApproachLocked,
    OutOfSequenceRelease,
}

/// <summary>Raised when a request would break a safety invariant. The interlocking state is left unchanged.</summary>
public sealed class InterlockingRefusedException : InvalidOperationException
{
    public InterlockingRefusedException(RefusalReason reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    public RefusalReason Reason { get; }
}
