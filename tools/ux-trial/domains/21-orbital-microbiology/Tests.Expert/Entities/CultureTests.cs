using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.Entities;

public sealed class CultureTests {

    // ---- construction ----------------------------------------------------------------------------------------

    [Fact]
    public void A_new_culture_has_grown_for_no_time_and_holds_nothing() {
        // Arrange
        CultureId              anyId         = Any.CultureId().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Act
        Culture culture = Any.Culture().WithId(anyId).WithConditions(anyConditions).Generate();

        // Assert
        Assert.Same(anyId, culture.Id);
        Assert.Same(anyConditions, culture.Conditions);
        Assert.Equal(0d, culture.ElapsedHours);
        Assert.False(culture.IsTerminated);
        Assert.Empty(culture.Populations);
        Assert.Empty(culture.Observations);
    }

    [Fact]
    public void A_new_culture_reads_the_time_it_was_started_at() {
        // Arrange
        DateTimeOffset anyStart = Any.Culture().Generate().StartedAtUtc;

        // Act
        Culture culture = Any.Culture().WithStartedAtUtc(anyStart).Generate();

        // Assert
        Assert.Equal(anyStart, culture.StartedAtUtc);
        Assert.Equal(culture.StartedAtUtc, culture.CurrentTimeUtc);
    }

