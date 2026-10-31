using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace SatelliteConjunction.Domain;

// Written by hand, never touched by `any generate --force`.
public sealed partial class AnyTrajectory {

    /// <summary>A trajectory whose span contains <paramref name="epoch" />, wherever inside it.</summary>
    public AnyTrajectory Covering(Epoch epoch) {
        IAny<IEnumerable<OrbitalState>> covering = Any.Combine(
            AnySteps(),
            Any.Double().Between(0, 1),
            (steps, position) => {
                TimeSpan total = steps.Aggregate(TimeSpan.Zero, (sum, step) => sum + step);
                Epoch    start = epoch.Add(-TimeSpan.FromTicks((long)(total.Ticks * position)));
                return StatesFrom(start, steps);
            });

        return new AnyTrajectory(_spaceObject, covering);
    }

    private static AnyList<TimeSpan> AnySteps() {
        return Any.ListOf(Any.TimeSpan().Between(TimeSpan.FromSeconds(1), Trajectory.MaximumStep)).WithCountBetween(1, 5);
    }

    private static IEnumerable<OrbitalState> StatesFrom(Epoch start, List<TimeSpan> steps) {
        // Independent positions per node would let StateAt interpolate a straight line through the
        // Earth and throw: every node shares the first node's position and velocity instead.
        OrbitalState       first  = new AnyOrbitalState().WithEpoch(start).Generate();
        AnyOrbitalState    node   = new AnyOrbitalState().WithPositionKm(first.PositionKm).WithVelocityKmPerSecond(first.VelocityKmPerSecond);
        List<OrbitalState> states = [first];
        Epoch              epoch  = start;
        foreach (TimeSpan step in steps) {
            epoch = epoch.Add(step);
            states.Add(node.WithEpoch(epoch).Generate());
        }

        return states;
    }

}
