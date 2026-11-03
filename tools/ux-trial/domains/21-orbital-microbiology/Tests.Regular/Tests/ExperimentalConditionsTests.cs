using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests;

public sealed class ExperimentalConditionsTests {

    private const double ReferenceGravity = 1;

    // Parameters whose value never matters to the refusal under test: any valid one will do.
    private static double AnyTemperature() => Any.Double().Between(ExperimentalConditions.MinTemperatureCelsius, ExperimentalConditions.MaxTemperatureCelsius).Generate();
    private static double AnyNutrient() => Any.Double().Positive().LessThanOrEqualTo(ExperimentalConditions.MaxNutrientGramsPerLitre).Generate();
    private static double AnySmallVolume() => Any.Double().Positive().LessThanOrEqualTo(ExperimentalConditions.MaxCentrifugeVolumeMillilitres).Generate();

    [Fact]
    public void Conditions_keep_what_they_are_given() {
        // Act
        ExperimentalConditions conditions = new ExperimentalConditions(0.38, 25.5, 12, 30);

        // Assert
        Assert.Equal(0.38, conditions.GravityG);
        Assert.Equal(25.5, conditions.TemperatureCelsius);
        Assert.Equal(12, conditions.NutrientGramsPerLitre);
        Assert.Equal(30, conditions.VolumeMillilitres);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(2.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_gravity_outside_the_range_is_refused(double gravity) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ExperimentalConditions(gravity, AnyTemperature(), AnyNutrient(), AnySmallVolume()));

        // Assert
        Assert.Equal("gravityG", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void The_gravity_range_is_closed(double gravity) {
        // Act
        ExperimentalConditions conditions = new ExperimentalConditions(gravity, AnyTemperature(), AnyNutrient(), AnySmallVolume());

        // Assert
        Assert.Equal(gravity, conditions.GravityG);
    }

    [Theory]
    [InlineData(-20.001)]
    [InlineData(80.001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_temperature_outside_the_range_is_refused(double temperature) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ExperimentalConditions(ReferenceGravity, temperature, AnyNutrient(), AnySmallVolume()));

        // Assert
        Assert.Equal("temperatureCelsius", refusal.ParamName);
    }

    [Theory]
    [InlineData(-20)]
    [InlineData(80)]
    public void The_temperature_range_is_closed(double temperature) {
        // Act
        ExperimentalConditions conditions = new ExperimentalConditions(ReferenceGravity, temperature, AnyNutrient(), AnySmallVolume());

        // Assert
        Assert.Equal(temperature, conditions.TemperatureCelsius);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(50.001)]
    [InlineData(double.NaN)]
    public void A_nutrient_load_outside_its_range_is_refused(double nutrient) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ExperimentalConditions(ReferenceGravity, AnyTemperature(), nutrient, AnySmallVolume()));

        // Assert
        Assert.Equal("nutrientGramsPerLitre", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.000001)]
    [InlineData(50)]
    public void The_nutrient_range_is_open_at_zero_and_closed_at_fifty(double nutrient) {
        // Act
        ExperimentalConditions conditions = new ExperimentalConditions(ReferenceGravity, AnyTemperature(), nutrient, AnySmallVolume());

        // Assert
        Assert.Equal(nutrient, conditions.NutrientGramsPerLitre);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(50.001)]
    [InlineData(double.PositiveInfinity)]
    public void A_volume_outside_its_range_is_refused(double volume) {
        // Arrange
        double anyMicrogravity = Any.Double().Between(0, ExperimentalConditions.MicrogravityThresholdG).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ExperimentalConditions(anyMicrogravity, AnyTemperature(), AnyNutrient(), volume));

