using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class QuantityTests {

    [Fact]
    public void A_pressure_given_in_bars_is_held_in_pascals() {
        // Arrange
        // Act
        Pressure pressure = Pressure.FromBars(250);

        // Assert
        Assert.Equal(25_000_000.0, pressure.Pascals, 1e-6);
    }

    [Fact]
    public void A_temperature_given_in_celsius_is_held_in_kelvin() {
        // Arrange
        // Act
        Temperature temperature = Temperature.FromCelsius(150);

        // Assert
        Assert.Equal(423.15, temperature.Kelvin, 1e-9);
    }

    [Fact]
    public void Water_at_its_freezing_point_is_outside_a_liquid_reservoir() {
        // Arrange
        // Act
        Exception? error = Record.Exception(() => Temperature.FromCelsius(0));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

    [Fact]
    public void A_rock_without_pore_space_has_no_porosity() {
        // Arrange
        // Act
        Exception? error = Record.Exception(() => new Porosity(0.0));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

    [Fact]
    public void The_effective_permeability_across_a_face_is_dominated_by_the_tighter_rock() {
        // Arrange
        Permeability sandstone = new Permeability(1e-12);
        Permeability shale     = new Permeability(1e-14);

        // Act
        double effective = Permeability.HarmonicMean(sandstone, shale);

        // Assert
        Assert.Equal(2e-26 / 1.01e-12, effective, 1e-24);
    }

    [Fact]
    public void Two_cells_that_only_touch_at_an_edge_are_not_face_neighbours() {
        // Arrange
        CellCoordinate cell     = new CellCoordinate(1, 1, 1);
        CellCoordinate diagonal = new CellCoordinate(2, 2, 1);
        CellCoordinate below    = new CellCoordinate(1, 1, 2);

        // Act
        bool diagonalIsNeighbour = cell.IsFaceNeighbourOf(diagonal);
        bool belowIsNeighbour    = cell.IsFaceNeighbourOf(below);

        // Assert
        Assert.False(diagonalIsNeighbour);
        Assert.True(belowIsNeighbour);
    }

}
