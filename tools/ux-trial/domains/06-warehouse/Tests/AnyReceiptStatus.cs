// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReceiptStatus --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReceiptStatus" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReceiptStatus : IAny<ReceiptStatus> {

    /// <summary>Creates the generator.</summary>
    public AnyReceiptStatus() { }

    /// <summary>Produces one arbitrary <see cref="ReceiptStatus" />.</summary>
    public ReceiptStatus Generate() {
        return new ReceiptStatus();
    }

}
