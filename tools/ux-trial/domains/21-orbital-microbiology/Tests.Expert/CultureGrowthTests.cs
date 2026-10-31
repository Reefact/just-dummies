namespace OrbitalMicrobiology.Tests.Expert;

public sealed class CultureGrowthTests {

    [Fact]
    public void A_population_holds_its_concentration_during_its_lag() {
        // Arrange
        Species           anySpecies = Any.Species().WithLagHours(10).Generate();
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(anySpecies, inoculum: new Concentration(100_000)).Generate();

        // Act
        anyCulture.Culture.Advance(5);

        // Assert
        EstimatedState estimate = anyCulture.Culture.Estimate(anySpecies);
        Assert.Equal(GrowthPhase.Lag, estimate.Phase);
        Assert.Equal(100_000, estimate.Concentration.CellsPerMillilitre);
    }

    [Fact]
    public void A_population_enters_its_exponential_phase_once_the_lag_is_over() {
        // Arrange
        Species           anySpecies = Any.Species().WithLagHours(2).Generate();
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(anySpecies).Generate();

        // Act
        anyCulture.Culture.Advance(3);

        // Assert
        Assert.Equal(GrowthPhase.Exponential, anyCulture.Culture.Estimate(anySpecies).Phase);
    }

    [Fact]
    public void A_population_held_above_its_maximum_temperature_dies_at_a_tenth_per_hour() {
        // Arrange
        Species           anySpecies = Any.Species().WithCardinalTemperatures(minimum: 10, optimum: 30, maximum: 40).Generate();
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(anySpecies,
                                                                  culture => culture.WithConditions(c => c.Nourished().WithTemperatureCelsius(45)),
                                                                  inoculum: new Concentration(100_000)).Generate();

        // Act
        anyCulture.Culture.Advance(10);

        // Assert
        EstimatedState estimate = anyCulture.Culture.Estimate(anySpecies);
        Assert.Equal(GrowthPhase.Death, estimate.Phase);
        Assert.Equal(100_000 * Math.Exp(-1), estimate.Concentration.CellsPerMillilitre, 6);
    }

    [Fact]
    public void A_population_near_its_carrying_capacity_is_stationary() {
        // Arrange
        Species           anySpecies = Any.Species().WithoutLag().WithMaxGrowthRate(new GrowthRate(2)).Generate();
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(anySpecies).Generate();

        // Act
        anyCulture.Culture.Advance(48);

        // Assert
        EstimatedState estimate = anyCulture.Culture.Estimate(anySpecies);
        Assert.Equal(GrowthPhase.Stationary, estimate.Phase);
        Assert.True(estimate.Concentration.CompareTo(anyCulture.Culture.Conditions.CarryingCapacity(anySpecies)) <= 0);
    }

    [Fact]
    public void A_competitor_holds_a_species_below_what_it_reaches_alone() {
        // Arrange
        AnySpecies anyFastGrower = Any.Species().WithoutLag().WithMaxGrowthRate(new GrowthRate(1)).WithCardinalTemperatures(10, 30, 40);
        List<Species>          anyPair       = Any.ListOf(anyFastGrower).WithCount(2).Distinct().Generate();
        (Species affected, Species competitor) = (anyPair[0], anyPair[1]);
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Nourished().AtTheOptimumOf(affected).Generate();
        Culture                alone         = CultureOf(anyConditions, affected, competitor);
        Culture                competing     = CultureOf(anyConditions, affected, competitor);
        competing.SetInteraction(affected, competitor, 1.5);

        // Act
        alone.Advance(24);
        competing.Advance(24);

        // Assert
        Assert.True(competing.Estimate(affected).Concentration.CompareTo(alone.Estimate(affected).Concentration) < 0);
    }

    [Fact]
    public void A_model_estimate_grows_less_certain_by_two_percent_an_hour() {
        // Arrange
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(Any.Species().Generate()).Generate();

        // Act
        anyCulture.Culture.Advance(10);

        // Assert
        EstimatedState estimate = anyCulture.Culture.Estimate(anyCulture.Species);
        Assert.True(estimate.IsModelOnly);
        Assert.Equal(Culture.InitialRelativeUncertainty + 0.20, estimate.RelativeUncertainty, 9);
    }

    [Fact]
    public void A_prediction_looks_ahead_without_moving_the_culture() {
        // Arrange
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(Any.Species().Generate()).Generate();
        anyCulture.Culture.Advance(6);
        EstimatedState before = anyCulture.Culture.Estimate(anyCulture.Species);

        // Act
        Prediction prediction = anyCulture.Culture.Predict(anyCulture.Species, 12);

        // Assert
        EstimatedState after = anyCulture.Culture.Estimate(anyCulture.Species);
        Assert.Equal(18, prediction.TargetHours, 9);
        Assert.Equal(6, anyCulture.Culture.ElapsedHours, 9);
        Assert.Equal(before.Concentration, after.Concentration);
        Assert.True(prediction.RelativeUncertainty >= before.RelativeUncertainty);
    }

    private static Culture CultureOf(ExperimentalConditions conditions, Species first, Species second) {
        Culture culture = Any.Culture().WithConditions(conditions).Generate();
        culture.Inoculate(first, new Concentration(conditions.CarryingCapacity(first).CellsPerMillilitre / 10_000));
        culture.Inoculate(second, new Concentration(conditions.CarryingCapacity(second).CellsPerMillilitre / 10_000));

        return culture;
    }

}
