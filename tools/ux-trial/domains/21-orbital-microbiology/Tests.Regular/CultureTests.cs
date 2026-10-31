using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Regular;

public sealed class CultureTests {

    [Fact]
    public void Station_time_is_UTC() {
        // Arrange
        CultureId              anyId            = Arbitrary.CultureId().Generate();
        ExperimentalConditions anyConditions    = Arbitrary.ExperimentalConditions().Generate();
        DateTimeOffset         anyLocalStart    = Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(2)).Generate();

        // Act
        Exception? exception = Record.Exception(() => new Culture(anyId, anyConditions, anyLocalStart));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void A_species_is_inoculated_only_once() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.Fed).Generate();
        Species anySpecies = Arbitrary.Species().Generate();
        anyCulture.Inoculate(anySpecies, Lab.SmallInoculum().Generate());

        // Act
        Exception? exception = Record.Exception(() => anyCulture.Inoculate(anySpecies, Lab.SmallInoculum().Generate()));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void A_culture_holds_at_most_four_species() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.Fed).Generate();
        List<Species> anyFiveSpecies = Any.ListOf(Arbitrary.Species()).WithCount(5).Distinct().Generate();
        foreach (Species species in anyFiveSpecies.Take(4)) {
            anyCulture.Inoculate(species, Lab.SmallInoculum().Generate());
        }

        // Act
        Exception? exception = Record.Exception(() => anyCulture.Inoculate(anyFiveSpecies[4], Lab.SmallInoculum().Generate()));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void An_inoculum_at_the_carrying_capacity_is_refused() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().Generate();
        Species anySpecies = Arbitrary.Species().Generate();
        Concentration capacity = anyCulture.Conditions.CarryingCapacity(anySpecies);

        // Act
        Exception? exception = Record.Exception(() => anyCulture.Inoculate(anySpecies, capacity));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void Species_are_inoculated_at_time_zero_only() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.Fed).Generate();
        anyCulture.Inoculate(Arbitrary.Species().WithBinomialName("Bacillus subtilis").Generate(), Lab.SmallInoculum().Generate());
        anyCulture.Advance(1);

        // Act
        Exception? exception = Record.Exception(() => anyCulture.Inoculate(Arbitrary.Species().WithBinomialName("Escherichia coli").Generate(), Lab.SmallInoculum().Generate()));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void A_culture_cannot_grow_before_it_is_inoculated() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().Generate();

        // Act
        Exception? exception = Record.Exception(() => anyCulture.Advance(1));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void A_species_does_not_interact_with_itself() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.Fed).Generate();
        Species anySpecies = Arbitrary.Species().Generate();
        anyCulture.Inoculate(anySpecies, Lab.SmallInoculum().Generate());

        // Act
        Exception? exception = Record.Exception(() => anyCulture.SetInteraction(anySpecies, anySpecies, 0.5));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void A_population_in_its_lag_phase_does_not_grow() {
        // Arrange
        Culture       anyCulture   = Arbitrary.Culture().WithConditions(Lab.WhereMesophilesGrow).Generate();
        Species       anySlowStart = Lab.Mesophile().WithLagHours(10).Generate();
        Concentration anyInoculum  = Lab.SmallInoculum().Generate();
        anyCulture.Inoculate(anySlowStart, anyInoculum);

        // Act
        anyCulture.Advance(5);

        // Assert
        EstimatedState estimate = anyCulture.Estimate(anySlowStart);
        Assert.Equal(GrowthPhase.Lag, estimate.Phase);
        Assert.Equal(anyInoculum, estimate.Concentration);
    }

    [Fact]
    public void An_unobserved_estimate_loses_two_points_of_certainty_per_hour() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.Fed).Generate();
        Species anySpecies = Arbitrary.Species().Generate();
        anyCulture.Inoculate(anySpecies, Lab.SmallInoculum().Generate());

        // Act
        anyCulture.Advance(10);

        // Assert
        EstimatedState estimate = anyCulture.Estimate(anySpecies);
        Assert.True(estimate.IsModelOnly);
        Assert.Equal(0.05 + 0.20, estimate.RelativeUncertainty, 9);
    }

    [Fact]
    public void The_same_culture_grows_further_in_weightlessness_than_on_the_centrifuge() {
        // Arrange
        Species anyMesophile = Lab.Mesophile().WithLagHours(0).Generate();
        Concentration anyInoculum = Lab.SmallInoculum().Generate();
        ExperimentalConditions anyCentrifuge = Lab.WhereMesophilesGrow(Arbitrary.ExperimentalConditions()).WithGravityG(1.0).Generate();
        ExperimentalConditions anyWeightless = new ExperimentalConditions(0, anyCentrifuge.TemperatureCelsius, anyCentrifuge.NutrientGramsPerLitre, anyCentrifuge.VolumeMillilitres);
        Culture onCentrifuge = Arbitrary.Culture().WithConditions(anyCentrifuge).Generate();
        Culture inOrbit      = Arbitrary.Culture().WithConditions(anyWeightless).Generate();
        onCentrifuge.Inoculate(anyMesophile, anyInoculum);
        inOrbit.Inoculate(anyMesophile, anyInoculum);

        // Act
        onCentrifuge.Advance(5);
        inOrbit.Advance(5);

        // Assert
        Assert.True(inOrbit.GenerationsOf(anyMesophile) > onCentrifuge.GenerationsOf(anyMesophile));
    }

    [Fact]
    public void A_population_levels_off_near_the_carrying_capacity() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(c => Lab.Fed(c).WithTemperatureCelsius(Lab.OptimalTemperature)).Generate();
        Species anyFastStarter = Lab.Mesophile().WithLagHours(0).WithMaxGrowthRate(new GrowthRate(1)).Generate();
        anyCulture.Inoculate(anyFastStarter, Lab.SmallInoculum().Generate());
        double capacity = anyCulture.Conditions.CarryingCapacity(anyFastStarter).CellsPerMillilitre;

        // Act
        anyCulture.Advance(Culture.MaxAdvanceHours);

        // Assert
        EstimatedState estimate = anyCulture.Estimate(anyFastStarter);
        Assert.Equal(GrowthPhase.Stationary, estimate.Phase);
        Assert.InRange(estimate.Concentration.CellsPerMillilitre, 0.95 * capacity, capacity);
    }

    [Fact]
    public void Above_its_maximum_temperature_a_population_dies_at_a_tenth_per_hour() {
        // Arrange
        Culture       anyCulture   = Arbitrary.Culture().WithConditions(c => Lab.Fed(c).WithTemperatureCelsius(50)).Generate();
        Species       anyMesophile = Lab.Mesophile().Generate();
        Concentration anyInoculum  = Lab.SmallInoculum().Generate();
        anyCulture.Inoculate(anyMesophile, anyInoculum);

        // Act
        anyCulture.Advance(10);

        // Assert
        EstimatedState estimate = anyCulture.Estimate(anyMesophile);
        Assert.Equal(GrowthPhase.Death, estimate.Phase);
        Assert.Equal(anyInoculum.CellsPerMillilitre * Math.Exp(-1), estimate.Concentration.CellsPerMillilitre, 1e-6 * anyInoculum.CellsPerMillilitre);
    }

    [Fact]
    public void A_competitor_holds_a_species_back() {
        // Arrange
        List<Species> anyTwo = Any.ListOf(Lab.Mesophile().WithLagHours(0)).WithCount(2).Distinct().Generate();
        (Species anyFocus, Species anyCompetitor) = (anyTwo[0], anyTwo[1]);
        ExperimentalConditions anyConditions = Lab.WhereMesophilesGrow(Arbitrary.ExperimentalConditions()).Generate();
        Concentration anyInoculum = Lab.SmallInoculum().Generate();
        Culture alone     = Arbitrary.Culture().WithConditions(anyConditions).Generate();
        Culture contested = Arbitrary.Culture().WithConditions(anyConditions).Generate();
        alone.Inoculate(anyFocus, anyInoculum);
        contested.Inoculate(anyFocus, anyInoculum);
        contested.Inoculate(anyCompetitor, anyInoculum);
        contested.SetInteraction(anyFocus, anyCompetitor, 1.5);

        // Act
        alone.Advance(Culture.MaxAdvanceHours);
        contested.Advance(Culture.MaxAdvanceHours);

        // Assert
        Assert.True(contested.Estimate(anyFocus).Concentration.CellsPerMillilitre < alone.Estimate(anyFocus).Concentration.CellsPerMillilitre);
    }

    [Fact]
    public void An_observation_of_another_culture_is_refused() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.Fed).Generate();
        Species anySpecies = Arbitrary.Species().Generate();
        anyCulture.Inoculate(anySpecies, Lab.SmallInoculum().Generate());
        Observation anyForeign = Arbitrary.Observation()
                                          .WithCultureId(id => id.WithValue(v => v.DifferentFrom(anyCulture.Id.Value)))
                                          .WithSpecies(anySpecies)
                                          .WithElapsedHours(0)
                                          .Generate();

        // Act
        Exception? exception = Record.Exception(() => anyCulture.Record(anyForeign));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void An_observation_taken_an_hour_away_from_now_is_refused() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.Fed).Generate();
        Species anySpecies = Arbitrary.Species().Generate();
        anyCulture.Inoculate(anySpecies, Lab.SmallInoculum().Generate());
        anyCulture.Advance(5);
        Observation anyStale = Arbitrary.Observation().WithCultureId(anyCulture.Id).WithSpecies(anySpecies).WithElapsedHours(4).Generate();

        // Act
        Exception? exception = Record.Exception(() => anyCulture.Record(anyStale));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void An_equally_certain_measurement_pulls_the_estimate_halfway_and_tightens_it() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.Fed).Generate();
        Species anySpecies = Arbitrary.Species().Generate();
        anyCulture.Inoculate(anySpecies, new Concentration(1_000));
        // 5 % is the model's initial uncertainty; optical density cannot claim it, so a direct method is pinned.
        Observation anyPlateCount = Arbitrary.Observation()
                                             .WithCultureId(anyCulture.Id)
                                             .WithSpecies(anySpecies)
                                             .WithElapsedHours(0)
                                             .WithMeasured(new Concentration(3_000))
                                             .WithRelativeUncertainty(0.05)
                                             .WithMethod(m => m.Except(ObservationMethod.OpticalDensity))
                                             .Generate();

        // Act
        anyCulture.Record(anyPlateCount);

        // Assert
        EstimatedState estimate = anyCulture.Estimate(anySpecies);
        Assert.Equal(2_000, estimate.Concentration.CellsPerMillilitre, 6);
        Assert.Equal(0.05 / Math.Sqrt(2), estimate.RelativeUncertainty, 9);
        Assert.Equal(1, estimate.ObservationCount);
    }

    [Fact]
    public void A_prediction_leaves_the_culture_where_it_was() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.WhereMesophilesGrow).Generate();
        Species anyMesophile = Lab.Mesophile().Generate();
        anyCulture.Inoculate(anyMesophile, Lab.SmallInoculum().Generate());
        anyCulture.Advance(12);
        Concentration before = anyCulture.Estimate(anyMesophile).Concentration;

        // Act
        Prediction prediction = anyCulture.Predict(anyMesophile, 24);

        // Assert
        Assert.Equal(36, prediction.TargetHours, 9);
        Assert.Equal(12, anyCulture.ElapsedHours, 9);
        Assert.Equal(before, anyCulture.Estimate(anyMesophile).Concentration);
    }

    [Fact]
    public void A_terminated_culture_refuses_to_grow() {
        // Arrange
        Culture anyCulture = Arbitrary.Culture().WithConditions(Lab.Fed).Generate();
        anyCulture.Inoculate(Arbitrary.Species().Generate(), Lab.SmallInoculum().Generate());
        anyCulture.Terminate();

        // Act
        Exception? exception = Record.Exception(() => anyCulture.Advance(1));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

}
