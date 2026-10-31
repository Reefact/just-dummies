using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class CultureTests {

    // A species whose cardinal temperatures are 10 / 30 / 40 °C: the tests below place the
    // chamber inside or outside that range, so those three are chosen, the rest is arbitrary.
    private static AnySpecies AnyMesophile() {
        return Any.Species()
                  .WithMinTemperatureCelsius(10)
                  .WithOptimalTemperatureCelsius(30)
                  .WithMaxTemperatureCelsius(40);
    }

    // Conditions in which a mesophile grows: the temperature lies strictly inside its range.
    private static readonly Func<AnyExperimentalConditions, AnyExperimentalConditions> GrowingConditions =
        c => c.WithTemperatureCelsius(t => t.GreaterThan(10).LessThan(40));

    private static Concentration AnyInoculumFor(ExperimentalConditions conditions, Species species) {
        double capacity = conditions.CarryingCapacity(species).CellsPerMillilitre;

        return Any.Concentration().WithCellsPerMillilitre(c => c.GreaterThanOrEqualTo(1).LessThan(capacity)).Generate();
    }

    [Fact]
    public void Station_time_is_utc_so_a_culture_started_at_another_offset_is_refused() {
        // Arrange
        CultureId              anyId         = Any.CultureId().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        DateTimeOffset         startedInParis = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.FromHours(1));

        // Act
        Exception? error = Record.Exception(() => new Culture(anyId, anyConditions, startedInParis));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_species_is_inoculated_at_time_zero_only() {
        // Arrange
        Species anyFirst  = AnyMesophile().WithBinomialName("Escherichia coli").Generate();
        Species anyLate   = AnyMesophile().WithBinomialName("Bacillus subtilis").Generate();
        Culture anyCulture = Any.Culture().WithConditions(GrowingConditions).Generate();
        anyCulture.Inoculate(anyFirst, AnyInoculumFor(anyCulture.Conditions, anyFirst));
        anyCulture.Advance(1);

        // Act
        Exception? error = Record.Exception(() => anyCulture.Inoculate(anyLate, AnyInoculumFor(anyCulture.Conditions, anyLate)));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void The_same_species_cannot_be_inoculated_twice() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Culture anyCulture = Any.Culture().Generate();
        anyCulture.Inoculate(anySpecies, AnyInoculumFor(anyCulture.Conditions, anySpecies));

        // Act
        Exception? error = Record.Exception(() => anyCulture.Inoculate(anySpecies, AnyInoculumFor(anyCulture.Conditions, anySpecies)));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_culture_holds_at_most_four_species() {
        // Arrange
        Culture anyCulture = Any.Culture().Generate();
        string[] names = ["Escherichia coli", "Bacillus subtilis", "Pseudomonas aeruginosa", "Staphylococcus aureus"];
        foreach (string name in names) {
            Species anySpecies = Any.Species().WithBinomialName(name).Generate();
            anyCulture.Inoculate(anySpecies, AnyInoculumFor(anyCulture.Conditions, anySpecies));
        }

        Species anyFifth = Any.Species().WithBinomialName("Candida albicans").Generate();

        // Act
        Exception? error = Record.Exception(() => anyCulture.Inoculate(anyFifth, AnyInoculumFor(anyCulture.Conditions, anyFifth)));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void An_inoculum_at_the_carrying_capacity_is_refused() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Culture anyCulture = Any.Culture().Generate();
        Concentration capacity = anyCulture.Conditions.CarryingCapacity(anySpecies);

        // Act
        Exception? error = Record.Exception(() => anyCulture.Inoculate(anySpecies, capacity));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

    [Fact]
    public void A_population_does_not_grow_during_its_lag() {
        // Arrange
        Species anyMesophile = AnyMesophile().WithLagHours(10).Generate();
        Culture anyCulture   = Any.Culture().WithConditions(GrowingConditions).Generate();
        Concentration anyInoculum = AnyInoculumFor(anyCulture.Conditions, anyMesophile);
        anyCulture.Inoculate(anyMesophile, anyInoculum);

        // Act
        anyCulture.Advance(5);

        // Assert
        EstimatedState state = anyCulture.Estimate(anyMesophile);
        Assert.Equal(GrowthPhase.Lag, state.Phase);
        Assert.Equal(0.0, anyCulture.GenerationsOf(anyMesophile));
    }

    [Fact]
    public void Above_its_maximum_temperature_a_population_dies_at_a_tenth_per_hour() {
        // Arrange
        Species anyMesophile = AnyMesophile().Generate();
        Culture anyCulture   = Any.Culture().WithConditions(c => c.WithTemperatureCelsius(50)).Generate();
        anyCulture.Inoculate(anyMesophile, new Concentration(1_000));

        // Act
        anyCulture.Advance(10);

        // Assert
        EstimatedState state = anyCulture.Estimate(anyMesophile);
        Assert.Equal(GrowthPhase.Death, state.Phase);
        Assert.Equal(1_000 * Math.Exp(-1), state.Concentration.CellsPerMillilitre, 1e-6);
    }

    [Fact]
    public void A_culture_left_long_enough_settles_at_its_carrying_capacity() {
        // Arrange
        Species anyFastMesophile = AnyMesophile().WithMaxGrowthRate(new GrowthRate(1.0)).WithLagHours(0).Generate();
        Culture anyCulture = Any.Culture()
                                .WithConditions(c => c.WithTemperatureCelsius(30).WithNutrientGramsPerLitre(10))
                                .Generate();
        anyCulture.Inoculate(anyFastMesophile, AnyInoculumFor(anyCulture.Conditions, anyFastMesophile));
        double capacity = anyCulture.Conditions.CarryingCapacity(anyFastMesophile).CellsPerMillilitre;

        // Act
        anyCulture.Advance(720);

        // Assert
        EstimatedState state = anyCulture.Estimate(anyFastMesophile);
        Assert.Equal(GrowthPhase.Stationary, state.Phase);
        Assert.InRange(state.Concentration.CellsPerMillilitre / capacity, 0.99, 1.0);
    }

    [Fact]
    public void Without_observation_the_estimate_loses_two_points_of_certainty_per_hour() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Culture anyCulture = Any.Culture().Generate();
        anyCulture.Inoculate(anySpecies, AnyInoculumFor(anyCulture.Conditions, anySpecies));

        // Act
        anyCulture.Advance(5);

        // Assert
        EstimatedState state = anyCulture.Estimate(anySpecies);
        Assert.Equal(Culture.InitialRelativeUncertainty + 0.10, state.RelativeUncertainty, 9);
        Assert.True(state.IsModelOnly);
    }

    [Fact]
    public void An_observation_as_certain_as_the_model_lands_the_estimate_halfway() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Culture anyCulture = Any.Culture().WithConditions(c => c.WithNutrientGramsPerLitre(10)).Generate();
        anyCulture.Inoculate(anySpecies, new Concentration(1_000));
        Observation anyPlateCount = Any.Observation()
                                       .WithCultureId(anyCulture.Id)
                                       .WithSpecies(anySpecies)
                                       .WithElapsedHours(0)
                                       .WithMeasured(new Concentration(3_000))
                                       .WithRelativeUncertainty(Culture.InitialRelativeUncertainty)
                                       .WithMethod(ObservationMethod.PlateCount)
                                       .Generate();

        // Act
        anyCulture.Record(anyPlateCount);

        // Assert
        EstimatedState state = anyCulture.Estimate(anySpecies);
        Assert.Equal(2_000, state.Concentration.CellsPerMillilitre, 1e-6);
        Assert.Equal(Culture.InitialRelativeUncertainty / Math.Sqrt(2), state.RelativeUncertainty, 9);
        Assert.Equal(1, state.ObservationCount);
    }

    [Fact]
    public void An_observation_of_another_culture_is_refused() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Culture anyCulture = Any.Culture().WithId(new CultureId("CH03-C0042")).Generate();
        anyCulture.Inoculate(anySpecies, AnyInoculumFor(anyCulture.Conditions, anySpecies));
        Observation anyObservationOfAnotherCulture = Any.Observation()
                                                        .WithCultureId(new CultureId("CH04-C0042"))
                                                        .WithSpecies(anySpecies)
                                                        .WithElapsedHours(0)
                                                        .Generate();

        // Act
        Exception? error = Record.Exception(() => anyCulture.Record(anyObservationOfAnotherCulture));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void An_observation_taken_an_hour_away_from_now_is_refused() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Culture anyCulture = Any.Culture().Generate();
        anyCulture.Inoculate(anySpecies, AnyInoculumFor(anyCulture.Conditions, anySpecies));
        anyCulture.Advance(2);
        Observation anyLateObservation = Any.Observation()
                                            .WithCultureId(anyCulture.Id)
                                            .WithSpecies(anySpecies)
                                            .WithElapsedHours(3)
                                            .Generate();

        // Act
        Exception? error = Record.Exception(() => anyCulture.Record(anyLateObservation));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_prediction_leaves_the_culture_where_it_was() {
        // Arrange
        Species anyMesophile = AnyMesophile().Generate();
        Culture anyCulture   = Any.Culture().WithConditions(GrowingConditions).Generate();
        anyCulture.Inoculate(anyMesophile, AnyInoculumFor(anyCulture.Conditions, anyMesophile));
        anyCulture.Advance(3);
        Concentration before = anyCulture.Estimate(anyMesophile).Concentration;

        // Act
        Prediction prediction = anyCulture.Predict(anyMesophile, 24);

        // Assert
        Assert.Equal(3.0, anyCulture.ElapsedHours);
        Assert.Equal(before, anyCulture.Estimate(anyMesophile).Concentration);
        Assert.Equal(27.0, prediction.TargetHours);
    }

    [Fact]
    public void A_terminated_culture_refuses_to_advance() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Culture anyCulture = Any.Culture().Generate();
        anyCulture.Inoculate(anySpecies, AnyInoculumFor(anyCulture.Conditions, anySpecies));
        anyCulture.Terminate();

        // Act
        Exception? error = Record.Exception(() => anyCulture.Advance(1));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_species_does_not_interact_with_itself() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Culture anyCulture = Any.Culture().Generate();
        anyCulture.Inoculate(anySpecies, AnyInoculumFor(anyCulture.Conditions, anySpecies));
        double anyCoefficient = Any.Double().Between(Culture.MinInteraction, Culture.MaxInteraction).Generate();

        // Act
        Exception? error = Record.Exception(() => anyCulture.SetInteraction(anySpecies, anySpecies, anyCoefficient));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

}
