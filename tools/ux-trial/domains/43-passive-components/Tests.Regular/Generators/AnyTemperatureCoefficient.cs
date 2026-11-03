// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TemperatureCoefficient --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TemperatureCoefficient" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTemperatureCoefficient : IAny<TemperatureCoefficient> {

    private IAny<double> _ppmPerKelvin;

    private AnyDouble _ppmPerKelvinRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTemperatureCoefficient()
        : this(ppmPerKelvin: AnyValidPpmPerKelvin()) { }

    private static AnyDouble AnyValidPpmPerKelvin() {
        return Any.Double().Between(0d, TemperatureCoefficient.MaxPpmPerKelvin);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTemperatureCoefficient(AnyDouble ppmPerKelvin) {
        _ppmPerKelvin       = ppmPerKelvin;
        _ppmPerKelvinRecipe = ppmPerKelvin;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTemperatureCoefficient(AnyTemperatureCoefficient from) {
        _ppmPerKelvin       = from._ppmPerKelvin;
        _ppmPerKelvinRecipe = from._ppmPerKelvinRecipe;
    }

    /// <summary>Pins <c>ppmPerKelvin</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTemperatureCoefficient WithPpmPerKelvin(double value) {
        AnyTemperatureCoefficient copy = new AnyTemperatureCoefficient(this);

        copy._ppmPerKelvin = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ppmPerKelvin</c> is drawn from — <c>AnyValidPpmPerKelvin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTemperatureCoefficient WithPpmPerKelvin(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTemperatureCoefficient copy = new AnyTemperatureCoefficient(this);

        copy._ppmPerKelvinRecipe = configure(_ppmPerKelvinRecipe);
        copy._ppmPerKelvin       = copy._ppmPerKelvinRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TemperatureCoefficient" />.</summary>
    public TemperatureCoefficient Generate() {
        return new TemperatureCoefficient(_ppmPerKelvin.Generate());
    }

}
