namespace OrbitalMicrobiology.Tests.Expert;

public sealed class ObservationAndPredictionTests {

    [Fact]
    public void An_optical_density_reading_cannot_claim_better_than_ten_percent() {
        // Arrange
        CultureId     anyCultureId = Any.CultureId().Generate();
        Species       anySpecies   = Any.Species().Generate();
        double        anyElapsed   = Any.Double().GreaterThanOrEqualTo(0).Generate();
        Concentration anyMeasured  = Any.Concentration().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Observation(anyCultureId, anySpecies, anyElapsed, anyMeasured, 0.05, ObservationMethod.OpticalDensity));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Theory]
    [InlineData(ObservationMethod.PlateCount)]
    [InlineData(ObservationMethod.FlowCytometry)]
    public void A_direct_count_may_claim_five_percent(ObservationMethod method) {
        // Arrange
        AnyObservation anyObservation = Any.Observation().WithMethod(method).WithRelativeUncertainty(0.05);

        // Act
        Observation observation = anyObservation.Generate();

        // Assert
        Assert.False(observation.IsIndirect);
    }

    [Fact]
    public void An_observation_spans_one_sigma_either_side_of_the_measurement() {
        // Arrange
        Observation anyObservation = Any.Observation()
                                        .WithMeasured(new Concentration(1_000_000))
                                        .WithRelativeUncertainty(0.10)
                                        .Generate();

        // Act
        double lower = anyObservation.LowerBoundCellsPerMillilitre;
        double upper = anyObservation.UpperBoundCellsPerMillilitre;

        // Assert
        Assert.Equal(900_000, lower, 6);
        Assert.Equal(1_100_000, upper, 6);
    }

    [Fact]
    public void An_observation_within_the_combined_sigma_confirms_the_prediction() {
        // Arrange
        Species    anySpecies = Any.Species().Generate();
        Prediction prediction = new Prediction(anySpecies, madeAtHours: 10, horizonHours: 12, new Concentration(1_000_000), 0.10);
        Observation anyObservationAtTarget = Any.Observation()
                                                .WithSpecies(anySpecies)
                                                .WithElapsedHours(22)
                                                .WithMeasured(new Concentration(1_100_000))
                                                .WithMethod(ObservationMethod.PlateCount)
                                                .WithRelativeUncertainty(0.05)
                                                .Generate();

        // Act
        bool confirmed = prediction.IsConfirmedBy(anyObservationAtTarget);

        // Assert
        Assert.True(confirmed);
    }

    [Fact]
    public void An_observation_beyond_the_combined_sigma_does_not_confirm_the_prediction() {
        // Arrange
        Species    anySpecies = Any.Species().Generate();
        Prediction prediction = new Prediction(anySpecies, madeAtHours: 10, horizonHours: 12, new Concentration(1_000_000), 0.10);
        Observation anyObservationAtTarget = Any.Observation()
                                                .WithSpecies(anySpecies)
                                                .WithElapsedHours(22)
                                                .WithMeasured(new Concentration(1_500_000))
                                                .WithMethod(ObservationMethod.PlateCount)
                                                .WithRelativeUncertainty(0.05)
                                                .Generate();

        // Act
        bool confirmed = prediction.IsConfirmedBy(anyObservationAtTarget);

        // Assert
        Assert.False(confirmed);
    }

    [Fact]
    public void A_prediction_is_not_checked_against_another_species() {
        // Arrange
        List<Species> anyTwoSpecies = Any.ListOf(Any.Species()).WithCount(2).Distinct().Generate();
        Prediction    anyPrediction = new Prediction(anyTwoSpecies[0], madeAtHours: 10, horizonHours: 12, Any.Concentration().Generate(), 0.10);
        Observation   anyOfTheOther = Any.Observation().WithSpecies(anyTwoSpecies[1]).WithElapsedHours(22).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPrediction.IsConfirmedBy(anyOfTheOther));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
