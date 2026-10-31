using System;

using JustDummies;

using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Discovery;

/// <summary>
///     Builds a conjunction whose geometry at the time of closest approach is stated by the test:
///     positions, velocities, covariances and hard-body radii are what an assessment is about.
///     The identifier, the epoch, the catalogue numbers and the names are dummies.
/// </summary>
internal sealed class Encounter {

    private Vector3            _primaryPosition    = new(7000, 0, 0);
    private Vector3            _primaryVelocity    = new(0, 7.5, 0);
    private Vector3            _secondaryPosition  = new(7001, 0, 0);
    private Vector3            _secondaryVelocity  = new(0, 0, 7.5);
    private PositionCovariance _primaryCovariance   = Isotropic(0.25);
    private PositionCovariance _secondaryCovariance = Isotropic(0.25);
    private double             _primaryRadius      = 10;
    private double             _secondaryRadius    = 10;

    public static PositionCovariance Isotropic(double varianceKm2) {
        return new PositionCovariance(varianceKm2, varianceKm2, varianceKm2, 0, 0, 0);
    }

    public Encounter PrimaryAt(Vector3 position, Vector3 velocity) {
        _primaryPosition = position;
        _primaryVelocity = velocity;
        return this;
    }

    public Encounter SecondaryAt(Vector3 position, Vector3 velocity) {
        _secondaryPosition = position;
        _secondaryVelocity = velocity;
        return this;
    }

    public Encounter WithCovariances(PositionCovariance primary, PositionCovariance secondary) {
        _primaryCovariance   = primary;
        _secondaryCovariance = secondary;
        return this;
    }

    public Encounter WithHardBodyRadii(double primaryMeters, double secondaryMeters) {
        _primaryRadius   = primaryMeters;
        _secondaryRadius = secondaryMeters;
        return this;
    }

    public Conjunction Build() {
        Epoch       anyTimeOfClosestApproach = Any.Epoch().Generate();
        SpaceObject anyPrimaryObject         = Any.SpaceObject().Active().WithHardBodyRadiusMeters(_primaryRadius).Generate();
        SpaceObject anySecondaryObject = Any.SpaceObject()
                                            .WithHardBodyRadiusMeters(_secondaryRadius)
                                            .WithCatalogNumber(n => n.DifferentFrom(anyPrimaryObject.CatalogNumber))
                                            .Generate();

        Trajectory primary   = Through(anyPrimaryObject, anyTimeOfClosestApproach, _primaryPosition, _primaryVelocity, _primaryCovariance);
        Trajectory secondary = Through(anySecondaryObject, anyTimeOfClosestApproach, _secondaryPosition, _secondaryVelocity, _secondaryCovariance);

        return Any.Conjunction()
                  .WithPrimary(primary)
                  .WithSecondary(secondary)
                  .WithTimeOfClosestApproach(anyTimeOfClosestApproach)
                  .Generate();
    }

    /// <summary>Two nodes a minute either side of the epoch, on a straight line: the midpoint is exact.</summary>
    public static Trajectory Through(SpaceObject spaceObject, Epoch epoch, Vector3 position, Vector3 velocity, PositionCovariance covariance) {
        TimeSpan minute = TimeSpan.FromMinutes(1);
        OrbitalState before = new(epoch.Add(-minute), position.Minus(velocity.Scale(60)), velocity, covariance);
        OrbitalState after  = new(epoch.Add(minute), position.Plus(velocity.Scale(60)), velocity, covariance);

        return new Trajectory(spaceObject, [before, after]);
    }

}
