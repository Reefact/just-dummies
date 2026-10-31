namespace OrbitalMicrobiology.Domain;

/// <summary>The phase a population is in, as read from its current dynamics.</summary>
public enum GrowthPhase
{
    Lag,
    Exponential,
    Stationary,
    Death,
}

/// <summary>How a concentration was obtained. Optical density is indirect: it is converted from turbidity.</summary>
public enum ObservationMethod
{
    PlateCount,
    FlowCytometry,
    OpticalDensity,
}
