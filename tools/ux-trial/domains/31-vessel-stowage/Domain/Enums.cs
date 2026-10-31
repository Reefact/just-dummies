namespace VesselStowage.Domain;

public enum ContainerSize
{
    TwentyFoot,
    FortyFoot,
}

/// <summary>IMDG dangerous-goods class; <see cref="None"/> for general cargo.</summary>
public enum HazardClass
{
    None = 0,
    Explosives = 1,
    Gases = 2,
    FlammableLiquids = 3,
    FlammableSolids = 4,
    Oxidizers = 5,
    Toxic = 6,
    Radioactive = 7,
    Corrosives = 8,
    Miscellaneous = 9,
}

public enum StowageViolation
{
    None,
    SlotOutsideVessel,
    BayDoesNotFitSize,
    SlotOccupied,
    AlreadyStowed,
    PortNotInRotation,
    NoSupportBelow,
    HeavierThanContainerBelow,
    StackWeightExceeded,
    Overstow,
    HatchAccessBlocked,
    ExplosivesUnderDeck,
    SegregationConflict,
    DisplacementExceeded,
    VerticalStabilityExceeded,
    ListExceeded,
    ContainerStackedAbove,
    NotStowed,
}
