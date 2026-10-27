// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Role --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Role" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRole : IAny<Role> {

    /// <summary>Creates the generator.</summary>
    public AnyRole() { }

    /// <summary>Produces one arbitrary <see cref="Role" />.</summary>
    public Role Generate() {
        return new Role();
    }

}
