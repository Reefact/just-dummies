// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Inductance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Inductance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyInductance : IAny<Inductance> {

    private IAny<double> _henries;

    private AnyDouble _henriesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyInductance()
        : this(henries: AnyValidHenries()) { }

    private static AnyDouble AnyValidHenries() {
        return Any.Double().Between(Inductance.MinHenries, Inductance.MaxHenries);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyInductance(AnyDouble henries) {
        _henries       = henries;
        _henriesRecipe = henries;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyInductance(AnyInductance from) {
        _henries       = from._henries;
        _henriesRecipe = from._henriesRecipe;
    }

    /// <summary>Pins <c>henries</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInductance WithHenries(double value) {
        AnyInductance copy = new AnyInductance(this);

        copy._henries = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>henries</c> is drawn from — <c>AnyValidHenries()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInductance WithHenries(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInductance copy = new AnyInductance(this);

        copy._henriesRecipe = configure(_henriesRecipe);
        copy._henries       = copy._henriesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Inductance" />.</summary>
    public Inductance Generate() {
        return new Inductance(_henries.Generate());
    }

}
