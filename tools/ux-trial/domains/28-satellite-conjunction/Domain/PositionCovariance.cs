namespace SatelliteConjunction.Domain;

/// <summary>
/// The 3x3 position covariance of a predicted state, in km², in the same inertial frame as the position.
/// Symmetric by construction (six independent terms) and required to be positive definite.
/// </summary>
public sealed class PositionCovariance
{
    /// <summary>A standard deviation beyond this (km) means the object is effectively lost.</summary>
    public const double MaximumSigmaKm = 1000;

    public PositionCovariance(double xx, double yy, double zz, double xy, double xz, double yz)
    {
        GuardVariance(xx, nameof(xx));
        GuardVariance(yy, nameof(yy));
        GuardVariance(zz, nameof(zz));
        GuardFinite(xy, nameof(xy));
        GuardFinite(xz, nameof(xz));
        GuardFinite(yz, nameof(yz));

        GuardCorrelation(xy, xx, yy, nameof(xy));
        GuardCorrelation(xz, xx, zz, nameof(xz));
        GuardCorrelation(yz, yy, zz, nameof(yz));

        double determinant = xx * (yy * zz - yz * yz) - xy * (xy * zz - yz * xz) + xz * (xy * yz - yy * xz);
        if (determinant <= 0)
        {
            throw new ArgumentException("A position covariance must be positive definite (its determinant is not positive).");
        }

        Xx = xx;
        Yy = yy;
        Zz = zz;
        Xy = xy;
        Xz = xz;
        Yz = yz;
    }

    public double Xx { get; }

    public double Yy { get; }

    public double Zz { get; }

    public double Xy { get; }

    public double Xz { get; }

    public double Yz { get; }

    /// <summary>The root of the mean variance: a single figure for how dispersed the position is (km).</summary>
    public double MeanSigmaKm => Math.Sqrt((Xx + Yy + Zz) / 3);

    /// <summary>Sum of two independent uncertainties, as used for the relative position of two objects.</summary>
    public PositionCovariance Combine(PositionCovariance other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new PositionCovariance(Xx + other.Xx, Yy + other.Yy, Zz + other.Zz, Xy + other.Xy, Xz + other.Xz, Yz + other.Yz);
    }

    /// <summary>The variance along u and the covariance between u and v, both unit vectors.</summary>
    public double Project(Vector3 u, Vector3 v) =>
        u.X * (Xx * v.X + Xy * v.Y + Xz * v.Z)
        + u.Y * (Xy * v.X + Yy * v.Y + Yz * v.Z)
        + u.Z * (Xz * v.X + Yz * v.Y + Zz * v.Z);

    private static void GuardFinite(double value, string name)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, value, "A covariance term must be a finite number.");
        }
    }

    private static void GuardVariance(double value, string name)
    {
        GuardFinite(value, name);
        if (value <= 0 || value > MaximumSigmaKm * MaximumSigmaKm)
        {
            throw new ArgumentOutOfRangeException(name, value, $"A variance must be positive and at most {MaximumSigmaKm * MaximumSigmaKm} km².");
        }
    }

    private static void GuardCorrelation(double covariance, double varianceA, double varianceB, string name)
    {
        if (Math.Abs(covariance) >= Math.Sqrt(varianceA * varianceB))
        {
            throw new ArgumentOutOfRangeException(name, covariance, "A cross term must give a correlation strictly between -1 and 1.");
        }
    }
}
