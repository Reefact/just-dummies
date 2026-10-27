// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShiftKind --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ShiftKind" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShiftKind : IAny<ShiftKind> {

    /// <summary>Creates the generator.</summary>
    public AnyShiftKind() { }

    /// <summary>Produces one arbitrary <see cref="ShiftKind" />.</summary>
    public ShiftKind Generate() {
        return new ShiftKind();
    }

}
