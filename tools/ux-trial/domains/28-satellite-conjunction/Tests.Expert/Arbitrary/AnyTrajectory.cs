// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Trajectory --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;
using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Trajectory" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTrajectory : IAny<Trajectory> {

    private readonly IAny<SpaceObject>               _spaceObject;
    private readonly IAny<IEnumerable<OrbitalState>> _states;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTrajectory()
        : this(spaceObject: AnyValidSpaceObject(),
               states:      AnyValidStates()) { }

    private static AnySpaceObject AnyValidSpaceObject() {
        return new AnySpaceObject();
    }

    private static IAny<IEnumerable<OrbitalState>> AnyValidStates() {
        // Epochs strictly increasing and at most ten minutes apart, along a straight-line motion so that
        // every interpolated state stays in orbit: a sequence of independent states could not promise that.
        return new AnyEphemeris();
    }

    private AnyTrajectory(IAny<SpaceObject>               spaceObject,
                          IAny<IEnumerable<OrbitalState>> states) {
        _spaceObject = spaceObject;
        _states      = states;
    }

    /// <summary>Pins <c>spaceObject</c> to a fixed value.</summary>
    public AnyTrajectory WithSpaceObject(SpaceObject value) {
        return new AnyTrajectory(new FixedValue<SpaceObject>(value), _states);
    }

    /// <summary>Draws <c>spaceObject</c> from <c>AnyValidSpaceObject()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTrajectory WithSpaceObject(Func<AnySpaceObject, AnySpaceObject> configure) {
        return new AnyTrajectory(configure(AnyValidSpaceObject()), _states);
    }

    /// <summary>Pins <c>states</c> to a fixed value.</summary>
    public AnyTrajectory WithStates(IEnumerable<OrbitalState> value) {
        return new AnyTrajectory(_spaceObject, new FixedValue<IEnumerable<OrbitalState>>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Trajectory" />.</summary>
    public Trajectory Generate() {
        return new Trajectory(_spaceObject.Generate(),
                              _states.Generate());
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
