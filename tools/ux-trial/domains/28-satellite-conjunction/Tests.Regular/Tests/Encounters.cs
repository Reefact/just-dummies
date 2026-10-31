using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Regular.Tests;

/// <summary>
/// Hand-written encounter geometry. The geometry decides the risk, so it is never drawn: a crossing at
/// 7000 km radius, the primary moving along +y, the secondary along +z, the miss offset along x (which lies
/// in the encounter plane). Only the epoch, the identities and the names are arbitrary.
/// </summary>
internal static class Encounters {

    public const double HardBodyRadiusMeters = 5;

    public static readonly Vector3 PrimaryPositionKm   = new(7000, 0, 0);
    public static readonly Vector3 PrimaryVelocity     = new(0, 7.5, 0);
    public static readonly Vector3 SecondaryVelocity   = new(0, 0, 7.5);

    public static PositionCovariance Isotropic(double sigmaKm) {
        double variance = sigmaKm * sigmaKm;
        return new PositionCovariance(variance, variance, variance, 0, 0, 0);
    }

    /// <summary>Two states a minute either side of <paramref name="epoch" />, moving in a straight line.</summary>
    public static Trajectory Straight(SpaceObject spaceObject, Epoch epoch, Vector3 positionKm, Vector3 velocity, PositionCovariance covariance) {
        TimeSpan minute = TimeSpan.FromMinutes(1);
        return new Trajectory(spaceObject, [
            new OrbitalState(epoch.Add(-minute), positionKm.Minus(velocity.Scale(60)), velocity, covariance),
            new OrbitalState(epoch.Add(minute), positionKm.Plus(velocity.Scale(60)), velocity, covariance),
        ]);
    }

    public static Conjunction Crossing(double missDistanceKm, double sigmaKm) {
        return Crossing(missDistanceKm, sigmaKm, SecondaryVelocity);
    }

    public static Conjunction Crossing(double missDistanceKm, double sigmaKm, Vector3 secondaryVelocity) {
        Epoch       anyTimeOfClosestApproach = Arbitrary.Epoch().Generate();
        SpaceObject anyActiveSatellite       = Arbitrary.SpaceObject().WithKind(ObjectKind.Payload).WithIsActive(true).WithHardBodyRadiusMeters(HardBodyRadiusMeters).Generate();
        SpaceObject anyOtherObject           = Arbitrary.SpaceObject().WithCatalogNumber(c => c.DifferentFrom(anyActiveSatellite.CatalogNumber)).WithHardBodyRadiusMeters(HardBodyRadiusMeters).Generate();
        PositionCovariance covariance        = Isotropic(sigmaKm);

        Trajectory primary   = Straight(anyActiveSatellite, anyTimeOfClosestApproach, PrimaryPositionKm, PrimaryVelocity, covariance);
        Trajectory secondary = Straight(anyOtherObject, anyTimeOfClosestApproach, PrimaryPositionKm.Plus(new Vector3(missDistanceKm, 0, 0)), secondaryVelocity, covariance);

        return new Conjunction(Any.Guid().NonEmpty().Generate(), primary, secondary, anyTimeOfClosestApproach);
    }

    /// <summary>Zero miss, 100 m dispersion, 5 m bodies: Pc about 2.5e-3.</summary>
    public static Conjunction HighRisk() => Crossing(missDistanceKm: 0, sigmaKm: 0.1);

    /// <summary>10 km miss, 100 m dispersion: Pc vanishes.</summary>
    public static Conjunction LowRisk() => Crossing(missDistanceKm: 10, sigmaKm: 0.1);

}
