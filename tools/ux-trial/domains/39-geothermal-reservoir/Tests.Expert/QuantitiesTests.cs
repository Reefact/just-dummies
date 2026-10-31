using GeothermalReservoir.Domain;

using Xunit;

namespace GeothermalReservoir.Tests.Expert;

public sealed class QuantitiesTests {

    [Fact]
    public void A_pressure_given_in_bars_is_stored_in_pascals() {
        // Arrange
        const double bars = 250;

        // Act
        Pressure pressure = Pressure.FromBars(bars);

        // Assert
        Assert.Equal(25e6, pressure.Pascals, 1e-6);
    }

    [Fact]
    public void A_temperature_given_in_celsius_is_stored_in_kelvin() {
        // Arrange
        const double celsius = 150;

        // Act
        Temperature temperature = Temperature.FromCelsius(celsius);

        // Assert
        Assert.Equal(423.15, temperature.Kelvin, 1e-9);
    }

    [Fact]
    public void No_rock_is_more_than_half_pore_space() {
        // Arrange
        const double moreThanHalf = 0.51;

        // Act
        Exception? refusal = Record.Exception(() => new Porosity(moreThanHalf));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void The_effective_face_permeability_is_dominated_by_the_tighter_rock() {
        // Arrange
        Permeability sandstone = new Permeability(1e-12);
        Permeability shale     = new Permeability(1e-18);

        // Act
        double effective = Permeability.HarmonicMean(sandstone, shale);

        // Assert
        // Twice the tighter rock's permeability, to within a millionth: the sandstone barely counts.
        Assert.Equal(2e-18, effective, 1e-23);
    }

}
