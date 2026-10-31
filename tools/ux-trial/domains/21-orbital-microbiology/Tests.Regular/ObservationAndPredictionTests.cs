using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Regular;

public sealed class ObservationAndPredictionTests {

    [Fact]
    public void An_optical_density_reading_cannot_claim_better_than_10_percent() {
        // Arrange
        AnyObservation anyReading = Arbitrary.Observation().WithMethod(ObservationMethod.OpticalDensity).WithRelativeUncertainty(0.05);

        // Act
        Exception? exception = Record.Exception(() => anyReading.Generate());

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void An_observation_spans_one_sigma_either_side_of_the_measurement() {
        // Arrange
        Observation anyObservation = Arbitrary.Observation()
                                              .WithMeasured(new Concentration(1_000_000))
                                              .WithRelativeUncertainty(0.2)
                                              .Generate();

        // Act
        (double lower, double upper) = (anyObservation.LowerBoundCellsPerMillilitre, anyObservation.UpperBoundCellsPerMillilitre);

        // Assert
        Assert.Equal(800_000, lower, 6);
        Assert.Equal(1_200_000, upper, 6);
    }

    [Theory]
    [InlineData(1_050_000, true)]
    [InlineData(2_000_000, false)]
    public void A_prediction_is_confirmed_by_a_measurement_within_combined_one_sigma(double measured, bool expected) {
        // Arrange
        Species    anySpecies = Arbitrary.Species().Generate();
        Prediction prediction = new Prediction(anySpecies, 10, 5, new Concentration(1_000_000), 0.1);
        Observation anyObservationAtTarget = Arbitrary.Observation()
                                                      .WithSpecies(anySpecies)
                                                      .WithElapsedHours(15)
                                                      .WithMeasured(new Concentration(measured))
                                                      .WithRelativeUncertainty(0.1)
                                                      .Generate();

        // Act
        bool confirmed = prediction.IsConfirmedBy(anyObservationAtTarget);

        // Assert
        Assert.Equal(expected, confirmed);
    }

    [Fact]
    public void A_prediction_is_not_judged_by_a_measurement_far_from_its_target_time() {
        // Arrange
        Species     anySpecies    = Arbitrary.Species().Generate();
        Prediction  anyPrediction = new Prediction(anySpecies, 10, 5, Arbitrary.Concentration().Generate(), 0.1);
        Observation anyTooEarly   = Arbitrary.Observation().WithSpecies(anySpecies).WithElapsedHours(12).Generate();

        // Act
        Exception? exception = Record.Exception(() => anyPrediction.IsConfirmedBy(anyTooEarly));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

}
