// Scaffolded by `any` (JustDummies), then edited: the base is drawn per decade (see LogScale), so the
// recipe is an IAny<double> and the refining overload, which needs a builder, was removed.

using System;

using JustDummies;
using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

/// <summary>A generator of arbitrary <see cref="Capacitance" /> values, spread over every decade of its range.</summary>
public sealed partial class AnyCapacitance : IAny<Capacitance> {

    private IAny<double> _farads;

    public AnyCapacitance() {
        _farads = LogScale.Between(Capacitance.MinFarads, Capacitance.MaxFarads);
    }

    private AnyCapacitance(AnyCapacitance from) {
        _farads = from._farads;
    }

    /// <summary>Pins <c>farads</c>: the value is the test's, not a dummy.</summary>
    public AnyCapacitance WithFarads(double value) {
        AnyCapacitance copy = new AnyCapacitance(this);

        copy._farads = Any.Fixed(value);

        return copy;
    }

    public Capacitance Generate() {
        return new Capacitance(_farads.Generate());
    }

}
