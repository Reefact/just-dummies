using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.ValueObjects;

public sealed class EstimatedStateTests {

    [Fact]
    public void An_estimate_made_before_any_observation_is_model_only() {
        // Arrange
        EstimatedState state = Any.EstimatedState().WithObservationCount(0).Generate();

        // Assert
        Assert.True(state.IsModelOnly);
    }

    [Fact]
    public void An_estimate_that_absorbed_an_observation_is_not_model_only() {
        // Arrange
        EstimatedState state = Any.EstimatedState().WithObservationCount(count => count.Positive()).Generate();

        // Assert
        Assert.False(state.IsModelOnly);
    }

    [Fact]
    public void An_estimate_keeps_what_it_was_given() {
        // Arrange
        Species       anySpecies    = Any.Species().Generate();
        Concentration concentration = new Concentration(3e5);

        // Act
        EstimatedState state = new EstimatedState(anySpecies, 36d, concentration, 0.3, GrowthPhase.Stationary, 4);

        // Assert
        Assert.Same(anySpecies, state.Species);
        Assert.Equal(36d, state.ElapsedHours);
        Assert.Same(concentration, state.Concentration);
        Assert.Equal(0.3, state.RelativeUncertainty);
        Assert.Equal(GrowthPhase.Stationary, state.Phase);
        Assert.Equal(4, state.ObservationCount);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_estimate_is_made_at_a_non_negative_finite_time(double elapsedHours) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.EstimatedState().WithElapsedHours(elapsedHours).Generate());
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void An_estimate_has_a_relative_uncertainty_in_zero_exclusive_to_one(double relativeUncertainty) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.EstimatedState().WithRelativeUncertainty(relativeUncertainty).Generate());
    }

    [Fact]
    public void An_estimate_cannot_have_absorbed_a_negative_number_of_observations() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.EstimatedState().WithObservationCount(-1).Generate());
    }

    [Fact]
    public void A_phase_the_enumeration_does_not_declare_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.EstimatedState().WithPhase((GrowthPhase)9).Generate());
    }

    [Fact]
    public void An_estimate_needs_a_species_and_a_concentration() {
        Assert.Throws<ArgumentNullException>(() => Any.EstimatedState().WithSpecies((Species)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.EstimatedState().WithConcentration((Concentration)null!).Generate());
    }

}
