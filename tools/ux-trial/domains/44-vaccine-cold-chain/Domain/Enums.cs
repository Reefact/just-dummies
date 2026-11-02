namespace ColdChain.Domain;

/// <summary>Where a consignment stands between the shipper's dock and the receiving warehouse.</summary>
public enum ConsignmentStatus
{
    Planned,
    InTransit,
    Delivered,
    Closed,
}

/// <summary>Which side of the labelled range a temperature excursion went.</summary>
public enum ExcursionKind
{
    Cold,
    Warm,
}

/// <summary>What the receiving warehouse's quality unit does with a pallet.</summary>
public enum ReleaseOutcome
{
    Released,
    Quarantined,
    Rejected,
}

/// <summary>Why a pallet is not simply released. The first three reject it; the others hold it for review.</summary>
public enum DecisionReason
{
    HardLimitBreached,
    StabilityBudgetExhausted,
    PackExpired,
    ShortShelfLife,
    SingleExcursionTooLong,
    ArrivedOutOfRange,
    IncompleteTemperatureRecord,
    LoggerCalibrationLapsed,
}

/// <summary>Why the consignment refused an operation.</summary>
public enum ColdChainRefusal
{
    NotPlanned,
    NotDispatched,
    NotInTransit,
    NotDelivered,
    ShipmentNotBooked,
    LoggerCalibrationExpired,
    PackExpiredBeforeDispatch,
    UnknownPallet,
    LoggerMismatch,
    ReadingOutsideTransit,
    ReadingOutOfOrder,
    PalletAlreadyDecided,
    NoFurtherLeg,
    BeforeCurrentCustody,
    ItineraryIncomplete,
    WrongDestination,
    ColdRoomOutOfSpecification,
}
