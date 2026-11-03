using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests;

public sealed class EstimatedStateTests {

    [Fact]
    public void An_estimate_keeps_what_it_was_given() {
        // Arrange
        Species       anySpecies    = Any.Species().Generate();
        Concentration concentration = new Concentration(3e5);

        // Act
        EstimatedState estimate = new EstimatedState(anySpecies, 8.5, concentration, 0.25, GrowthPhase.Stationary, 4);

        // Assert
        Assert.Same(anySpecies, estimate.Species);
        Assert.Equal(8.5, estimate.ElapsedHours);
        Assert.Same(concentration, estimate.Concentration);
        Assert.Equal(0.25, estimate.RelativeUncertainty);
        Assert.Equal(GrowthPhase.Stationary, estimate.Phase);
        Assert.Equal(4, estimate.ObservationCount);
    }

    [Fact]
    public void An_estimate_without_observation_is_pure_model() {
        // Arrange
        EstimatedState estimate = Any.EstimatedState().WithObservationCount(0).Generate();

        // Assert
        Assert.True(estimate.IsModelOnly);
    }

    [Fact]
    public void An_estimate_that_absorbed_an_observation_is_no_longer_pure_model() {
        // Arrange
        EstimatedState estimate = Any.EstimatedState().WithObservationCount(c => c.Positive()).Generate();

        // Assert
        Assert.False(estimate.IsModelOnly);
    }

    [Fact]
    public void A_negative_observation_count_is_refused() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new EstimatedState(anySpecies, 1, anyConcentration, 0.5, GrowthPhase.Lag, -1));

        // Assert
        Assert.Equal("observationCount", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    public void An_estimate_cannot_lie_before_the_start(double elapsedHours) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EstimatedState(anySpecies, elapsedHours, anyConcentration, 0.5, GrowthPhase.Lag, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    public void An_estimate_uncertainty_outside_zero_exclusive_to_one_is_refused(double relativeUncertainty) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EstimatedState(anySpecies, 1, anyConcentration, relativeUncertainty, GrowthPhase.Lag, 0));
    }

    [Fact]
    public void An_unknown_phase_is_refused() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new EstimatedState(anySpecies, 1, anyConcentration, 0.5, (GrowthPhase)42, 0));

        // Assert
        Assert.Equal("phase", refusal.ParamName);
    }

    [Fact]
    public void An_estimate_needs_a_species_and_a_concentration() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new EstimatedState(null!, 1, anyConcentration, 0.5, GrowthPhase.Lag, 0));
        Assert.Throws<ArgumentNullException>(() => new EstimatedState(anySpecies, 1, null!, 0.5, GrowthPhase.Lag, 0));
    }

}
