namespace PassiveComponents.Domain;

/// <summary>The three passive component families the library holds.</summary>
public enum ComponentKind
{
    Capacitor,
    Inductor,
    Resistor,
}

/// <summary>IEC 60063 preferred-number series: the number of values per decade.</summary>
public enum ESeries
{
    E6,
    E12,
    E24,
    E96,
}

/// <summary>
/// The footprint a part is soldered to. Chip sizes use the imperial EIA code (0603 = 1.6 mm × 0.8 mm).
/// <see cref="Chip2512"/> is a resistor package only; <see cref="Radial"/> holds capacitors and inductors only.
/// </summary>
public enum Package
{
    Chip0201,
    Chip0402,
    Chip0603,
    Chip0805,
    Chip1206,
    Chip2512,
    Axial,
    Radial,
}

/// <summary>Why a part cannot stand in for another; <see cref="None"/> when it can.</summary>
public enum SubstitutionRefusal
{
    None,
    SamePart,
    DifferentKind,
    DifferentPackage,
    ToleranceBandNotContained,
    RatingTooLow,
    TemperatureCoefficientWorse,
}
