namespace SatelliteConjunction.Domain;

/// <summary>A Cartesian vector in the Earth-centred inertial frame (km for positions, km/s for velocities).</summary>
public sealed class Vector3 : IEquatable<Vector3>
{
    public static readonly Vector3 Zero = new(0, 0, 0);

    public Vector3(double x, double y, double z)
    {
        Guard(x, nameof(x));
        Guard(y, nameof(y));
        Guard(z, nameof(z));
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }

    public double Y { get; }

    public double Z { get; }

    public double Magnitude => Math.Sqrt(Dot(this));

    public Vector3 Plus(Vector3 other) => new(X + other.X, Y + other.Y, Z + other.Z);

    public Vector3 Minus(Vector3 other) => new(X - other.X, Y - other.Y, Z - other.Z);

    public Vector3 Scale(double factor) => new(X * factor, Y * factor, Z * factor);

    public double Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3 Cross(Vector3 other) =>
        new(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);

    public Vector3 Normalized()
    {
        double magnitude = Magnitude;
        if (magnitude == 0)
        {
            throw new InvalidOperationException("The zero vector has no direction.");
        }

        return Scale(1 / magnitude);
    }

    public bool Equals(Vector3? other) => other is not null && X == other.X && Y == other.Y && Z == other.Z;

    public override bool Equals(object? obj) => Equals(obj as Vector3);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override string ToString() => FormattableString.Invariant($"({X}, {Y}, {Z})");

    private static void Guard(double component, string name)
    {
        if (!double.IsFinite(component))
        {
            throw new ArgumentOutOfRangeException(name, component, "A vector component must be a finite number.");
        }
    }
}
