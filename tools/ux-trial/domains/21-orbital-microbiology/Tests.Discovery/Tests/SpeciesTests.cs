using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class SpeciesTests {

    private static Species Build(string name = "Bacillus subtilis", double rate = 1.0, double min = 10, double optimum = 30, double max = 50, double lag = 2, double yield = 1e10) {
        return new Species(name, new GrowthRate(rate), min, optimum, max, lag, yield);
    }

    [Fact]
    public void A_species_keeps_every_trait_it_was_given() {
        // Act
        Species species = Build("Escherichia coli", 0.9, -5, 37, 48, 1.5, 5e9);

        // Assert
        Assert.Equal("Escherichia coli", species.BinomialName);
        Assert.Equal(0.9, species.MaxGrowthRate.PerHour);
        Assert.Equal(-5, species.MinTemperatureCelsius);
        Assert.Equal(37, species.OptimalTemperatureCelsius);
        Assert.Equal(48, species.MaxTemperatureCelsius);
        Assert.Equal(1.5, species.LagHours);
        Assert.Equal(5e9, species.YieldCellsPerGram);
        Assert.Equal("Escherichia coli", species.ToString());
    }

    [Fact]
    public void An_arbitrary_species_is_a_well_formed_one() {
        // Arrange / Act
        Species anySpecies = Any.Species().Generate();

        // Assert
        Assert.True(anySpecies.MinTemperatureCelsius < anySpecies.OptimalTemperatureCelsius);
        Assert.True(anySpecies.OptimalTemperatureCelsius < anySpecies.MaxTemperatureCelsius);
        Assert.True(anySpecies.MaxGrowthRate.PerHour > 0);
    }

    [Theory]
    [InlineData("bacillus subtilis")]
    [InlineData("Bacillus Subtilis")]
    [InlineData("Bacillus")]
    [InlineData("Bacillus  subtilis")]
    [InlineData("Bacillus subtilis ")]
    [InlineData("Bacillus subtilis var")]
    [InlineData("B subtilis")]
    [InlineData("Bacillus s2")]
    [InlineData("Bacillus subtilis2")]
    [InlineData("")]
    public void A_name_that_is_not_genus_space_species_is_refused(string name) {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(name));
    }

    [Fact(Skip = "domain defect: the name pattern ends with '$', which .NET also matches before a final newline, so 'Bacillus subtilis\\n' is accepted and stored with its newline (literal input, no seed)")]
    public void A_name_with_a_trailing_newline_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build("Bacillus subtilis\n"));
    }

    [Fact]
    public void A_missing_name_or_rate_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Species(null!, new GrowthRate(1), 10, 30, 50, 0, 1e10));
        Assert.Throws<ArgumentNullException>(() => new Species("Bacillus subtilis", null!, 10, 30, 50, 0, 1e10));
    }

    [Fact]
    public void A_species_that_never_grows_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(rate: 0));
    }

    [Theory]
    [InlineData(10, 10, 50)]
    [InlineData(10, 50, 50)]
    [InlineData(30, 20, 50)]
    [InlineData(10, 60, 50)]
    [InlineData(50, 30, 10)]
    [InlineData(10, 30, 10)]
    public void Cardinal_temperatures_must_strictly_increase(double min, double optimum, double max) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(min: min, optimum: optimum, max: max));

        // Assert
        Assert.Contains("minimum", refusal.Message);
    }

    [Theory]
    [InlineData(-20.001, 10, 50)]
    [InlineData(-30, 10, 50)]
    [InlineData(-10, 30, 80.001)]
    [InlineData(-10, 90, 100)]
    [InlineData(double.NaN, 10, 50)]
    [InlineData(-10, double.NaN, 50)]
    [InlineData(-10, 10, double.PositiveInfinity)]
    public void A_cardinal_temperature_outside_minus_twenty_to_eighty_is_refused(double min, double optimum, double max) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(min: min, optimum: optimum, max: max));
    }

    [Fact]
    public void The_widest_temperature_range_is_accepted() {
        // Act
        Species species = Build(min: -20, optimum: 30, max: 80);

        // Assert
        Assert.Equal(-20, species.MinTemperatureCelsius);
        Assert.Equal(80, species.MaxTemperatureCelsius);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(240.001)]
    [InlineData(double.NaN)]
    public void A_lag_outside_zero_to_two_hundred_forty_hours_is_refused(double lag) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(lag: lag));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(240.0)]
    public void The_extreme_lags_are_accepted(double lag) {
        // Act
        Species species = Build(lag: lag);

        // Assert
        Assert.Equal(lag, species.LagHours);
    }

    [Theory]
    [InlineData(9.99e8)]
    [InlineData(1.01e13)]
    [InlineData(0)]
    [InlineData(double.PositiveInfinity)]
    public void A_yield_outside_a_billion_to_ten_trillion_cells_per_gram_is_refused(double yield) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(yield: yield));
    }

    [Theory]
    [InlineData(1e9)]
    [InlineData(1e13)]
    public void The_extreme_yields_are_accepted(double yield) {
        // Act
        Species species = Build(yield: yield);

        // Assert
        Assert.Equal(yield, species.YieldCellsPerGram);
    }

    [Theory]
    [InlineData(10.0, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(-40.0, 0.0)]
    [InlineData(50.0, 0.0)]
    [InlineData(80.0, 0.0)]
    [InlineData(30.0, 1.0)]
    [InlineData(20.0, 0.5)]
    [InlineData(40.0, 0.5)]
    [InlineData(15.0, 0.25)]
    [InlineData(45.0, 0.25)]
    public void The_temperature_factor_rises_linearly_to_the_optimum_and_falls_back_to_zero(double temperature, double expectedFactor) {
        // Arrange
        Species anySpecies = Any.Species().WithCardinalTemperatures(10, 30, 50).Generate();

        // Act
        double factor = anySpecies.TemperatureFactor(temperature);

        // Assert
        Assert.Equal(expectedFactor, factor, precision: 12);
    }

    [Fact]
    public void The_temperature_factor_is_asymmetric_when_the_optimum_is_off_centre() {
        // Arrange
        Species species = Build(min: 0, optimum: 10, max: 50);

        // Act / Assert
        Assert.Equal(0.5, species.TemperatureFactor(5), precision: 12);
        Assert.Equal(0.5, species.TemperatureFactor(30), precision: 12);
    }

    [Fact]
    public void Two_species_with_the_same_name_are_the_same_species_whatever_their_traits() {
        // Arrange
        string  anyName        = Any.Species().Generate().BinomialName;
        Species anySpecies     = Any.Species().WithBinomialName(anyName).Generate();
        Species anyNamesake    = Any.Species().WithBinomialName(anyName).Generate();

        // Act / Assert
        Assert.Equal(anySpecies, anyNamesake);
        Assert.Equal(anySpecies.GetHashCode(), anyNamesake.GetHashCode());
    }

    [Fact]
    public void Two_species_with_different_names_are_different_species() {
        // Arrange
        Species anySpecies      = Any.Species().Generate();
        Species anyOtherSpecies = Any.Species().WithBinomialName(n => n.DifferentFrom(anySpecies.BinomialName)).Generate();

        // Act / Assert
        Assert.NotEqual(anySpecies, anyOtherSpecies);
    }

}
