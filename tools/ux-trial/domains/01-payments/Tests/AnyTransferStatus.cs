// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TransferStatus --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TransferStatus" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTransferStatus : IAny<TransferStatus> {

    /// <summary>Creates the generator.</summary>
    public AnyTransferStatus() { }

    /// <summary>Produces one arbitrary <see cref="TransferStatus" />.</summary>
    public TransferStatus Generate() {
        return new TransferStatus();
    }

}
