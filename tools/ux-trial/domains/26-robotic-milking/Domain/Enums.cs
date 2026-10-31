namespace RoboticMilking.Domain;

/// <summary>One of the four quarters of a cow's udder, each with its own teat and teat cup.</summary>
public enum UdderQuarter
{
    LeftFront,
    RightFront,
    LeftRear,
    RightRear,
}

/// <summary>The operational phase of a milking session, each mapped to a physical event in the box.</summary>
public enum SessionPhase
{
    /// <summary>The cow entered the box and her ear tag was read.</summary>
    Identified,

    /// <summary>The brushes are cleaning and stimulating the teats, one at a time.</summary>
    Preparing,

    /// <summary>Every milkable teat is prepared; the robot arm is attaching cups.</summary>
    Attaching,

    /// <summary>At least one quarter yields milk.</summary>
    Milking,

    /// <summary>Every milkable quarter is finished (completed or given up); the cups are off.</summary>
    Completed,

    /// <summary>A critical anomaly detached every cup and shut the vacuum off.</summary>
    SafeState,

    /// <summary>The exit gate opened and the cow left the box.</summary>
    Released,
}

/// <summary>The state of one quarter within a session.</summary>
public enum QuarterState
{
    Pending,
    Prepared,
    Attached,
    Milking,
    Completed,
    AttachmentFailed,
    Aborted,
}

/// <summary>What the sensors or the operator reported.</summary>
public enum AnomalyKind
{
    /// <summary>The cow kicked a cup off one quarter.</summary>
    CupKickOff,

    /// <summary>Electrical conductivity of one quarter's milk suggests mastitis; its milk is diverted.</summary>
    HighConductivity,

    /// <summary>The colour sensor saw blood in one quarter's milk; its milk is diverted.</summary>
    BloodInMilk,

    /// <summary>The cow is restless: the activity sensor crossed its alarm level.</summary>
    AnimalAgitation,

    /// <summary>The vacuum system lost pressure.</summary>
    VacuumLoss,

    /// <summary>An operator pressed the emergency stop.</summary>
    EmergencyStop,
}
