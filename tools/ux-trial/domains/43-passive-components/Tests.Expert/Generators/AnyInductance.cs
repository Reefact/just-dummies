// Scaffolded by `any` (JustDummies), then edited: the base is drawn per decade (see LogScale), so the
// recipe is an IAny<double> and the refining overload, which needs a builder, was removed.

using System;

using JustDummies;
using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

/// <summary>A generator of arbitrary <see cref="Inductance" /> values, spread over every decade of its range.</summary>
public sealed partial class AnyInductance : IAny<Inductance> {

    private IAny<double> _henries;

    public AnyInductance() {
        _henries = LogScale.Between(Inductance.MinHenries, Inductance.MaxHenries);
    }

    private AnyInductance(AnyInductance from) {
        _henries = from._henries;
    }

    /// <summary>Pins <c>henries</c>: the value is the test's, not a dummy.</summary>
    public AnyInductance WithHenries(double value) {
        AnyInductance copy = new AnyInductance(this);

        copy._henries = Any.Fixed(value);

        return copy;
    }

    public Inductance Generate() {
        return new Inductance(_henries.Generate());
    }

}
