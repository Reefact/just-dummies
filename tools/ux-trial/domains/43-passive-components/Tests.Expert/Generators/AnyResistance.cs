// Scaffolded by `any` (JustDummies), then edited: the base is drawn per decade (see LogScale), so the
// recipe is an IAny<double> and the refining overload, which needs a builder, was removed.

using System;

using JustDummies;
using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

/// <summary>A generator of arbitrary <see cref="Resistance" /> values, spread over every decade of its range.</summary>
public sealed partial class AnyResistance : IAny<Resistance> {

    private IAny<double> _ohms;

    public AnyResistance() {
        _ohms = LogScale.Between(Resistance.MinOhms, Resistance.MaxOhms);
    }

    private AnyResistance(AnyResistance from) {
        _ohms = from._ohms;
    }

    /// <summary>Pins <c>ohms</c>: the value is the test's, not a dummy.</summary>
    public AnyResistance WithOhms(double value) {
        AnyResistance copy = new AnyResistance(this);

        copy._ohms = Any.Fixed(value);

        return copy;
    }

    public Resistance Generate() {
        return new Resistance(_ohms.Generate());
    }

}
