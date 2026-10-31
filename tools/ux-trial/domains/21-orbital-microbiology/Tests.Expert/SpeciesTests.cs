namespace OrbitalMicrobiology.Tests.Expert;

public sealed class SpeciesTests {

    [Fact]
    public void Two_species_with_the_same_binomial_name_are_the_same_species() {
        // Arrange
        Species anySubtilis      = Any.Species().WithBinomialName("Bacillus subtilis").Generate();
        Species anyOtherSubtilis = Any.Species().WithBinomialName("Bacillus subtilis").Generate();

        // Act
        bool same = anySubtilis.Equals(anyOtherSubtilis);

        // Assert
        Assert.True(same);
    }

    [Theory]
    [InlineData(20, 0.5)]   // halfway from the minimum to the optimum
    [InlineData(30, 1.0)]   // the optimum
    [InlineData(35, 0.5)]   // halfway from the optimum to the maximum
    [InlineData(10, 0.0)]   // the minimum itself
    [InlineData(45, 0.0)]   // above the maximum
    public void The_temperature_factor_rises_to_the_optimum_and_falls_back_to_the_maximum(double temperature, double expected) {
        // Arrange
        Species anySpecies = Any.Species().WithCardinalTemperatures(minimum: 10, optimum: 30, maximum: 40).Generate();

        // Act
        double factor = anySpecies.TemperatureFactor(temperature);

        // Assert
        Assert.Equal(expected, factor, 12);
    }

    [Fact]
    public void A_species_whose_optimum_lies_below_its_minimum_is_refused() {
        // Arrange
        string     anyName  = Any.StringMatching("[A-Z][a-z]{2,11} [a-z]{3,12}").Generate();
        GrowthRate anyRate  = Any.GrowthRate().WithPerHour(r => r.GreaterThan(0)).Generate();
        double     anyLag   = Any.Double().Between(0, Species.MaxLagHours).Generate();
        double     anyYield = Any.Double().Between(Species.MinYieldCellsPerGram, Species.MaxYieldCellsPerGram).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Species(anyName, anyRate, 30, 20, 40, anyLag, anyYield));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
