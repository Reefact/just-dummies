using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class ExperimentalConditionsTests {

    // Gravity 0 g, 30 °C, 10 g/L, 5 mL unless a test says otherwise.
    private static ExperimentalConditions Build(double gravity = 0, double temperature = 30, double nutrient = 10, double volume = 5) {
        return new ExperimentalConditions(gravity, temperature, nutrient, volume);
    }

    [Fact]
    public void Conditions_keep_every_value_they_were_given() {
        // Act
        ExperimentalConditions conditions = Build(0.3, -5, 2.5, 40);

        // Assert
        Assert.Equal(0.3, conditions.GravityG);
        Assert.Equal(-5, conditions.TemperatureCelsius);
        Assert.Equal(2.5, conditions.NutrientGramsPerLitre);
        Assert.Equal(40, conditions.VolumeMillilitres);
    }

    [Fact]
    public void Arbitrary_conditions_are_always_legal_ones() {
        // Arrange / Act
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Assert: a centrifuged chamber (0.5 g or more) is a small one
        if (anyConditions.GravityG >= 0.5) {
            Assert.True(anyConditions.VolumeMillilitres <= 10);
        }
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(2.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_gravity_outside_zero_to_two_g_is_refused(double gravity) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(gravity: gravity, volume: 5));
    }

    [Theory]
    [InlineData(-20.001)]
    [InlineData(80.001)]
    [InlineData(double.NaN)]
    public void A_temperature_outside_minus_twenty_to_eighty_is_refused(double temperature) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(temperature: temperature));
    }

    [Theory]
    [InlineData(-20.0)]
    [InlineData(80.0)]
    public void The_extreme_temperatures_are_accepted(double temperature) {
        // Act
        ExperimentalConditions conditions = Build(temperature: temperature);

        // Assert
        Assert.Equal(temperature, conditions.TemperatureCelsius);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(50.001)]
    [InlineData(double.NaN)]
    public void A_nutrient_load_outside_zero_exclusive_to_fifty_is_refused(double nutrient) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(nutrient: nutrient));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(50.001)]
    [InlineData(double.NaN)]
    public void A_volume_outside_zero_exclusive_to_fifty_is_refused(double volume) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(volume: volume));
    }

    [Theory]
    [InlineData(0.5, 10.0)]
    [InlineData(1.0, 10.0)]
    [InlineData(2.0, 0.001)]
    [InlineData(0.499, 50.0)]
    [InlineData(0.0, 50.0)]
    public void A_chamber_within_the_centrifuge_rule_is_accepted(double gravity, double volume) {
        // Act
        ExperimentalConditions conditions = Build(gravity: gravity, volume: volume);

        // Assert
        Assert.Equal(volume, conditions.VolumeMillilitres);
    }

    [Theory]
    [InlineData(0.5, 10.001)]
    [InlineData(1.0, 11.0)]
    [InlineData(2.0, 50.0)]
    public void A_centrifuged_chamber_larger_than_ten_millilitres_is_refused(double gravity, double volume) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(gravity: gravity, volume: volume));

        // Assert
        Assert.Contains("10", refusal.Message);
        Assert.Equal("volumeMillilitres", refusal.ParamName);
    }

    [Fact]
    public void A_centrifuged_chamber_of_arbitrary_size_is_always_a_small_one() {
        // Arrange / Act
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Centrifuged().Generate();

        // Assert
        Assert.True(anyConditions.GravityG >= 0.5);
        Assert.True(anyConditions.VolumeMillilitres <= 10);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.0099, true)]
    [InlineData(0.01, false)]
    [InlineData(1.0, false)]
    public void Microgravity_is_anything_under_a_hundredth_of_a_g(double gravity, bool expected) {
        // Arrange
        ExperimentalConditions conditions = Build(gravity: gravity);

        // Act / Assert
        Assert.Equal(expected, conditions.IsMicrogravity);
    }

    [Fact]
    public void At_the_optimum_with_a_half_saturated_medium_at_one_g_the_species_grows_at_half_its_maximum() {
        // Arrange: nutrient = Ks gives S / (Ks + S) = 1/2; 1 g gives no microgravity boost
        Species                anySpecies = Scenario.Mesophile().Generate();
        ExperimentalConditions conditions = Build(gravity: 1, temperature: 30, nutrient: 0.5, volume: 5);

        // Act
        GrowthRate rate = conditions.EffectiveGrowthRate(anySpecies);

        // Assert: Mesophile grows at up to 1 per hour
        Assert.Equal(0.5, rate.PerHour, precision: 12);
    }

    [Fact]
    public void Microgravity_speeds_growth_up_by_twenty_percent() {
        // Arrange
        Species                anySpecies = Scenario.Mesophile().Generate();
        ExperimentalConditions conditions = Build(gravity: 0, temperature: 30, nutrient: 0.5, volume: 5);

        // Act
        GrowthRate rate = conditions.EffectiveGrowthRate(anySpecies);

        // Assert
        Assert.Equal(0.6, rate.PerHour, precision: 12);
    }

    [Fact]
    public void Half_a_g_speeds_growth_up_by_ten_percent() {
        // Arrange
        Species                anySpecies = Scenario.Mesophile().Generate();
        ExperimentalConditions conditions = Build(gravity: 0.5, temperature: 30, nutrient: 0.5, volume: 5);

        // Act / Assert
        Assert.Equal(0.55, conditions.EffectiveGrowthRate(anySpecies).PerHour, precision: 12);
    }

    [Fact]
    public void Above_one_g_the_boost_is_gone_and_does_not_turn_into_a_penalty() {
        // Arrange
        Species                anySpecies = Scenario.Mesophile().Generate();
        ExperimentalConditions conditions = Build(gravity: 1.8, temperature: 30, nutrient: 0.5, volume: 5);

        // Act / Assert
        Assert.Equal(0.5, conditions.EffectiveGrowthRate(anySpecies).PerHour, precision: 12);
    }

    [Fact]
    public void Growth_is_scaled_by_the_temperature_factor() {
        // Arrange: 20 °C is halfway up from 10 to 30
        Species                anySpecies = Scenario.Mesophile().Generate();
        ExperimentalConditions conditions = Build(gravity: 1, temperature: 20, nutrient: 0.5, volume: 5);

        // Act / Assert
        Assert.Equal(0.25, conditions.EffectiveGrowthRate(anySpecies).PerHour, precision: 12);
    }

    [Fact]
    public void A_richer_medium_never_slows_growth_down_and_saturates() {
        // Arrange
        Species                anySpecies = Scenario.Mesophile().Generate();
        ExperimentalConditions poor       = Build(gravity: 1, nutrient: 0.5);
        ExperimentalConditions rich       = Build(gravity: 1, nutrient: 50);

        // Act
        double poorRate = poor.EffectiveGrowthRate(anySpecies).PerHour;
        double richRate = rich.EffectiveGrowthRate(anySpecies).PerHour;

        // Assert
        Assert.True(richRate > poorRate);
        Assert.True(richRate < 1.0);
    }

    [Theory]
    [InlineData(5.0)]
    [InlineData(10.0)]
    [InlineData(50.0)]
    [InlineData(-20.0)]
    [InlineData(80.0)]
    public void A_species_does_not_grow_outside_its_cardinal_temperatures(double temperature) {
        // Arrange: Mesophile grows strictly between 10 and 50 °C
        Species                anySpecies = Scenario.Mesophile().Generate();
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithTemperatureCelsius(temperature).Generate();

        // Act
        GrowthRate rate = conditions.EffectiveGrowthRate(anySpecies);

        // Assert
        Assert.Equal(0, rate.PerHour);
    }

    [Fact]
    public void The_effective_rate_never_exceeds_five_per_hour() {
        // Arrange: 5 * 0.99 * 1.2 would be 5.94
        Species anySpecies = Any.Species()
                                .WithCardinalTemperatures(10, 30, 50)
                                .WithMaxGrowthRate(r => r.WithPerHour(5.0))
                                .Generate();
        ExperimentalConditions conditions = Build(gravity: 0, temperature: 30, nutrient: 50, volume: 5);

        // Act
        GrowthRate rate = conditions.EffectiveGrowthRate(anySpecies);

        // Assert
        Assert.Equal(GrowthRate.MaxPerHour, rate.PerHour);
    }

    [Fact]
    public void Growth_cannot_be_computed_for_nobody() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyConditions.EffectiveGrowthRate(null!));
        Assert.Throws<ArgumentNullException>(() => anyConditions.CarryingCapacity(null!));
    }

    [Fact]
    public void The_carrying_capacity_at_one_g_is_the_nutrient_load_times_the_yield() {
        // Arrange: 10 g/L = 0.01 g/mL; 0.01 * 1e10 cells/g = 1e8 cells/mL
        Species                anySpecies = Any.Species().WithYieldCellsPerGram(1e10).Generate();
        ExperimentalConditions conditions = Build(gravity: 1, nutrient: 10, volume: 5);

        // Act
        Concentration capacity = conditions.CarryingCapacity(anySpecies);

        // Assert
        Assert.Equal(1e8, capacity.CellsPerMillilitre, precision: 3);
    }

    [Fact]
    public void Microgravity_raises_the_carrying_capacity_by_thirty_percent() {
        // Arrange
        Species                anySpecies = Any.Species().WithYieldCellsPerGram(1e10).Generate();
        ExperimentalConditions conditions = Build(gravity: 0, nutrient: 10, volume: 5);

        // Act / Assert
        Assert.Equal(1.3e8, conditions.CarryingCapacity(anySpecies).CellsPerMillilitre, precision: 3);
    }

    [Fact]
    public void The_carrying_capacity_does_not_depend_on_the_temperature() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        ExperimentalConditions cold = Build(temperature: -10);
        ExperimentalConditions warm = Build(temperature: 60);

        // Act / Assert
        Assert.Equal(cold.CarryingCapacity(anySpecies), warm.CarryingCapacity(anySpecies));
    }

    [Fact]
    public void Growing_in_microgravity_against_one_g_is_twenty_percent_faster() {
        // Arrange: same temperature and medium, so only the gravity term differs
        Species                anySpecies = Scenario.Mesophile().Generate();
        ExperimentalConditions microgravity = Build(gravity: 0, nutrient: 2);
        ExperimentalConditions reference    = Build(gravity: 1, nutrient: 2);

        // Act
        double relative = microgravity.RelativeGrowthAgainst(reference, anySpecies);

        // Assert
        Assert.Equal(1.2, relative, precision: 12);
    }

    [Fact]
    public void Growing_against_oneself_is_a_ratio_of_one() {
        // Arrange
        Species                anySpecies    = Scenario.Mesophile().Generate();
        ExperimentalConditions anyConditions = Scenario.Favourable(Any.ExperimentalConditions()).Generate();

        // Act
        double relative = anyConditions.RelativeGrowthAgainst(anyConditions, anySpecies);

        // Assert
        Assert.Equal(1.0, relative, precision: 12);
    }

    [Fact]
    public void A_ratio_is_refused_when_the_species_does_not_grow_under_the_reference() {
        // Arrange
        Species                anySpecies    = Scenario.Mesophile().Generate();
        ExperimentalConditions anyConditions = Scenario.Favourable(Any.ExperimentalConditions()).Generate();
        ExperimentalConditions frozen        = Any.ExperimentalConditions().WithTemperatureCelsius(0).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyConditions.RelativeGrowthAgainst(frozen, anySpecies));

        // Assert
        Assert.Contains(anySpecies.BinomialName, refusal.Message);
    }

    [Fact]
    public void A_species_that_stops_growing_has_a_ratio_of_zero_against_a_reference_where_it_grows() {
        // Arrange
        Species                anySpecies    = Scenario.Mesophile().Generate();
        ExperimentalConditions anyConditions = Scenario.Favourable(Any.ExperimentalConditions()).Generate();
        ExperimentalConditions frozen        = Any.ExperimentalConditions().WithTemperatureCelsius(0).Generate();

        // Act
        double relative = frozen.RelativeGrowthAgainst(anyConditions, anySpecies);

        // Assert
        Assert.Equal(0, relative);
    }

    [Fact]
    public void Two_conditions_with_the_same_values_are_equal() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Act
        ExperimentalConditions copy = new ExperimentalConditions(anyConditions.GravityG, anyConditions.TemperatureCelsius, anyConditions.NutrientGramsPerLitre, anyConditions.VolumeMillilitres);

        // Assert
        Assert.Equal(anyConditions, copy);
        Assert.Equal(anyConditions.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Conditions_differing_by_one_value_are_not_equal() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().WithTemperatureCelsius(20).Generate();
        ExperimentalConditions warmer        = new ExperimentalConditions(anyConditions.GravityG, 21, anyConditions.NutrientGramsPerLitre, anyConditions.VolumeMillilitres);

        // Act / Assert
        Assert.NotEqual(anyConditions, warmer);
    }

}
