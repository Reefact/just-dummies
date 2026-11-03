using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.Entities;

public sealed class CultureEstimationTests {

    private const double ModelUncertainty = Culture.InitialRelativeUncertainty;

    private static Culture AnyCultureInoculatedWith(Species species, double cellsPerMillilitre) {
        Culture culture = Scenes.AnyControlCulture().Generate();
        culture.Inoculate(species, new Concentration(cellsPerMillilitre));
        return culture;
    }

    private static AnyObservation AnyPlateCountOf(Culture culture, Species species) {
        return Any.Observation()
                  .WithCultureId(culture.Id)
                  .WithSpecies(species)
                  .WithElapsedHours(culture.ElapsedHours)
                  .WithMethod(ObservationMethod.PlateCount);
    }

    // ---- estimate --------------------------------------------------------------------------------------------

    [Fact]
    public void Before_any_observation_the_estimate_is_the_model_alone() {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e6);
        culture.Advance(3d);

        // Act
        EstimatedState state = culture.Estimate(species);

        // Assert
        Assert.True(state.IsModelOnly);
        Assert.Equal(0, state.ObservationCount);
        Assert.Equal(3d, state.ElapsedHours);
        Assert.Equal(species, state.Species);
        Assert.Equal(culture.Populations.Single().Concentration, state.Concentration);
    }

    [Fact]
    public void An_estimate_of_a_species_that_is_not_in_the_culture_is_refused() {
        // Arrange
        Species present = Scenes.AnyGrowingSpecies().Generate();
        Species absent  = Scenes.AnotherSpeciesThan(present);
        Culture culture = AnyCultureInoculatedWith(present, 1e6);

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.Estimate(absent));
    }

    [Fact]
    public void An_estimate_is_asked_for_a_species() {
        // Arrange
        Culture culture = AnyCultureInoculatedWith(Scenes.AnyGrowingSpecies().Generate(), 1e6);

        // Assert
        Assert.Throws<ArgumentNullException>(() => culture.Estimate(null!));
    }

    // ---- recording an observation ----------------------------------------------------------------------------

    [Fact]
    public void An_observation_as_uncertain_as_the_model_pulls_the_estimate_half_way() {
        // Arrange: model 1e6, measure 2e6, both at the same relative uncertainty
        Species     species     = Scenes.AnyGrowingSpecies().Generate();
        Culture     culture     = AnyCultureInoculatedWith(species, 1e6);
        Observation observation = AnyPlateCountOf(culture, species).WithMeasured(new Concentration(2e6)).WithRelativeUncertainty(ModelUncertainty).Generate();

        // Act
        culture.Record(observation);

        // Assert
        EstimatedState state = culture.Estimate(species);
        Assert.Equal(1.5e6, state.Concentration.CellsPerMillilitre, tolerance: 1e-3);
        Assert.Equal(ModelUncertainty / Math.Sqrt(2d), state.RelativeUncertainty, precision: 9);
    }

    [Fact]
    public void An_observation_twice_as_uncertain_as_the_model_weighs_a_quarter_as_much() {
        // Arrange: weights 1 and 1/4: (1 x 1e6 + 0.25 x 2e6) / 1.25 = 1.2e6
        Species     species     = Scenes.AnyGrowingSpecies().Generate();
        Culture     culture     = AnyCultureInoculatedWith(species, 1e6);
        Observation observation = AnyPlateCountOf(culture, species).WithMeasured(new Concentration(2e6)).WithRelativeUncertainty(2 * ModelUncertainty).Generate();

        // Act
        culture.Record(observation);

        // Assert
        EstimatedState state = culture.Estimate(species);
        Assert.Equal(1.2e6, state.Concentration.CellsPerMillilitre, tolerance: 1e-3);
        Assert.Equal(ModelUncertainty / Math.Sqrt(1.25), state.RelativeUncertainty, precision: 9);
    }

    [Fact]
    public void An_observation_that_agrees_with_the_model_leaves_the_estimate_where_it_was_and_makes_it_surer() {
        // Arrange
        Species     species     = Scenes.AnyGrowingSpecies().Generate();
        Culture     culture     = AnyCultureInoculatedWith(species, 1e6);
        Observation observation = AnyPlateCountOf(culture, species).WithMeasured(new Concentration(1e6)).WithRelativeUncertainty(ModelUncertainty).Generate();

        // Act
        culture.Record(observation);

        // Assert
        EstimatedState state = culture.Estimate(species);
        Assert.Equal(1e6, state.Concentration.CellsPerMillilitre, tolerance: 1e-3);
        Assert.True(state.RelativeUncertainty < ModelUncertainty);
    }

    [Fact]
    public void An_observation_taken_later_is_blended_with_the_model_state_of_that_moment() {
        // Arrange: after 10 hours the model is as uncertain as the observation, which claims the double of the model
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e4);
        culture.Advance(10d);
        EstimatedState model = culture.Estimate(species);
        Observation observation = AnyPlateCountOf(culture, species)
                                  .WithMeasured(new Concentration(2 * model.Concentration.CellsPerMillilitre))
                                  .WithRelativeUncertainty(model.RelativeUncertainty)
                                  .Generate();

        // Act
        culture.Record(observation);

        // Assert
        EstimatedState state = culture.Estimate(species);
        Assert.Equal(1.5 * model.Concentration.CellsPerMillilitre, state.Concentration.CellsPerMillilitre, tolerance: model.Concentration.CellsPerMillilitre * 1e-9);
        Assert.Equal(model.RelativeUncertainty / Math.Sqrt(2d), state.RelativeUncertainty, precision: 9);
    }

    [Fact]
    public void A_recorded_observation_is_counted_and_kept() {
        // Arrange
        Species     species     = Scenes.AnyGrowingSpecies().Generate();
        Culture     culture     = AnyCultureInoculatedWith(species, 1e6);
        Observation observation = AnyPlateCountOf(culture, species).Generate();

        // Act
        culture.Record(observation);

        // Assert
        EstimatedState state = culture.Estimate(species);
        Assert.Equal(1, state.ObservationCount);
        Assert.False(state.IsModelOnly);
        Assert.Same(observation, Assert.Single(culture.Observations));
    }

    [Fact]
    public void Observations_are_counted_per_species() {
        // Arrange
        Species observed   = Scenes.AnyGrowingSpecies().Generate();
        Species unobserved = Scenes.AnotherSpeciesThan(observed);
        Culture culture    = AnyCultureInoculatedWith(observed, 1e6);
        culture.Inoculate(unobserved, Any.Concentration().Dilute().Generate());

        // Act
        culture.Record(AnyPlateCountOf(culture, observed).Generate());
        culture.Record(AnyPlateCountOf(culture, observed).Generate());

        // Assert
        Assert.Equal(2, culture.Estimate(observed).ObservationCount);
        Assert.Equal(0, culture.Estimate(unobserved).ObservationCount);
        Assert.True(culture.Estimate(unobserved).IsModelOnly);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(-0.5)]
    [InlineData(0.2)]
    public void An_observation_taken_within_half_an_hour_of_now_is_accepted(double offsetHours) {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e6);
        culture.Advance(10d);
        Observation observation = AnyPlateCountOf(culture, species).WithElapsedHours(10d + offsetHours).Generate();

        // Act
        culture.Record(observation);

        // Assert
        Assert.Equal(1, culture.Estimate(species).ObservationCount);
    }

    [Theory]
    [InlineData(0.51)]
    [InlineData(-0.51)]
    [InlineData(5d)]
    public void An_observation_taken_more_than_half_an_hour_from_now_is_refused(double offsetHours) {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e6);
        culture.Advance(10d);
        Observation observation = AnyPlateCountOf(culture, species).WithElapsedHours(10d + offsetHours).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => culture.Record(observation));
        Assert.Empty(culture.Observations);
    }

    [Fact]
    public void An_observation_of_another_culture_is_refused() {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e6);
        Observation foreign = AnyPlateCountOf(culture, species)
                              .WithCultureId(id => id.WithValue(text => text.DifferentFrom(culture.Id.Value)))
                              .Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => culture.Record(foreign));
        Assert.Empty(culture.Observations);
    }

    [Fact]
    public void An_observation_of_a_species_the_culture_does_not_hold_is_refused() {
        // Arrange
        Species present = Scenes.AnyGrowingSpecies().Generate();
        Species absent  = Scenes.AnotherSpeciesThan(present);
        Culture culture = AnyCultureInoculatedWith(present, 1e6);

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.Record(AnyPlateCountOf(culture, absent).Generate()));
        Assert.Empty(culture.Observations);
    }

    [Fact]
    public void An_observation_is_needed_to_record_one() {
        // Arrange
        Culture culture = AnyCultureInoculatedWith(Scenes.AnyGrowingSpecies().Generate(), 1e6);

        // Assert
        Assert.Throws<ArgumentNullException>(() => culture.Record(null!));
    }

    // ---- prediction ------------------------------------------------------------------------------------------

    [Fact]
    public void A_prediction_is_made_now_for_a_horizon_ahead() {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e4);
        culture.Advance(5d);

        // Act
        Prediction prediction = culture.Predict(species, 24d);

        // Assert
        Assert.Equal(species, prediction.Species);
        Assert.Equal(5d, prediction.MadeAtHours);
        Assert.Equal(24d, prediction.HorizonHours);
        Assert.Equal(29d, prediction.TargetHours);
    }

    [Fact]
    public void Predicting_leaves_the_culture_as_it_was() {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e4);
        culture.Advance(5d);
        EstimatedState before = culture.Estimate(species);
        double         age    = culture.Populations.Single().AgeHours;

        // Act
        culture.Predict(species, 100d);

        // Assert
        EstimatedState after = culture.Estimate(species);
        Assert.Equal(5d, culture.ElapsedHours);
        Assert.Equal(age, culture.Populations.Single().AgeHours);
        Assert.Equal(before.Concentration, after.Concentration);
        Assert.Equal(before.RelativeUncertainty, after.RelativeUncertainty);
        Assert.Equal(before.Phase, after.Phase);
    }

    [Fact]
    public void The_forecast_is_what_the_culture_becomes_when_it_is_really_advanced() {
        // Arrange: two identical cultures, one forecast and one lived
        Species   species  = Scenes.AnyGrowingSpecies().Generate();
        CultureId id       = Any.CultureId().Generate();
        Culture   forecast = Scenes.AnyControlCulture().WithId(id).Generate();
        Culture   lived    = Scenes.AnyControlCulture().WithId(id).WithConditions(forecast.Conditions).Generate();
        forecast.Inoculate(species, new Concentration(1e4));
        lived.Inoculate(species, new Concentration(1e4));
        forecast.Advance(5d);
        lived.Advance(5d);

        // Act
        Prediction prediction = forecast.Predict(species, 24d);
        lived.Advance(24d);

        // Assert
        double actual = lived.Estimate(species).Concentration.CellsPerMillilitre;
        Assert.Equal(actual, prediction.Expected.CellsPerMillilitre, tolerance: actual * 1e-9);
    }

    [Fact]
    public void The_forecast_accounts_for_the_interactions_of_the_culture() {
        // Arrange
        Species   affected   = Scenes.AnyGrowingSpecies().Generate();
        Species   competitor = Scenes.AnotherSpeciesThan(affected);
        CultureId id         = Any.CultureId().Generate();
        Culture   forecast   = Scenes.AnyControlCulture().WithId(id).Generate();
        Culture   lived      = Scenes.AnyControlCulture().WithId(id).WithConditions(forecast.Conditions).Generate();
        foreach (Culture culture in new[] { forecast, lived }) {
            culture.Inoculate(affected, new Concentration(1e4));
            culture.Inoculate(competitor, new Concentration(1e5));
            culture.SetInteraction(affected, competitor, 1.5);
        }

        // Act
        Prediction prediction = forecast.Predict(affected, 30d);
        lived.Advance(30d);

        // Assert
        double actual = lived.Estimate(affected).Concentration.CellsPerMillilitre;
        Assert.Equal(actual, prediction.Expected.CellsPerMillilitre, tolerance: actual * 1e-9);
    }

    [Fact]
    public void A_forecast_is_less_certain_than_the_estimate_it_started_from_by_two_hundredths_per_hour() {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e4);

        // Act
        Prediction prediction = culture.Predict(species, 10d);

        // Assert: 0.05 + 10 x 0.02
        Assert.Equal(0.25, prediction.RelativeUncertainty, precision: 9);
        Assert.True(prediction.RelativeUncertainty >= culture.Estimate(species).RelativeUncertainty);
    }

    [Fact]
    public void A_forecast_made_after_an_observation_starts_from_the_sharper_estimate() {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e4);
        culture.Record(AnyPlateCountOf(culture, species).WithRelativeUncertainty(ModelUncertainty).Generate());
        double sharper = culture.Estimate(species).RelativeUncertainty;

        // Act
        Prediction prediction = culture.Predict(species, 10d);

        // Assert
        Assert.Equal(sharper + 0.2, prediction.RelativeUncertainty, precision: 9);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(720.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_forecast_looks_more_than_nothing_and_at_most_720_hours_ahead(double horizonHours) {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e4);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => culture.Predict(species, horizonHours));
    }

    [Fact]
    public void A_forecast_may_look_a_month_ahead() {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e4);

        // Act
        Prediction prediction = culture.Predict(species, 720d);

        // Assert
        Assert.Equal(720d, prediction.HorizonHours);
        Assert.Equal(1d, prediction.RelativeUncertainty);
    }

    [Fact]
    public void A_forecast_of_a_species_that_is_not_in_the_culture_is_refused() {
        // Arrange
        Species present = Scenes.AnyGrowingSpecies().Generate();
        Species absent  = Scenes.AnotherSpeciesThan(present);
        Culture culture = AnyCultureInoculatedWith(present, 1e4);

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.Predict(absent, 10d));
    }

    [Fact]
    public void A_forecast_is_asked_for_a_species() {
        // Arrange
        Culture culture = AnyCultureInoculatedWith(Scenes.AnyGrowingSpecies().Generate(), 1e4);

        // Assert
        Assert.Throws<ArgumentNullException>(() => culture.Predict(null!, 10d));
    }

    // ---- prediction and observation, end to end --------------------------------------------------------------

    [Fact]
    public void A_forecast_is_confirmed_by_the_observation_of_the_state_it_announced() {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e4);
        Prediction prediction = culture.Predict(species, 24d);
        culture.Advance(24d);
        Observation observation = AnyPlateCountOf(culture, species)
                                  .WithMeasured(culture.Estimate(species).Concentration)
                                  .WithRelativeUncertainty(0.1)
                                  .Generate();

        // Assert
        Assert.Equal(prediction.TargetHours, observation.ElapsedHours);
        Assert.True(prediction.IsConfirmedBy(observation));
    }

    [Fact]
    public void A_forecast_is_not_confirmed_by_an_observation_three_times_what_it_announced() {
        // Arrange
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e4);
        Prediction prediction = culture.Predict(species, 24d);
        culture.Advance(24d);
        Observation observation = AnyPlateCountOf(culture, species)
                                  .WithMeasured(new Concentration(3 * culture.Estimate(species).Concentration.CellsPerMillilitre))
                                  .WithRelativeUncertainty(0.1)
                                  .Generate();

        // Assert
        Assert.False(prediction.IsConfirmedBy(observation));
    }

    // ---- generations -----------------------------------------------------------------------------------------

    [Fact]
    public void The_generations_of_a_species_that_is_not_in_the_culture_are_refused() {
        // Arrange
        Species present = Scenes.AnyGrowingSpecies().Generate();
        Species absent  = Scenes.AnotherSpeciesThan(present);
        Culture culture = AnyCultureInoculatedWith(present, 1e4);

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.GenerationsOf(absent));
        Assert.Throws<ArgumentNullException>(() => culture.GenerationsOf(null!));
    }

    [Fact]
    public void The_generations_are_counted_from_the_inoculum_not_from_the_last_observation() {
        // Arrange: an observation 8 times the model drags the estimate up by a factor
        Species species = Scenes.AnyGrowingSpecies().Generate();
        Culture culture = AnyCultureInoculatedWith(species, 1e6);
        culture.Record(AnyPlateCountOf(culture, species).WithMeasured(new Concentration(8e6)).WithRelativeUncertainty(ModelUncertainty).Generate());

        // Act: blended estimate = (1e6 + 8e6) / 2 = 4.5e6
        double generations = culture.GenerationsOf(species);

        // Assert
        Assert.Equal(Math.Log2(4.5), generations, precision: 9);
    }

}