        // Assert
        Assert.Equal("volumeMillilitres", refusal.ParamName);
    }

    [Fact]
    public void A_chamber_in_microgravity_holds_up_to_fifty_millilitres() {
        // Arrange
        double anyGravityBelowCentrifuge = Any.Double().Between(0, 0.49).Generate();

        // Act
        ExperimentalConditions conditions = new ExperimentalConditions(anyGravityBelowCentrifuge, AnyTemperature(), AnyNutrient(), 50);

        // Assert
        Assert.Equal(50, conditions.VolumeMillilitres);
    }

    [Theory]
    [InlineData(0.5, 10, true)]
    [InlineData(0.5, 10.001, false)]
    [InlineData(1, 10.5, false)]
    [InlineData(2, 50, false)]
    [InlineData(0.49, 50, true)]
    [InlineData(0.499999, 50, true)]
    [InlineData(0, 50, true)]
    public void A_centrifuged_chamber_holds_at_most_ten_millilitres(double gravity, double volume, bool accepted) {
        // Act
        Action build = () => new ExperimentalConditions(gravity, AnyTemperature(), AnyNutrient(), volume);

        // Assert
        if (accepted) {
            build();
        } else {
            ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(build);
            Assert.Equal("volumeMillilitres", refusal.ParamName);
        }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(0.0099, true)]
    [InlineData(0.01, false)]
    [InlineData(1, false)]
    public void Microgravity_is_below_a_hundredth_of_a_g(double gravity, bool expectedMicrogravity) {
        // Act
        ExperimentalConditions conditions = new ExperimentalConditions(gravity, AnyTemperature(), AnyNutrient(), AnySmallVolume());

        // Assert
        Assert.Equal(expectedMicrogravity, conditions.IsMicrogravity);
    }

    [Theory]
    [InlineData(1, 0.5)]
    [InlineData(2, 0.5)]
    [InlineData(0, 0.6)]
    [InlineData(0.5, 0.55)]
    public void The_effective_rate_scales_the_maximum_rate_by_the_nutrient_term_and_the_gravity_boost(double gravity, double expectedPerHour) {
        // Arrange: at the optimum the temperature factor is 1; at 0.5 g/L the Monod term S / (0.5 + S) is 0.5.
        Species                species    = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        ExperimentalConditions conditions = new ExperimentalConditions(gravity, 30, 0.5, AnySmallVolume());

        // Act
        GrowthRate rate = conditions.EffectiveGrowthRate(species);

        // Assert
        Assert.Equal(expectedPerHour, rate.PerHour, precision: 12);
    }

    [Fact]
    public void The_effective_rate_follows_the_temperature_factor() {
        // Arrange: 17.5 °C is half way from the minimum (5) to the optimum (30).
        Species                species    = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        ExperimentalConditions conditions = new ExperimentalConditions(1, 17.5, 0.5, AnySmallVolume());

        // Act
        double perHour = conditions.EffectiveGrowthRate(species).PerHour;

        // Assert
        Assert.Equal(0.25, perHour, precision: 12);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(45)]
    [InlineData(-20)]
    [InlineData(80)]
    public void A_species_does_not_grow_outside_its_cardinal_temperatures(double temperature) {
        // Arrange
        Species                species    = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        ExperimentalConditions conditions = new ExperimentalConditions(ReferenceGravity, temperature, AnyNutrient(), AnySmallVolume());

        // Act
        double perHour = conditions.EffectiveGrowthRate(species).PerHour;

        // Assert
        Assert.Equal(0, perHour);
    }

    [Fact]
    public void The_effective_rate_never_exceeds_the_ceiling_of_a_growth_rate() {
        // Arrange: the fastest species, richest medium, microgravity: 5 * 0.99 * 1.2 would be 5.94 h-1.
        Species                species    = Samples.GrowerAt30C(maxRatePerHour: 5, lagHours: 0, yieldCellsPerGram: 1e10);
        ExperimentalConditions conditions = new ExperimentalConditions(0, 30, 50, AnySmallVolume());

        // Act
        double perHour = conditions.EffectiveGrowthRate(species).PerHour;

        // Assert
        Assert.Equal(GrowthRate.MaxPerHour, perHour);
    }

    [Theory]
    [InlineData(1, 1e8)]
    [InlineData(2, 1e8)]
    [InlineData(0, 1.3e8)]
    [InlineData(0.5, 1.15e8)]
    public void The_carrying_capacity_is_the_nutrient_times_the_yield_raised_in_low_gravity(double gravity, double expectedCellsPerMillilitre) {
        // Arrange: 10 g/L = 0.01 g/mL; 0.01 g/mL * 1e10 cells/g = 1e8 cells/mL.
        Species                species    = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        ExperimentalConditions conditions = new ExperimentalConditions(gravity, AnyTemperature(), 10, AnySmallVolume());

        // Act
        Concentration capacity = conditions.CarryingCapacity(species);

        // Assert
        Assert.Equal(expectedCellsPerMillilitre, capacity.CellsPerMillilitre, precision: 0);
    }

    [Fact]
    public void The_carrying_capacity_does_not_depend_on_the_temperature() {
        // Arrange
        Species species          = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        double  anyTemperature   = AnyTemperature();
        double  anotherTemp      = AnyTemperature();
        double  anyNutrient      = AnyNutrient();

        // Act
        Concentration first  = new ExperimentalConditions(ReferenceGravity, anyTemperature, anyNutrient, AnySmallVolume()).CarryingCapacity(species);
        Concentration second = new ExperimentalConditions(ReferenceGravity, anotherTemp, anyNutrient, AnySmallVolume()).CarryingCapacity(species);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Growth_in_microgravity_is_one_fifth_faster_than_at_one_g() {
        // Arrange
        Species                species        = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        double                 anyNutrient    = AnyNutrient();
        double                 anyVolume      = AnySmallVolume();
        ExperimentalConditions microgravity   = new ExperimentalConditions(0, 30, anyNutrient, anyVolume);
        ExperimentalConditions reference      = new ExperimentalConditions(1, 30, anyNutrient, anyVolume);

        // Act
        double ratio = microgravity.RelativeGrowthAgainst(reference, species);

        // Assert
        Assert.Equal(1.2, ratio, precision: 12);
    }

    [Fact]
    public void Conditions_compared_to_themselves_give_a_ratio_of_one() {
        // Arrange: only the temperature matters (it must let the species grow); everything else is arbitrary.
        Species                species = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().WithTemperatureCelsius(30).Generate();

        // Act
        double ratio = anyConditions.RelativeGrowthAgainst(anyConditions, species);

        // Assert
        Assert.Equal(1, ratio, precision: 12);
    }

    [Fact]
    public void No_ratio_can_be_formed_against_conditions_where_the_species_does_not_grow() {
        // Arrange
        Species                species   = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        ExperimentalConditions growing   = Any.ExperimentalConditions().WithTemperatureCelsius(30).Generate();
        ExperimentalConditions freezing  = Any.ExperimentalConditions().WithTemperatureCelsius(-10).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => growing.RelativeGrowthAgainst(freezing, species));
    }

    [Fact]
    public void A_species_that_stops_growing_has_a_ratio_of_zero() {
        // Arrange
        Species                species  = Samples.GrowerAt30C(maxRatePerHour: 1, lagHours: 0, yieldCellsPerGram: 1e10);
        ExperimentalConditions growing  = Any.ExperimentalConditions().WithTemperatureCelsius(30).Generate();
        ExperimentalConditions freezing = Any.ExperimentalConditions().WithTemperatureCelsius(-10).Generate();

        // Act
        double ratio = freezing.RelativeGrowthAgainst(growing, species);

        // Assert
        Assert.Equal(0, ratio);
    }

    [Fact]
    public void Conditions_need_a_species_and_a_reference() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        Species                anySpecies    = Any.Species().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyConditions.EffectiveGrowthRate(null!));
        Assert.Throws<ArgumentNullException>(() => anyConditions.CarryingCapacity(null!));
        Assert.Throws<ArgumentNullException>(() => anyConditions.RelativeGrowthAgainst(null!, anySpecies));
        Assert.Throws<ArgumentNullException>(() => anyConditions.RelativeGrowthAgainst(anyConditions, null!));
    }

    [Fact]
    public void Conditions_with_the_same_four_values_are_equal() {
        // Act
        ExperimentalConditions first  = new ExperimentalConditions(0.38, 25.5, 12, 8);
        ExperimentalConditions second = new ExperimentalConditions(0.38, 25.5, 12, 8);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData(0.39, 25.5, 12, 8)]
    [InlineData(0.38, 25.6, 12, 8)]
    [InlineData(0.38, 25.5, 13, 8)]
    [InlineData(0.38, 25.5, 12, 9)]
    public void Conditions_that_differ_by_one_value_are_not_equal(double gravity, double temperature, double nutrient, double volume) {
        // Arrange
        ExperimentalConditions reference = new ExperimentalConditions(0.38, 25.5, 12, 8);

        // Act
        ExperimentalConditions other = new ExperimentalConditions(gravity, temperature, nutrient, volume);

        // Assert
        Assert.NotEqual(reference, other);
        Assert.False(reference.Equals(null));
    }

}
