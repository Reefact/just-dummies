// Scaffolded by `any` (JustDummies), then edited by hand: the base spans the twenty decades instead of
// drawing uniformly in value, so the refining overload (which needs an AnyDouble builder) is gone.

using JustDummies;

namespace PassiveComponents.Domain;

public sealed partial class AnyCapacitance : IAny<Capacitance> {

    private IAny<double> _farads;

    public AnyCapacitance()
        : this(farads: AnyValidFarads()) { }

    private static IAny<double> AnyValidFarads() {
        // [1e-13, 1e-2) : the closed upper end 1e-2 itself is not drawn
        return Magnitudes.AcrossDecades(-13, -2);
    }

    private AnyCapacitance(IAny<double> farads) {
        _farads = farads;
    }

    private AnyCapacitance(AnyCapacitance from) {
        _farads = from._farads;
    }

    public AnyCapacitance WithFarads(double value) {
        AnyCapacitance copy = new AnyCapacitance(this);

        copy._farads = Any.Fixed(value);

        return copy;
    }

    public Capacitance Generate() {
        return new Capacitance(_farads.Generate());
    }

}
