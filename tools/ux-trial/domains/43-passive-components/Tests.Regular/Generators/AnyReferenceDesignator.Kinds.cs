using JustDummies;

namespace PassiveComponents.Domain;

public sealed partial class AnyReferenceDesignator {

    /// <summary>A designator whose letter names <paramref name="kind" />: a named recipe beside the generator.</summary>
    public AnyReferenceDesignator OfKind(ComponentKind kind) {
        string letter = kind switch {
            ComponentKind.Capacitor => "C",
            ComponentKind.Inductor  => "L",
            _                       => "R"
        };

        return WithValue(_ => Any.StringMatching(letter + "[1-9][0-9]{0,3}"));
    }

}