    [Fact]
    public void The_current_time_is_the_start_plus_the_hours_grown() {
        // Arrange
        Culture    culture = Scenes.AnyControlCulture().Generate();
        Species    species = Scenes.AnyGrowingSpecies().Generate();
        culture.Inoculate(species, new Concentration(1e4));

        // Act
        culture.Advance(36.5);

        // Assert
        Assert.Equal(TimeSpan.FromHours(36.5), culture.CurrentTimeUtc - culture.StartedAtUtc);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-5)]
    public void A_start_time_with_an_offset_from_UTC_is_refused(int offsetHours) {
        // Arrange
        AnyCulture anyCulture = Any.Culture();

        // Assert
        Assert.Throws<ArgumentException>(() => anyCulture.WithStartedAtUtc(start => start.WithOffset(TimeSpan.FromHours(offsetHours))).Generate());
    }

    [Fact]
    public void A_start_time_with_an_offset_of_thirty_minutes_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.Culture().WithStartedAtUtc(start => start.WithOffset(TimeSpan.FromMinutes(30))).Generate());
    }

    [Fact]
    public void A_culture_needs_an_id_and_conditions() {
        Assert.Throws<ArgumentNullException>(() => Any.Culture().WithId((CultureId)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Culture().WithConditions((ExperimentalConditions)null!).Generate());
    }

    // ---- inoculation -----------------------------------------------------------------------------------------

    [Fact]
    public void An_inoculated_species_enters_the_culture_at_the_concentration_it_was_given() {
        // Arrange
        Culture       culture  = Scenes.AnyControlCulture().Generate();
        Species       species  = Scenes.AnyGrowingSpecies().Generate();
        Concentration inoculum = new Concentration(2.5e5);

        // Act
        MicrobialPopulation population = culture.Inoculate(species, inoculum);

        // Assert
        Assert.Same(species, population.Species);
        Assert.Same(inoculum, population.Concentration);
        Assert.Equal(0d, population.AgeHours);
        Assert.Equal(Culture.InitialRelativeUncertainty, population.RelativeUncertainty);
        Assert.Same(population, Assert.Single(culture.Populations));
    }

    [Fact]
    public void An_inoculated_species_with_a_lag_starts_in_lag() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species laggard = Scenes.AnyGrowingSpecies().WithLagHours(6d).Generate();

        // Act
        MicrobialPopulation population = culture.Inoculate(laggard, Any.Concentration().Dilute().Generate());

        // Assert
        Assert.Equal(GrowthPhase.Lag, population.Phase);
    }

    [Fact]
    public void An_inoculated_species_without_lag_starts_in_exponential_phase() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species eager   = Scenes.AnyGrowingSpecies().WithLagHours(0d).Generate();

        // Act
        MicrobialPopulation population = culture.Inoculate(eager, Any.Concentration().Dilute().Generate());

        // Assert
        Assert.Equal(GrowthPhase.Exponential, population.Phase);
    }

    [Fact]
    public void Up_to_four_species_share_a_culture() {
        // Arrange
        Culture       culture = Scenes.AnyControlCulture().Generate();
        List<Species> anyFour = Any.ListOf(Any.Species()).WithCount(Culture.MaxSpecies).Distinct().Generate();

        // Act
        foreach (Species species in anyFour) {
            culture.Inoculate(species, Any.Concentration().Dilute().Generate());
        }

        // Assert
        Assert.Equal(4, culture.Populations.Count);
    }

    [Fact]
    public void A_fifth_species_is_refused() {
        // Arrange
        Culture       culture = Scenes.AnyControlCulture().Generate();
        List<Species> anyFive = Any.ListOf(Any.Species()).WithCount(Culture.MaxSpecies + 1).Distinct().Generate();
        foreach (Species species in anyFive.Take(Culture.MaxSpecies)) {
            culture.Inoculate(species, Any.Concentration().Dilute().Generate());
        }

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => culture.Inoculate(anyFive[Culture.MaxSpecies], Any.Concentration().Dilute().Generate()));
        Assert.Equal(4, culture.Populations.Count);
    }

    [Fact]
    public void The_same_species_is_not_inoculated_twice() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species species = Any.Species().Generate();
        culture.Inoculate(species, Any.Concentration().Dilute().Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.Inoculate(species, Any.Concentration().Dilute().Generate()));
    }

    [Fact]
    public void A_species_is_the_same_when_its_name_is_the_same_even_if_its_traits_differ() {
        // Arrange
        Culture culture      = Scenes.AnyControlCulture().Generate();
        Species species     = Any.Species().Generate();
        Species namesake    = Any.Species().WithBinomialName(species.BinomialName).Generate();
        culture.Inoculate(species, Any.Concentration().Dilute().Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.Inoculate(namesake, Any.Concentration().Dilute().Generate()));
    }

    [Fact]
    public void A_species_is_inoculated_at_time_zero_only() {
        // Arrange
        Culture culture      = Scenes.AnyControlCulture().Generate();
        Species species      = Scenes.AnyGrowingSpecies().Generate();
        Species latecomer    = Scenes.AnotherSpeciesThan(species);
        culture.Inoculate(species, Any.Concentration().Dilute().Generate());
        culture.Advance(0.1);

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.Inoculate(latecomer, Any.Concentration().Dilute().Generate()));
    }

    [Fact]
    public void An_empty_inoculum_is_refused() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species species = Any.Species().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => culture.Inoculate(species, Concentration.Zero));
    }

    [Fact]
    public void An_inoculum_at_the_carrying_capacity_is_refused() {
        // Arrange
        Culture       culture  = Scenes.AnyControlCulture().Generate();
        Species       species  = Any.Species().Generate();
        Concentration capacity = culture.Conditions.CarryingCapacity(species);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => culture.Inoculate(species, capacity));
    }

    [Fact]
    public void An_inoculum_above_the_carrying_capacity_is_refused() {
        // Arrange
        Culture       culture  = Scenes.AnyControlCulture().Generate();
        Species       species  = Any.Species().WithYieldCellsPerGram(1e9).Generate();
        Concentration tooMany  = new Concentration(culture.Conditions.CarryingCapacity(species).CellsPerMillilitre * 1.5);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => culture.Inoculate(species, tooMany));
    }

    [Fact]
    public void An_inoculum_just_below_the_carrying_capacity_is_accepted() {
        // Arrange
        Culture       culture  = Scenes.AnyControlCulture().Generate();
        Species       species  = Any.Species().Generate();
        Concentration nearlyFull = new Concentration(culture.Conditions.CarryingCapacity(species).CellsPerMillilitre * 0.999);

        // Act
        MicrobialPopulation population = culture.Inoculate(species, nearlyFull);

        // Assert
        Assert.Same(nearlyFull, population.Concentration);
    }

    [Fact]
    public void Microgravity_makes_room_for_an_inoculum_that_one_g_refuses() {
        // Arrange: capacity 5e7 at 1 g, 6.5e7 in microgravity
        Species       species  = Scenes.AnyGrowingSpecies().Generate();
        Concentration inoculum = new Concentration(6e7);
        Culture       control  = Scenes.AnyControlCulture().Generate();
        Culture       inOrbit  = Scenes.AnyControlCulture().WithConditions(conditions => conditions.OnTheControlBench().WithGravityG(0d)).Generate();

        // Act
        MicrobialPopulation population = inOrbit.Inoculate(species, inoculum);

        // Assert
        Assert.Same(inoculum, population.Concentration);
        Assert.Throws<ArgumentOutOfRangeException>(() => control.Inoculate(species, inoculum));
    }

    [Fact]
    public void A_species_and_an_inoculum_are_needed_to_inoculate() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => culture.Inoculate(null!, Any.Concentration().Dilute().Generate()));
        Assert.Throws<ArgumentNullException>(() => culture.Inoculate(Any.Species().Generate(), null!));
    }

    // ---- interactions ----------------------------------------------------------------------------------------

    private static (Culture Culture, Species First, Species Second) AnyCultureOfTwoSpecies() {
        Culture culture = Scenes.AnyControlCulture().Generate();
        Species first   = Scenes.AnyGrowingSpecies().Generate();
        Species second  = Scenes.AnotherSpeciesThan(first);
        culture.Inoculate(first, Any.Concentration().Dilute().Generate());
        culture.Inoculate(second, Any.Concentration().Dilute().Generate());
        return (culture, first, second);
    }

    [Fact]
    public void Species_that_were_never_given_an_interaction_do_not_interact() {
        // Arrange
        (Culture culture, Species first, Species second) = AnyCultureOfTwoSpecies();

        // Assert
        Assert.Equal(0d, culture.InteractionOf(first, second));
        Assert.Equal(0d, culture.InteractionOf(second, first));
    }

    [Fact]
    public void An_interaction_weighs_one_way_only() {
        // Arrange
        (Culture culture, Species affected, Species competitor) = AnyCultureOfTwoSpecies();

        // Act
        culture.SetInteraction(affected, competitor, 0.75);

        // Assert
        Assert.Equal(0.75, culture.InteractionOf(affected, competitor));
        Assert.Equal(0d, culture.InteractionOf(competitor, affected));
    }

    [Fact]
    public void An_interaction_set_again_replaces_the_previous_one() {
        // Arrange
        (Culture culture, Species affected, Species competitor) = AnyCultureOfTwoSpecies();
        culture.SetInteraction(affected, competitor, 0.75);

        // Act
        culture.SetInteraction(affected, competitor, -0.25);

        // Assert
        Assert.Equal(-0.25, culture.InteractionOf(affected, competitor));
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(0d)]
    [InlineData(2d)]
    public void An_interaction_from_facilitation_at_minus_one_to_competition_at_two_is_accepted(double coefficient) {
        // Arrange
        (Culture culture, Species affected, Species competitor) = AnyCultureOfTwoSpecies();

        // Act
        culture.SetInteraction(affected, competitor, coefficient);

        // Assert
        Assert.Equal(coefficient, culture.InteractionOf(affected, competitor));
    }

    [Theory]
    [InlineData(-1.001)]
    [InlineData(2.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_interaction_outside_minus_one_to_two_or_not_finite_is_refused(double coefficient) {
        // Arrange
        (Culture culture, Species affected, Species competitor) = AnyCultureOfTwoSpecies();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => culture.SetInteraction(affected, competitor, coefficient));
    }

    [Fact]
    public void A_species_does_not_interact_with_itself() {
        // Arrange
        (Culture culture, Species species, Species _) = AnyCultureOfTwoSpecies();

        // Assert
        Assert.Throws<ArgumentException>(() => culture.SetInteraction(species, species, Any.Double().Between(Culture.MinInteraction, Culture.MaxInteraction).Generate()));
    }

    [Fact]
    public void An_interaction_needs_both_species_in_the_culture() {
        // Arrange
        (Culture culture, Species present, Species other) = AnyCultureOfTwoSpecies();
        Species absent = Scenes.AnotherSpeciesThan(present, other);

        // Assert
        Assert.Throws<InvalidOperationException>(() => culture.SetInteraction(absent, present, 0.5));
        Assert.Throws<InvalidOperationException>(() => culture.SetInteraction(present, absent, 0.5));
    }

    [Fact]
    public void An_interaction_needs_two_species() {
        // Arrange
        (Culture culture, Species species, Species _) = AnyCultureOfTwoSpecies();

        // Assert
        Assert.Throws<ArgumentNullException>(() => culture.SetInteraction(null!, species, 0.5));
        Assert.Throws<ArgumentNullException>(() => culture.SetInteraction(species, null!, 0.5));
    }

    // ---- termination -----------------------------------------------------------------------------------------

    [Fact]
    public void A_terminated_culture_says_so() {
        // Arrange
        Culture culture = Scenes.AnyControlCulture().Generate();

        // Act
        culture.Terminate();

        // Assert
        Assert.True(culture.IsTerminated);
    }

    [Theory]
    [InlineData("Terminate")]
    [InlineData("Inoculate")]
    [InlineData("SetInteraction")]
    [InlineData("Advance")]
    [InlineData("Record")]
    public void A_terminated_culture_refuses_every_change(string change) {
        // Arrange
        (Culture culture, Species first, Species second) = AnyCultureOfTwoSpecies();
        Observation observation = Any.Observation().WithCultureId(culture.Id).WithSpecies(first).WithElapsedHours(0d).Generate();
        Species     newcomer    = Scenes.AnotherSpeciesThan(first, second);
        culture.Terminate();

        // Act
        Action attempt = change switch {
            "Terminate"      => () => culture.Terminate(),
            "Inoculate"      => () => culture.Inoculate(newcomer, Any.Concentration().Dilute().Generate()),
            "SetInteraction" => () => culture.SetInteraction(first, second, 0.5),
            "Advance"        => () => culture.Advance(1d),
            "Record"         => () => culture.Record(observation),
            _                => throw new ArgumentOutOfRangeException(nameof(change), change, "Unknown change.")
        };

        // Assert
        Assert.Throws<InvalidOperationException>(attempt);
    }

    [Fact]
    public void A_terminated_culture_can_still_be_read() {
        // Arrange
        (Culture culture, Species affected, Species competitor) = AnyCultureOfTwoSpecies();
        culture.SetInteraction(affected, competitor, 0.5);
        culture.Advance(5d);
        culture.Terminate();

        // Assert
        Assert.Equal(5d, culture.ElapsedHours);
        Assert.Equal(0.5, culture.InteractionOf(affected, competitor));
        Assert.Equal(affected, culture.Estimate(affected).Species);
        Assert.Equal(2, culture.Populations.Count);
    }

}
