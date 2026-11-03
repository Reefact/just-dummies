using JustDummies;

using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Domain.Tests.Expert;

/// <summary>
///     Test data builder for a conjunction whose geometry is known, so that what <see cref="Conjunction.Assess" /> must
///     compute can be worked out by hand.
///     <para>
///         The primary sits at (7000, 0, 0) km moving along +y at 7.5 km/s and the secondary moves along -y at the same
///         speed (or along +y too, see <see cref="CoMoving" />): the relative velocity is along y, so the encounter plane is
///         the x-z plane, and the miss is read there directly. Everything that is not set is a dummy.
///     </para>
/// </summary>
internal sealed class Encounter {

    private const double RadiusOfOrbitKm  = 7000;
    private const double SpeedKmPerSecond = 7.5;

    private double?               _primaryRadiusMeters;
    private double?               _secondaryRadiusMeters;
    private double                _missXKm;
    private double                _missZKm;
    private double                _alongTrackKm;
    private bool                  _coMoving;
    private PositionCovariance?   _covarianceOfEach;
    private Epoch?                _timeOfClosestApproach;

    /// <summary>A head-on encounter with no miss at all, covariances and radii left arbitrary.</summary>
    public static Encounter HeadOn() {
        return new Encounter();
    }

    /// <summary>
    ///     A head-on encounter with no miss, built to land in <paramref name="level" /> whatever else is drawn: ten metres
    ///     of combined hard body and an isotropic dispersion that makes Pc = R² / (2 σ²) = 5e-4 (High), 5e-5 (Elevated)
    ///     or 5e-6 (Low).
    /// </summary>
    public static Encounter OfRisk(RiskLevel level) {
        double varianceOfEach = level switch {
            RiskLevel.High     => 0.05,
            RiskLevel.Elevated => 0.5,
            RiskLevel.Low      => 5,
            _                  => throw new ArgumentOutOfRangeException(nameof(level))
        };

        return HeadOn().WithHardBodyRadiiMeters(5, 5).WithCovarianceOfEach(Isotropic(varianceOfEach));
    }

    public static PositionCovariance Isotropic(double variance) {
        return new PositionCovariance(variance, variance, variance, 0, 0, 0);
    }

    public Encounter WithMissInPlane(double xKm, double zKm) {
        Encounter copy = Copy();
        copy._missXKm = xKm;
        copy._missZKm = zKm;

        return copy;
    }

    public Encounter WithAlongTrackOffsetKm(double km) {
        Encounter copy = Copy();
        copy._alongTrackKm = km;

        return copy;
    }

    public Encounter WithHardBodyRadiiMeters(double primary, double secondary) {
        Encounter copy = Copy();
        copy._primaryRadiusMeters   = primary;
        copy._secondaryRadiusMeters = secondary;

        return copy;
    }

    public Encounter WithCovarianceOfEach(PositionCovariance covariance) {
        Encounter copy = Copy();
        copy._covarianceOfEach = covariance;

        return copy;
    }

    public Encounter CoMoving() {
        Encounter copy = Copy();
        copy._coMoving = true;

        return copy;
    }

    public Encounter At(Epoch timeOfClosestApproach) {
        Encounter copy = Copy();
        copy._timeOfClosestApproach = timeOfClosestApproach;

        return copy;
    }

    public Conjunction Build() {
        Epoch tca = _timeOfClosestApproach ?? Any.Epoch().Generate();

        AnySpaceObject primaryObject = Any.SpaceObject().Active();
        if (_primaryRadiusMeters is double primaryRadius) { primaryObject = primaryObject.WithHardBodyRadiusMeters(primaryRadius); }

        SpaceObject primary = primaryObject.Generate();

        AnySpaceObject secondaryObject = Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(primary.CatalogNumber));
        if (_secondaryRadiusMeters is double secondaryRadius) { secondaryObject = secondaryObject.WithHardBodyRadiusMeters(secondaryRadius); }

        SpaceObject secondary = secondaryObject.Generate();

        // Two objects whose uncertainty is not what the test is about still need one the code can combine: the sum of the
        // two must itself stay under the variance ceiling (see the test that documents this), hence the bound on each.
        AnyPositionCovariance wellKnown = Any.PositionCovariance().WithXx(v => v.LessThanOrEqualTo(250_000)).WithYy(v => v.LessThanOrEqualTo(250_000)).WithZz(v => v.LessThanOrEqualTo(250_000));
        PositionCovariance    primaryCovariance   = _covarianceOfEach ?? wellKnown.Generate();
        PositionCovariance    secondaryCovariance = _covarianceOfEach ?? wellKnown.Generate();

        Vector3 primaryPosition = new(RadiusOfOrbitKm, 0, 0);
        Vector3 primaryVelocity = new(0, SpeedKmPerSecond, 0);
        Vector3 secondaryPosition = new(RadiusOfOrbitKm + _missXKm, _alongTrackKm, _missZKm);
        Vector3 secondaryVelocity = new(0, _coMoving ? SpeedKmPerSecond : -SpeedKmPerSecond, 0);

        return new Conjunction(
            Any.Guid().NonEmpty().Generate(),
            Stationary(primary, tca, primaryPosition, primaryVelocity, primaryCovariance),
            Stationary(secondary, tca, secondaryPosition, secondaryVelocity, secondaryCovariance),
            tca);
    }

    /// <summary>
    ///     A trajectory whose two states, a minute either side of <paramref name="at" />, carry the same position, velocity
    ///     and covariance: the state at <paramref name="at" /> is that state, interpolated or not.
    /// </summary>
    public static Trajectory Stationary(SpaceObject spaceObject, Epoch at, Vector3 position, Vector3 velocity, PositionCovariance covariance) {
        return new Trajectory(spaceObject, [
            new OrbitalState(at.Add(TimeSpan.FromMinutes(-1)), position, velocity, covariance),
            new OrbitalState(at.Add(TimeSpan.FromMinutes(1)),  position, velocity, covariance)
        ]);
    }

    private Encounter Copy() {
        return (Encounter)MemberwiseClone();
    }

}
