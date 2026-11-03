using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests;

public sealed class PredictionTests {

    private static Prediction PredictionOf(Species species, double madeAtHours, double expectedCellsPerMillilitre, double relativeUncertainty) {
        return Any.Prediction()
                  .WithSpecies(species)
                  .WithMadeAtHours(madeAtHours)
                  .WithHorizonHours(2)
                  .WithExpected(new Concentration(expectedCellsPerMillilitre))
                  .WithRelativeUncertainty(relativeUncertainty)
                  .Generate();
    }

    private static Observation ObservationOf(Species species, double elapsedHours, double cellsPerMillilitre, double relativeUncertainty) {
        return Any.Observation()
                  .WithSpecies(species)
                  .WithElapsedHours(elapsedHours)
                  .WithMeasured(new Concentration(cellsPerMillilitre))
                  .WithRelativeUncertainty(relativeUncertainty)
                  .WithMethod(ObservationMethod.PlateCount)
                  .Generate();
    }

    [Fact]
    public void A_prediction_targets_the_moment_it_was_made_plus_its_horizon() {
        // Arrange
        Species       anySpecies = Any.Species().Generate();
        Concentration expected   = new Concentration(1e6);

        // Act
        Prediction prediction = new Prediction(anySpecies, 10, 5, expected, 0.2);

        // Assert
        Assert.Same(anySpecies, prediction.Species);
        Assert.Equal(10, prediction.MadeAtHours);
        Assert.Equal(5, prediction.HorizonHours);
        Assert.Equal(15, prediction.TargetHours);
        Assert.Same(expected, prediction.Expected);
        Assert.Equal(0.2, prediction.RelativeUncertainty);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(720)]
    public void A_horizon_up_to_thirty_days_is_accepted(double horizonHours) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        Prediction prediction = new Prediction(anySpecies, 0, horizonHours, anyConcentration, 0.5);

        // Assert
        Assert.Equal(horizonHours, prediction.HorizonHours);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(720.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_horizon_outside_zero_exclusive_to_thirty_days_is_refused(double horizonHours) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Prediction(anySpecies, 0, horizonHours, anyConcentration, 0.5));

        // Assert
        Assert.Equal("horizonHours", refusal.ParamName);
    }

    [Fact]
    public void A_prediction_cannot_be_made_before_the_start() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Prediction(anySpecies, -0.5, 1, anyConcentration, 0.5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void A_prediction_uncertainty_outside_zero_exclusive_to_one_is_refused(double relativeUncertainty) {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Prediction(anySpecies, 0, 1, anyConcentration, relativeUncertainty));
    }

    [Fact]
    public void An_observation_within_one_combined_sigma_confirms_the_prediction() {
        // Arrange: expected 1e6 +/- 10 % (1e5); measured 1.05e6 +/- 5 % (5.25e4); combined sigma = sqrt(1e10 + 2.76e9) = 1.13e5 > 5e4 apart.
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = PredictionOf(anySpecies, 10, 1e6, 0.1);
        Observation observation = ObservationOf(anySpecies, 12, 1.05e6, 0.05);

        // Act
        bool confirmed = prediction.IsConfirmedBy(observation);

        // Assert
        Assert.True(confirmed);
    }

    [Fact]
    public void An_observation_beyond_one_combined_sigma_does_not_confirm_the_prediction() {
        // Arrange: 5e5 apart, against a combined sigma of sqrt(1e10 + (7.5e4)^2) = 1.25e5.
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = PredictionOf(anySpecies, 10, 1e6, 0.1);
        Observation observation = ObservationOf(anySpecies, 12, 1.5e6, 0.05);

        // Act
        bool confirmed = prediction.IsConfirmedBy(observation);

        // Assert
        Assert.False(confirmed);
    }

    [Fact]
    public void Confirmation_does_not_depend_on_which_side_the_measure_falls() {
        // Arrange
        Species     anySpecies = Any.Species().Generate();
        Prediction  prediction = PredictionOf(anySpecies, 10, 1e6, 0.1);
        Observation above      = ObservationOf(anySpecies, 12, 1.05e6, 0.05);
        Observation below      = ObservationOf(anySpecies, 12, 0.95e6, 0.05);

        // Assert
        Assert.True(prediction.IsConfirmedBy(above));
        Assert.True(prediction.IsConfirmedBy(below));
    }

    [Theory]
    [InlineData(11.5)]
    [InlineData(12)]
    [InlineData(12.5)]
    public void An_observation_within_half_an_hour_of_the_target_can_be_compared(double observationHours) {
        // Arrange: the target is at 10 + 2 = 12 h.
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = PredictionOf(anySpecies, 10, 1e6, 0.1);
        Observation observation = ObservationOf(anySpecies, observationHours, 1e6, 0.05);

        // Act / Assert
        Assert.True(prediction.IsConfirmedBy(observation));
    }

    [Theory]
    [InlineData(11.49)]
    [InlineData(12.51)]
    [InlineData(10)]
    public void An_observation_taken_far_from_the_target_cannot_be_compared(double observationHours) {
        // Arrange
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = PredictionOf(anySpecies, 10, 1e6, 0.1);
        Observation observation = ObservationOf(anySpecies, observationHours, 1e6, 0.05);

        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => prediction.IsConfirmedBy(observation));

        // Assert
        Assert.Equal("observation", refusal.ParamName);
    }

    [Fact]
    public void An_observation_of_another_species_cannot_be_compared() {
        // Arrange
        Species     anySpecies   = Any.Species().Generate();
        Species     anotherOne   = Samples.Sibling(anySpecies);
        Prediction  prediction   = PredictionOf(anySpecies, 10, 1e6, 0.1);
        Observation observation  = ObservationOf(anotherOne, 12, 1e6, 0.05);

        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => prediction.IsConfirmedBy(observation));

        // Assert
        Assert.Equal("observation", refusal.ParamName);
    }

    [Fact]
    public void A_prediction_is_confirmed_by_an_observation_of_the_same_named_species_even_if_it_is_another_instance() {
        // Arrange
        Species     anySpecies = Any.Species().Generate();
        Species     sameName   = Any.Species().WithBinomialName(anySpecies.BinomialName).Generate();
        Prediction  prediction = PredictionOf(anySpecies, 10, 1e6, 0.1);
        Observation observation = ObservationOf(sameName, 12, 1e6, 0.05);

        // Act / Assert
        Assert.True(prediction.IsConfirmedBy(observation));
    }

    [Fact]
    public void Confirmation_needs_an_observation() {
        // Arrange
        Prediction anyPrediction = Any.Prediction().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyPrediction.IsConfirmedBy(null!));
    }

}
