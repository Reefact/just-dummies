using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.ValueObjects;

public sealed class MicrobialPopulationTests {

    [Fact]
    public void A_population_keeps_what_it_was_given() {
        // Arrange
        Species       anySpecies    = Any.Species().Generate();
        Concentration concentration = new Concentration(4e4);

        // Act
        MicrobialPopulation population = new MicrobialPopulation(anySpecies, concentration, 0.15, 7d);

        // Assert
        Assert.Same(anySpecies, population.Species);
        Assert.Same(concentration, population.Concentration);
        Assert.Equal(0.15, population.RelativeUncertainty);
        Assert.Equal(7d, population.AgeHours);
    }

    [Theory]
    [InlineData(0d, GrowthPhase.Lag)]
    [InlineData(5d, GrowthPhase.Lag)]
    [InlineData(9.99, GrowthPhase.Lag)]
    [InlineData(10d, GrowthPhase.Exponential)]
    [InlineData(48d, GrowthPhase.Exponential)]
    public void A_population_is_in_lag_until_it_reaches_the_lag_of_its_species(double ageHours, GrowthPhase expectedPhase) {
        // Arrange
        Species anySpecies = Any.Species().WithLagHours(10d).Generate();

        // Act
        MicrobialPopulation population = Any.MicrobialPopulation().WithSpecies(anySpecies).WithAgeHours(ageHours).Generate();

        // Assert
        Assert.Equal(expectedPhase, population.Phase);
    }

    [Fact]
    public void A_species_without_lag_starts_growing_at_once() {
        // Arrange
        Species anySpecies = Any.Species().WithLagHours(0d).Generate();

        // Act
        MicrobialPopulation population = Any.MicrobialPopulation().WithSpecies(anySpecies).WithAgeHours(0d).Generate();

        // Assert
        Assert.Equal(GrowthPhase.Exponential, population.Phase);
    }

    [Fact]
    public void A_population_of_zero_cells_per_millilitre_is_extinct() {
        // Arrange
        MicrobialPopulation population = Any.MicrobialPopulation().WithConcentration(Concentration.Zero).Generate();

        // Assert
        Assert.True(population.IsExtinct);
    }

    [Fact]
    public void A_population_with_cells_is_not_extinct() {
        // Arrange
        MicrobialPopulation population = Any.MicrobialPopulation().WithConcentration(c => c.WithCellsPerMillilitre(cells => cells.Positive())).Generate();

        // Assert
        Assert.False(population.IsExtinct);
    }

    [Fact]
    public void The_generations_since_an_inoculum_are_the_log2_of_the_fold_change() {
        // Arrange
        MicrobialPopulation population = Any.MicrobialPopulation().WithConcentration(new Concentration(8e6)).Generate();

        // Act
        double generations = population.GenerationsSince(new Concentration(1e6));

        // Assert
        Assert.Equal(3d, generations, precision: 10);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void A_relative_uncertainty_outside_zero_exclusive_to_one_is_refused(double relativeUncertainty) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.MicrobialPopulation().WithRelativeUncertainty(relativeUncertainty).Generate());
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_age_that_is_negative_or_not_finite_is_refused(double ageHours) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.MicrobialPopulation().WithAgeHours(ageHours).Generate());
    }

    [Fact]
    public void A_population_needs_a_species_and_a_concentration() {
        Assert.Throws<ArgumentNullException>(() => Any.MicrobialPopulation().WithSpecies((Species)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.MicrobialPopulation().WithConcentration((Concentration)null!).Generate());
    }

}
