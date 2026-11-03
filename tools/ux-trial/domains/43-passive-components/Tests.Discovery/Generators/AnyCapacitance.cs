// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Capacitance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Capacitance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCapacitance : IAny<Capacitance> {

    private IAny<double> _farads;

    private AnyDouble _faradsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCapacitance()
        : this(farads: AnyValidFarads()) { }

    private static AnyDouble AnyValidFarads() {
        return Any.Double().Between(Capacitance.MinFarads, Capacitance.MaxFarads);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCapacitance(AnyDouble farads) {
        _farads       = farads;
        _faradsRecipe = farads;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCapacitance(AnyCapacitance from) {
        _farads       = from._farads;
        _faradsRecipe = from._faradsRecipe;
    }

    /// <summary>Pins <c>farads</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCapacitance WithFarads(double value) {
        AnyCapacitance copy = new AnyCapacitance(this);

        copy._farads = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>farads</c> is drawn from — <c>AnyValidFarads()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCapacitance WithFarads(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCapacitance copy = new AnyCapacitance(this);

        copy._faradsRecipe = configure(_faradsRecipe);
        copy._farads       = copy._faradsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Capacitance" />.</summary>
    public Capacitance Generate() {
        return new Capacitance(_farads.Generate());
    }

}
