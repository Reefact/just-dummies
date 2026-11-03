using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class MicrobialPopulationTests {

    [Fact]
    public void A_population_keeps_what_it_was_given() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        MicrobialPopulation population = new MicrobialPopulation(anySpecies, anyConcentration, 0.05, 3);

        // Assert
        Assert.Same(anySpecies, population.Species);
        Assert.Same(anyConcentration, population.Concentration);
        Assert.Equal(0.05, population.RelativeUncertainty);
        Assert.Equal(3, population.AgeHours);
    }

    [Fact]
    public void A_population_younger_than_the_lag_of_its_species_is_in_lag() {
        // Arrange
        Species anySpecies = Any.Species().WithLagHours(10).Generate();

        // Act
        MicrobialPopulation population = Any.MicrobialPopulation().WithSpecies(anySpecies).WithAgeHours(9.99).Generate();

        // Assert
        Assert.Equal(GrowthPhase.Lag, population.Phase);
    }

    [Theory]
    [InlineData(10.0)]
    [InlineData(10.01)]
    [InlineData(500.0)]
    public void A_population_as_old_as_the_lag_of_its_species_or_older_is_exponential(double age) {
        // Arrange
        Species anySpecies = Any.Species().WithLagHours(10).Generate();

        // Act
        MicrobialPopulation population = Any.MicrobialPopulation().WithSpecies(anySpecies).WithAgeHours(age).Generate();

        // Assert
        Assert.Equal(GrowthPhase.Exponential, population.Phase);
    }

    [Fact]
    public void A_species_without_lag_is_exponential_from_the_start() {
        // Arrange
        Species anySpecies = Any.Species().WithLagHours(0).Generate();

        // Act
        MicrobialPopulation population = Any.MicrobialPopulation().WithSpecies(anySpecies).WithAgeHours(0).Generate();

        // Assert
        Assert.Equal(GrowthPhase.Exponential, population.Phase);
    }

    [Fact]
    public void A_population_with_no_cell_is_extinct() {
        // Arrange
        MicrobialPopulation population = Any.MicrobialPopulation().WithConcentration(Concentration.Zero).Generate();

        // Act / Assert
        Assert.True(population.IsExtinct);
    }

    [Fact]
    public void A_population_with_cells_is_not_extinct() {
        // Arrange
        MicrobialPopulation population = Any.MicrobialPopulation().WithConcentration(c => c.WithCellsPerMillilitre(x => x.Positive())).Generate();

        // Act / Assert
        Assert.False(population.IsExtinct);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void An_uncertainty_outside_zero_exclusive_to_one_is_refused(double uncertainty) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MicrobialPopulation(anySpecies, anyConcentration, uncertainty, 0));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_age_that_is_negative_or_not_finite_is_refused(double age) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MicrobialPopulation(anySpecies, anyConcentration, 0.1, age));
    }

    [Fact]
    public void A_population_of_nobody_or_of_nothing_is_refused() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new MicrobialPopulation(null!, anyConcentration, 0.1, 0));
        Assert.Throws<ArgumentNullException>(() => new MicrobialPopulation(anySpecies, null!, 0.1, 0));
    }

    [Fact]
    public void Eight_times_the_inoculum_is_three_generations() {
        // Arrange
        MicrobialPopulation population = Any.MicrobialPopulation().WithConcentration(new Concentration(8e6)).Generate();

        // Act
        double generations = population.GenerationsSince(new Concentration(1e6));

        // Assert
        Assert.Equal(3.0, generations, precision: 12);
    }

    [Fact]
    public void An_extinct_population_has_no_generations_to_count() {
        // Arrange
        MicrobialPopulation population = Any.MicrobialPopulation().WithConcentration(Concentration.Zero).Generate();
        Concentration       anyInoculum = Scenario.AnyInoculum();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => population.GenerationsSince(anyInoculum));
    }

    [Fact]
    public void Generations_cannot_be_counted_since_nothing() {
        // Arrange
        MicrobialPopulation anyPopulation = Any.MicrobialPopulation().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyPopulation.GenerationsSince(null!));
    }

}
