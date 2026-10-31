namespace SatelliteConjunction.Domain;

/// <summary>A predicted state: where an object is expected at an epoch, how fast it moves, and how uncertain that is.</summary>
public sealed class OrbitalState
{
    public const double EarthRadiusKm = 6378.137;
    public const double MaximumAltitudeKm = 100_000;
    public const double MaximumSpeedKmPerSecond = 12;

    public OrbitalState(Epoch epoch, Vector3 positionKm, Vector3 velocityKmPerSecond, PositionCovariance covariance)
    {
        ArgumentNullException.ThrowIfNull(epoch);
        ArgumentNullException.ThrowIfNull(positionKm);
        ArgumentNullException.ThrowIfNull(velocityKmPerSecond);
        ArgumentNullException.ThrowIfNull(covariance);

        double radius = positionKm.Magnitude;
        if (radius <= EarthRadiusKm || radius > EarthRadiusKm + MaximumAltitudeKm)
        {
            throw new ArgumentOutOfRangeException(nameof(positionKm), radius, $"An orbital position lies above the Earth's surface and below {MaximumAltitudeKm} km altitude.");
        }

        double speed = velocityKmPerSecond.Magnitude;
        if (speed == 0 || speed > MaximumSpeedKmPerSecond)
        {
            throw new ArgumentOutOfRangeException(nameof(velocityKmPerSecond), speed, $"An orbital speed is positive and at most {MaximumSpeedKmPerSecond} km/s.");
        }

        Epoch = epoch;
        PositionKm = positionKm;
        VelocityKmPerSecond = velocityKmPerSecond;
        Covariance = covariance;
    }

    public Epoch Epoch { get; }

    public Vector3 PositionKm { get; }

    public Vector3 VelocityKmPerSecond { get; }

    public PositionCovariance Covariance { get; }

    public double AltitudeKm => PositionKm.Magnitude - EarthRadiusKm;
}
