using JustDummies;
using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Conjunction" /> values, written by hand rather than scaffolded:
///     the constructor's rules hold <em>between</em> its parameters (an active primary, two different
///     objects, a TCA both trajectories cover), so the generator draws the encounter instead.
///     <para>
///         What decides a risk is the relative geometry at TCA — the miss vector, the relative velocity,
///         the two covariances and the two hard-body radii — and each has a <c>With…</c> to pin it.
///         Where the encounter happens in orbit, when, and between which catalogued objects is drawn.
///     </para>
///     <para>
///         At a closest approach the miss vector is perpendicular to the relative velocity — that is what makes
///         it the closest. Whichever of the two is drawn is drawn perpendicular to the other; pin both and
///         the geometry is yours.
///     </para>
/// </summary>
public sealed class AnyConjunction : IAny<Conjunction> {

    /// <summary>The primary moves at 1 to 3.5 km/s, so a relative speed up to 8 km/s keeps the secondary under 12 km/s.</summary>
    public const double MaximumRelativeSpeedKmPerSecond = 8;

    private readonly IAny<Guid>               _id;
    private readonly IAny<Epoch>              _timeOfClosestApproach;
    private readonly AnySpaceObject           _primaryObject;
    private readonly AnySpaceObject           _secondaryObject;
    private readonly Vector3?                 _missVector;
    private readonly Vector3?                 _relativeVelocity;
    private readonly IAny<PositionCovariance> _primaryCovariance;
    private readonly IAny<PositionCovariance> _secondaryCovariance;

    public AnyConjunction()
        : this(Any.Guid().NonEmpty(),
               new AnyEpoch(),
               new AnySpaceObject().Active(),
               new AnySpaceObject(),
               missVector: null,
               relativeVelocity: null,
               new AnyPositionCovariance(),
               new AnyPositionCovariance()) { }

    private AnyConjunction(IAny<Guid>               id,
                           IAny<Epoch>              timeOfClosestApproach,
                           AnySpaceObject           primaryObject,
                           AnySpaceObject           secondaryObject,
                           Vector3?                 missVector,
                           Vector3?                 relativeVelocity,
                           IAny<PositionCovariance> primaryCovariance,
                           IAny<PositionCovariance> secondaryCovariance) {
        _id                    = id;
        _timeOfClosestApproach = timeOfClosestApproach;
        _primaryObject         = primaryObject;
        _secondaryObject       = secondaryObject;
        _missVector            = missVector;
        _relativeVelocity      = relativeVelocity;
        _primaryCovariance     = primaryCovariance;
        _secondaryCovariance   = secondaryCovariance;
    }

    /// <summary>Refines the primary, which starts active (only an active satellite can be a primary).</summary>
    public AnyConjunction WithPrimary(Func<AnySpaceObject, AnySpaceObject> configure) {
        return new AnyConjunction(_id, _timeOfClosestApproach, configure(new AnySpaceObject().Active()), _secondaryObject, _missVector, _relativeVelocity, _primaryCovariance, _secondaryCovariance);
    }

    /// <summary>Refines the secondary. Its catalogue number is always drawn different from the primary's.</summary>
    public AnyConjunction WithSecondary(Func<AnySpaceObject, AnySpaceObject> configure) {
        return new AnyConjunction(_id, _timeOfClosestApproach, _primaryObject, configure(new AnySpaceObject()), _missVector, _relativeVelocity, _primaryCovariance, _secondaryCovariance);
    }

    /// <summary>Both hard-body radii, which together make the collision cross-section.</summary>
    public AnyConjunction WithHardBodyRadiiMeters(double primary, double secondary) {
        return WithPrimary(o => o.WithHardBodyRadiusMeters(primary)).WithSecondary(o => o.WithHardBodyRadiusMeters(secondary));
    }

    public AnyConjunction WithTimeOfClosestApproach(Epoch value) {
        return new AnyConjunction(_id, new Fixed<Epoch>(value), _primaryObject, _secondaryObject, _missVector, _relativeVelocity, _primaryCovariance, _secondaryCovariance);
    }

    /// <summary>The secondary's position minus the primary's, at TCA (km).</summary>
    public AnyConjunction WithMissVector(Vector3 value) {
        return new AnyConjunction(_id, _timeOfClosestApproach, _primaryObject, _secondaryObject, value, _relativeVelocity, _primaryCovariance, _secondaryCovariance);
    }

