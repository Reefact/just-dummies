using GeothermalReservoir.Domain;

namespace GeothermalReservoir.Tests.Regular;

public sealed class QuantitiesTests {

    [Fact]
    public void A_pressure_given_in_bars_is_held_in_pascals() {
        // Arrange
        const double bars = 250.0;

        // Act
        Pressure pressure = Pressure.FromBars(bars);

        // Assert
        Assert.Equal(25e6, pressure.Pascals);
    }

    [Fact]
    public void A_temperature_given_in_celsius_is_held_in_kelvin() {
        // Arrange
        const double celsius = 150.0;

        // Act
        Temperature temperature = Temperature.FromCelsius(celsius);

        // Assert
        Assert.Equal(423.15, temperature.Kelvin, 1e-9);
    }

    [Fact]
    public void Water_at_its_freezing_point_is_outside_a_liquid_water_reservoir() {
        // Arrange
        // Act
        Action freezing = () => Temperature.FromCelsius(0.0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(freezing);
    }

    [Fact]
    public void A_rock_more_than_half_pore_space_is_refused() {
        // Arrange
        // Act
        Action tooPorous = () => new Porosity(0.6);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(tooPorous);
    }

    [Fact]
    public void The_effective_permeability_across_a_face_is_dominated_by_the_tighter_rock() {
        // Arrange
        Permeability sandstone = new(1e-12);
        Permeability shale     = new(1e-18);

        // Act
        double effective = Permeability.HarmonicMean(sandstone, shale);

        // Assert
        Assert.Equal(2e-18, effective, 1e-21);
    }

    [Fact]
    public void Cells_differing_by_one_index_share_a_face_and_diagonal_cells_do_not() {
        // Arrange
        CellCoordinate centre = new(1, 1, 1);

        // Act
        bool below    = centre.IsFaceNeighbourOf(new CellCoordinate(1, 1, 2));
        bool diagonal = centre.IsFaceNeighbourOf(new CellCoordinate(2, 2, 1));

        // Assert
        Assert.True(below);
        Assert.False(diagonal);
    }

    [Fact]
    public void A_boundary_with_a_zero_multiplier_is_sealed() {
        // Arrange
        BoundaryCondition boundary = Arbitrary.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate();

        // Act
        bool isSealed = boundary.IsSealed;

        // Assert
        Assert.True(isSealed);
    }

    [Fact]
    public void A_boundary_with_a_positive_multiplier_lets_the_aquifer_flow() {
        // Arrange
        BoundaryCondition boundary = Arbitrary.BoundaryCondition().WithTransmissibilityMultiplier(m => m.GreaterThan(0.0)).Generate();

        // Act
        bool isSealed = boundary.IsSealed;

        // Assert
        Assert.False(isSealed);
    }

}
