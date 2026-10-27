// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StorageZone --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StorageZone" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStorageZone : IAny<StorageZone> {

    /// <summary>Creates the generator.</summary>
    public AnyStorageZone() { }

    /// <summary>Produces one arbitrary <see cref="StorageZone" />.</summary>
    public StorageZone Generate() {
        return new StorageZone();
    }

}
