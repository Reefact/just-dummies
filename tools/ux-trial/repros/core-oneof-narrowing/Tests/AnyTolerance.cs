using JustDummies;

namespace Zone.Domain;

// Shape emitted by `any generate` for a guarded string parameter, reduced to one parameter.
public sealed class AnyTolerance : IAny<Tolerance> {

    private readonly IAny<string> _zone;

    public AnyTolerance() : this(AnyValidZone()) { }

    private AnyTolerance(IAny<string> zone) {
        _zone = zone;
    }

    private static AnyString AnyValidZone() {
        return Any.String().OneOf("H", "h", "JS", "js");
    }

    public AnyTolerance WithZone(Func<AnyString, AnyString> configure) {
        return new AnyTolerance(configure(AnyValidZone()));
    }

    public Tolerance Generate() {
        return new Tolerance(_zone.Generate());
    }

}
