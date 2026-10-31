using JustDummies;
using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

/// <summary>
///     The states of one object along a straight-line motion through an anchor epoch: at least two
///     nodes, strictly increasing, 10 s to 60 s apart, and always one node at or before the anchor and one
///     at or after it, so the ephemeris covers the anchor. The motion is linear, so an interpolation at the
///     anchor gives back exactly the position and velocity declared there.
/// </summary>
public sealed class AnyEphemeris : IAny<IEnumerable<OrbitalState>> {

    // 180 s at most either side of the anchor at 11 km/s is under 2 000 km: from 2 500 km of altitude,
    // no node and no interpolated state can reach the ground.
    private const double MinimumAltitudeKm = 2_500;

    private readonly IAny<Epoch>              _anchor;
    private readonly IAny<Vector3>            _position;
    private readonly IAny<Vector3>            _velocity;
    private readonly IAny<PositionCovariance> _covariance;

    public AnyEphemeris()
        : this(new AnyEpoch(), Orbital.Positions(MinimumAltitudeKm), Orbital.Velocities(), new AnyPositionCovariance()) { }

    private AnyEphemeris(IAny<Epoch> anchor, IAny<Vector3> position, IAny<Vector3> velocity, IAny<PositionCovariance> covariance) {
        _anchor     = anchor;
        _position   = position;
        _velocity   = velocity;
        _covariance = covariance;
    }

    public AnyEphemeris WithAnchor(Epoch anchor) {
        return new AnyEphemeris(new Fixed<Epoch>(anchor), _position, _velocity, _covariance);
    }

    public AnyEphemeris WithPositionAtAnchor(Vector3 positionKm) {
        return new AnyEphemeris(_anchor, new Fixed<Vector3>(positionKm), _velocity, _covariance);
    }

    public AnyEphemeris WithVelocity(Vector3 velocityKmPerSecond) {
        return new AnyEphemeris(_anchor, _position, new Fixed<Vector3>(velocityKmPerSecond), _covariance);
    }

    /// <summary>Every node carries this covariance, so the state at the anchor does too.</summary>
    public AnyEphemeris WithCovariance(PositionCovariance covariance) {
        return new AnyEphemeris(_anchor, _position, _velocity, new Fixed<PositionCovariance>(covariance));
    }

    public IEnumerable<OrbitalState> Generate() {
        Epoch              anchor     = _anchor.Generate();
        Vector3            position   = _position.Generate();
        Vector3            velocity   = _velocity.Generate();
        PositionCovariance covariance = _covariance.Generate();

        int stepSeconds = Any.Int32().Between(10, 60).Generate();
        int leadSeconds = Any.Int32().Between(0, stepSeconds).Generate();    // the first node, at or before the anchor
        int nodeCount   = Any.Int32().Between(2, 4).Generate();              // the second node is already at or after it

        List<OrbitalState> states = [];
        for (int node = 0; node < nodeCount; node++) {
            double  secondsFromAnchor = node * stepSeconds - leadSeconds;
            Vector3 nodePosition      = position.Plus(velocity.Scale(secondsFromAnchor));
            states.Add(new OrbitalState(anchor.Add(TimeSpan.FromSeconds(secondsFromAnchor)), nodePosition, velocity, covariance));
        }

        return states;
    }

    private sealed class Fixed<TValue>(TValue value) : IAny<TValue> {

        public TValue Generate() {
            return value;
        }

    }

}
