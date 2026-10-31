using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Regular;

public sealed class ValueObjectTests {

    [Fact]
    public void A_rate_of_ln2_per_hour_doubles_every_hour() {
        // Arrange
        GrowthRate rate = new GrowthRate(Math.Log(2));

        // Act
        double generationTime = rate.GenerationTimeHours;

        // Assert
        Assert.Equal(1.0, generationTime, 9);
    }

    [Fact]
    public void A_population_that_does_not_grow_has_no_generation_time() {
        // Arrange
        GrowthRate rate = GrowthRate.Zero;

        // Act
        Exception? exception = Record.Exception(() => rate.GenerationTimeHours);

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Three_hours_at_ln2_per_hour_is_three_generations() {
        // Arrange
        GrowthRate rate = new GrowthRate(Math.Log(2));

        // Act
        double generations = rate.GenerationsIn(3);

        // Assert
        Assert.Equal(3.0, generations, 9);
    }

    [Fact]
    public void An_eightfold_increase_in_concentration_is_three_generations() {
        // Arrange
        Concentration start = new Concentration(1_000);

        // Act
        double generations = start.GenerationsTo(new Concentration(8_000));

        // Assert
        Assert.Equal(3.0, generations, 9);
    }

    [Fact]
    public void Generations_are_not_counted_from_an_empty_culture() {
        // Arrange
        Concentration anyTarget = Arbitrary.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Act
        Exception? exception = Record.Exception(() => Concentration.Zero.GenerationsTo(anyTarget));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void A_culture_id_names_its_chamber() {
        // Arrange
        CultureId id = new CultureId("CH07-C0042");

        // Act
        int chamber = id.Chamber;

        // Assert
        Assert.Equal(7, chamber);
    }

    [Fact]
    public void The_station_has_no_thirteenth_chamber() {
        // Act
        Exception? exception = Record.Exception(() => new CultureId("CH13-C0001"));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Theory]
    [InlineData(30, 1.0)]  // at the optimum
    [InlineData(20, 0.5)]  // halfway up from the minimum
    [InlineData(35, 0.5)]  // halfway down to the maximum
    [InlineData(5, 0.0)]   // too cold
    [InlineData(40, 0.0)]  // at the maximum: no growth
    public void The_temperature_factor_rises_to_the_optimum_and_falls_to_the_maximum(double temperature, double expectedFactor) {
        // Arrange
        Species anyMesophile = Lab.Mesophile().Generate();

        // Act
        double factor = anyMesophile.TemperatureFactor(temperature);

        // Assert
        Assert.Equal(expectedFactor, factor, 9);
    }

    [Fact]
    public void Two_species_with_the_same_binomial_name_are_the_same_species() {
        // Arrange
        Species anyStrain      = Arbitrary.Species().WithBinomialName("Bacillus subtilis").Generate();
        Species anyOtherStrain = Arbitrary.Species().WithBinomialName("Bacillus subtilis").Generate();

        // Act
        bool same = anyStrain.Equals(anyOtherStrain);

        // Assert
        Assert.True(same);
    }

}
