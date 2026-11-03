// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Porosity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Porosity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPorosity : IAny<Porosity> {

    private IAny<double> _fraction;

    private AnyDouble _fractionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPorosity()
        : this(fraction: AnyValidFraction()) { }

    private static AnyDouble AnyValidFraction() {
        return Any.Double().Positive().LessThanOrEqualTo(Porosity.Maximum);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPorosity(AnyDouble fraction) {
        _fraction       = fraction;
        _fractionRecipe = fraction;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPorosity(AnyPorosity from) {
        _fraction       = from._fraction;
        _fractionRecipe = from._fractionRecipe;
    }

    /// <summary>Pins <c>fraction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPorosity WithFraction(double value) {
        AnyPorosity copy = new AnyPorosity(this);

        copy._fraction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fraction</c> is drawn from — <c>AnyValidFraction()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPorosity WithFraction(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPorosity copy = new AnyPorosity(this);

        copy._fractionRecipe = configure(_fractionRecipe);
        copy._fraction       = copy._fractionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Porosity" />.</summary>
    public Porosity Generate() {
        return new Porosity(_fraction.Generate());
    }

}
