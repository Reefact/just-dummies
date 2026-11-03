using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Expert.Tests;

public sealed class RockCellTests {

    [Fact]
    public void A_cell_keeps_the_properties_it_was_given() {
        CellCoordinate coordinate  = new CellCoordinate(1, 2, 3);
        Porosity       porosity    = new Porosity(0.2);
        Permeability   permeability = new Permeability(1e-12);
        Pressure       pressure    = Pressure.FromBars(300.0);
        Temperature    temperature = Temperature.FromCelsius(90.0);

        RockCell cell = new RockCell(coordinate, porosity, permeability, 2.5, pressure, temperature);

        Assert.Same(coordinate, cell.Coordinate);
        Assert.Same(porosity, cell.Porosity);
        Assert.Same(permeability, cell.Permeability);
        Assert.Equal(2.5, cell.ThermalConductivity);
        Assert.Same(pressure, cell.Pressure);
        Assert.Same(temperature, cell.Temperature);
    }

    [Theory]
    [InlineData(RockCell.MinimumThermalConductivity)]
    [InlineData(3.0)]
    [InlineData(RockCell.MaximumThermalConductivity)]
    public void A_conductivity_from_a_porous_sandstone_to_a_dense_quartzite_is_accepted(double conductivity) {
        RockCell cell = Any.RockCell().WithThermalConductivity(conductivity).Generate();

        Assert.Equal(conductivity, cell.ThermalConductivity);
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    [InlineData(10.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_conductivity_outside_half_to_ten_watts_per_metre_kelvin_is_refused(double conductivity) {
        // Arrange
        AnyRockCell cellGenerator = Any.RockCell().WithThermalConductivity(conductivity);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => cellGenerator.Generate());

        // Assert
        Assert.Equal("thermalConductivity", refusal.ParamName);
    }

    [Fact]
    public void A_cell_without_a_coordinate_is_refused() {
        RockCell anyCell = Any.RockCell().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new RockCell(null!, anyCell.Porosity, anyCell.Permeability, anyCell.ThermalConductivity, anyCell.Pressure, anyCell.Temperature));
    }

    [Fact]
    public void A_cell_without_a_porosity_is_refused() {
        RockCell anyCell = Any.RockCell().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new RockCell(anyCell.Coordinate, null!, anyCell.Permeability, anyCell.ThermalConductivity, anyCell.Pressure, anyCell.Temperature));
    }

    [Fact]
    public void A_cell_without_a_permeability_is_refused() {
        RockCell anyCell = Any.RockCell().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new RockCell(anyCell.Coordinate, anyCell.Porosity, null!, anyCell.ThermalConductivity, anyCell.Pressure, anyCell.Temperature));
    }

    [Fact]
    public void A_cell_without_a_pressure_is_refused() {
        RockCell anyCell = Any.RockCell().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new RockCell(anyCell.Coordinate, anyCell.Porosity, anyCell.Permeability, anyCell.ThermalConductivity, null!, anyCell.Temperature));
    }

    [Fact]
    public void A_cell_without_a_temperature_is_refused() {
        RockCell anyCell = Any.RockCell().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new RockCell(anyCell.Coordinate, anyCell.Porosity, anyCell.Permeability, anyCell.ThermalConductivity, anyCell.Pressure, null!));
    }

}
