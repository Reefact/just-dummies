// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ChargeBearer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ChargeBearer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyChargeBearer : IAny<ChargeBearer> {

    /// <summary>Creates the generator.</summary>
    public AnyChargeBearer() { }

    /// <summary>Produces one arbitrary <see cref="ChargeBearer" />.</summary>
    public ChargeBearer Generate() {
        return new ChargeBearer();
    }

}
