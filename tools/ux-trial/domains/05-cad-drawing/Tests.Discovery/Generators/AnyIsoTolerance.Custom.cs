using JustDummies;

namespace Drafting.Domain;

public sealed partial class AnyIsoTolerance {

    /// <summary>A hole tolerance: zone H or JS.</summary>
    public AnyIsoTolerance Hole() {
        return WithZone(_ => Any.String().OneOf("H", "JS"));
    }

    /// <summary>A shaft tolerance: zone h or js.</summary>
    public AnyIsoTolerance Shaft() {
        return WithZone(_ => Any.String().OneOf("h", "js"));
    }

}
