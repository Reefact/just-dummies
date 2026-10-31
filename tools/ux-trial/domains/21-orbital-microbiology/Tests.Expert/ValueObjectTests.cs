namespace OrbitalMicrobiology.Tests.Expert;

public sealed class ValueObjectTests {

    [Fact]
    public void A_population_growing_at_ln2_per_hour_doubles_every_hour() {
        // Arrange
        GrowthRate rate = new GrowthRate(Math.Log(2));

        // Act
        double generationTime = rate.GenerationTimeHours;
        double generations    = rate.GenerationsIn(5);

        // Assert
        Assert.Equal(1, generationTime, 12);
        Assert.Equal(5, generations, 12);
    }

    [Fact]
    public void A_zero_growth_rate_has_no_generation_time() {
        // Arrange
        GrowthRate rate = GrowthRate.Zero;

        // Act
        Exception? refusal = Record.Exception(() => rate.GenerationTimeHours);

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Growing_eightfold_takes_three_generations() {
        // Arrange
        Concentration start  = new Concentration(1_000);
        Concentration target = new Concentration(8_000);

        // Act
        double generations = start.GenerationsTo(target);

        // Assert
        Assert.Equal(3, generations, 12);
    }

    [Fact]
    public void Generations_cannot_be_counted_from_an_empty_culture() {
        // Arrange
        Concentration anyTarget = Any.Concentration().WithCellsPerMillilitre(c => c.GreaterThan(0)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Concentration.Zero.GenerationsTo(anyTarget));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_culture_id_names_the_chamber_it_grows_in() {
        // Arrange
        string anySequence = Any.StringMatching("C[0-9]{4}").Generate();

        // Act
        CultureId id = new CultureId($"CH07-{anySequence}");

        // Assert
        Assert.Equal(7, id.Chamber);
    }

    [Fact]
    public void The_station_has_no_thirteenth_chamber() {
        // Arrange
        string anySequence = Any.StringMatching("C[0-9]{4}").Generate();

        // Act
        Exception? refusal = Record.Exception(() => new CultureId($"CH13-{anySequence}"));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

}
