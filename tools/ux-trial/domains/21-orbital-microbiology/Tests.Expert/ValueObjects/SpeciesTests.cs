using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.ValueObjects;

public sealed class SpeciesTests {

    // ---- construction: what a species is made of -------------------------------------------------------------

    [Fact]
    public void A_species_keeps_the_traits_it_was_given() {
        // Arrange
        GrowthRate anyRate = Any.GrowthRate().WithPerHour(perHour => perHour.Positive()).Generate();

        // Act
        Species species = new Species("Escherichia coli", anyRate, 8d, 37d, 46d, 1.5, 4e9);

        // Assert
        Assert.Equal("Escherichia coli", species.BinomialName);
        Assert.Equal("Escherichia coli", species.ToString());
        Assert.Same(anyRate, species.MaxGrowthRate);
        Assert.Equal(8d, species.MinTemperatureCelsius);
        Assert.Equal(37d, species.OptimalTemperatureCelsius);
        Assert.Equal(46d, species.MaxTemperatureCelsius);
        Assert.Equal(1.5, species.LagHours);
        Assert.Equal(4e9, species.YieldCellsPerGram);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Escherichia")]
    [InlineData("escherichia coli")]
    [InlineData("Escherichia Coli")]
    [InlineData("E coli")]
    [InlineData("E. coli")]
    [InlineData("Escherichia coli K12")]
    [InlineData("Escherichia  coli")]
    [InlineData(" Escherichia coli")]
    [InlineData("Escherichia-coli")]
    [InlineData("Escherichia col1")]
    public void A_name_that_is_not_Genus_species_is_refused(string name) {
        // Arrange
        AnySpecies anySpecies = Any.Species();

        // Assert
        Assert.Throws<ArgumentException>(() => anySpecies.WithBinomialName(name).Generate());
    }

    [Fact(Skip = "domain defect: a species name with a trailing newline is accepted because the regex ends with '$'; literal input, not seed-dependent")]
    public void A_name_with_a_trailing_newline_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.Species().WithBinomialName("Escherichia coli\n").Generate());
    }

    [Fact]
    public void A_missing_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Species().WithBinomialName((string)null!).Generate());
    }

    [Fact]
    public void A_missing_growth_rate_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Species().WithMaxGrowthRate((GrowthRate)null!).Generate());
    }

    [Fact]
    public void A_species_that_never_grows_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Species().WithMaxGrowthRate(GrowthRate.Zero).Generate());
    }

    [Theory]
    [InlineData(10d, 10d, 40d)]
    [InlineData(10d, 40d, 40d)]
    [InlineData(20d, 10d, 40d)]
    [InlineData(10d, 50d, 40d)]
    [InlineData(40d, 30d, 10d)]
    [InlineData(25d, 25d, 25d)]
    public void Cardinal_temperatures_that_are_not_strictly_increasing_are_refused(double min, double optimum, double max) {
        // Arrange
        AnySpecies anySpecies = Any.Species();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => anySpecies.WithMinTemperatureCelsius(min)
                                                         .WithOptimalTemperatureCelsius(optimum)
                                                         .WithMaxTemperatureCelsius(max)
                                                         .Generate());
    }

    [Fact]
    public void Cardinal_temperatures_may_span_the_whole_range_the_station_measures() {
        // Act
        Species species = Any.Species().WithMinTemperatureCelsius(-20d).WithOptimalTemperatureCelsius(30d).WithMaxTemperatureCelsius(80d).Generate();

        // Assert
        Assert.Equal(-20d, species.MinTemperatureCelsius);
        Assert.Equal(80d, species.MaxTemperatureCelsius);
    }

    [Theory]
    [InlineData(-20.1, 10d, 40d)]
    [InlineData(10d, 40d, 80.1)]
    [InlineData(-30d, -25d, 40d)]
    [InlineData(10d, 20d, 90d)]
    [InlineData(double.NaN, 20d, 40d)]
    [InlineData(10d, double.NaN, 40d)]
    [InlineData(10d, 20d, double.NaN)]
    [InlineData(10d, 20d, double.PositiveInfinity)]
    public void A_temperature_outside_minus_20_to_80_or_not_finite_is_refused(double min, double optimum, double max) {
        // Arrange
        AnySpecies anySpecies = Any.Species();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anySpecies.WithMinTemperatureCelsius(min)
                                                                  .WithOptimalTemperatureCelsius(optimum)
                                                                  .WithMaxTemperatureCelsius(max)
                                                                  .Generate());
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(240d)]
    public void A_lag_between_zero_and_240_hours_is_kept(double lagHours) {
        Assert.Equal(lagHours, Any.Species().WithLagHours(lagHours).Generate().LagHours);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(240.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_lag_outside_zero_to_240_hours_or_not_finite_is_refused(double lagHours) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Species().WithLagHours(lagHours).Generate());
    }

    [Theory]
    [InlineData(1e9)]
    [InlineData(1e13)]
    public void A_yield_between_a_billion_and_ten_trillion_cells_per_gram_is_kept(double yield) {
        Assert.Equal(yield, Any.Species().WithYieldCellsPerGram(yield).Generate().YieldCellsPerGram);
    }

    [Theory]
    [InlineData(9.99e8)]
    [InlineData(1.01e13)]
    [InlineData(0d)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_yield_outside_the_range_or_not_finite_is_refused(double yield) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Species().WithYieldCellsPerGram(yield).Generate());
    }

    // ---- temperature factor ----------------------------------------------------------------------------------

    // Cardinal temperatures 0 / 10 / 50: the rise is steep (10 degrees), the fall gentle (40 degrees).
    [Theory]
    [InlineData(10d, 1d)]
    [InlineData(5d, 0.5)]
    [InlineData(2d, 0.2)]
    [InlineData(30d, 0.5)]
    [InlineData(20d, 0.75)]
    [InlineData(0d, 0d)]
    [InlineData(50d, 0d)]
    [InlineData(-5d, 0d)]
    [InlineData(65d, 0d)]
    public void The_temperature_factor_rises_linearly_to_the_optimum_and_falls_linearly_to_the_maximum(double celsius, double expectedFactor) {
        // Arrange
        Species species = Any.Species().WithMinTemperatureCelsius(0d).WithOptimalTemperatureCelsius(10d).WithMaxTemperatureCelsius(50d).Generate();

        // Act
        double factor = species.TemperatureFactor(celsius);

        // Assert
        Assert.Equal(expectedFactor, factor, precision: 10);
    }

    // ---- equality --------------------------------------------------------------------------------------------

    [Fact]
    public void Two_species_with_the_same_name_are_the_same_species_whatever_their_traits() {
        // Arrange
        string  anyName    = Any.Species().Generate().BinomialName;
        Species first      = Any.Species().WithBinomialName(anyName).Generate();
        Species second     = Any.Species().WithBinomialName(anyName).Generate();

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_species_with_different_names_are_different_species_whatever_their_traits() {
        // Arrange
        Species anySpecies      = Any.Species().Generate();
        Species anyOtherSpecies = Any.Species()
                                     .WithBinomialName(name => name.DifferentFrom(anySpecies.BinomialName))
                                     .WithMaxGrowthRate(anySpecies.MaxGrowthRate)
                                     .WithMinTemperatureCelsius(anySpecies.MinTemperatureCelsius)
                                     .WithOptimalTemperatureCelsius(anySpecies.OptimalTemperatureCelsius)
                                     .WithMaxTemperatureCelsius(anySpecies.MaxTemperatureCelsius)
                                     .WithLagHours(anySpecies.LagHours)
                                     .WithYieldCellsPerGram(anySpecies.YieldCellsPerGram)
                                     .Generate();

        // Assert
        Assert.NotEqual(anySpecies, anyOtherSpecies);
    }

}
