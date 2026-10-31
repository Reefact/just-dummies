using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

public sealed partial class AnySpaceObject {

    /// <summary>An active satellite: only a payload may be one, so both parameters are set together.</summary>
    public AnySpaceObject Active() {
        return WithKind(ObjectKind.Payload).WithIsActive(true);
    }

}
