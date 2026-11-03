using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class EstimatedStateTests {

    [Fact]
    public void An_estimate_keeps_what_it_was_given() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        EstimatedState state = new EstimatedState(anySpecies, 48, anyConcentration, 0.3, GrowthPhase.Stationary, 4);

        // Assert
        Assert.Same(anySpecies, state.Species);
        Assert.Same(anyConcentration, state.Concentration);
        Assert.Equal(48, state.ElapsedHours);
        Assert.Equal(0.3, state.RelativeUncertainty);
        Assert.Equal(GrowthPhase.Stationary, state.Phase);
        Assert.Equal(4, state.ObservationCount);
    }

    [Fact]
    public void An_estimate_that_absorbed_no_observation_is_model_only() {
        // Arrange
        EstimatedState anyState = Any.EstimatedState().WithObservationCount(0).Generate();

        // Act / Assert
        Assert.True(anyState.IsModelOnly);
    }

    [Fact]
    public void An_estimate_that_absorbed_an_observation_is_not_model_only() {
        // Arrange
        EstimatedState anyState = Any.EstimatedState().WithObservationCount(c => c.GreaterThanOrEqualTo(1)).Generate();

        // Act / Assert
        Assert.False(anyState.IsModelOnly);
    }

    [Fact]
    public void An_arbitrary_estimate_is_a_well_formed_one() {
        // Arrange / Act
        EstimatedState anyState = Any.EstimatedState().Generate();

        // Assert
        Assert.True(anyState.ObservationCount >= 0);
        Assert.Equal(anyState.ObservationCount == 0, anyState.IsModelOnly);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_negative_observation_count_is_refused(int count) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EstimatedState(anySpecies, 1, anyConcentration, 0.1, GrowthPhase.Lag, count));
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    public void An_elapsed_time_that_is_negative_or_not_a_number_is_refused(double elapsed) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EstimatedState(anySpecies, elapsed, anyConcentration, 0.1, GrowthPhase.Lag, 0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void An_uncertainty_outside_zero_exclusive_to_one_is_refused(double uncertainty) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EstimatedState(anySpecies, 1, anyConcentration, uncertainty, GrowthPhase.Lag, 0));
    }

    [Fact]
    public void An_unknown_phase_is_refused() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EstimatedState(anySpecies, 1, anyConcentration, 0.1, (GrowthPhase)42, 0));
    }

    [Fact]
    public void An_estimate_of_nobody_or_of_no_concentration_is_refused() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new EstimatedState(null!, 1, anyConcentration, 0.1, GrowthPhase.Lag, 0));
        Assert.Throws<ArgumentNullException>(() => new EstimatedState(anySpecies, 1, null!, 0.1, GrowthPhase.Lag, 0));
    }

}
