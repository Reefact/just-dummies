using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class GrowthRateTests {

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
    public void A_zero_rate_has_no_generation_time() {
        // Arrange
        GrowthRate rate = GrowthRate.Zero;

        // Act
        Exception? error = Record.Exception(() => rate.GenerationTimeHours);

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Three_hours_at_ln2_per_hour_are_three_generations() {
        // Arrange
        GrowthRate rate = new GrowthRate(Math.Log(2));

        // Act
        double generations = rate.GenerationsIn(3);

        // Assert
        Assert.Equal(3.0, generations, 9);
    }

}

public sealed class ConcentrationTests {

    [Fact]
    public void Going_from_a_thousand_to_eight_thousand_cells_takes_three_generations() {
        // Arrange
        Concentration from = new Concentration(1_000);
        Concentration to   = new Concentration(8_000);

        // Act
        double generations = from.GenerationsTo(to);

        // Assert
        Assert.Equal(3.0, generations, 9);
    }

    [Fact]
    public void Generations_cannot_be_counted_from_an_empty_culture() {
        // Arrange
        Concentration anyTarget = Any.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Act
        Exception? error = Record.Exception(() => Concentration.Zero.GenerationsTo(anyTarget));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

}

public sealed class CultureIdTests {

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
        // Arrange
        string value = "CH13-C0001";

        // Act
        Exception? error = Record.Exception(() => new CultureId(value));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

}

public sealed class SpeciesTests {

    [Theory]
    [InlineData(20.0, 0.5)]
    [InlineData(30.0, 1.0)]
    [InlineData(35.0, 0.5)]
    [InlineData(45.0, 0.0)]
    [InlineData(5.0, 0.0)]
    public void The_temperature_factor_rises_to_the_optimum_and_falls_to_the_maximum(double temperature, double expectedFactor) {
        // Arrange
        Species anySpecies10To40 = Any.Species()
                             .WithMinTemperatureCelsius(10)
                             .WithOptimalTemperatureCelsius(30)
                             .WithMaxTemperatureCelsius(40)
                             .Generate();

        // Act
        double factor = anySpecies10To40.TemperatureFactor(temperature);

        // Assert
        Assert.Equal(expectedFactor, factor, 9);
    }

    [Fact]
    public void Two_species_with_the_same_binomial_name_are_the_same_species() {
        // Arrange
        Species anyBacillus      = Any.Species().WithBinomialName("Bacillus subtilis").Generate();
        Species anyOtherBacillus = Any.Species().WithBinomialName("Bacillus subtilis").Generate();

        // Act
        bool same = anyBacillus.Equals(anyOtherBacillus);

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void A_species_name_must_be_binomial() {
        // Arrange
        string name = "bacillus";

        // Act
        Exception? error = Record.Exception(() => Any.Species().WithBinomialName(name).Generate());

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

}
