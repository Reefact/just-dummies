using JustDummies;

using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Regular;

/// <summary>
///     Hand-built encounters. What a test is about (the geometry, the covariances, the hard-body radii) is passed in
///     as literals; what it is not about (identifiers, names, epoch, absolute position, the speeds when unspecified)
///     is drawn.
/// </summary>
internal static class Scene {

    /// <summary>Two nodes around <paramref name="epoch" />, on a straight line that is exactly at <paramref name="position" /> at that epoch.</summary>
    public static Trajectory Straight(SpaceObject spaceObject, Epoch epoch, Vector3 position, Vector3 velocity, PositionCovariance covariance, double secondsBefore = 30, double secondsAfter = 30) {
        OrbitalState before = new(epoch.Add(TimeSpan.FromSeconds(-secondsBefore)), Moved(position, velocity, -secondsBefore), velocity, covariance);
        OrbitalState after  = new(epoch.Add(TimeSpan.FromSeconds(secondsAfter)), Moved(position, velocity, secondsAfter), velocity, covariance);

        return new Trajectory(spaceObject, [before, after]);
    }

    /// <summary>
    ///     A conjunction whose relative position at the time of closest approach is <paramref name="offset" /> and whose relative
    ///     velocity runs along the y axis, hence perpendicular to an offset in the x-z plane.
    /// </summary>
    public static Conjunction Encounter(Vector3 offset, PositionCovariance primaryCovariance, PositionCovariance secondaryCovariance, double radiusMeters = 10, double? primarySpeed = null, double? secondarySpeed = null) {
        Epoch       anyEpoch           = Any.Epoch().Generate();
        Vector3     anyPosition        = Any.OrbitalState().Generate().PositionKm;
        double      anyPrimarySpeed    = primarySpeed ?? Any.Double().Between(0.5, 6).Generate();
        double      anySecondarySpeed  = secondarySpeed ?? Any.Double().Between(0.5, 6).Generate();
        SpaceObject anyPrimaryObject   = Any.SpaceObject().Active().WithHardBodyRadiusMeters(radiusMeters).Generate();
        SpaceObject anySecondaryObject = Any.SpaceObject().WithHardBodyRadiusMeters(radiusMeters).WithCatalogNumber(n => n.DifferentFrom(anyPrimaryObject.CatalogNumber)).Generate();

        Trajectory primary   = Straight(anyPrimaryObject, anyEpoch, anyPosition, new Vector3(0, anyPrimarySpeed, 0), primaryCovariance);
        Trajectory secondary = Straight(anySecondaryObject, anyEpoch, anyPosition.Plus(offset), new Vector3(0, -anySecondarySpeed, 0), secondaryCovariance);

        return new Conjunction(Any.Guid().NonEmpty().Generate(), primary, secondary, anyEpoch);
    }

    public static Conjunction Encounter(Vector3 offset, double variance, double radiusMeters = 10) {
        PositionCovariance covariance = new(variance, variance, variance, 0, 0, 0);

        return Encounter(offset, covariance, covariance, radiusMeters);
    }

    /// <summary>A screened conjunction that, once assessed, is at the given level (hard-body radii of 10 m each).</summary>
    public static Conjunction ScreenedAt(RiskLevel level) {
        return level switch {
            RiskLevel.High     => Encounter(new Vector3(0, 0, 0), variance: 0.1),     // Pc 1e-3
            RiskLevel.Elevated => Encounter(new Vector3(1.9478, 0, 0), variance: 0.5), // Pc about 3e-5
            _                  => Encounter(new Vector3(100, 0, 0), variance: 0.5)    // Pc 0
        };
    }

    public static Conjunction AssessedAt(RiskLevel level) {
        Conjunction conjunction = ScreenedAt(level);
        conjunction.Assess();

        return conjunction;
    }

    /// <summary>The 2D Gaussian collision probability for an isotropic plane variance and a miss distance in that plane.</summary>
    public static double IsotropicProbability(double radiusKm, double planeVariance, double missKm) {
        return radiusKm * radiusKm / (2 * planeVariance) * Math.Exp(-missKm * missKm / (2 * planeVariance));
    }

    private static Vector3 Moved(Vector3 position, Vector3 velocity, double seconds) {
        return new Vector3(position.X + velocity.X * seconds, position.Y + velocity.Y * seconds, position.Z + velocity.Z * seconds);
    }

}
