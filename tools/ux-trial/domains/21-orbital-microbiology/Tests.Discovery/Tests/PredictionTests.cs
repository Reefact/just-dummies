using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class PredictionTests {

    private static Prediction Build(Species species, double madeAt = 100, double horizon = 20, double expected = 1e6, double uncertainty = 0.1) {
        return new Prediction(species, madeAt, horizon, new Concentration(expected), uncertainty);
    }

    private static Observation ObservationOf(Species species, double elapsed, double measured, double uncertainty) {
        return new Observation(Any.CultureId().Generate(), species, elapsed, new Concentration(measured), uncertainty, ObservationMethod.PlateCount);
    }

    [Fact]
    public void A_prediction_targets_the_moment_it_was_made_plus_its_horizon() {
        // Arrange
        Species anySpecies = Any.Species().Generate();

        // Act
        Prediction prediction = Build(anySpecies, madeAt: 100, horizon: 20);

        // Assert
        Assert.Equal(120, prediction.TargetHours);
        Assert.Equal(100, prediction.MadeAtHours);
        Assert.Equal(20, prediction.HorizonHours);
    }

    [Fact]
    public void An_arbitrary_prediction_is_a_well_formed_one() {
        // Arrange / Act
        Prediction anyPrediction = Any.Prediction().Generate();

        // Assert
        Assert.InRange(anyPrediction.HorizonHours, double.Epsilon, Prediction.MaxHorizonHours);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(720.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_horizon_outside_zero_exclusive_to_seven_hundred_twenty_hours_is_refused(double horizon) {
        // Arrange
        Species anySpecies = Any.Species().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(anySpecies, horizon: horizon));
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(720.0)]
    public void The_extreme_horizons_are_accepted(double horizon) {
        // Arrange
        Species anySpecies = Any.Species().Generate();

        // Act
        Prediction prediction = Build(anySpecies, horizon: horizon);

        // Assert
        Assert.Equal(horizon, prediction.HorizonHours);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void A_prediction_made_before_time_zero_or_at_no_time_is_refused(double madeAt) {
        // Arrange
        Species anySpecies = Any.Species().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(anySpecies, madeAt: madeAt));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    public void An_uncertainty_outside_zero_exclusive_to_one_is_refused(double uncertainty) {
        // Arrange
        Species anySpecies = Any.Species().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(anySpecies, uncertainty: uncertainty));
    }

    [Fact]
    public void A_prediction_of_nobody_or_of_nothing_is_refused() {
        // Arrange
        Species       anySpecies       = Any.Species().Generate();
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Prediction(null!, 0, 10, anyConcentration, 0.1));
        Assert.Throws<ArgumentNullException>(() => new Prediction(anySpecies, 0, 10, null!, 0.1));
    }

    [Fact]
    public void An_observation_inside_the_combined_one_sigma_confirms_the_forecast() {
        // Arrange: tolerance = sqrt((1e6 * 0.1)^2 + (1.1e6 * 0.05)^2) = about 1.14e5, the gap is 1e5
        Species    anySpecies  = Any.Species().Generate();
        Prediction prediction  = Build(anySpecies, madeAt: 100, horizon: 20, expected: 1e6, uncertainty: 0.1);
        Observation observation = ObservationOf(anySpecies, elapsed: 120, measured: 1.1e6, uncertainty: 0.05);

        // Act
        bool confirmed = prediction.IsConfirmedBy(observation);

        // Assert
        Assert.True(confirmed);
    }

    [Fact]
    public void An_observation_outside_the_combined_one_sigma_does_not_confirm_the_forecast() {
        // Arrange: tolerance = sqrt(1e10 + 3.6e9) = about 1.17e5, the gap is 2e5
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = Build(anySpecies, madeAt: 100, horizon: 20, expected: 1e6, uncertainty: 0.1);
        Observation observation = ObservationOf(anySpecies, elapsed: 120, measured: 1.2e6, uncertainty: 0.05);

        // Act / Assert
        Assert.False(prediction.IsConfirmedBy(observation));
    }

    [Fact]
    public void An_observation_below_the_forecast_is_judged_like_one_above_it() {
        // Arrange
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = Build(anySpecies, madeAt: 100, horizon: 20, expected: 1e6, uncertainty: 0.1);
        Observation inside      = ObservationOf(anySpecies, elapsed: 120, measured: 0.9e6, uncertainty: 0.05);
        Observation outside     = ObservationOf(anySpecies, elapsed: 120, measured: 0.8e6, uncertainty: 0.05);

        // Act / Assert
        Assert.True(prediction.IsConfirmedBy(inside));
        Assert.False(prediction.IsConfirmedBy(outside));
    }

    [Fact]
    public void An_exact_match_is_confirmed_whatever_the_uncertainties() {
        // Arrange
        Species     anySpecies  = Any.Species().Generate();
        double      anyUncertainty = Any.Double().Positive().LessThanOrEqualTo(1).Generate();
        Prediction  prediction  = Build(anySpecies, madeAt: 100, horizon: 20, expected: 1e6, uncertainty: anyUncertainty);
        Observation observation = ObservationOf(anySpecies, elapsed: 120, measured: 1e6, uncertainty: anyUncertainty);

        // Act / Assert
        Assert.True(prediction.IsConfirmedBy(observation));
    }

    [Fact]
    public void An_observation_half_an_hour_from_the_target_is_still_comparable() {
        // Arrange
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = Build(anySpecies, madeAt: 100, horizon: 20, expected: 1e6, uncertainty: 0.1);
        Observation early       = ObservationOf(anySpecies, elapsed: 119.5, measured: 1e6, uncertainty: 0.1);
        Observation late        = ObservationOf(anySpecies, elapsed: 120.5, measured: 1e6, uncertainty: 0.1);

        // Act / Assert
        Assert.True(prediction.IsConfirmedBy(early));
        Assert.True(prediction.IsConfirmedBy(late));
    }

    [Theory]
    [InlineData(119.49)]
    [InlineData(120.51)]
    [InlineData(0.0)]
    public void An_observation_taken_more_than_half_an_hour_from_the_target_is_refused(double elapsed) {
        // Arrange
        Species     anySpecies  = Any.Species().Generate();
        Prediction  prediction  = Build(anySpecies, madeAt: 100, horizon: 20);
        Observation observation = ObservationOf(anySpecies, elapsed: elapsed, measured: 1e6, uncertainty: 0.1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => prediction.IsConfirmedBy(observation));

        // Assert
        Assert.Equal("observation", refusal.ParamName);
    }

    [Fact]
    public void An_observation_of_another_species_cannot_confirm_a_forecast() {
        // Arrange
        Species     anySpecies      = Any.Species().Generate();
        Species     anyOtherSpecies = Any.Species().WithBinomialName(n => n.DifferentFrom(anySpecies.BinomialName)).Generate();
        Prediction  prediction      = Build(anySpecies, madeAt: 100, horizon: 20);
        Observation observation     = ObservationOf(anyOtherSpecies, elapsed: 120, measured: 1e6, uncertainty: 0.1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => prediction.IsConfirmedBy(observation));

        // Assert
        Assert.Contains(anyOtherSpecies.BinomialName, refusal.Message);
    }

    [Fact]
    public void Nothing_confirms_a_forecast() {
        // Arrange
        Prediction anyPrediction = Any.Prediction().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyPrediction.IsConfirmedBy(null!));
    }

}
