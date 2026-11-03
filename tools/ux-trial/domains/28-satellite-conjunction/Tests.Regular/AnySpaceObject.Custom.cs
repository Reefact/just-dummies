namespace SatelliteConjunction.Domain;

public sealed partial class AnySpaceObject {

    /// <summary>An active payload: the only kind of object that can be the primary of a conjunction.</summary>
    public AnySpaceObject Active() {
        return WithKind(ObjectKind.Payload).WithIsActive(true);
    }

    /// <summary>An inert object, whatever its kind.</summary>
    public AnySpaceObject Inert() {
        return WithIsActive(false);
    }

}
