namespace PulmonaryNodules.Domain;

/// <summary>A position in a scanner's patient coordinate system, in millimetres.</summary>
public sealed class SpatialPoint : IEquatable<SpatialPoint>
{
    /// <summary>The largest absolute coordinate a thoracic CT field of view can hold.</summary>
    public const double MaxAbsoluteCoordinateMm = 2000.0;

    public SpatialPoint(double xMm, double yMm, double zMm)
    {
        XMm = Guard(xMm, nameof(xMm));
        YMm = Guard(yMm, nameof(yMm));
        ZMm = Guard(zMm, nameof(zMm));
    }

    public double XMm { get; }

    public double YMm { get; }

    public double ZMm { get; }

    public double DistanceTo(SpatialPoint other)
    {
        ArgumentNullException.ThrowIfNull(other);
        double dx = XMm - other.XMm;
        double dy = YMm - other.YMm;
        double dz = ZMm - other.ZMm;
        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }

    public bool Equals(SpatialPoint? other) =>
        other is not null && XMm.Equals(other.XMm) && YMm.Equals(other.YMm) && ZMm.Equals(other.ZMm);

    public override bool Equals(object? obj) => Equals(obj as SpatialPoint);

    public override int GetHashCode() => HashCode.Combine(XMm, YMm, ZMm);

    public override string ToString() => $"({XMm:0.##}, {YMm:0.##}, {ZMm:0.##}) mm";

    private static double Guard(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, value, "A coordinate must be a finite number.");
        }

        if (Math.Abs(value) > MaxAbsoluteCoordinateMm)
        {
            throw new ArgumentOutOfRangeException(name, value, $"A coordinate must lie within ±{MaxAbsoluteCoordinateMm} mm.");
        }

        return value;
    }
}
