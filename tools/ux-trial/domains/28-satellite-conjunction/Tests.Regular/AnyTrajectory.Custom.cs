using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace SatelliteConjunction.Domain;

public sealed partial class AnyTrajectory {

    private Func<AnyPositionCovariance, AnyPositionCovariance> _stateCovariance = covariance => covariance;

    /// <summary>
    ///     Refines the covariance of each generated state. Call it before <see cref="StartingAt" /> or <see cref="Covering" />,
    ///     which build the states with the refinement known at that moment; it has no effect on states pinned through <c>WithStates</c>.
    /// </summary>
    public AnyTrajectory WithStateCovariance(Func<AnyPositionCovariance, AnyPositionCovariance> configure) {
        AnyTrajectory copy = new AnyTrajectory(this);

        copy._stateCovariance = configure;
        copy._states          = StatesFrom(new AnyEpoch(), configure);

        return copy;
    }

    /// <summary>A trajectory whose first state is at <paramref name="start" />.</summary>
    public AnyTrajectory StartingAt(Epoch start) {
        return WithStates(StatesFrom(Any.Fixed(start), _stateCovariance));
    }

    /// <summary>A trajectory whose span contains <paramref name="epoch" />, wherever inside it.</summary>
    public AnyTrajectory Covering(Epoch epoch) {
        Func<AnyPositionCovariance, AnyPositionCovariance> covariance = _stateCovariance;

        IAny<IEnumerable<OrbitalState>> states = Steps().Then(steps => Any.Int32().Between(0, steps.Sum())
                                                                          .As(back => Build(epoch.Add(TimeSpan.FromSeconds(-back)), steps, covariance)));

        return WithStates(states);
    }

    private AnyTrajectory WithStates(IAny<IEnumerable<OrbitalState>> states) {
        AnyTrajectory copy = new AnyTrajectory(this);

        copy._states = states;

        return copy;
    }

    private static IAny<IList<int>> Steps() {
        return Any.ListOf(Any.Int32().Between(1, (int)Trajectory.MaximumStep.TotalSeconds))
                  .WithCountBetween(1, 5)
                  .As(steps => (IList<int>)steps);
    }

    private static IAny<IEnumerable<OrbitalState>> StatesFrom(IAny<Epoch> start, Func<AnyPositionCovariance, AnyPositionCovariance> covariance) {
        return Any.Combine(start, Steps(), (first, steps) => Build(first, steps, covariance));
    }

    private static IEnumerable<OrbitalState> Build(Epoch start, IList<int> steps, Func<AnyPositionCovariance, AnyPositionCovariance> covariance) {
        List<OrbitalState> states = new List<OrbitalState>();
        Epoch              epoch  = start;

        states.Add(AnyStateAt(epoch, covariance));
        foreach (int step in steps) {
            epoch = epoch.Add(TimeSpan.FromSeconds(step));
            states.Add(AnyStateAt(epoch, covariance));
        }

        return states;
    }

    private static OrbitalState AnyStateAt(Epoch epoch, Func<AnyPositionCovariance, AnyPositionCovariance> covariance) {
        return new AnyOrbitalState().WithEpoch(epoch).WithCovariance(covariance).Generate();
    }

}
