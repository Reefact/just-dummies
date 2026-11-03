// Scaffolded by `any` (JustDummies), then edited by hand: the base spans the fifteen decades instead of
// drawing uniformly in value.

using JustDummies;

namespace PassiveComponents.Domain;

public sealed partial class AnyResistance : IAny<Resistance> {

    private IAny<double> _ohms;

    public AnyResistance()
        : this(ohms: AnyValidOhms()) { }

    private static IAny<double> AnyValidOhms() {
        return Magnitudes.AcrossDecades(-3, 12);
    }

    private AnyResistance(IAny<double> ohms) {
        _ohms = ohms;
    }

    private AnyResistance(AnyResistance from) {
        _ohms = from._ohms;
    }

    public AnyResistance WithOhms(double value) {
        AnyResistance copy = new AnyResistance(this);

        copy._ohms = Any.Fixed(value);

        return copy;
    }

    public Resistance Generate() {
        return new Resistance(_ohms.Generate());
    }

}
