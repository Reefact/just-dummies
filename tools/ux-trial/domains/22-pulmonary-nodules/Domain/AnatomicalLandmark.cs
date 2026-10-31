namespace PulmonaryNodules.Domain;

/// <summary>The anatomical structures used as fiducials to register two thoracic CT exams.</summary>
public enum LandmarkKind
{
    CarinaBifurcation,
    AorticArchApex,
    RightMainBronchusOrigin,
    LeftMainBronchusOrigin,
    SternalAngle,
    T4VertebralBody,
    RightDiaphragmDome,
    LeftDiaphragmDome,
}

/// <summary>An anatomical landmark located in one exam.</summary>
public sealed class AnatomicalLandmark
{
    public AnatomicalLandmark(LandmarkKind kind, SpatialPoint position)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown landmark kind.");
        }

        ArgumentNullException.ThrowIfNull(position);
        Kind = kind;
        Position = position;
    }

    public LandmarkKind Kind { get; }

    public SpatialPoint Position { get; }

    public override string ToString() => $"{Kind} at {Position}";
}
