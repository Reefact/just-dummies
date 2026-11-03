using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class CultureTests {

    // A culture in which Scenario.Mesophile() grows: microgravity, 30 °C, at least 5 g/L.
    private static Culture AnyFavourableCulture() {
        return Scenario.FavourableCulture().Generate();
    }

    private static Species AnyOtherSpeciesThan(params Species[] others) {
        AnySpecies generator = Scenario.Mesophile();
        foreach (Species other in others) {
            generator = generator.WithBinomialName(n => n.DifferentFrom(other.BinomialName));
        }

        return generator.Generate();
    }

    private static Observation ObservationFor(Culture culture, Species species, double elapsed, double measured = 1e5, double uncertainty = 0.05) {
        return new Observation(culture.Id, species, elapsed, new Concentration(measured), uncertainty, ObservationMethod.PlateCount);
    }

    // ---------------------------------------------------------------- construction

    [Fact]
    public void A_new_culture_is_empty_at_time_zero_and_alive() {
        // Arrange
        CultureId              anyId         = Any.CultureId().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        DateTimeOffset         startedAt     = new DateTimeOffset(2031, 5, 17, 8, 30, 0, TimeSpan.Zero);

        // Act
        Culture culture = new Culture(anyId, anyConditions, startedAt);

        // Assert
        Assert.Same(anyId, culture.Id);
        Assert.Same(anyConditions, culture.Conditions);
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
    public void A_start_time_that_is_not_utc_is_refused(double offsetHours) {
        // Arrange
        CultureId              anyId         = Any.CultureId().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        DateTimeOffset         startedAt     = new DateTimeOffset(2031, 5, 17, 8, 30, 0, TimeSpan.FromHours(offsetHours));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Culture(anyId, anyConditions, startedAt));

        // Assert
        Assert.Equal("startedAtUtc", refusal.ParamName);
    }

    [Fact]
    public void A_start_time_expressed_in_utc_from_another_zone_is_accepted() {
        // Arrange: 10:30 at +02:00 is 08:30 UTC
        DateTimeOffset local = new DateTimeOffset(2031, 5, 17, 10, 30, 0, TimeSpan.FromHours(2));

        // Act
        Culture culture = Any.Culture().WithStartedAtUtc(local.ToUniversalTime()).Generate();

        // Assert
        Assert.Equal(TimeSpan.Zero, culture.StartedAtUtc.Offset);
    }

    [Fact]
    public void A_culture_without_an_id_or_conditions_is_refused() {
        // Arrange
        CultureId              anyId         = Any.CultureId().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Culture(null!, anyConditions, DateTimeOffset.UnixEpoch));
        Assert.Throws<ArgumentNullException>(() => new Culture(anyId, null!, DateTimeOffset.UnixEpoch));
    }

    // ---------------------------------------------------------------- inoculation

    [Fact]
    public void Inoculating_adds_a_population_at_the_inoculum_with_the_initial_uncertainty() {
        // Arrange
        Culture       anyCulture = AnyFavourableCulture();
        Species       anySpecies = Scenario.Mesophile().Generate();
        Concentration inoculum   = new Concentration(2500);

        // Act
        MicrobialPopulation population = anyCulture.Inoculate(anySpecies, inoculum);

        // Assert
        Assert.Same(inoculum, population.Concentration);
        Assert.Equal(Culture.InitialRelativeUncertainty, population.RelativeUncertainty);
        Assert.Equal(0, population.AgeHours);
        Assert.Same(population, Assert.Single(anyCulture.Populations));
    }

    [Fact]
    public void A_species_with_a_lag_starts_in_lag_and_one_without_starts_exponential() {
        // Arrange
        Culture anyCulture      = AnyFavourableCulture();
        Species laggy           = Scenario.Mesophile().WithLagHours(8).Generate();
        Species eager           = AnyOtherSpeciesThan(laggy);

        // Act
        MicrobialPopulation laggyPopulation = anyCulture.Inoculate(laggy, Scenario.AnyInoculum());
        MicrobialPopulation eagerPopulation = anyCulture.Inoculate(eager, Scenario.AnyInoculum());

        // Assert
        Assert.Equal(GrowthPhase.Lag, laggyPopulation.Phase);
        Assert.Equal(GrowthPhase.Exponential, eagerPopulation.Phase);
    }

    [Fact]
    public void The_same_species_cannot_be_inoculated_twice() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum()));

        // Assert
        Assert.Contains(anySpecies.BinomialName, refusal.Message);
    }

    [Fact]
    public void A_namesake_with_other_traits_counts_as_the_same_species() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        Species namesake   = Scenario.Mesophile().WithBinomialName(anySpecies.BinomialName).WithMaxGrowthRate(r => r.WithPerHour(0.3)).Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyCulture.Inoculate(namesake, Scenario.AnyInoculum()));
    }

    [Fact]
    public void Four_species_fit_and_a_fifth_does_not() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species first      = Scenario.Mesophile().Generate();
        Species second     = AnyOtherSpeciesThan(first);
        Species third      = AnyOtherSpeciesThan(first, second);
        Species fourth     = AnyOtherSpeciesThan(first, second, third);
        Species fifth      = AnyOtherSpeciesThan(first, second, third, fourth);
        foreach (Species species in new[] { first, second, third, fourth }) {
            anyCulture.Inoculate(species, Scenario.AnyInoculum());
        }

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyCulture.Inoculate(fifth, Scenario.AnyInoculum()));

        // Assert
        Assert.Equal(4, anyCulture.Populations.Count);
        Assert.Contains("4", refusal.Message);
    }

    [Fact]
    public void An_inoculum_is_refused_once_the_culture_has_grown() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species first      = Scenario.Mesophile().Generate();
        Species second     = AnyOtherSpeciesThan(first);
        anyCulture.Inoculate(first, Scenario.AnyInoculum());
        anyCulture.Advance(1);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyCulture.Inoculate(second, Scenario.AnyInoculum()));
    }

    [Fact]
    public void An_empty_inoculum_is_refused() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCulture.Inoculate(anySpecies, Concentration.Zero));
        Assert.Empty(anyCulture.Populations);
    }

    [Fact]
    public void An_inoculum_just_under_the_carrying_capacity_is_accepted_and_one_at_it_is_refused() {
        // Arrange
        Culture       anyCulture = AnyFavourableCulture();
        Species       anySpecies = Scenario.Mesophile().Generate();
        Concentration capacity   = anyCulture.Conditions.CarryingCapacity(anySpecies);
        Concentration below      = new Concentration(capacity.CellsPerMillilitre * 0.999);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyCulture.Inoculate(anySpecies, capacity));
        MicrobialPopulation         accepted = anyCulture.Inoculate(anySpecies, below);

        // Assert
        Assert.Equal("inoculum", refusal.ParamName);
        Assert.Same(below, accepted.Concentration);
    }

    [Fact]
    public void An_inoculum_above_the_carrying_capacity_is_refused() {
        // Arrange
        Culture       anyCulture = AnyFavourableCulture();
        Species       anySpecies = Scenario.Mesophile().Generate();
        Concentration capacity   = anyCulture.Conditions.CarryingCapacity(anySpecies);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCulture.Inoculate(anySpecies, new Concentration(capacity.CellsPerMillilitre * 1.5)));
    }

    [Fact]
    public void Inoculating_nobody_or_nothing_is_refused() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyCulture.Inoculate(null!, Scenario.AnyInoculum()));
        Assert.Throws<ArgumentNullException>(() => anyCulture.Inoculate(anySpecies, null!));
    }

    // ---------------------------------------------------------------- interactions

    [Fact]
    public void Two_species_do_not_interact_until_told_to() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species first      = Scenario.Mesophile().Generate();
        Species second     = AnyOtherSpeciesThan(first);
        anyCulture.Inoculate(first, Scenario.AnyInoculum());
        anyCulture.Inoculate(second, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Equal(0, anyCulture.InteractionOf(first, second));
        Assert.Equal(0, anyCulture.InteractionOf(second, first));
    }

    [Fact]
    public void An_interaction_is_directional() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species first      = Scenario.Mesophile().Generate();
        Species second     = AnyOtherSpeciesThan(first);
        anyCulture.Inoculate(first, Scenario.AnyInoculum());
        anyCulture.Inoculate(second, Scenario.AnyInoculum());

        // Act
        anyCulture.SetInteraction(first, second, 0.7);

        // Assert
        Assert.Equal(0.7, anyCulture.InteractionOf(first, second));
        Assert.Equal(0, anyCulture.InteractionOf(second, first));
    }

    [Fact]
    public void Setting_an_interaction_again_replaces_it() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species first      = Scenario.Mesophile().Generate();
        Species second     = AnyOtherSpeciesThan(first);
        anyCulture.Inoculate(first, Scenario.AnyInoculum());
        anyCulture.Inoculate(second, Scenario.AnyInoculum());
        anyCulture.SetInteraction(first, second, 0.7);

        // Act
        anyCulture.SetInteraction(first, second, -0.4);

        // Assert
        Assert.Equal(-0.4, anyCulture.InteractionOf(first, second));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(2.0)]
    public void The_extreme_interaction_coefficients_are_accepted(double coefficient) {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species first      = Scenario.Mesophile().Generate();
        Species second     = AnyOtherSpeciesThan(first);
        anyCulture.Inoculate(first, Scenario.AnyInoculum());
        anyCulture.Inoculate(second, Scenario.AnyInoculum());

        // Act
        anyCulture.SetInteraction(first, second, coefficient);

        // Assert
        Assert.Equal(coefficient, anyCulture.InteractionOf(first, second));
    }

    [Theory]
    [InlineData(-1.001)]
    [InlineData(2.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_interaction_coefficient_outside_minus_one_to_two_is_refused(double coefficient) {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species first      = Scenario.Mesophile().Generate();
        Species second     = AnyOtherSpeciesThan(first);
        anyCulture.Inoculate(first, Scenario.AnyInoculum());
        anyCulture.Inoculate(second, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCulture.SetInteraction(first, second, coefficient));
        Assert.Equal(0, anyCulture.InteractionOf(first, second));
    }

    [Fact]
    public void A_species_does_not_interact_with_itself() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyCulture.SetInteraction(anySpecies, anySpecies, 0.5));

        // Assert
        Assert.Contains("carrying capacity", refusal.Message);
    }

    [Fact]
    public void An_interaction_needs_both_species_to_be_in_the_culture() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species inside     = Scenario.Mesophile().Generate();
        Species outside    = AnyOtherSpeciesThan(inside);
        anyCulture.Inoculate(inside, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyCulture.SetInteraction(inside, outside, 0.5));
        Assert.Throws<InvalidOperationException>(() => anyCulture.SetInteraction(outside, inside, 0.5));
    }

    [Fact]
    public void An_interaction_with_nobody_is_refused() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyCulture.SetInteraction(null!, anySpecies, 0.5));
        Assert.Throws<ArgumentNullException>(() => anyCulture.SetInteraction(anySpecies, null!, 0.5));
    }

    [Fact]
    public void A_competitor_slows_the_species_it_weighs_on_and_a_facilitator_speeds_it_up() {
        // Arrange: identical cultures but for the interaction; same species, same inocula
        Species       target        = Scenario.Mesophile().Generate();
        Species       other         = AnyOtherSpeciesThan(target);
        Concentration inoculum      = new Concentration(1e5);
        Concentration otherInoculum = new Concentration(1e5);
        Culture       alone         = AnyFavourableCulture();
        Culture       competed      = new Culture(alone.Id, alone.Conditions, alone.StartedAtUtc);
        Culture       facilitated   = new Culture(alone.Id, alone.Conditions, alone.StartedAtUtc);
        foreach (Culture culture in new[] { alone, competed, facilitated }) {
            culture.Inoculate(target, inoculum);
            culture.Inoculate(other, otherInoculum);
        }

        competed.SetInteraction(target, other, 1.0);
        facilitated.SetInteraction(target, other, -0.5);

        // Act
        foreach (Culture culture in new[] { alone, competed, facilitated }) {
            culture.Advance(10);
        }

        // Assert
        double neutral = alone.Estimate(target).Concentration.CellsPerMillilitre;
        Assert.True(competed.Estimate(target).Concentration.CellsPerMillilitre < neutral);
        Assert.True(facilitated.Estimate(target).Concentration.CellsPerMillilitre > neutral);
    }

    // ---------------------------------------------------------------- advancing

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(720.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_duration_outside_zero_exclusive_to_seven_hundred_twenty_hours_is_refused(double hours) {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        anyCulture.Inoculate(Scenario.Mesophile().Generate(), Scenario.AnyInoculum());

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCulture.Advance(hours));
        Assert.Equal(0, anyCulture.ElapsedHours);
    }

    [Fact]
    public void A_culture_cannot_advance_before_it_is_inoculated() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyCulture.Advance(1));

        // Assert
        Assert.Contains(anyCulture.Id.Value, refusal.Message);
    }

    [Fact]
    public void Advancing_moves_the_clock_by_that_many_hours() {
        // Arrange
        DateTimeOffset startedAt = new DateTimeOffset(2031, 5, 17, 8, 30, 0, TimeSpan.Zero);
        Culture        culture   = Scenario.FavourableCulture().WithStartedAtUtc(startedAt).Generate();
        culture.Inoculate(Scenario.Mesophile().Generate(), Scenario.AnyInoculum());

        // Act
        culture.Advance(5);
        culture.Advance(2.5);

        // Assert
        Assert.Equal(7.5, culture.ElapsedHours);
        Assert.Equal(startedAt.AddHours(7.5), culture.CurrentTimeUtc);
    }

    [Fact]
    public void The_longest_advance_of_seven_hundred_twenty_hours_is_accepted() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        anyCulture.Inoculate(Scenario.Mesophile().Generate(), Scenario.AnyInoculum());

        // Act
        anyCulture.Advance(Culture.MaxAdvanceHours);

        // Assert
        Assert.Equal(720, anyCulture.ElapsedHours);
    }

    [Fact]
    public void A_species_in_a_favourable_chamber_grows() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        Concentration inoculum = new Concentration(1000);
        anyCulture.Inoculate(anySpecies, inoculum);

        // Act
        anyCulture.Advance(5);

        // Assert
        EstimatedState state = anyCulture.Estimate(anySpecies);
        Assert.True(state.Concentration.CellsPerMillilitre > 1000);
        Assert.Equal(GrowthPhase.Exponential, state.Phase);
        Assert.True(anyCulture.GenerationsOf(anySpecies) > 0);
    }

    [Fact]
    public void Early_growth_follows_the_explicit_euler_steps_of_a_tenth_of_an_hour() {
        // Arrange: microgravity, 30 °C (the optimum) and 0.5 g/L give 1.0 * 1 * 0.5 * 1.2 = 0.6 per hour; a tenth of an hour
        // multiplies a population far from its capacity by 1 + 0.06 each step
        Culture anyCulture = Scenario.FavourableCulture()
                                     .WithConditions(c => c.WithNutrientGramsPerLitre(0.5))
                                     .Generate();
        Species anySpecies = Scenario.Mesophile().WithYieldCellsPerGram(1e10).Generate();
        anyCulture.Inoculate(anySpecies, new Concentration(1000));

        // Act
        anyCulture.Advance(2);

        // Assert: 20 steps
        double expected = 1000 * Math.Pow(1.06, 20);
        Assert.Equal(expected, anyCulture.Estimate(anySpecies).Concentration.CellsPerMillilitre, tolerance: expected * 0.01);
    }

    [Fact]
    public void A_population_in_lag_holds_its_concentration_then_grows() {
        // Arrange
        Culture       anyCulture = AnyFavourableCulture();
        Species       laggy      = Scenario.Mesophile().WithLagHours(10).Generate();
        Concentration inoculum   = new Concentration(1000);
        anyCulture.Inoculate(laggy, inoculum);

        // Act
        anyCulture.Advance(5);
        EstimatedState during = anyCulture.Estimate(laggy);
        anyCulture.Advance(10);
        EstimatedState after = anyCulture.Estimate(laggy);

        // Assert
        Assert.Equal(1000, during.Concentration.CellsPerMillilitre);
        Assert.Equal(GrowthPhase.Lag, during.Phase);
        Assert.True(after.Concentration.CellsPerMillilitre > 1000);
        Assert.Equal(GrowthPhase.Exponential, after.Phase);
    }

    [Fact]
    public void A_culture_left_to_grow_levels_off_below_its_carrying_capacity_and_is_stationary() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        Concentration capacity = anyCulture.Conditions.CarryingCapacity(anySpecies);

        // Act
        anyCulture.Advance(Culture.MaxAdvanceHours);

        // Assert
        EstimatedState state = anyCulture.Estimate(anySpecies);
        Assert.Equal(GrowthPhase.Stationary, state.Phase);
        Assert.InRange(state.Concentration.CellsPerMillilitre, capacity.CellsPerMillilitre * 0.95, capacity.CellsPerMillilitre * 1.0001);
    }

    [Fact]
    public void Outside_its_cardinal_temperatures_a_population_dies_at_a_tenth_per_hour() {
        // Arrange: 5 °C is under the 10 °C minimum of the species
        Culture       anyCulture = Scenario.FavourableCultureAt(5).Generate();
        Species       anySpecies = Scenario.Mesophile().Generate();
        Concentration inoculum   = new Concentration(1e6);
        anyCulture.Inoculate(anySpecies, inoculum);

        // Act
        anyCulture.Advance(10);

        // Assert
        EstimatedState state = anyCulture.Estimate(anySpecies);
        Assert.Equal(GrowthPhase.Death, state.Phase);
        Assert.Equal(1e6 * Math.Exp(-0.1 * 10), state.Concentration.CellsPerMillilitre, tolerance: 1e6 * 1e-6);
    }

    [Fact]
    public void A_population_dying_below_one_cell_per_millilitre_goes_extinct() {
        // Arrange
        Culture anyCulture = Scenario.FavourableCultureAt(60).Generate();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, new Concentration(5));

        // Act: 5 * exp(-0.1 * 30) is about 0.25 cells/mL
        anyCulture.Advance(30);

        // Assert
        MicrobialPopulation population = Assert.Single(anyCulture.Populations);
        Assert.True(population.IsExtinct);
        Assert.Equal(GrowthPhase.Death, population.Phase);
        Assert.Equal(0, anyCulture.Estimate(anySpecies).Concentration.CellsPerMillilitre);
    }

    [Fact]
    public void A_dead_population_stays_dead_when_conditions_would_otherwise_suit_it() {
        // Arrange
        Culture anyCulture = Scenario.FavourableCultureAt(60).Generate();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, new Concentration(5));
        anyCulture.Advance(40);

        // Act
        anyCulture.Advance(40);

        // Assert
        Assert.Equal(0, anyCulture.Estimate(anySpecies).Concentration.CellsPerMillilitre);
    }

    [Fact]
    public void Without_an_observation_the_uncertainty_drifts_up_by_two_hundredths_an_hour() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());

        // Act
        anyCulture.Advance(10);

        // Assert: 0.05 + 0.02 * 10
        Assert.Equal(0.25, anyCulture.Estimate(anySpecies).RelativeUncertainty, precision: 9);
    }

    [Fact]
    public void The_uncertainty_never_exceeds_one() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());

        // Act
        anyCulture.Advance(200);

        // Assert
        Assert.Equal(1.0, anyCulture.Estimate(anySpecies).RelativeUncertainty);
    }

    [Fact]
    public void Several_species_each_follow_their_own_dynamics() {
        // Arrange: the second one is too cold-loving for a 30 °C chamber
        Culture anyCulture = AnyFavourableCulture();
        Species warm       = Scenario.Mesophile().Generate();
        Species cold       = Scenario.Mesophile().WithCardinalTemperatures(-10, 0, 20).WithBinomialName(n => n.DifferentFrom(warm.BinomialName)).Generate();
        anyCulture.Inoculate(warm, new Concentration(1000));
        anyCulture.Inoculate(cold, new Concentration(1000));

        // Act
        anyCulture.Advance(5);

        // Assert
        Assert.True(anyCulture.Estimate(warm).Concentration.CellsPerMillilitre > 1000);
        Assert.True(anyCulture.Estimate(cold).Concentration.CellsPerMillilitre < 1000);
    }

    // ---------------------------------------------------------------- observations

    [Fact]
    public void Recording_an_observation_blends_it_into_the_estimate_by_inverse_variance() {
        // Arrange: model at 1e5 +-5 %, measurement at 2e5 +-5 %: equal weights, so the mean, and the uncertainty shrinks by sqrt(2)
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, new Concentration(1e5));
        Observation observation = ObservationFor(anyCulture, anySpecies, elapsed: 0, measured: 2e5, uncertainty: 0.05);

        // Act
        anyCulture.Record(observation);

        // Assert
        EstimatedState state = anyCulture.Estimate(anySpecies);
        Assert.Equal(1.5e5, state.Concentration.CellsPerMillilitre, precision: 3);
        Assert.Equal(0.05 / Math.Sqrt(2), state.RelativeUncertainty, precision: 9);
        Assert.Equal(1, state.ObservationCount);
        Assert.False(state.IsModelOnly);
        Assert.Same(observation, Assert.Single(anyCulture.Observations));
    }

    [Fact]
    public void A_more_precise_observation_pulls_the_estimate_harder() {
        // Arrange
        Species anySpecies = Scenario.Mesophile().Generate();
        Culture precise    = AnyFavourableCulture();
        Culture vague      = new Culture(precise.Id, precise.Conditions, precise.StartedAtUtc);
        precise.Inoculate(anySpecies, new Concentration(1e5));
        vague.Inoculate(anySpecies, new Concentration(1e5));

        // Act
        precise.Record(ObservationFor(precise, anySpecies, 0, measured: 2e5, uncertainty: 0.01));
        vague.Record(ObservationFor(vague, anySpecies, 0, measured: 2e5, uncertainty: 0.5));

        // Assert
        double preciseEstimate = precise.Estimate(anySpecies).Concentration.CellsPerMillilitre;
        double vagueEstimate   = vague.Estimate(anySpecies).Concentration.CellsPerMillilitre;
        Assert.True(preciseEstimate > vagueEstimate);
        Assert.InRange(vagueEstimate, 1e5, 2e5);
        Assert.InRange(preciseEstimate, 1e5, 2e5);
    }

    [Fact]
    public void An_observation_makes_the_estimate_more_certain_than_the_model_alone() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        anyCulture.Advance(10);
        double before = anyCulture.Estimate(anySpecies).RelativeUncertainty;

        // Act
        anyCulture.Record(ObservationFor(anyCulture, anySpecies, 10, measured: anyCulture.Estimate(anySpecies).Concentration.CellsPerMillilitre, uncertainty: 0.1));

        // Assert
        Assert.True(anyCulture.Estimate(anySpecies).RelativeUncertainty < before);
    }

    [Theory]
    [InlineData(10.5)]
    [InlineData(9.5)]
    [InlineData(10.0)]
    public void An_observation_within_half_an_hour_of_now_is_accepted(double elapsed) {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        anyCulture.Advance(10);

        // Act
        anyCulture.Record(ObservationFor(anyCulture, anySpecies, elapsed));

        // Assert
        Assert.Single(anyCulture.Observations);
    }

    [Theory]
    [InlineData(10.51)]
    [InlineData(9.49)]
    [InlineData(0.0)]
    public void An_observation_taken_more_than_half_an_hour_from_now_is_refused(double elapsed) {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        anyCulture.Advance(10);
        Observation observation = ObservationFor(anyCulture, anySpecies, elapsed);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyCulture.Record(observation));

        // Assert
        Assert.Equal("observation", refusal.ParamName);
        Assert.Empty(anyCulture.Observations);
    }

    [Fact]
    public void An_observation_of_another_culture_is_refused() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        CultureId   anotherId   = Any.CultureId().WithValue(v => v.DifferentFrom(anyCulture.Id.Value)).Generate();
        Observation observation = new Observation(anotherId, anySpecies, 0, Scenario.AnyInoculum(), 0.05, ObservationMethod.FlowCytometry);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyCulture.Record(observation));

        // Assert
        Assert.Contains(anotherId.Value, refusal.Message);
        Assert.Empty(anyCulture.Observations);
    }

    [Fact]
    public void An_observation_of_a_species_that_is_not_in_the_culture_is_refused() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species inside     = Scenario.Mesophile().Generate();
        Species outside    = AnyOtherSpeciesThan(inside);
        anyCulture.Inoculate(inside, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyCulture.Record(ObservationFor(anyCulture, outside, 0)));
        Assert.Empty(anyCulture.Observations);
    }

    [Fact]
    public void Nothing_can_be_recorded() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyCulture.Record(null!));
    }

    [Fact]
    public void Observations_are_counted_per_species() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species first      = Scenario.Mesophile().Generate();
        Species second     = AnyOtherSpeciesThan(first);
        anyCulture.Inoculate(first, Scenario.AnyInoculum());
        anyCulture.Inoculate(second, Scenario.AnyInoculum());

        // Act
        anyCulture.Record(ObservationFor(anyCulture, first, 0));
        anyCulture.Record(ObservationFor(anyCulture, first, 0));
        anyCulture.Record(ObservationFor(anyCulture, second, 0));

        // Assert
        Assert.Equal(2, anyCulture.Estimate(first).ObservationCount);
        Assert.Equal(1, anyCulture.Estimate(second).ObservationCount);
        Assert.Equal(3, anyCulture.Observations.Count);
    }

    // ---------------------------------------------------------------- estimates

    [Fact]
    public void Before_any_observation_the_estimate_is_the_model_alone() {
        // Arrange
        Culture       anyCulture = AnyFavourableCulture();
        Species       anySpecies = Scenario.Mesophile().Generate();
        Concentration inoculum   = Scenario.AnyInoculum();
        anyCulture.Inoculate(anySpecies, inoculum);

        // Act
        EstimatedState state = anyCulture.Estimate(anySpecies);

        // Assert
        Assert.True(state.IsModelOnly);
        Assert.Equal(inoculum, state.Concentration);
        Assert.Equal(0, state.ElapsedHours);
        Assert.Same(anySpecies, state.Species);
    }

    [Fact]
    public void An_estimate_is_stamped_with_the_time_of_the_culture() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        anyCulture.Advance(12.5);

        // Act / Assert
        Assert.Equal(12.5, anyCulture.Estimate(anySpecies).ElapsedHours);
    }

    [Fact]
    public void A_species_that_is_not_grown_has_no_estimate() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species inside     = Scenario.Mesophile().Generate();
        Species outside    = AnyOtherSpeciesThan(inside);
        anyCulture.Inoculate(inside, Scenario.AnyInoculum());

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyCulture.Estimate(outside));

        // Assert
        Assert.Contains(outside.BinomialName, refusal.Message);
    }

    // ---------------------------------------------------------------- predictions

    [Fact]
    public void A_prediction_looks_ahead_without_changing_the_culture() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        anyCulture.Advance(3);
        EstimatedState before = anyCulture.Estimate(anySpecies);

        // Act
        anyCulture.Predict(anySpecies, 50);

        // Assert
        EstimatedState after = anyCulture.Estimate(anySpecies);
        Assert.Equal(3, anyCulture.ElapsedHours);
        Assert.Equal(before.Concentration, after.Concentration);
        Assert.Equal(before.RelativeUncertainty, after.RelativeUncertainty);
        Assert.Equal(before.Phase, after.Phase);
    }

    [Fact]
    public void A_prediction_is_what_advancing_for_the_horizon_would_give() {
        // Arrange
        Species anySpecies = Scenario.Mesophile().Generate();
        Culture predicted  = AnyFavourableCulture();
        Culture advanced   = new Culture(predicted.Id, predicted.Conditions, predicted.StartedAtUtc);
        Concentration inoculum = Scenario.AnyInoculum();
        predicted.Inoculate(anySpecies, inoculum);
        advanced.Inoculate(anySpecies, inoculum);
        predicted.Advance(2);
        advanced.Advance(2);

        // Act
        Prediction prediction = predicted.Predict(anySpecies, 25);
        advanced.Advance(25);

        // Assert
        EstimatedState reached = advanced.Estimate(anySpecies);
        Assert.Equal(reached.Concentration.CellsPerMillilitre, prediction.Expected.CellsPerMillilitre, tolerance: reached.Concentration.CellsPerMillilitre * 1e-9);
        Assert.Equal(reached.RelativeUncertainty, prediction.RelativeUncertainty, precision: 9);
    }

    [Fact]
    public void A_prediction_is_stamped_with_when_it_was_made_and_how_far_it_looks() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        anyCulture.Advance(4);

        // Act
        Prediction prediction = anyCulture.Predict(anySpecies, 6);

        // Assert
        Assert.Equal(4, prediction.MadeAtHours);
        Assert.Equal(6, prediction.HorizonHours);
        Assert.Equal(10, prediction.TargetHours);
        Assert.Same(anySpecies, prediction.Species);
    }

    [Fact]
    public void A_prediction_is_never_more_certain_than_the_estimate_it_started_from() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        anyCulture.Advance(2);
        double now = anyCulture.Estimate(anySpecies).RelativeUncertainty;

        // Act
        Prediction prediction = anyCulture.Predict(anySpecies, 10);

        // Assert
        Assert.True(prediction.RelativeUncertainty > now);
    }

    [Fact]
    public void A_forecast_confirmed_by_the_culture_own_later_state_is_confirmed() {
        // Arrange
        Species anySpecies = Scenario.Mesophile().Generate();
        Culture anyCulture = AnyFavourableCulture();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        Prediction prediction = anyCulture.Predict(anySpecies, 5);
        anyCulture.Advance(5);
        Observation measured = ObservationFor(anyCulture, anySpecies, 5, measured: anyCulture.Estimate(anySpecies).Concentration.CellsPerMillilitre, uncertainty: 0.05);

        // Act / Assert
        Assert.True(prediction.IsConfirmedBy(measured));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-3.0)]
    [InlineData(720.001)]
    [InlineData(double.NaN)]
    public void A_horizon_outside_zero_exclusive_to_seven_hundred_twenty_hours_is_refused(double horizon) {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCulture.Predict(anySpecies, horizon));
    }

    [Fact]
    public void A_species_that_is_not_grown_cannot_be_predicted() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species inside     = Scenario.Mesophile().Generate();
        Species outside    = AnyOtherSpeciesThan(inside);
        anyCulture.Inoculate(inside, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyCulture.Predict(outside, 10));
    }

    // ---------------------------------------------------------------- generations

    [Fact]
    public void No_generation_has_elapsed_at_inoculation() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Equal(0, anyCulture.GenerationsOf(anySpecies));
    }

    [Fact]
    public void Generations_count_the_doublings_since_the_inoculum() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        Concentration inoculum = new Concentration(1000);
        anyCulture.Inoculate(anySpecies, inoculum);
        anyCulture.Advance(4);

        // Act
        double generations = anyCulture.GenerationsOf(anySpecies);

        // Assert
        double now = anyCulture.Estimate(anySpecies).Concentration.CellsPerMillilitre;
        Assert.Equal(Math.Log2(now / 1000), generations, precision: 9);
    }

    [Fact]
    public void A_species_that_is_not_grown_has_no_generations() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species inside     = Scenario.Mesophile().Generate();
        Species outside    = AnyOtherSpeciesThan(inside);
        anyCulture.Inoculate(inside, Scenario.AnyInoculum());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyCulture.GenerationsOf(outside));
    }

    [Fact]
    public void Generations_of_an_extinct_species_cannot_be_counted() {
        // Arrange
        Culture anyCulture = Scenario.FavourableCultureAt(60).Generate();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, new Concentration(5));
        anyCulture.Advance(40);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyCulture.GenerationsOf(anySpecies));
    }

    // ---------------------------------------------------------------- termination

    [Fact]
    public void A_terminated_culture_refuses_every_change() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species first      = Scenario.Mesophile().Generate();
        Species second     = AnyOtherSpeciesThan(first);
        anyCulture.Inoculate(first, Scenario.AnyInoculum());
        anyCulture.Inoculate(second, Scenario.AnyInoculum());
        anyCulture.Terminate();

        // Act / Assert
        Assert.True(anyCulture.IsTerminated);
        Assert.Throws<InvalidOperationException>(() => anyCulture.Advance(1));
        Assert.Throws<InvalidOperationException>(() => anyCulture.Record(ObservationFor(anyCulture, first, 0)));
        Assert.Throws<InvalidOperationException>(() => anyCulture.SetInteraction(first, second, 0.5));
        Assert.Throws<InvalidOperationException>(() => anyCulture.Inoculate(AnyOtherSpeciesThan(first, second), Scenario.AnyInoculum()));
        Assert.Throws<InvalidOperationException>(() => anyCulture.Terminate());
    }

    [Fact]
    public void A_terminated_culture_can_still_be_read() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        Species anySpecies = Scenario.Mesophile().Generate();
        anyCulture.Inoculate(anySpecies, Scenario.AnyInoculum());
        anyCulture.Advance(5);
        EstimatedState before = anyCulture.Estimate(anySpecies);

        // Act
        anyCulture.Terminate();

        // Assert
        Assert.Equal(before.Concentration, anyCulture.Estimate(anySpecies).Concentration);
        Assert.Equal(5, anyCulture.ElapsedHours);
        Assert.Single(anyCulture.Populations);
    }

    [Fact]
    public void An_untouched_culture_can_be_terminated() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();

        // Act
        anyCulture.Terminate();

        // Assert
        Assert.True(anyCulture.IsTerminated);
    }

    [Fact]
    public void The_refusal_names_the_terminated_culture() {
        // Arrange
        Culture anyCulture = AnyFavourableCulture();
        anyCulture.Terminate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyCulture.Terminate());

        // Assert
        Assert.Contains(anyCulture.Id.Value, refusal.Message);
        Assert.Contains("terminated", refusal.Message);
    }

}
