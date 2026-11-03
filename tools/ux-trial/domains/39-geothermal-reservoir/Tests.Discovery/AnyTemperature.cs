// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Temperature --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Temperature" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTemperature : IAny<Temperature> {

    private IAny<double> _kelvin;

    private AnyDouble _kelvinRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTemperature()
        : this(kelvin: AnyValidKelvin()) { }

    private static AnyDouble AnyValidKelvin() {
        return Any.Double().GreaterThan(Temperature.FreezingKelvin).LessThanOrEqualTo(Temperature.MaximumKelvin);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTemperature(AnyDouble kelvin) {
        _kelvin       = kelvin;
        _kelvinRecipe = kelvin;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTemperature(AnyTemperature from) {
        _kelvin       = from._kelvin;
        _kelvinRecipe = from._kelvinRecipe;
    }

    /// <summary>Pins <c>kelvin</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTemperature WithKelvin(double value) {
        AnyTemperature copy = new AnyTemperature(this);

        copy._kelvin = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kelvin</c> is drawn from — <c>AnyValidKelvin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTemperature WithKelvin(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTemperature copy = new AnyTemperature(this);

        copy._kelvinRecipe = configure(_kelvinRecipe);
        copy._kelvin       = copy._kelvinRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Temperature" />.</summary>
    public Temperature Generate() {
        return new Temperature(_kelvin.Generate());
    }

}
