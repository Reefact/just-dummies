// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Resistance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Resistance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyResistance : IAny<Resistance> {

    private IAny<double> _ohms;

    private AnyDouble _ohmsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyResistance()
        : this(ohms: AnyValidOhms()) { }

    private static AnyDouble AnyValidOhms() {
        return Any.Double().Between(Resistance.MinOhms, Resistance.MaxOhms);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyResistance(AnyDouble ohms) {
        _ohms       = ohms;
        _ohmsRecipe = ohms;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyResistance(AnyResistance from) {
        _ohms       = from._ohms;
        _ohmsRecipe = from._ohmsRecipe;
    }

    /// <summary>Pins <c>ohms</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyResistance WithOhms(double value) {
        AnyResistance copy = new AnyResistance(this);

        copy._ohms = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ohms</c> is drawn from — <c>AnyValidOhms()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyResistance WithOhms(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyResistance copy = new AnyResistance(this);

        copy._ohmsRecipe = configure(_ohmsRecipe);
        copy._ohms       = copy._ohmsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Resistance" />.</summary>
    public Resistance Generate() {
        return new Resistance(_ohms.Generate());
    }

}
