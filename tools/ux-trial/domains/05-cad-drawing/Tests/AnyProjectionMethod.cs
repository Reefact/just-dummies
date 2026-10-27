// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ProjectionMethod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ProjectionMethod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyProjectionMethod : IAny<ProjectionMethod> {

    /// <summary>Creates the generator.</summary>
    public AnyProjectionMethod() { }

    /// <summary>Produces one arbitrary <see cref="ProjectionMethod" />.</summary>
    public ProjectionMethod Generate() {
        return new ProjectionMethod();
    }

}
