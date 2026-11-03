using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace SatelliteConjunction.Domain;

// Written by hand, beside the scaffolded file: `any generate Trajectory --force` leaves it alone.
public sealed partial class AnyTrajectory {

    private static readonly TimeSpan SmallestStep = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     A trajectory whose span contains <paramref name="epoch" /> (its ends included): what a conjunction needs of the
    ///     trajectories it is made of. Where the epoch falls inside the span is drawn.
    /// </summary>
    public AnyTrajectory Covering(Epoch epoch) {
        if (epoch is null) { throw new ArgumentNullException(nameof(epoch)); }

        AnyTrajectory copy = new AnyTrajectory(this);

        copy._states = StatesStartingAt(steps => Any.TimeSpan()
                                                    .Between(TimeSpan.Zero, TimeSpan.FromTicks(steps.Sum(step => step.Ticks)))
                                                    .As(before => epoch.Add(-before)));

        return copy;
    }

    /// <summary>
    ///     Two to six states. They share one position and one velocity, because a trajectory does not require its states
    ///     to be continuous and an interpolation between two unrelated valid states is not always a valid state: here
    ///     only the epochs and the covariances differ. A test about the positions builds its states itself.
    /// </summary>
    private static IAny<IEnumerable<OrbitalState>> StatesStartingAt(Func<IReadOnlyList<TimeSpan>, IAny<Epoch>> start) {
        return Any.Int32().Between(2, 6).Then(count =>
            Any.ListOf(Any.TimeSpan().Between(SmallestStep, Trajectory.MaximumStep)).WithCount(count - 1).Then(steps =>
                start(steps).Then(first =>
                    Any.Combine(new AnyOrbitalState(),
                                Any.ListOf(new AnyPositionCovariance()).WithCount(count - 1),
                                (state, covariances) => Chain(first, steps, state, covariances)))));
    }

    private static IEnumerable<OrbitalState> Chain(Epoch first, IReadOnlyList<TimeSpan> steps, OrbitalState state, IReadOnlyList<PositionCovariance> covariances) {
        List<OrbitalState> states = [new OrbitalState(first, state.PositionKm, state.VelocityKmPerSecond, state.Covariance)];
        Epoch              epoch  = first;

        for (int index = 0; index < steps.Count; index++) {
            epoch = epoch.Add(steps[index]);
            states.Add(new OrbitalState(epoch, state.PositionKm, state.VelocityKmPerSecond, covariances[index]));
        }

        return states;
    }

}
