namespace Drafting.Domain;

/// <summary>An ISO 286 tolerance class such as H7 (a hole) or h6 (a shaft).</summary>
public sealed class IsoTolerance : IEquatable<IsoTolerance> {

    public const int FinestGrade   = 6;
    public const int CoarsestGrade = 11;

    public IsoTolerance(string zone, int grade) {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentOutOfRangeException.ThrowIfLessThan(grade, FinestGrade);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(grade, CoarsestGrade);

        IsHole = zone switch {
            "H" or "JS" => true,
            "h" or "js" => false,
            _           => throw new ArgumentException($"Unsupported fundamental deviation '{zone}'.", nameof(zone))
        };
        Zone  = zone;
        Grade = grade;
    }

    public string Zone   { get; }
    public int    Grade  { get; }
    public bool   IsHole { get; }

    public bool Equals(IsoTolerance? other) {
        return other is not null && other.Zone == Zone && other.Grade == Grade;
    }

    public override bool Equals(object? obj) {
        return Equals(obj as IsoTolerance);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Zone, Grade);
    }

    public override string ToString() {
        return $"{Zone}{Grade}";
    }

}
