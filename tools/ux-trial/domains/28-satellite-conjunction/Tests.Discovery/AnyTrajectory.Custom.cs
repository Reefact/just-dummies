using System;
using System.Collections.Generic;

using JustDummies;

namespace SatelliteConjunction.Domain;

public sealed partial class AnyTrajectory {

    /// <summary>A trajectory whose span covers <paramref name="epoch" />: one node on each side of it.</summary>
    public AnyTrajectory Covering(Epoch epoch) {
        IAny<IEnumerable<OrbitalState>> covering = Any.Combine(
            Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5)),
            Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5)),
            (lead, trail) => (IEnumerable<OrbitalState>)[
                new AnyOrbitalState().WithEpoch(epoch.Add(-lead)).Generate(),
                new AnyOrbitalState().WithEpoch(epoch.Add(trail)).Generate(),
            ]);

        return new AnyTrajectory(_spaceObject, covering);
    }

}
