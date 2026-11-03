using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.ValueObjects;

public sealed class ExperimentalConditionsTests {

    // ---- construction ----------------------------------------------------------------------------------------

    [Fact]
    public void Conditions_keep_what_they_were_given() {
        // Act
        ExperimentalConditions conditions = new ExperimentalConditions(0.3, 37d, 2.5, 20d);

        // Assert
        Assert.Equal(0.3, conditions.GravityG);
        Assert.Equal(37d, conditions.TemperatureCelsius);
        Assert.Equal(2.5, conditions.NutrientGramsPerLitre);
        Assert.Equal(20d, conditions.VolumeMillilitres);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(2.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_gravity_outside_zero_to_two_g_or_not_finite_is_refused(double gravityG) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.ExperimentalConditions().WithGravityG(gravityG).Generate());
    }

    [Theory]
    [InlineData(-20.001)]
    [InlineData(80.001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_temperature_outside_minus_20_to_80_degrees_or_not_finite_is_refused(double celsius) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.ExperimentalConditions().WithTemperatureCelsius(celsius).Generate());
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(50.001)]
    [InlineData(double.NaN)]
    public void A_nutrient_load_that_is_not_in_zero_exclusive_to_50_g_per_litre_is_refused(double gramsPerLitre) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.ExperimentalConditions().WithNutrientGramsPerLitre(gramsPerLitre).Generate());
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(50.001)]
    [InlineData(double.PositiveInfinity)]
    public void A_volume_that_is_not_in_zero_exclusive_to_50_millilitres_is_refused(double millilitres) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.ExperimentalConditions().WithVolumeMillilitres(millilitres).Generate());
    }

    [Theory]
    [InlineData(-20d)]
    [InlineData(80d)]
    public void The_temperature_limits_themselves_are_held(double celsius) {
        Assert.Equal(celsius, Any.ExperimentalConditions().WithTemperatureCelsius(celsius).Generate().TemperatureCelsius);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(50d)]
    public void The_nutrient_ceiling_and_any_positive_load_are_held(double gramsPerLitre) {
        Assert.Equal(gramsPerLitre, Any.ExperimentalConditions().WithNutrientGramsPerLitre(gramsPerLitre).Generate().NutrientGramsPerLitre);
    }

    // ---- the centrifuge rule ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(0.5, 10d)]
    [InlineData(1d, 10d)]
    [InlineData(2d, 10d)]
    [InlineData(2d, 0.001)]
    [InlineData(0.499, 50d)]
    [InlineData(0d, 50d)]
    public void A_chamber_at_half_a_g_or_more_holds_at_most_ten_millilitres(double gravityG, double millilitres) {
        // Act
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithGravityG(gravityG).WithVolumeMillilitres(millilitres).Generate();

        // Assert
        Assert.Equal(gravityG, conditions.GravityG);
        Assert.Equal(millilitres, conditions.VolumeMillilitres);
    }

    [Theory]
    [InlineData(0.5, 10.001)]
    [InlineData(1d, 11d)]
    [InlineData(2d, 50d)]
    public void A_centrifuged_chamber_larger_than_ten_millilitres_is_refused(double gravityG, double millilitres) {
        // Arrange
        AnyExperimentalConditions anyConditions = Any.ExperimentalConditions();

        // Assert
        Assert.Throws<ArgumentException>(() => anyConditions.WithGravityG(gravityG).WithVolumeMillilitres(millilitres).Generate());
    }

    // ---- microgravity ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0d, true)]
    [InlineData(0.009, true)]
    [InlineData(0.01, false)]
    [InlineData(0.3, false)]
    [InlineData(1d, false)]
    public void Below_a_hundredth_of_a_g_is_microgravity(double gravityG, bool expected) {
        // Arrange
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithGravityG(gravityG).Generate();

        // Assert
        Assert.Equal(expected, conditions.IsMicrogravity);
    }

    // ---- growth rate -----------------------------------------------------------------------------------------

    // A species of 1.2 per hour growing from 10 to 50 degrees with its optimum at 30. At 30 degrees and 0.5 g/L the
    // Monod term is 0.5 / (0.5 + 0.5) = 0.5, and at 1 g there is no gravity boost: 1.2 x 1 x 0.5 x 1 = 0.6 per hour.
    private static AnySpecies AnyFastMesophile() {
        return Any.Species().Mesophilic().GrowingAt(1.2);
    }

    [Fact]
    public void At_the_optimum_temperature_with_half_saturation_and_one_g_a_species_grows_at_half_its_maximum() {
        // Arrange
        Species                anySpecies = AnyFastMesophile().Generate();
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithGravityG(1d).WithTemperatureCelsius(30d).WithNutrientGramsPerLitre(0.5).Generate();

        // Act
        GrowthRate rate = conditions.EffectiveGrowthRate(anySpecies);

        // Assert
        Assert.Equal(0.6, rate.PerHour, precision: 10);
    }

    [Theory]
    [InlineData(20d, 0.3)]   // temperature factor 0.5
    [InlineData(40d, 0.3)]   // temperature factor 0.5
    [InlineData(10d, 0d)]    // at the minimum
    [InlineData(50d, 0d)]    // at the maximum
    [InlineData(-5d, 0d)]
    [InlineData(70d, 0d)]
    public void The_temperature_scales_the_rate_and_stops_it_outside_the_cardinal_range(double celsius, double expectedPerHour) {
        // Arrange
        Species                anySpecies = AnyFastMesophile().Generate();
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithGravityG(1d).WithTemperatureCelsius(celsius).WithNutrientGramsPerLitre(0.5).Generate();

        // Assert
        Assert.Equal(expectedPerHour, conditions.EffectiveGrowthRate(anySpecies).PerHour, precision: 10);
    }

    [Theory]
    [InlineData(0.5, 0.6)]    // S / (Ks + S) = 0.5
    [InlineData(1.5, 0.9)]    // 0.75
    [InlineData(4.5, 1.08)]   // 0.9
    public void The_nutrient_saturates_the_rate_following_the_Monod_term(double gramsPerLitre, double expectedPerHour) {
        // Arrange
        Species                anySpecies = AnyFastMesophile().Generate();
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithGravityG(1d).WithTemperatureCelsius(30d).WithNutrientGramsPerLitre(gramsPerLitre).Generate();

        // Assert
        Assert.Equal(expectedPerHour, conditions.EffectiveGrowthRate(anySpecies).PerHour, precision: 10);
    }

    [Fact]
    public void Microgravity_speeds_the_growth_by_a_fifth() {
        // Arrange
        Species                anySpecies = AnyFastMesophile().Generate();
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithGravityG(0d).WithTemperatureCelsius(30d).WithNutrientGramsPerLitre(0.5).Generate();

        // Assert
        Assert.Equal(0.72, conditions.EffectiveGrowthRate(anySpecies).PerHour, precision: 10);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1.5)]
    [InlineData(2d)]
    public void From_one_g_up_gravity_no_longer_changes_the_rate(double gravityG) {
        // Arrange
        Species                anySpecies = AnyFastMesophile().Generate();
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithGravityG(gravityG).WithTemperatureCelsius(30d).WithNutrientGramsPerLitre(0.5).Generate();

        // Assert
        Assert.Equal(0.6, conditions.EffectiveGrowthRate(anySpecies).PerHour, precision: 10);
    }

    [Fact]
    public void The_rate_is_capped_at_the_highest_rate_a_growth_rate_can_be() {
        // Arrange: 5 per hour x 1 x (50 / 50.5) x 1.2 would be about 5.94
        Species                anyFastestSpecies = Any.Species().Mesophilic().GrowingAt(5d).Generate();
        ExperimentalConditions conditions        = Any.ExperimentalConditions().WithGravityG(0d).WithTemperatureCelsius(30d).WithNutrientGramsPerLitre(50d).Generate();

        // Assert
        Assert.Equal(GrowthRate.MaxPerHour, conditions.EffectiveGrowthRate(anyFastestSpecies).PerHour);
    }

    [Fact]
    public void A_growth_rate_is_asked_for_a_species() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyConditions.EffectiveGrowthRate(null!));
    }

    // ---- carrying capacity -----------------------------------------------------------------------------------

    // Capacity = nutrient (g/L) / 1000 x yield (cells/g): 2 g/L of a species yielding 1e10 cells/g holds 2e7 cells/mL.
    [Fact]
    public void The_carrying_capacity_is_the_nutrient_load_times_the_yield() {
        // Arrange
        Species                anySpecies = Any.Species().WithYieldCellsPerGram(1e10).Generate();
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithGravityG(1d).WithNutrientGramsPerLitre(2d).Generate();

        // Act
        Concentration capacity = conditions.CarryingCapacity(anySpecies);

        // Assert
        Assert.Equal(2e7, capacity.CellsPerMillilitre, precision: 3);
    }

    [Fact]
    public void Microgravity_raises_the_carrying_capacity_by_three_tenths() {
        // Arrange
        Species                anySpecies = Any.Species().WithYieldCellsPerGram(1e10).Generate();
        ExperimentalConditions conditions = Any.ExperimentalConditions().WithGravityG(0d).WithNutrientGramsPerLitre(2d).Generate();

        // Assert
        Assert.Equal(2.6e7, conditions.CarryingCapacity(anySpecies).CellsPerMillilitre, precision: 3);
    }

    [Fact]
    public void A_capacity_is_asked_for_a_species() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyConditions.CarryingCapacity(null!));
    }

    // ---- relative growth -------------------------------------------------------------------------------------

    [Fact]
    public void A_species_grows_one_fifth_faster_in_microgravity_than_on_the_one_g_control() {
        // Arrange
        Species                anySpecies = AnyFastMesophile().Generate();
        ExperimentalConditions inOrbit    = Any.ExperimentalConditions().OnTheControlBench().WithGravityG(0d).Generate();
        ExperimentalConditions control    = Any.ExperimentalConditions().OnTheControlBench().Generate();

        // Act
        double ratio = inOrbit.RelativeGrowthAgainst(control, anySpecies);

        // Assert
        Assert.Equal(1.2, ratio, precision: 10);
    }

    [Fact]
    public void A_cooler_chamber_slows_the_growth_by_the_temperature_factor_ratio() {
        // Arrange: factor 0.5 at 20 degrees, 1 at 30
        Species                anySpecies = AnyFastMesophile().Generate();
        ExperimentalConditions cooler     = Any.ExperimentalConditions().OnTheControlBench().WithTemperatureCelsius(20d).Generate();
        ExperimentalConditions reference  = Any.ExperimentalConditions().OnTheControlBench().Generate();

        // Assert
        Assert.Equal(0.5, cooler.RelativeGrowthAgainst(reference, anySpecies), precision: 10);
    }

    [Fact]
    public void A_species_is_one_times_as_fast_as_itself() {
        // Arrange
        Species                anySpecies    = AnyFastMesophile().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().WithTemperatureCelsius(30d).Generate();

        // Assert
        Assert.Equal(1d, anyConditions.RelativeGrowthAgainst(anyConditions, anySpecies), precision: 10);
    }

    [Fact]
    public void A_species_that_does_not_grow_here_is_zero_times_as_fast_as_where_it_does() {
        // Arrange
        Species                anySpecies = AnyFastMesophile().Generate();
        ExperimentalConditions tooHot     = Any.ExperimentalConditions().OnTheControlBench().WithTemperatureCelsius(60d).Generate();
        ExperimentalConditions reference  = Any.ExperimentalConditions().OnTheControlBench().Generate();

        // Assert
        Assert.Equal(0d, tooHot.RelativeGrowthAgainst(reference, anySpecies));
    }

    [Fact]
    public void No_ratio_is_formed_against_conditions_where_the_species_does_not_grow() {
        // Arrange
        Species                anySpecies = AnyFastMesophile().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().OnTheControlBench().Generate();
        ExperimentalConditions tooCold       = Any.ExperimentalConditions().OnTheControlBench().WithTemperatureCelsius(5d).Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => anyConditions.RelativeGrowthAgainst(tooCold, anySpecies));
    }

    [Fact]
    public void A_ratio_is_formed_between_two_conditions_and_a_species() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        Species                anySpecies    = Any.Species().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyConditions.RelativeGrowthAgainst(null!, anySpecies));
        Assert.Throws<ArgumentNullException>(() => anyConditions.RelativeGrowthAgainst(anyConditions, null!));
    }

    // ---- equality --------------------------------------------------------------------------------------------

    [Fact]
    public void Conditions_with_the_same_four_values_are_equal() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Act
        ExperimentalConditions twin = new ExperimentalConditions(anyConditions.GravityG,
                                                                 anyConditions.TemperatureCelsius,
                                                                 anyConditions.NutrientGramsPerLitre,
                                                                 anyConditions.VolumeMillilitres);

        // Assert
        Assert.Equal(anyConditions, twin);
        Assert.Equal(anyConditions.GetHashCode(), twin.GetHashCode());
    }

    [Fact]
    public void Conditions_that_differ_by_one_value_are_not_equal() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        ExperimentalConditions warmer        = Any.ExperimentalConditions()
                                                  .WithGravityG(anyConditions.GravityG)
                                                  .WithVolumeMillilitres(anyConditions.VolumeMillilitres)
                                                  .WithNutrientGramsPerLitre(anyConditions.NutrientGramsPerLitre)
                                                  .WithTemperatureCelsius(t => t.DifferentFrom(anyConditions.TemperatureCelsius))
                                                  .Generate();

        // Assert
        Assert.NotEqual(anyConditions, warmer);
    }

}