    /// <summary>The secondary's velocity minus the primary's (km/s), at most <see cref="MaximumRelativeSpeedKmPerSecond" />.</summary>
    public AnyConjunction WithRelativeVelocity(Vector3 value) {
        if (value.Magnitude > MaximumRelativeSpeedKmPerSecond) {
            throw new ArgumentOutOfRangeException(nameof(value), value.Magnitude, $"This generator keeps relative speeds at or below {MaximumRelativeSpeedKmPerSecond} km/s, so the secondary stays under 12 km/s.");
        }

        return new AnyConjunction(_id, _timeOfClosestApproach, _primaryObject, _secondaryObject, _missVector, value, _primaryCovariance, _secondaryCovariance);
    }

    public AnyConjunction WithCovariances(PositionCovariance primary, PositionCovariance secondary) {
        return new AnyConjunction(_id, _timeOfClosestApproach, _primaryObject, _secondaryObject, _missVector, _relativeVelocity, new Fixed<PositionCovariance>(primary), new Fixed<PositionCovariance>(secondary));
    }

    public Conjunction Generate() {
        Epoch       timeOfClosestApproach = _timeOfClosestApproach.Generate();
        SpaceObject primaryObject         = _primaryObject.Generate();
        SpaceObject secondaryObject       = _secondaryObject.WithCatalogNumber(n => n.DifferentFrom(primaryObject.CatalogNumber)).Generate();

        (Vector3 missVector, Vector3 relativeVelocity) = Geometry();

        Vector3 primaryPosition = Orbital.Positions(minAltitudeKm: 2_500, maxAltitudeKm: 80_000).Generate();
        Vector3 primaryVelocity = Orbital.Velocities(1, 3.5).Generate();

        IEnumerable<OrbitalState> primaryStates = new AnyEphemeris().WithAnchor(timeOfClosestApproach)
                                                                    .WithPositionAtAnchor(primaryPosition)
                                                                    .WithVelocity(primaryVelocity)
                                                                    .WithCovariance(_primaryCovariance.Generate())
                                                                    .Generate();
        IEnumerable<OrbitalState> secondaryStates = new AnyEphemeris().WithAnchor(timeOfClosestApproach)
                                                                      .WithPositionAtAnchor(primaryPosition.Plus(missVector))
                                                                      .WithVelocity(primaryVelocity.Plus(relativeVelocity))
                                                                      .WithCovariance(_secondaryCovariance.Generate())
                                                                      .Generate();

        return new Conjunction(_id.Generate(),
                               new Trajectory(primaryObject, primaryStates),
                               new Trajectory(secondaryObject, secondaryStates),
                               timeOfClosestApproach);
    }

    private (Vector3 MissVector, Vector3 RelativeVelocity) Geometry() {
        IAny<double> anyMissDistance   = Any.Double().Between(0, 50);
        IAny<double> anyRelativeSpeed  = Any.Double().Between(1, MaximumRelativeSpeedKmPerSecond);

        Vector3 missVector       = _missVector ?? PerpendicularTo(_relativeVelocity).Scale(anyMissDistance.Generate());
        Vector3 relativeVelocity = _relativeVelocity ?? PerpendicularTo(missVector).Scale(anyRelativeSpeed.Generate());

        return (missVector, relativeVelocity);
    }

    /// <summary>A unit vector perpendicular to <paramref name="axis" />, or any unit vector when there is no axis.</summary>
    private static Vector3 PerpendicularTo(Vector3? axis) {
        for (int attempt = 0; attempt < 100; attempt++) {
            Vector3 direction = Orbital.Directions().Generate();
            if (axis is null || axis.Magnitude == 0) {
                return direction;
            }

            Vector3 perpendicular = axis.Cross(direction);
            if (perpendicular.Magnitude > 1e-3 * axis.Magnitude) {
                return perpendicular.Normalized();
            }
        }

        throw new InvalidOperationException($"No direction perpendicular to {axis} was found in 100 draws.");
    }

    private sealed class Fixed<TValue>(TValue value) : IAny<TValue> {

        public TValue Generate() {
            return value;
        }

    }

}
