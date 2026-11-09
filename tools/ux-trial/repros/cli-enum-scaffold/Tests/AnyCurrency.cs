// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Currency --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace EnumRepro.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Currency" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCurrency : IAny<Currency> {

    /// <summary>Creates the generator.</summary>
    public AnyCurrency() { }

    /// <summary>Produces one arbitrary <see cref="Currency" />.</summary>
    public Currency Generate() {
        return new Currency();
    }

}
