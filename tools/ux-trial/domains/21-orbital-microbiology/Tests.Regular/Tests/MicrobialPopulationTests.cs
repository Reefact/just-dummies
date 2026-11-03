using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests;

public sealed class MicrobialPopulationTests {

    [Fact]
    public void A_population_keeps_what_it_was_given() {
        // Arrange
        Species       anySpecies    = Any.Species().Generate();
        Concentration concentration = new Concentration(4e3);

        // Act
        MicrobialPopulation population = new MicrobialPopulation(anySpecies, concentration, 0.05, 2.5);

        // Assert
        Assert.Same(anySpecies, population.Species);
        Assert.Same(concentration, population.Concentration);
        Assert.Equal(0.05, population.RelativeUncertainty);
        Assert.Equal(2.5, population.AgeHours);
    }

    [Theory]
    [InlineData(0, GrowthPhase.Lag)]
    [InlineData(4.999, GrowthPhase.Lag)]
    [InlineData(5, GrowthPhase.Exponential)]
    [InlineData(100, GrowthPhase.Exponential)]
    public void A_population_is_in_lag_until_the_species_lag_has_elapsed(double ageHours, GrowthPhase expectedPhase) {
        // Arrange
        Species       species          = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 5, yieldCellsPerGram: 1e10);
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        MicrobialPopulation population = new MicrobialPopulation(species, anyConcentration, 0.5, ageHours);

        // Assert
        Assert.Equal(expectedPhase, population.Phase);
    }

    [Fact]
    public void A_species_without_lag_starts_exponential() {
        // Arrange
        Species       species          = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        MicrobialPopulation population = new MicrobialPopulation(species, anyConcentration, 0.5, 0);

        // Assert
        Assert.Equal(GrowthPhase.Exponential, population.Phase);
    }

    [Fact]
    public void A_population_without_cells_is_extinct() {
        // Arrange
        Species anySpecies = Any.Species().Generate();

        // Act
        MicrobialPopulation population = new MicrobialPopulation(anySpecies, Concentration.Zero, 0.5, 1);

        // Assert
        Assert.True(population.IsExtinct);
    }

    [Fact]
    public void A_population_with_cells_is_not_extinct() {
        // Arrange
        MicrobialPopulation population = Any.MicrobialPopulation().WithConcentration(c => c.WithCellsPerMillilitre(x => x.Positive())).Generate();

        // Assert
        Assert.False(population.IsExtinct);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    public void A_population_uncertainty_outside_zero_exclusive_to_one_is_refused(double relativeUncertainty) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MicrobialPopulation(anySpecies, anyConcentration, relativeUncertainty, 0));

        // Assert
        Assert.Equal("relativeUncertainty", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_population_cannot_have_a_negative_or_infinite_age(double ageHours) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MicrobialPopulation(anySpecies, anyConcentration, 0.5, ageHours));

        // Assert
        Assert.Equal("ageHours", refusal.ParamName);
    }

    [Fact]
    public void Generations_are_counted_from_the_given_inoculum() {
        // Arrange
        MicrobialPopulation population = Any.MicrobialPopulation().WithConcentration(new Concentration(800)).Generate();

        // Act
        double generations = population.GenerationsSince(new Concentration(100));

        // Assert
        Assert.Equal(3, generations, precision: 12);
    }

    [Fact]
    public void Generations_need_an_inoculum() {
        // Arrange
        MicrobialPopulation anyPopulation = Any.MicrobialPopulation().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyPopulation.GenerationsSince(null!));
    }

    [Fact]
    public void A_population_needs_a_species_and_a_concentration() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new MicrobialPopulation(null!, anyConcentration, 0.5, 0));
        Assert.Throws<ArgumentNullException>(() => new MicrobialPopulation(anySpecies, null!, 0.5, 0));
    }

}
