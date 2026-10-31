namespace SatelliteConjunction.Domain;

public sealed partial class AnySpaceObject {

    /// <summary>An active satellite: only a payload can be one, so both parameters move together.</summary>
    public AnySpaceObject Active() {
        return WithKind(ObjectKind.Payload).WithIsActive(true);
    }

}
