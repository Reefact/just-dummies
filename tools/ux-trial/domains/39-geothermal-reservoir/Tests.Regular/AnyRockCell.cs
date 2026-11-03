// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RockCell --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RockCell" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRockCell : IAny<RockCell> {

    private IAny<CellCoordinate> _coordinate;
    private IAny<Porosity>       _porosity;
    private IAny<Permeability>   _permeability;
    private IAny<double>         _thermalConductivity;
    private IAny<Pressure>       _pressure;
    private IAny<Temperature>    _temperature;

    private AnyCellCoordinate _coordinateRecipe;
    private AnyPorosity       _porosityRecipe;
    private AnyPermeability   _permeabilityRecipe;
    private AnyDouble         _thermalConductivityRecipe;
    private AnyPressure       _pressureRecipe;
    private AnyTemperature    _temperatureRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRockCell()
        : this(coordinate:          AnyValidCoordinate(),
               porosity:            AnyValidPorosity(),
               permeability:        AnyValidPermeability(),
               thermalConductivity: AnyValidThermalConductivity(),
               pressure:            AnyValidPressure(),
               temperature:         AnyValidTemperature()) { }

    private static AnyCellCoordinate AnyValidCoordinate() {
        return new AnyCellCoordinate();
    }

    private static AnyPorosity AnyValidPorosity() {
        return new AnyPorosity();
    }

    private static AnyPermeability AnyValidPermeability() {
        return new AnyPermeability();
    }

    private static AnyDouble AnyValidThermalConductivity() {
        return Any.Double().Between(RockCell.MinimumThermalConductivity, RockCell.MaximumThermalConductivity);
    }

    private static AnyPressure AnyValidPressure() {
        return new AnyPressure();
    }

    private static AnyTemperature AnyValidTemperature() {
        return new AnyTemperature();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRockCell(AnyCellCoordinate coordinate,
                        AnyPorosity       porosity,
                        AnyPermeability   permeability,
                        AnyDouble         thermalConductivity,
                        AnyPressure       pressure,
                        AnyTemperature    temperature) {
        _coordinate                = coordinate;
        _coordinateRecipe          = coordinate;
        _porosity                  = porosity;
        _porosityRecipe            = porosity;
        _permeability              = permeability;
        _permeabilityRecipe        = permeability;
        _thermalConductivity       = thermalConductivity;
        _thermalConductivityRecipe = thermalConductivity;
        _pressure                  = pressure;
        _pressureRecipe            = pressure;
        _temperature               = temperature;
        _temperatureRecipe         = temperature;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRockCell(AnyRockCell from) {
        _coordinate                = from._coordinate;
        _coordinateRecipe          = from._coordinateRecipe;
        _porosity                  = from._porosity;
        _porosityRecipe            = from._porosityRecipe;
        _permeability              = from._permeability;
        _permeabilityRecipe        = from._permeabilityRecipe;
        _thermalConductivity       = from._thermalConductivity;
        _thermalConductivityRecipe = from._thermalConductivityRecipe;
        _pressure                  = from._pressure;
        _pressureRecipe            = from._pressureRecipe;
        _temperature               = from._temperature;
        _temperatureRecipe         = from._temperatureRecipe;
    }

    /// <summary>Pins <c>coordinate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRockCell WithCoordinate(CellCoordinate value) {
        AnyRockCell copy = new AnyRockCell(this);

        copy._coordinate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>coordinate</c> is drawn from — <c>AnyValidCoordinate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCellCoordinate, AnyCellCoordinate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRockCell WithCoordinate(Func<AnyCellCoordinate, AnyCellCoordinate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRockCell copy = new AnyRockCell(this);

        copy._coordinateRecipe = configure(_coordinateRecipe);
        copy._coordinate       = copy._coordinateRecipe;

        return copy;
    }

    /// <summary>Pins <c>porosity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRockCell WithPorosity(Porosity value) {
        AnyRockCell copy = new AnyRockCell(this);

        copy._porosity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>porosity</c> is drawn from — <c>AnyValidPorosity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPorosity, AnyPorosity&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRockCell WithPorosity(Func<AnyPorosity, AnyPorosity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRockCell copy = new AnyRockCell(this);

        copy._porosityRecipe = configure(_porosityRecipe);
        copy._porosity       = copy._porosityRecipe;

        return copy;
    }

    /// <summary>Pins <c>permeability</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRockCell WithPermeability(Permeability value) {
        AnyRockCell copy = new AnyRockCell(this);

        copy._permeability = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>permeability</c> is drawn from — <c>AnyValidPermeability()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPermeability, AnyPermeability&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRockCell WithPermeability(Func<AnyPermeability, AnyPermeability> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRockCell copy = new AnyRockCell(this);

        copy._permeabilityRecipe = configure(_permeabilityRecipe);
        copy._permeability       = copy._permeabilityRecipe;

        return copy;
    }

    /// <summary>Pins <c>thermalConductivity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRockCell WithThermalConductivity(double value) {
        AnyRockCell copy = new AnyRockCell(this);

        copy._thermalConductivity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>thermalConductivity</c> is drawn from — <c>AnyValidThermalConductivity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRockCell WithThermalConductivity(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRockCell copy = new AnyRockCell(this);

        copy._thermalConductivityRecipe = configure(_thermalConductivityRecipe);
        copy._thermalConductivity       = copy._thermalConductivityRecipe;

        return copy;
    }

    /// <summary>Pins <c>pressure</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRockCell WithPressure(Pressure value) {
        AnyRockCell copy = new AnyRockCell(this);

        copy._pressure = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pressure</c> is drawn from — <c>AnyValidPressure()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPressure, AnyPressure&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRockCell WithPressure(Func<AnyPressure, AnyPressure> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRockCell copy = new AnyRockCell(this);

        copy._pressureRecipe = configure(_pressureRecipe);
        copy._pressure       = copy._pressureRecipe;

        return copy;
    }

    /// <summary>Pins <c>temperature</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRockCell WithTemperature(Temperature value) {
        AnyRockCell copy = new AnyRockCell(this);

        copy._temperature = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>temperature</c> is drawn from — <c>AnyValidTemperature()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTemperature, AnyTemperature&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRockCell WithTemperature(Func<AnyTemperature, AnyTemperature> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRockCell copy = new AnyRockCell(this);

        copy._temperatureRecipe = configure(_temperatureRecipe);
        copy._temperature       = copy._temperatureRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RockCell" />.</summary>
    public RockCell Generate() {
        return new RockCell(_coordinate.Generate(),
                            _porosity.Generate(),
                            _permeability.Generate(),
                            _thermalConductivity.Generate(),
                            _pressure.Generate(),
                            _temperature.Generate());
    }

}
