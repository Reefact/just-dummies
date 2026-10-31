using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

public sealed partial class AnyTrajectory {

    /// <summary>A trajectory whose states bracket <paramref name="epoch" />.</summary>
    public AnyTrajectory Covering(Epoch epoch) {
        return new AnyTrajectory(_spaceObject, new AnyEphemeris().WithAnchor(epoch));
    }

}
