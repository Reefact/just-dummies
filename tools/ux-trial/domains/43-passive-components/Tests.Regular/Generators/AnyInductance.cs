// Scaffolded by `any` (JustDummies), then edited by hand: the base spans the nine decades instead of
// drawing uniformly in value.

using JustDummies;

namespace PassiveComponents.Domain;

public sealed partial class AnyInductance : IAny<Inductance> {

    private IAny<double> _henries;

    public AnyInductance()
        : this(henries: AnyValidHenries()) { }

    private static IAny<double> AnyValidHenries() {
        return Magnitudes.AcrossDecades(-9, 0);
    }

    private AnyInductance(IAny<double> henries) {
        _henries = henries;
    }

    private AnyInductance(AnyInductance from) {
        _henries = from._henries;
    }

    public AnyInductance WithHenries(double value) {
        AnyInductance copy = new AnyInductance(this);

        copy._henries = Any.Fixed(value);

        return copy;
    }

    public Inductance Generate() {
        return new Inductance(_henries.Generate());
    }

}
