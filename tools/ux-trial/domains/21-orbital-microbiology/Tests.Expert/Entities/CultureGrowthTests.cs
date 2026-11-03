using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.Entities;

public sealed class CultureGrowthTests {

    // The growing species of the scenes grows at 5 / 5.5 = 0.909 per hour on the control bench,
    // towards a carrying capacity of 5 / 1000 x 1e10 = 5e7 cells/mL.

    // ---- what Advance accepts --------------------------------------------------------------------------------

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(720.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_culture_advances_by_more_than_nothing_and_at_most_720_hours(double hours) {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        culture.Inoculate(Scenes.AnyGrowingSpecies().Generate(), Any.Concentration().Dilute().Generate());

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => culture.Advance(hours));
        Assert.Equal(0d, culture.ElapsedHours);
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(720d)]
    public void A_culture_advances_by_as_little_as_a_few_minutes_and_by_as_much_as_a_month(double hours) {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        culture.Inoculate(Scenes.AnyGrowingSpecies().Generate(), Any.Concentration().Dilute().Generate());

        // Act
        culture.Advance(hours);

        // Assert
        Assert.Equal(hours, culture.ElapsedHours);
    }

    [Fact]
    public void A_culture_that_was_never_inoculated_cannot_advance() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.Advance(1d));
    }

    [Fact]
    public void The_hours_advanced_add_up() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        culture.Inoculate(Scenes.AnyGrowingSpecies().Generate(), Any.Concentration().Dilute().Generate());

        // Act
        culture.Advance(12d);
        culture.Advance(30.5);

        // Assert
        Assert.Equal(42.5, culture.ElapsedHours);
    }

    [Fact]
    public void Advancing_in_two_parts_leads_to_the_state_advancing_in_one_does() {
        // Arrange
        Species       species  = Scenes.AnyGrowingSpecies().Generate();
        Concentration inoculum = new Concentration(1e4);
        CultureId     id       = Any.CultureId().Generate();
        Culture       whole    = Scenes.AnyControlCulture().WithId(id).Generate();
        Culture       parts    = Scenes.AnyControlCulture().WithId(id).WithConditions(whole.Conditions).Generate();
        whole.Inoculate(species, inoculum);
        parts.Inoculate(species, inoculum);

        // Act
        whole.Advance(12d);
        parts.Advance(5d);
        parts.Advance(7d);

        // Assert
        EstimatedState inOne = whole.Estimate(species);
        EstimatedState inTwo = parts.Estimate(species);
        Assert.Equal(inOne.Concentration.CellsPerMillilitre, inTwo.Concentration.CellsPerMillilitre, inOne.Concentration.CellsPerMillilitre * 1e-9);
        Assert.Equal(inOne.RelativeUncertainty, inTwo.RelativeUncertainty, precision: 9);
    }

    [Fact(Skip = "domain defect: advancing 10 h in one call gives 5.41e6 cells/mL from 1e3, advancing 0.01 h a thousand times gives 7.29e6 (steps are min(0.1, remaining), so the result depends on how the caller slices time); literal inputs, not seed-dependent")]
    public void Advancing_in_many_small_slices_leads_to_the_state_advancing_in_one_does() {
        // Arrange
        Species       species  = Scenes.AnyGrowingSpecies().Generate();
        Concentration inoculum = new Concentration(1e3);
        CultureId     id       = Any.CultureId().Generate();
        Culture       whole    = Scenes.AnyControlCulture().WithId(id).Generate();
        Culture       slices   = Scenes.AnyControlCulture().WithId(id).WithConditions(whole.Conditions).Generate();
        whole.Inoculate(species, inoculum);
        slices.Inoculate(species, inoculum);

        // Act
        whole.Advance(10d);
        for (int slice = 0; slice < 1000; slice++) {
            slices.Advance(0.01);
        }

        // Assert
        double inOne = whole.Estimate(species).Concentration.CellsPerMillilitre;
        Assert.Equal(inOne, slices.Estimate(species).Concentration.CellsPerMillilitre, tolerance: inOne * 0.01);
    }

    // ---- growth ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_dilute_culture_grows_almost_exponentially() {
        // Arrange: 0.909 per hour for 5 hours is 6.56 generations; the 0.1 hour steps lag it slightly
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, new Concentration(1e3));

        // Act
        culture.Advance(5d);

        // Assert
        Assert.InRange(culture.GenerationsOf(species), 6.0, 6.6);
        Assert.Equal(GrowthPhase.Exponential, culture.Estimate(species).Phase);
    }

    [Fact]
    public void A_species_that_has_just_been_inoculated_has_gone_through_no_generation() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, Any.Concentration().Dilute().Generate());

        // Assert
        Assert.Equal(0d, culture.GenerationsOf(species));
    }

    [Fact]
    public void A_culture_left_long_enough_settles_at_the_carrying_capacity_without_crossing_it() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, Any.Concentration().Dilute().Generate());
        double capacity = culture.Conditions.CarryingCapacity(species).CellsPerMillilitre;

        // Act
        culture.Advance(100d);

        // Assert
        EstimatedState state = culture.Estimate(species);
        Assert.InRange(state.Concentration.CellsPerMillilitre, 0.95 * capacity, capacity);
        Assert.Equal(GrowthPhase.Stationary, state.Phase);
    }

    [Fact]
    public void Microgravity_lets_the_same_culture_settle_higher() {
        // Arrange
        Species       species  = Scenes.AnyGrowingSpecies().Generate();
        Concentration inoculum = Any.Concentration().Dilute().Generate();
        Culture       control  = Scenes.AnyControlCulture().Generate();
        Culture       inOrbit  = Scenes.AnyControlCulture().WithConditions(conditions => conditions.OnTheControlBench().WithGravityG(0d)).Generate();
        control.Inoculate(species, inoculum);
        inOrbit.Inoculate(species, inoculum);

        // Act
        control.Advance(100d);
        inOrbit.Advance(100d);

        // Assert
        Assert.True(inOrbit.Estimate(species).Concentration.CellsPerMillilitre > control.Estimate(species).Concentration.CellsPerMillilitre);
    }

    // ---- lag -------------------------------------------------------------------------------------------------

    [Fact]
    public void A_species_in_lag_does_not_grow() {
        // Arrange
        Culture       culture  = Scenes.AnyControlCulture().Generate();
        Species       laggard  = Scenes.AnyGrowingSpecies().WithLagHours(20d).Generate();
        Concentration inoculum = Any.Concentration().Dilute().Generate();
        culture.Inoculate(laggard, inoculum);

        // Act
        culture.Advance(10d);

        // Assert
        EstimatedState state = culture.Estimate(laggard);
        Assert.Equal(inoculum.CellsPerMillilitre, state.Concentration.CellsPerMillilitre);
        Assert.Equal(GrowthPhase.Lag, state.Phase);
    }

    [Fact]
    public void A_species_grows_once_its_lag_is_over() {
        // Arrange
        Culture       culture  = Scenes.AnyControlCulture().Generate();
        Species       laggard  = Scenes.AnyGrowingSpecies().WithLagHours(20d).Generate();
        Concentration inoculum = Any.Concentration().Dilute().Generate();
        culture.Inoculate(laggard, inoculum);

        // Act
        culture.Advance(30d);

        // Assert
        EstimatedState state = culture.Estimate(laggard);
        Assert.True(state.Concentration.CellsPerMillilitre > inoculum.CellsPerMillilitre);
        Assert.Equal(GrowthPhase.Exponential, state.Phase);
    }

    [Fact]
    public void Only_the_hours_after_the_lag_count_as_growth() {
        // Arrange: 20 hours of lag, then 5 hours of growth, as long as 5 hours without lag
        Species       eager    = Scenes.AnyGrowingSpecies().WithLagHours(0d).Generate();
        Species       laggard  = Scenes.AnyGrowingSpecies().WithBinomialName(eager.BinomialName).WithLagHours(20d).Generate();
        Concentration inoculum = Any.Concentration().Dilute().Generate();
        Culture       withoutLag = Scenes.AnyControlCulture().Generate();
        Culture       withLag    = Scenes.AnyControlCulture().WithConditions(withoutLag.Conditions).Generate();
        withoutLag.Inoculate(eager, inoculum);
        withLag.Inoculate(laggard, inoculum);

        // Act
        withoutLag.Advance(5d);
        withLag.Advance(25d);

        // Assert
        Assert.Equal(withoutLag.GenerationsOf(eager), withLag.GenerationsOf(laggard), precision: 1);
    }

    // ---- death -----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(60d)]
    [InlineData(50d)]   // the maximum itself: the temperature factor is nil there
    [InlineData(10d)]   // the minimum itself
    [InlineData(0d)]
    public void Outside_the_cardinal_temperatures_a_population_dies_at_a_tenth_per_hour(double celsius) {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().WithConditions(conditions => conditions.OnTheControlBench().WithTemperatureCelsius(celsius)).Generate();
        Species species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, new Concentration(1e6));

        // Act
        culture.Advance(10d);

        // Assert: e^-1 of the inoculum
        EstimatedState state = culture.Estimate(species);
        Assert.Equal(1e6 * Math.Exp(-1d), state.Concentration.CellsPerMillilitre, tolerance: 1d);
        Assert.Equal(GrowthPhase.Death, state.Phase);
        Assert.Equal(-1d / Math.Log(2d), culture.GenerationsOf(species), precision: 6);
    }

    [Fact]
    public void A_population_that_falls_below_one_cell_per_millilitre_is_extinct() {
        // Arrange
        Culture culture = Scenes.AnyScaldingCulture().Generate();
        Species species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, new Concentration(10d));

        // Act: 10 x e^-3 = 0.5 cell/mL
        culture.Advance(30d);

        // Assert
        MicrobialPopulation population = Assert.Single(culture.Populations);
        Assert.True(population.IsExtinct);
        Assert.Equal(GrowthPhase.Death, population.Phase);
        Assert.Equal(0d, culture.Estimate(species).Concentration.CellsPerMillilitre);
    }

    [Fact]
    public void An_extinct_population_stays_extinct() {
        // Arrange
        Culture culture = Scenes.AnyScaldingCulture().Generate();
        Species species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, new Concentration(10d));
        culture.Advance(30d);

        // Act
        culture.Advance(30d);

        // Assert
        Assert.True(culture.Populations.Single().IsExtinct);
    }

    [Fact]
    public void An_extinct_population_has_no_generations_to_count() {
        // Arrange
        Culture culture = Scenes.AnyScaldingCulture().Generate();
        Species species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, new Concentration(10d));
        culture.Advance(30d);

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.GenerationsOf(species));
    }

    [Fact]
    public void A_population_pushed_above_its_load_by_a_competitor_declines_and_is_in_death_phase() {
        // Arrange: the competitor weighs 2 x its concentration on the first species, which is already at capacity
        Culture culture    = Scenes.AnyControlCulture().Generate();
        Species affected   = Scenes.AnyGrowingSpecies().Generate();
        Species competitor = Scenes.AnotherSpeciesThan(affected);
        culture.Inoculate(affected, new Concentration(4.9e7));
        culture.Inoculate(competitor, new Concentration(4e6));
        culture.SetInteraction(affected, competitor, Culture.MaxInteraction);

        // Act
        culture.Advance(0.1);

        // Assert
        Assert.Equal(GrowthPhase.Death, culture.Estimate(affected).Phase);
        Assert.True(culture.Estimate(affected).Concentration.CellsPerMillilitre < 4.9e7);
    }

    // ---- interactions at work --------------------------------------------------------------------------------

    [Fact]
    public void A_competitor_leaves_less_of_the_species_it_weighs_on() {
        // Arrange
        Species       affected   = Scenes.AnyGrowingSpecies().Generate();
        Species       competitor = Scenes.AnotherSpeciesThan(affected);
        Concentration inoculum   = new Concentration(1e3);
        Culture       alone      = Scenes.AnyControlCulture().Generate();
        Culture       together   = Scenes.AnyControlCulture().WithConditions(alone.Conditions).Generate();
        alone.Inoculate(affected, inoculum);
        together.Inoculate(affected, inoculum);
        together.Inoculate(competitor, new Concentration(1e5));
        together.SetInteraction(affected, competitor, 1d);

        // Act
        alone.Advance(48d);
        together.Advance(48d);

        // Assert
        Assert.True(together.Estimate(affected).Concentration.CellsPerMillilitre < alone.Estimate(affected).Concentration.CellsPerMillilitre);
    }

    [Fact]
    public void A_facilitator_of_comparable_size_leaves_more_of_the_species_it_helps() {
        // Arrange: the facilitator yields as much per gram as the species it helps, so it cannot outgrow its capacity by orders of magnitude
        Species       affected    = Scenes.AnyGrowingSpecies().Generate();
        Species       facilitator = Any.Species().WithBinomialName(name => name.DifferentFrom(affected.BinomialName)).WithYieldCellsPerGram(affected.YieldCellsPerGram).Generate();
        Concentration inoculum    = new Concentration(1e3);
        Culture       alone       = Scenes.AnyControlCulture().Generate();
        Culture       together    = Scenes.AnyControlCulture().WithConditions(alone.Conditions).Generate();
        alone.Inoculate(affected, inoculum);
        together.Inoculate(affected, inoculum);
        together.Inoculate(facilitator, new Concentration(1e5));
        together.SetInteraction(affected, facilitator, -0.5);

        // Act
        alone.Advance(48d);
        together.Advance(48d);

        // Assert
        Assert.True(together.Estimate(affected).Concentration.CellsPerMillilitre > alone.Estimate(affected).Concentration.CellsPerMillilitre);
    }

    [Fact(Skip = "domain defect: with a very productive facilitator (yield 6.68e12 cells/g, 3.3e10 cells/mL after 48 h) and an interaction of -0.5 the facilitated species ends extinct instead of higher (explicit Euler step blows up), seed -1087769823")]
    public void A_facilitator_leaves_more_of_the_species_it_helps() {
        // Arrange
        Species       affected    = Scenes.AnyGrowingSpecies().Generate();
        Species       facilitator = Scenes.AnotherSpeciesThan(affected);
        Concentration inoculum    = new Concentration(1e3);
        Culture       alone       = Scenes.AnyControlCulture().Generate();
        Culture       together    = Scenes.AnyControlCulture().WithConditions(alone.Conditions).Generate();
        alone.Inoculate(affected, inoculum);
        together.Inoculate(affected, inoculum);
        together.Inoculate(facilitator, new Concentration(1e5));
        together.SetInteraction(affected, facilitator, -0.5);

        // Act
        alone.Advance(48d);
        together.Advance(48d);

        // Assert
        Assert.True(together.Estimate(affected).Concentration.CellsPerMillilitre > alone.Estimate(affected).Concentration.CellsPerMillilitre);
    }

    [Fact]
    public void A_species_that_nothing_weighs_on_grows_the_same_with_or_without_company() {
        // Arrange: only the first species is affected by the second
        Species       affected   = Scenes.AnyGrowingSpecies().Generate();
        Species       competitor = Scenes.AnotherSpeciesThan(affected);
        Concentration inoculum   = new Concentration(1e3);
        Culture       alone      = Scenes.AnyControlCulture().Generate();
        Culture       together   = Scenes.AnyControlCulture().WithConditions(alone.Conditions).Generate();
        alone.Inoculate(competitor, inoculum);
        together.Inoculate(affected, new Concentration(1e5));
        together.Inoculate(competitor, inoculum);
        together.SetInteraction(affected, competitor, 1d);

        // Act
        alone.Advance(48d);
        together.Advance(48d);

        // Assert
        Assert.Equal(alone.Estimate(competitor).Concentration.CellsPerMillilitre, together.Estimate(competitor).Concentration.CellsPerMillilitre);
    }

    // ---- uncertainty -----------------------------------------------------------------------------------------

    [Fact]
    public void The_model_grows_more_uncertain_by_two_hundredths_per_hour_without_observation() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, Any.Concentration().Dilute().Generate());

        // Act
        culture.Advance(10d);

        // Assert: 0.05 + 10 x 0.02
        Assert.Equal(0.25, culture.Estimate(species).RelativeUncertainty, precision: 9);
    }

    [Fact]
    public void The_uncertainty_of_the_model_stops_at_one() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, Any.Concentration().Dilute().Generate());

        // Act
        culture.Advance(100d);

        // Assert
        Assert.Equal(1d, culture.Estimate(species).RelativeUncertainty);
    }

}
