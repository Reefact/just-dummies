// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RockCell --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="RockCell" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRockCell : IAny<RockCell> {

    private readonly IAny<CellCoordinate> _coordinate;
    private readonly IAny<Porosity>       _porosity;
    private readonly IAny<Permeability>   _permeability;
    private readonly IAny<double>         _thermalConductivity;
    private readonly IAny<Pressure>       _pressure;
    private readonly IAny<Temperature>    _temperature;

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
        // W/(m·K), the range RockCell declares.
        return Any.Double().Between(RockCell.MinimumThermalConductivity, RockCell.MaximumThermalConductivity);
    }

    private static AnyPressure AnyValidPressure() {
        return new AnyPressure();
    }

    private static AnyTemperature AnyValidTemperature() {
        return new AnyTemperature();
    }

    private AnyRockCell(IAny<CellCoordinate> coordinate,
                        IAny<Porosity>       porosity,
                        IAny<Permeability>   permeability,
                        IAny<double>         thermalConductivity,
                        IAny<Pressure>       pressure,
                        IAny<Temperature>    temperature) {
        _coordinate          = coordinate;
        _porosity            = porosity;
        _permeability        = permeability;
        _thermalConductivity = thermalConductivity;
        _pressure            = pressure;
        _temperature         = temperature;
    }

    /// <summary>Pins <c>coordinate</c> to a fixed value.</summary>
    public AnyRockCell WithCoordinate(CellCoordinate value) {
        return new AnyRockCell(new FixedValue<CellCoordinate>(value), _porosity, _permeability, _thermalConductivity, _pressure, _temperature);
    }

    /// <summary>Draws <c>coordinate</c> from <c>AnyValidCoordinate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRockCell WithCoordinate(Func<AnyCellCoordinate, AnyCellCoordinate> configure) {
        return new AnyRockCell(configure(AnyValidCoordinate()), _porosity, _permeability, _thermalConductivity, _pressure, _temperature);
    }

    /// <summary>Pins <c>porosity</c> to a fixed value.</summary>
    public AnyRockCell WithPorosity(Porosity value) {
        return new AnyRockCell(_coordinate, new FixedValue<Porosity>(value), _permeability, _thermalConductivity, _pressure, _temperature);
    }

    /// <summary>Draws <c>porosity</c> from <c>AnyValidPorosity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRockCell WithPorosity(Func<AnyPorosity, AnyPorosity> configure) {
        return new AnyRockCell(_coordinate, configure(AnyValidPorosity()), _permeability, _thermalConductivity, _pressure, _temperature);
    }

    /// <summary>Pins <c>permeability</c> to a fixed value.</summary>
    public AnyRockCell WithPermeability(Permeability value) {
        return new AnyRockCell(_coordinate, _porosity, new FixedValue<Permeability>(value), _thermalConductivity, _pressure, _temperature);
    }

    /// <summary>Draws <c>permeability</c> from <c>AnyValidPermeability()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRockCell WithPermeability(Func<AnyPermeability, AnyPermeability> configure) {
        return new AnyRockCell(_coordinate, _porosity, configure(AnyValidPermeability()), _thermalConductivity, _pressure, _temperature);
    }

    /// <summary>Pins <c>thermalConductivity</c> to a fixed value.</summary>
    public AnyRockCell WithThermalConductivity(double value) {
        return new AnyRockCell(_coordinate, _porosity, _permeability, new FixedValue<double>(value), _pressure, _temperature);
    }

    /// <summary>Draws <c>thermalConductivity</c> from <c>AnyValidThermalConductivity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRockCell WithThermalConductivity(Func<AnyDouble, AnyDouble> configure) {
        return new AnyRockCell(_coordinate, _porosity, _permeability, configure(AnyValidThermalConductivity()), _pressure, _temperature);
    }

    /// <summary>Pins <c>pressure</c> to a fixed value.</summary>
    public AnyRockCell WithPressure(Pressure value) {
        return new AnyRockCell(_coordinate, _porosity, _permeability, _thermalConductivity, new FixedValue<Pressure>(value), _temperature);
    }

    /// <summary>Draws <c>pressure</c> from <c>AnyValidPressure()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRockCell WithPressure(Func<AnyPressure, AnyPressure> configure) {
        return new AnyRockCell(_coordinate, _porosity, _permeability, _thermalConductivity, configure(AnyValidPressure()), _temperature);
    }

    /// <summary>Pins <c>temperature</c> to a fixed value.</summary>
    public AnyRockCell WithTemperature(Temperature value) {
        return new AnyRockCell(_coordinate, _porosity, _permeability, _thermalConductivity, _pressure, new FixedValue<Temperature>(value));
    }

    /// <summary>Draws <c>temperature</c> from <c>AnyValidTemperature()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRockCell WithTemperature(Func<AnyTemperature, AnyTemperature> configure) {
        return new AnyRockCell(_coordinate, _porosity, _permeability, _thermalConductivity, _pressure, configure(AnyValidTemperature()));
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

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
