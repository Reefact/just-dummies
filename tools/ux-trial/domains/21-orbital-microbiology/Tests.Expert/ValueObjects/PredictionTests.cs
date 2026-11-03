using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.ValueObjects;

public sealed class PredictionTests {

    [Fact]
    public void The_target_is_the_moment_it_was_made_plus_the_horizon() {
        // Arrange
        Prediction prediction = Any.Prediction().WithMadeAtHours(2.5).WithHorizonHours(10d).Generate();

        // Assert
        Assert.Equal(12.5, prediction.TargetHours);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(720d)]
    public void A_horizon_up_to_720_hours_is_kept(double horizonHours) {
        Assert.Equal(horizonHours, Any.Prediction().WithHorizonHours(horizonHours).Generate().HorizonHours);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(720.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_horizon_that_is_not_in_zero_exclusive_to_720_hours_is_refused(double horizonHours) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Prediction().WithHorizonHours(horizonHours).Generate());
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    public void A_prediction_is_made_at_a_non_negative_finite_time(double madeAtHours) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Prediction().WithMadeAtHours(madeAtHours).Generate());
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1.01)]
    public void A_prediction_has_a_relative_uncertainty_in_zero_exclusive_to_one(double relativeUncertainty) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Prediction().WithRelativeUncertainty(relativeUncertainty).Generate());
    }

    [Fact]
    public void A_prediction_needs_a_species_and_an_expected_concentration() {
        Assert.Throws<ArgumentNullException>(() => Any.Prediction().WithSpecies((Species)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Prediction().WithExpected((Concentration)null!).Generate());
    }

    // ---- confirmation ----------------------------------------------------------------------------------------

    // A forecast of 1e6 cells/mL at hour 10 with 10 % uncertainty: sigma 1e5.
    private static AnyPrediction AnyForecastOfAMillionAtHour10(Species species) {
        return Any.Prediction()
                  .WithSpecies(species)
                  .WithMadeAtHours(2d)
                  .WithHorizonHours(8d)
                  .WithExpected(new Concentration(1e6))
                  .WithRelativeUncertainty(0.1);
    }

    private static AnyObservation AnyPlateCountAtHour10(Species species) {
        return Any.Observation()
                  .WithSpecies(species)
                  .WithElapsedHours(10d)
                  .WithMethod(ObservationMethod.PlateCount);
    }

    [Fact]
    public void An_observation_equal_to_the_forecast_confirms_it() {
        // Arrange
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = AnyForecastOfAMillionAtHour10(anySpecies).Generate();
        Observation observation = AnyPlateCountAtHour10(anySpecies).WithMeasured(new Concentration(1e6)).WithRelativeUncertainty(0.1).Generate();

        // Assert
        Assert.True(prediction.IsConfirmedBy(observation));
    }

    // Combined sigma = sqrt((1e6 x 0.1)^2 + (measured x 0.1)^2): about 1.41e5 at 1e6, 1.45e5 at 1.05e6, 1.8e5 at 1.5e6.
    [Theory]
    [InlineData(1.05e6, true)]
    [InlineData(9.5e5, true)]
    [InlineData(1.1e6, true)]
    [InlineData(1.2e6, false)]
    [InlineData(1.5e6, false)]
    [InlineData(5e5, false)]
    public void An_observation_confirms_a_forecast_within_the_combined_one_sigma_and_only_there(double measured, bool expectedConfirmed) {
        // Arrange
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = AnyForecastOfAMillionAtHour10(anySpecies).Generate();
        Observation observation = AnyPlateCountAtHour10(anySpecies).WithMeasured(new Concentration(measured)).WithRelativeUncertainty(0.1).Generate();

        // Assert
        Assert.Equal(expectedConfirmed, prediction.IsConfirmedBy(observation));
    }

    [Fact]
    public void A_vaguer_observation_confirms_what_a_sharper_one_would_not() {
        // Arrange: 1.2e6 is 2e5 away from the forecast; sigma is about 1.7e5 at 5 % and 3.3e5 at 30 %
        Species     anySpecies = Any.Species().Generate();
        Prediction  prediction = AnyForecastOfAMillionAtHour10(anySpecies).Generate();
        Observation sharp      = AnyPlateCountAtHour10(anySpecies).WithMeasured(new Concentration(1.2e6)).WithRelativeUncertainty(0.05).Generate();
        Observation vague      = AnyPlateCountAtHour10(anySpecies).WithMeasured(new Concentration(1.2e6)).WithRelativeUncertainty(0.3).Generate();

        // Assert
        Assert.False(prediction.IsConfirmedBy(sharp));
        Assert.True(prediction.IsConfirmedBy(vague));
    }

    [Theory]
    [InlineData(9.5)]
    [InlineData(10.5)]
    [InlineData(10d)]
    public void An_observation_taken_within_half_an_hour_of_the_target_is_compared(double takenAtHours) {
        // Arrange
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = AnyForecastOfAMillionAtHour10(anySpecies).Generate();
        Observation observation = AnyPlateCountAtHour10(anySpecies).WithElapsedHours(takenAtHours).WithMeasured(new Concentration(1e6)).Generate();

        // Assert
        Assert.True(prediction.IsConfirmedBy(observation));
    }

    [Theory]
    [InlineData(9.49)]
    [InlineData(10.51)]
    [InlineData(0d)]
    public void An_observation_taken_further_than_half_an_hour_from_the_target_cannot_be_compared(double takenAtHours) {
        // Arrange
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = AnyForecastOfAMillionAtHour10(anySpecies).Generate();
        Observation observation = AnyPlateCountAtHour10(anySpecies).WithElapsedHours(takenAtHours).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => prediction.IsConfirmedBy(observation));
    }

    [Fact]
    public void An_observation_of_another_species_cannot_be_compared() {
        // Arrange
        Species     anySpecies      = Any.Species().Generate();
        Species     anyOtherSpecies = Any.Species().WithBinomialName(name => name.DifferentFrom(anySpecies.BinomialName)).Generate();
        Prediction  prediction      = AnyForecastOfAMillionAtHour10(anySpecies).Generate();
        Observation observation     = AnyPlateCountAtHour10(anyOtherSpecies).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => prediction.IsConfirmedBy(observation));
    }

    [Fact]
    public void Nothing_is_compared_to_a_forecast() {
        // Arrange
        Prediction anyPrediction = Any.Prediction().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyPrediction.IsConfirmedBy(null!));
    }

}
