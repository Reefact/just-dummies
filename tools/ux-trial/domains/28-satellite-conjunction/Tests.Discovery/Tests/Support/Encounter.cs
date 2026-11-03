using JustDummies;

using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Discovery;

/// <summary>
///     Head-on encounters whose geometry is spelled out, because the collision probability they produce is what
///     the tests are about. Everything the geometry does not decide (identifiers, names, the epoch) is drawn.
///     <para>
///         The primary sits at (7000, 0, 0) km moving at +7.5 km/s along y; the secondary sits <c>miss</c> km further
///         along x moving at -7.5 km/s along y. The relative velocity (15 km/s along y) is normal to the x-z encounter
///         plane, and the relative position lies along x in that plane.
///     </para>
///     <para>
///         With two hard bodies of 10 m, R = 0.02 km. With two isotropic covariances of 0.01 km2 the combined
///         covariance on the plane is 0.02 km2 on both axes, so sqrt(det) = 0.02 and
///         Pc = R2 / (2 sqrt(det)) exp(-d2 / 2) = 0.01 exp(-miss2 / 0.04).
///     </para>
/// </summary>
internal static class Encounter {

    /// <summary>Miss distance 0: Pc = 0.01, High.</summary>
    public static Conjunction HighRisk() {
        return Between(missKm: 0);
    }

    /// <summary>Miss distance 0.48 km: Pc = 0.01 exp(-5.76), about 3.15e-5, Elevated.</summary>
    public static Conjunction ElevatedRisk() {
        return Between(missKm: 0.48);
    }

    /// <summary>Miss distance 2 km: Pc = 0.01 exp(-100), about 3.7e-46, Low.</summary>
    public static Conjunction LowRisk() {
        return Between(missKm: 2);
    }

    public static Conjunction Between(double missKm,
                                      double varianceKm2 = 0.01,
                                      double radiusMeters = 10,
                                      double secondaryVelocityYKmPerSecond = -7.5,
                                      double secondsBeforeClosestApproach = 60,
                                      double secondsAfterClosestApproach = 60) {
        Epoch       tca             = Any.Epoch().Generate();
        SpaceObject primaryObject   = Any.SpaceObject().Active().WithHardBodyRadiusMeters(radiusMeters).Generate();
        SpaceObject secondaryObject = Any.SpaceObject()
                                         .WithHardBodyRadiusMeters(radiusMeters)
                                         .WithCatalogNumber(number => number.DifferentFrom(primaryObject.CatalogNumber))
                                         .Generate();

        Trajectory primary = Any.Trajectory()
                                .WithSpaceObject(primaryObject)
                                .WithStates(StraightLine(tca, new Vector3(7000, 0, 0), new Vector3(0, 7.5, 0), varianceKm2, secondsBeforeClosestApproach, secondsAfterClosestApproach))
                                .Generate();
        Trajectory secondary = Any.Trajectory()
                                  .WithSpaceObject(secondaryObject)
                                  .WithStates(StraightLine(tca, new Vector3(7000 + missKm, 0, 0), new Vector3(0, secondaryVelocityYKmPerSecond, 0), varianceKm2, secondsBeforeClosestApproach, secondsAfterClosestApproach))
                                  .Generate();

        return Any.Conjunction().WithPrimary(primary).WithSecondary(secondary).WithTimeOfClosestApproach(tca).Generate();
    }

    private static OrbitalState[] StraightLine(Epoch tca, Vector3 positionAtClosestApproach, Vector3 velocity, double varianceKm2, double secondsBefore, double secondsAfter) {
        PositionCovariance covariance = new PositionCovariance(varianceKm2, varianceKm2, varianceKm2, 0, 0, 0);

        return new[] {
            new OrbitalState(tca.Add(TimeSpan.FromSeconds(-secondsBefore)), positionAtClosestApproach.Minus(velocity.Scale(secondsBefore)), velocity, covariance),
            new OrbitalState(tca.Add(TimeSpan.FromSeconds(secondsAfter)), positionAtClosestApproach.Plus(velocity.Scale(secondsAfter)), velocity, covariance)
        };
    }

}
