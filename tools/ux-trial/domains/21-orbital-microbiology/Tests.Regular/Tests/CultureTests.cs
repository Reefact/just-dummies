using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests;

public sealed class CultureTests {

    // Species and conditions used by the growth tests: 30 °C at 1 g, 10 g/L, so a species
    // of yield 1e10 cells/g has a carrying capacity of exactly 1e8 cells/mL.
    private static Species FastGrower(double lagHours = 0) => Samples.GrowerAt30C(maxRatePerHour: 0.5, lagHours: lagHours, yieldCellsPerGram: 1e10);

    private static Culture NewCulture() => Samples.NewCulture(Samples.Earthlike30C());

    private static Concentration Inoculum(double cellsPerMillilitre) => new Concentration(cellsPerMillilitre);

    // ---- Construction ----------------------------------------------------------------

    [Fact]
    public void A_new_culture_has_not_grown_and_holds_nothing() {
        // Arrange
        CultureId              id         = new CultureId("CH05-C0007");
        DateTimeOffset         startedAt  = new DateTimeOffset(2031, 5, 1, 8, 30, 0, TimeSpan.Zero);
        ExperimentalConditions conditions = Samples.Earthlike30C();

        // Act
        Culture culture = new Culture(id, conditions, startedAt);

        // Assert
        Assert.Same(id, culture.Id);
        Assert.Same(conditions, culture.Conditions);
        Assert.Equal(startedAt, culture.StartedAtUtc);
        Assert.Equal(0, culture.ElapsedHours);
        Assert.Equal(startedAt, culture.CurrentTimeUtc);
        Assert.False(culture.IsTerminated);
        Assert.Empty(culture.Populations);
        Assert.Empty(culture.Observations);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-5)]
    [InlineData(0.5)]
    public void Station_time_is_utc_so_any_other_offset_is_refused(double offsetHours) {
        // Arrange
        DateTimeOffset anyUtcInstant = Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        DateTimeOffset shifted       = anyUtcInstant.ToOffset(TimeSpan.FromHours(offsetHours));
        CultureId      anyId         = Any.CultureId().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => new Culture(anyId, anyConditions, shifted));

        // Assert
        Assert.Equal("startedAtUtc", refusal.ParamName);
    }

    [Fact]
    public void A_culture_needs_an_id_and_conditions() {
        // Arrange
        CultureId              anyId         = Any.CultureId().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        DateTimeOffset         anyStart      = Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Culture(null!, anyConditions, anyStart));
        Assert.Throws<ArgumentNullException>(() => new Culture(anyId, null!, anyStart));
    }

    // ---- Inoculation -----------------------------------------------------------------

    [Fact]
    public void Inoculating_adds_a_population_at_age_zero_with_the_initial_uncertainty() {
        // Arrange
        Culture       culture  = NewCulture();
        Species       species  = FastGrower();
        Concentration inoculum = Inoculum(1e3);

        // Act
        MicrobialPopulation population = culture.Inoculate(species, inoculum);

        // Assert
        Assert.Same(species, population.Species);
        Assert.Same(inoculum, population.Concentration);
        Assert.Equal(0, population.AgeHours);
        Assert.Equal(Culture.InitialRelativeUncertainty, population.RelativeUncertainty);
        Assert.Single(culture.Populations);
        Assert.Same(population, culture.Populations[0]);
    }

    [Fact]
    public void A_species_with_a_lag_is_inoculated_in_the_lag_phase() {
        // Arrange
        Culture culture = NewCulture();

        // Act
        MicrobialPopulation population = culture.Inoculate(FastGrower(lagHours: 3), Inoculum(1e3));

        // Assert
        Assert.Equal(GrowthPhase.Lag, population.Phase);
    }

    [Fact]
    public void An_inoculum_just_below_the_carrying_capacity_is_accepted() {
        // Arrange
        Culture culture = NewCulture();

        // Act
        MicrobialPopulation population = culture.Inoculate(FastGrower(), Inoculum(99_999_999));

        // Assert
        Assert.Equal(99_999_999, population.Concentration.CellsPerMillilitre);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1e8)]
    [InlineData(2e8)]
    public void An_inoculum_that_is_empty_or_reaches_the_carrying_capacity_is_refused(double cellsPerMillilitre) {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => culture.Inoculate(species, Inoculum(cellsPerMillilitre)));

        // Assert
        Assert.Equal("inoculum", refusal.ParamName);
        Assert.Empty(culture.Populations);
    }

    [Fact]
    public void A_species_cannot_be_inoculated_twice() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.Inoculate(species, Inoculum(1e3)));
        Assert.Single(culture.Populations);
    }

    [Fact]
    public void Species_are_told_apart_by_name_not_by_instance() {
        // Arrange
        Culture culture  = NewCulture();
        Species species  = FastGrower();
        Species sameName = Any.Species().WithBinomialName(species.BinomialName).Generate();
        culture.Inoculate(species, Inoculum(1e3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.Inoculate(sameName, Inoculum(10)));
    }

    [Fact]
    public void A_culture_holds_up_to_four_species() {
        // Arrange
        Culture       culture = NewCulture();
        List<Species> four    = Any.ListOf(Any.Species()).WithCount(Culture.MaxSpecies).Distinct().Generate();

        // Act
        foreach (Species anySpecies in four) {
            culture.Inoculate(anySpecies, Inoculum(10));
        }

        // Assert
        Assert.Equal(Culture.MaxSpecies, culture.Populations.Count);
    }

    [Fact]
    public void A_fifth_species_is_refused() {
        // Arrange
        Culture       culture = NewCulture();
        List<Species> five    = Any.ListOf(Any.Species()).WithCount(Culture.MaxSpecies + 1).Distinct().Generate();
        foreach (Species anySpecies in five.Take(Culture.MaxSpecies)) {
            culture.Inoculate(anySpecies, Inoculum(10));
        }

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => culture.Inoculate(five[Culture.MaxSpecies], Inoculum(10)));

        // Assert
        Assert.Equal(Culture.MaxSpecies, culture.Populations.Count);
        Assert.Contains("4", refusal.Message);
    }

    [Fact]
    public void Species_are_inoculated_at_time_zero_only() {
        // Arrange
        Culture culture = NewCulture();
        Species first   = FastGrower();
        Species second  = Samples.Sibling(first);
        culture.Inoculate(first, Inoculum(1e3));
        culture.Advance(1);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.Inoculate(second, Inoculum(1e3)));
    }

    [Fact]
    public void Inoculation_needs_a_species_and_an_inoculum() {
        // Arrange
        Culture culture = NewCulture();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => culture.Inoculate(null!, Inoculum(1e3)));
        Assert.Throws<ArgumentNullException>(() => culture.Inoculate(FastGrower(), null!));
    }

    [Fact]
    public void The_capacity_a_species_is_judged_against_depends_on_the_chamber_conditions() {
        // Arrange: microgravity raises the capacity of 1e8 by 30 %: an inoculum of 1.2e8 fits there but not at 1 g.
        Culture microgravity = Samples.NewCulture(new ExperimentalConditions(0, 30, 10, 5));
        Culture earthlike    = NewCulture();
        Species species      = FastGrower();

        // Act
        microgravity.Inoculate(species, Inoculum(1.2e8));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => earthlike.Inoculate(species, Inoculum(1.2e8)));
    }

    // ---- Interactions ----------------------------------------------------------------

    [Fact]
    public void Species_do_not_interact_until_told_to() {
        // Arrange
        Culture culture = NewCulture();
        Species first   = FastGrower();
        Species second  = Samples.Sibling(first);
        culture.Inoculate(first, Inoculum(1e3));
        culture.Inoculate(second, Inoculum(1e3));

        // Assert
        Assert.Equal(0, culture.InteractionOf(first, second));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(0.37)]
    [InlineData(2)]
    public void An_interaction_inside_the_range_is_remembered_in_one_direction_only(double coefficient) {
        // Arrange
        Culture culture = NewCulture();
        Species first   = FastGrower();
        Species second  = Samples.Sibling(first);
        culture.Inoculate(first, Inoculum(1e3));
        culture.Inoculate(second, Inoculum(1e3));

        // Act
        culture.SetInteraction(first, second, coefficient);

        // Assert
        Assert.Equal(coefficient, culture.InteractionOf(first, second));
        Assert.Equal(0, culture.InteractionOf(second, first));
    }

    [Theory]
    [InlineData(-1.001)]
    [InlineData(2.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_interaction_outside_the_range_is_refused(double coefficient) {
        // Arrange
        Culture culture = NewCulture();
        Species first   = FastGrower();
        Species second  = Samples.Sibling(first);
        culture.Inoculate(first, Inoculum(1e3));
        culture.Inoculate(second, Inoculum(1e3));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => culture.SetInteraction(first, second, coefficient));

        // Assert
        Assert.Equal("coefficient", refusal.ParamName);
        Assert.Equal(0, culture.InteractionOf(first, second));
    }

    [Fact]
    public void A_species_does_not_interact_with_itself() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        Species sameName = Any.Species().WithBinomialName(species.BinomialName).Generate();
        culture.Inoculate(species, Inoculum(1e3));

        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => culture.SetInteraction(species, sameName, 0.5));

        // Assert
        Assert.Equal("competitor", refusal.ParamName);
    }

    [Fact]
    public void Both_species_of_an_interaction_must_be_in_the_culture() {
        // Arrange
        Culture culture  = NewCulture();
        Species inside   = FastGrower();
        Species outside  = Samples.Sibling(inside);
        culture.Inoculate(inside, Inoculum(1e3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.SetInteraction(inside, outside, 0.5));
        Assert.Throws<InvalidOperationException>(() => culture.SetInteraction(outside, inside, 0.5));
    }

    [Fact]
    public void An_interaction_needs_two_species() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => culture.SetInteraction(null!, species, 0.5));
        Assert.Throws<ArgumentNullException>(() => culture.SetInteraction(species, null!, 0.5));
    }

    // ---- Advancing -------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(720.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_culture_advances_by_zero_exclusive_to_thirty_days(double hours) {
        // Arrange
        Culture culture = NewCulture();
        culture.Inoculate(FastGrower(), Inoculum(1e3));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => culture.Advance(hours));

        // Assert
        Assert.Equal("hours", refusal.ParamName);
        Assert.Equal(0, culture.ElapsedHours);
    }

    [Fact]
    public void An_uninoculated_culture_cannot_advance() {
        // Arrange
        Culture culture = NewCulture();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.Advance(1));
    }

    [Fact]
    public void Time_accumulates_over_successive_advances() {
        // Arrange
        Culture culture = NewCulture();
        culture.Inoculate(FastGrower(), Inoculum(1e3));
        DateTimeOffset start = culture.StartedAtUtc;

        // Act
        culture.Advance(10);
        culture.Advance(2.5);

        // Assert
        Assert.Equal(12.5, culture.ElapsedHours);
        Assert.Equal(start.AddHours(12.5), culture.CurrentTimeUtc);
    }

    [Fact]
    public void A_maximum_advance_of_thirty_days_is_accepted() {
        // Arrange
        Culture culture = NewCulture();
        culture.Inoculate(FastGrower(), Inoculum(1e3));

        // Act
        culture.Advance(Culture.MaxAdvanceHours);

        // Assert
        Assert.Equal(720, culture.ElapsedHours);
    }

    [Fact]
    public void A_growing_population_follows_exponential_growth_while_the_medium_is_far_from_full() {
        // Arrange: 0.5 h-1 * 10 / 10.5 (nutrient term) = 0.476 h-1; 10 h make 0.476 * 10 / ln 2 = 6.87 generations in the continuous model.
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));

        // Act
        culture.Advance(10);

        // Assert: the 0.1 h Euler steps fall a little short of the continuous figure.
        Assert.InRange(culture.GenerationsOf(species), 6.5, 6.9);
        Assert.Equal(GrowthPhase.Exponential, culture.Estimate(species).Phase);
    }

    [Fact]
    public void Growth_slows_to_the_carrying_capacity_and_the_population_becomes_stationary() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));

        // Act
        culture.Advance(300);

        // Assert
        EstimatedState estimate = culture.Estimate(species);
        Assert.InRange(estimate.Concentration.CellsPerMillilitre, 0.95e8, 1e8);
        Assert.Equal(GrowthPhase.Stationary, estimate.Phase);
    }

    [Fact]
    public void A_population_never_exceeds_the_carrying_capacity() {
        // Arrange: a fast species in a rich medium, advanced in big steps, is the easiest way to overshoot an Euler scheme.
        Species  species = Samples.GrowerAt30C(maxRatePerHour: 5, lagHours: 0, yieldCellsPerGram: 1e10);
        Culture  culture = Samples.NewCulture(new ExperimentalConditions(1, 30, 50, 5));
        culture.Inoculate(species, Inoculum(1));

        // Act
        culture.Advance(720);

        // Assert: 50 g/L * 1e10 cells/g / 1000 = 5e8 cells/mL.
        Assert.InRange(culture.Estimate(species).Concentration.CellsPerMillilitre, 0, 5e8);
    }

    [Fact]
    public void The_population_holds_during_the_lag_phase() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower(lagHours: 5);
        culture.Inoculate(species, Inoculum(1e3));

        // Act
        culture.Advance(3);

        // Assert
        EstimatedState estimate = culture.Estimate(species);
        Assert.Equal(1e3, estimate.Concentration.CellsPerMillilitre);
        Assert.Equal(GrowthPhase.Lag, estimate.Phase);
        Assert.Equal(0, culture.GenerationsOf(species));
    }

    [Fact]
    public void Growth_resumes_once_the_lag_has_elapsed() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower(lagHours: 5);
        culture.Inoculate(species, Inoculum(1e3));

        // Act
        culture.Advance(5.5);
        culture.Advance(10);

        // Assert
        Assert.True(culture.GenerationsOf(species) > 4);
        Assert.Equal(GrowthPhase.Exponential, culture.Estimate(species).Phase);
    }

    [Fact]
    public void Time_in_lag_is_the_same_whether_advanced_at_once_or_in_pieces() {
        // Arrange
        Culture atOnce   = NewCulture();
        Culture inPieces = NewCulture();
        Species species  = FastGrower(lagHours: 5);
        atOnce.Inoculate(species, Inoculum(1e3));
        inPieces.Inoculate(species, Inoculum(1e3));

        // Act
        atOnce.Advance(10);
        inPieces.Advance(4);
        inPieces.Advance(6);

        // Assert
        Assert.Equal(atOnce.Estimate(species).Concentration.CellsPerMillilitre, inPieces.Estimate(species).Concentration.CellsPerMillilitre, precision: 3);
    }

    [Fact]
    public void Without_observation_the_uncertainty_drifts_by_two_percent_an_hour() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));

        // Act
        culture.Advance(10);

        // Assert: 0.05 + 10 * 0.02
        Assert.Equal(0.25, culture.Estimate(species).RelativeUncertainty, precision: 9);
    }

    [Fact]
    public void The_uncertainty_saturates_at_one() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));

        // Act
        culture.Advance(200);

        // Assert
        Assert.Equal(1, culture.Estimate(species).RelativeUncertainty);
    }

    [Fact]
    public void A_species_outside_its_cardinal_temperatures_dies_off_at_ten_percent_an_hour() {
        // Arrange: 50 °C is above the species' 45 °C maximum.
        Culture culture = Samples.NewCulture(new ExperimentalConditions(1, 50, 10, 5));
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e4));

        // Act
        culture.Advance(10);

        // Assert: e^(-0.1 * 10) = e^-1
        EstimatedState estimate = culture.Estimate(species);
        Assert.Equal(1e4 * Math.Exp(-1), estimate.Concentration.CellsPerMillilitre, precision: 3);
        Assert.Equal(GrowthPhase.Death, estimate.Phase);
    }

    [Fact]
    public void A_population_below_one_cell_per_millilitre_is_extinct() {
        // Arrange: from 100 cells/mL the die-off crosses 1 cell/mL after ln(100) / 0.1 = 46 h.
        Culture culture = Samples.NewCulture(new ExperimentalConditions(1, 50, 10, 5));
        Species species = FastGrower();
        MicrobialPopulation population = culture.Inoculate(species, Inoculum(100));

        // Act
        culture.Advance(60);

        // Assert
        Assert.True(population.IsExtinct);
        Assert.Equal(0, culture.Estimate(species).Concentration.CellsPerMillilitre);
        Assert.Equal(GrowthPhase.Death, culture.Estimate(species).Phase);
    }

    [Fact]
    public void A_competitor_slows_the_growth_of_the_species_it_weighs_on() {
        // Arrange
        Species affected   = FastGrower();
        Species competitor = Samples.Sibling(affected);
        Culture alone      = NewCulture();
        Culture competing  = NewCulture();
        alone.Inoculate(affected, Inoculum(1e6));
        competing.Inoculate(affected, Inoculum(1e6));
        competing.Inoculate(competitor, Inoculum(5e7));
        competing.SetInteraction(affected, competitor, 1);

        // Act
        alone.Advance(10);
        competing.Advance(10);

        // Assert
        Assert.True(competing.GenerationsOf(affected) < alone.GenerationsOf(affected));
    }

    [Fact]
    public void A_competitor_that_is_not_declared_has_no_effect() {
        // Arrange
        Species affected   = FastGrower();
        Species bystander  = Samples.Sibling(affected);
        Culture alone      = NewCulture();
        Culture together   = NewCulture();
        alone.Inoculate(affected, Inoculum(1e6));
        together.Inoculate(affected, Inoculum(1e6));
        together.Inoculate(bystander, Inoculum(5e7));

        // Act
        alone.Advance(10);
        together.Advance(10);

        // Assert
        Assert.Equal(alone.GenerationsOf(affected), together.GenerationsOf(affected), precision: 9);
    }

    [Fact]
    public void A_facilitating_species_speeds_up_growth() {
        // Arrange
        Species affected    = FastGrower();
        Species facilitator = Samples.Sibling(affected);
        Culture alone       = NewCulture();
        Culture helped      = NewCulture();
        alone.Inoculate(affected, Inoculum(1e6));
        helped.Inoculate(affected, Inoculum(1e6));
        helped.Inoculate(facilitator, Inoculum(5e7));
        helped.SetInteraction(affected, facilitator, -1);

        // Act
        alone.Advance(5);
        helped.Advance(5);

        // Assert
        Assert.True(helped.GenerationsOf(affected) > alone.GenerationsOf(affected));
    }

    [Fact]
    public void An_interaction_is_felt_by_the_affected_species_only() {
        // Arrange
        Species affected   = FastGrower();
        Species competitor = Samples.Sibling(affected);
        Culture reference  = NewCulture();
        Culture weighed    = NewCulture();
        foreach (Culture culture in new[] { reference, weighed }) {
            culture.Inoculate(affected, Inoculum(1e6));
            culture.Inoculate(competitor, Inoculum(1e6));
        }

        weighed.SetInteraction(affected, competitor, 2);

        // Act
        reference.Advance(8);
        weighed.Advance(8);

        // Assert: the competitor itself is not slowed down by an interaction declared the other way round.
        Assert.Equal(reference.GenerationsOf(competitor), weighed.GenerationsOf(competitor), precision: 6);
    }

    // ---- Observations ----------------------------------------------------------------

    [Fact]
    public void An_observation_is_blended_by_inverse_variance_into_the_estimate() {
        // Arrange: model 1000 +/- 5 %; observation 2000 +/- 5 %: equal weights give the mean and 5 % / sqrt 2.
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1000));
        Observation observation = Samples.ObservationOf(culture, species, 0, 2000, 0.05);

        // Act
        culture.Record(observation);

        // Assert
        EstimatedState estimate = culture.Estimate(species);
        Assert.Equal(1500, estimate.Concentration.CellsPerMillilitre, precision: 6);
        Assert.Equal(0.05 / Math.Sqrt(2), estimate.RelativeUncertainty, precision: 9);
        Assert.Equal(1, estimate.ObservationCount);
        Assert.False(estimate.IsModelOnly);
        Assert.Same(observation, Assert.Single(culture.Observations));
    }

    [Fact]
    public void A_precise_observation_pulls_the_estimate_almost_all_the_way() {
        // Arrange: weights 1/0.05^2 = 400 for the model, 1/0.01^2 = 10 000 for the observation.
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1000));

        // Act
        culture.Record(Samples.ObservationOf(culture, species, 0, 2000, 0.01));

        // Assert
        Assert.Equal((400 * 1000.0 + 10_000 * 2000.0) / 10_400, culture.Estimate(species).Concentration.CellsPerMillilitre, precision: 6);
    }

    [Fact]
    public void An_observation_makes_the_estimate_more_certain_than_the_model_alone() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1000));
        culture.Advance(10);
        double modelUncertainty = culture.Estimate(species).RelativeUncertainty;

        // Act
        culture.Record(Samples.ObservationOf(culture, species, 10, culture.Estimate(species).Concentration.CellsPerMillilitre, 0.2));

        // Assert
        Assert.True(culture.Estimate(species).RelativeUncertainty < modelUncertainty);
    }

    [Fact]
    public void An_estimate_without_observation_is_model_only() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));

        // Act
        EstimatedState estimate = culture.Estimate(species);

        // Assert
        Assert.True(estimate.IsModelOnly);
        Assert.Equal(0, estimate.ElapsedHours);
        Assert.Equal(1e3, estimate.Concentration.CellsPerMillilitre);
    }

    [Fact]
    public void Observations_are_counted_per_species() {
        // Arrange
        Culture culture = NewCulture();
        Species observed = FastGrower();
        Species ignored  = Samples.Sibling(observed);
        culture.Inoculate(observed, Inoculum(1e3));
        culture.Inoculate(ignored, Inoculum(1e3));

        // Act
        culture.Record(Samples.ObservationOf(culture, observed, 0, 1e3, 0.1));
        culture.Record(Samples.ObservationOf(culture, observed, 0, 1e3, 0.1));

        // Assert
        Assert.Equal(2, culture.Estimate(observed).ObservationCount);
        Assert.True(culture.Estimate(ignored).IsModelOnly);
    }

    [Theory]
    [InlineData(9.5)]
    [InlineData(10)]
    [InlineData(10.5)]
    public void An_observation_taken_within_half_an_hour_of_now_is_accepted(double elapsedHours) {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));
        culture.Advance(10);

        // Act
        culture.Record(Samples.ObservationOf(culture, species, elapsedHours, 1e5, 0.1));

        // Assert
        Assert.Single(culture.Observations);
    }

    [Theory]
    [InlineData(9.49)]
    [InlineData(10.51)]
    [InlineData(0)]
    public void An_observation_taken_at_another_time_is_refused(double elapsedHours) {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));
        culture.Advance(10);
        Observation stale = Samples.ObservationOf(culture, species, elapsedHours, 1e5, 0.1);

        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => culture.Record(stale));

        // Assert
        Assert.Equal("observation", refusal.ParamName);
        Assert.Empty(culture.Observations);
    }

    [Fact]
    public void An_observation_of_another_culture_is_refused() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));
        Observation foreign = Any.Observation()
                                 .WithCultureId(id => id.WithValue(v => v.DifferentFrom(culture.Id.Value)))
                                 .WithSpecies(species)
                                 .WithElapsedHours(0)
                                 .Generate();

        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => culture.Record(foreign));

        // Assert
        Assert.Equal("observation", refusal.ParamName);
        Assert.Empty(culture.Observations);
    }

    [Fact]
    public void An_observation_of_a_species_that_is_not_grown_is_refused() {
        // Arrange
        Culture culture = NewCulture();
        Species grown   = FastGrower();
        Species absent  = Samples.Sibling(grown);
        culture.Inoculate(grown, Inoculum(1e3));
        Observation observation = Samples.ObservationOf(culture, absent, 0, 1e3, 0.1);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.Record(observation));
        Assert.Empty(culture.Observations);
    }

    [Fact]
    public void Recording_needs_an_observation() {
        // Arrange
        Culture culture = NewCulture();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => culture.Record(null!));
    }

    [Fact]
    public void Estimating_a_species_that_is_not_grown_is_refused() {
        // Arrange
        Culture culture = NewCulture();
        Species anySpecies = Any.Species().Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.Estimate(anySpecies));
    }

    // ---- Predictions -----------------------------------------------------------------

    [Fact]
    public void A_prediction_leaves_the_culture_untouched() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));
        culture.Advance(2);
        EstimatedState before = culture.Estimate(species);

        // Act
        culture.Predict(species, 100);

        // Assert
        EstimatedState after = culture.Estimate(species);
        Assert.Equal(2, culture.ElapsedHours);
        Assert.Equal(before.Concentration, after.Concentration);
        Assert.Equal(before.RelativeUncertainty, after.RelativeUncertainty);
        Assert.Equal(before.Phase, after.Phase);
    }

    [Fact]
    public void A_prediction_is_what_advancing_would_have_produced() {
        // Arrange
        Culture predicting = NewCulture();
        Culture advancing  = NewCulture();
        Species species    = FastGrower();
        foreach (Culture culture in new[] { predicting, advancing }) {
            culture.Inoculate(species, Inoculum(1e3));
            culture.Advance(3);
        }

        // Act
        Prediction prediction = predicting.Predict(species, 12);
        advancing.Advance(12);

        // Assert
        Assert.Equal(advancing.Estimate(species).Concentration, prediction.Expected);
        Assert.Equal(advancing.Estimate(species).RelativeUncertainty, prediction.RelativeUncertainty, precision: 12);
    }

    [Fact]
    public void A_prediction_is_stamped_with_the_moment_it_was_made_and_its_horizon() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));
        culture.Advance(7);

        // Act
        Prediction prediction = culture.Predict(species, 5);

        // Assert
        Assert.Equal(7, prediction.MadeAtHours);
        Assert.Equal(5, prediction.HorizonHours);
        Assert.Equal(12, prediction.TargetHours);
        Assert.Same(species, prediction.Species);
    }

    [Fact]
    public void A_prediction_is_never_more_certain_than_the_estimate_it_started_from() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));
        culture.Advance(4);
        double now = culture.Estimate(species).RelativeUncertainty;

        // Act
        Prediction prediction = culture.Predict(species, 5);

        // Assert: 0.05 + 4 * 0.02 now, plus 5 * 0.02 further.
        Assert.Equal(now + 0.1, prediction.RelativeUncertainty, precision: 9);
    }

    [Fact]
    public void A_prediction_can_be_confirmed_by_the_observation_taken_at_its_target() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));
        Prediction prediction = culture.Predict(species, 6);
        culture.Advance(6);

        // Act
        Observation observation = Samples.ObservationOf(culture, species, 6, culture.Estimate(species).Concentration.CellsPerMillilitre, 0.1);

        // Assert
        Assert.True(prediction.IsConfirmedBy(observation));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(720.001)]
    [InlineData(double.NaN)]
    public void A_horizon_outside_zero_exclusive_to_thirty_days_is_refused(double horizonHours) {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => culture.Predict(species, horizonHours));

        // Assert
        Assert.Equal("horizonHours", refusal.ParamName);
    }

    [Fact]
    public void Predicting_a_species_that_is_not_grown_is_refused() {
        // Arrange
        Culture culture = NewCulture();
        Species anySpecies = Any.Species().Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.Predict(anySpecies, 5));
    }

    // ---- Generations -----------------------------------------------------------------

    [Fact]
    public void A_species_has_gone_through_no_generation_when_inoculated() {
        // Arrange
        Culture       culture      = NewCulture();
        Species       species      = FastGrower();
        Concentration anyInoculum  = Any.Concentration().WithCellsPerMillilitre(c => c.GreaterThan(0).LessThan(1e8)).Generate();
        culture.Inoculate(species, anyInoculum);

        // Act
        double generations = culture.GenerationsOf(species);

        // Assert
        Assert.Equal(0, generations);
    }

    [Fact]
    public void Generations_of_a_species_that_is_not_grown_are_refused() {
        // Arrange
        Culture culture = NewCulture();
        Species anySpecies = Any.Species().Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.GenerationsOf(anySpecies));
    }

    // ---- Termination -----------------------------------------------------------------

    [Fact]
    public void A_terminated_culture_says_so() {
        // Arrange
        Culture culture = NewCulture();

        // Act
        culture.Terminate();

        // Assert
        Assert.True(culture.IsTerminated);
    }

    [Fact]
    public void A_culture_is_terminated_once() {
        // Arrange
        Culture culture = NewCulture();
        culture.Terminate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.Terminate());
    }

    [Fact]
    public void A_terminated_culture_refuses_every_change() {
        // Arrange
        Culture culture = NewCulture();
        Species first   = FastGrower();
        Species second  = Samples.Sibling(first);
        culture.Inoculate(first, Inoculum(1e3));
        culture.Inoculate(second, Inoculum(1e3));
        Observation observation = Samples.ObservationOf(culture, first, 0, 1e3, 0.1);
        culture.Terminate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => culture.Inoculate(Any.Species().Generate(), Inoculum(10)));
        Assert.Throws<InvalidOperationException>(() => culture.SetInteraction(first, second, 0.5));
        Assert.Throws<InvalidOperationException>(() => culture.Advance(1));
        Assert.Throws<InvalidOperationException>(() => culture.Record(observation));
    }

    [Fact]
    public void A_terminated_culture_can_still_be_read() {
        // Arrange
        Culture culture = NewCulture();
        Species species = FastGrower();
        culture.Inoculate(species, Inoculum(1e3));
        culture.Advance(5);
        culture.Terminate();

        // Act
        EstimatedState estimate = culture.Estimate(species);

        // Assert
        Assert.Equal(5, estimate.ElapsedHours);
        Assert.Equal(5, culture.ElapsedHours);
    }

}
