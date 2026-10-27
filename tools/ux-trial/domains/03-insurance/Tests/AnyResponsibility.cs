// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Responsibility --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Responsibility" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyResponsibility : IAny<Responsibility> {

    /// <summary>Creates the generator.</summary>
    public AnyResponsibility() { }

    /// <summary>Produces one arbitrary <see cref="Responsibility" />.</summary>
    public Responsibility Generate() {
        return new Responsibility();
    }

}
