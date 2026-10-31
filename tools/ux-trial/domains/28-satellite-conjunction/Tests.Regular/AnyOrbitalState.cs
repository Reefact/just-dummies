// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OrbitalState --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="OrbitalState" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrbitalState : IAny<OrbitalState> {

    private readonly IAny<Epoch>              _epoch;
    private readonly IAny<Vector3>            _positionKm;
    private readonly IAny<Vector3>            _velocityKmPerSecond;
    private readonly IAny<PositionCovariance> _covariance;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrbitalState()
        : this(epoch:               AnyValidEpoch(),
               positionKm:          AnyValidPositionKm(),
               velocityKmPerSecond: AnyValidVelocityKmPerSecond(),
               covariance:          AnyValidCovariance()) { }

    private static AnyEpoch AnyValidEpoch() {
        return new AnyEpoch();
    }

    private static IAny<Vector3> AnyValidPositionKm() {
        // The guard is on the radius, which no per-component recipe can honour: draw a direction and an
        // altitude instead. The altitude keeps a margin under the ceiling for the rounding of sin/cos.
        return Any.Combine(Any.Double().Between(1, OrbitalState.MaximumAltitudeKm - 1),
                           AnyDirection(),
                           (altitude, direction) => direction.Scale(OrbitalState.EarthRadiusKm + altitude));
    }

    private static IAny<Vector3> AnyValidVelocityKmPerSecond() {
        return Any.Combine(Any.Double().Between(0.1, OrbitalState.MaximumSpeedKmPerSecond - 0.01),
                           AnyDirection(),
                           (speed, direction) => direction.Scale(speed));
    }

    private static IAny<Vector3> AnyDirection() {
        return Any.Combine(Any.Double().Between(-Math.PI / 2, Math.PI / 2),
                           Any.Double().Between(-Math.PI, Math.PI),
                           (latitude, longitude) => new Vector3(Math.Cos(latitude) * Math.Cos(longitude),
                                                                Math.Cos(latitude) * Math.Sin(longitude),
                                                                Math.Sin(latitude)));
    }

    private static AnyPositionCovariance AnyValidCovariance() {
        return new AnyPositionCovariance();
    }

    private AnyOrbitalState(IAny<Epoch>              epoch,
                            IAny<Vector3>            positionKm,
                            IAny<Vector3>            velocityKmPerSecond,
                            IAny<PositionCovariance> covariance) {
        _epoch               = epoch;
        _positionKm          = positionKm;
        _velocityKmPerSecond = velocityKmPerSecond;
        _covariance          = covariance;
    }

    /// <summary>Pins <c>epoch</c> to a fixed value.</summary>
    public AnyOrbitalState WithEpoch(Epoch value) {
        return new AnyOrbitalState(new FixedValue<Epoch>(value), _positionKm, _velocityKmPerSecond, _covariance);
    }

    /// <summary>Draws <c>epoch</c> from <c>AnyValidEpoch()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrbitalState WithEpoch(Func<AnyEpoch, AnyEpoch> configure) {
        return new AnyOrbitalState(configure(AnyValidEpoch()), _positionKm, _velocityKmPerSecond, _covariance);
    }

    /// <summary>Pins <c>positionKm</c> to a fixed value.</summary>
    public AnyOrbitalState WithPositionKm(Vector3 value) {
        return new AnyOrbitalState(_epoch, new FixedValue<Vector3>(value), _velocityKmPerSecond, _covariance);
    }

    /// <summary>Pins <c>velocityKmPerSecond</c> to a fixed value.</summary>
    public AnyOrbitalState WithVelocityKmPerSecond(Vector3 value) {
        return new AnyOrbitalState(_epoch, _positionKm, new FixedValue<Vector3>(value), _covariance);
    }

    /// <summary>Pins <c>covariance</c> to a fixed value.</summary>
    public AnyOrbitalState WithCovariance(PositionCovariance value) {
        return new AnyOrbitalState(_epoch, _positionKm, _velocityKmPerSecond, new FixedValue<PositionCovariance>(value));
    }

    /// <summary>Draws <c>covariance</c> from <c>AnyValidCovariance()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrbitalState WithCovariance(Func<AnyPositionCovariance, AnyPositionCovariance> configure) {
        return new AnyOrbitalState(_epoch, _positionKm, _velocityKmPerSecond, configure(AnyValidCovariance()));
    }

    /// <summary>Produces one arbitrary <see cref="OrbitalState" />.</summary>
    public OrbitalState Generate() {
        return new OrbitalState(_epoch.Generate(),
                                _positionKm.Generate(),
                                _velocityKmPerSecond.Generate(),
                                _covariance.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
