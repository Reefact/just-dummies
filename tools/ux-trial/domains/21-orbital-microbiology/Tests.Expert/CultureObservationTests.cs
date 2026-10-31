namespace OrbitalMicrobiology.Tests.Expert;

public sealed class CultureObservationTests {

    [Fact]
    public void An_observation_of_another_culture_is_refused() {
        // Arrange
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(Any.Species().Generate()).Generate();
        Observation anyFromElsewhere = Any.Observation()
                                          .WithCultureId(id => id.WithValue(v => v.DifferentFrom(anyCulture.Culture.Id.Value)))
                                          .WithSpecies(anyCulture.Species)
                                          .WithElapsedHours(0)
                                          .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Culture.Record(anyFromElsewhere));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void An_observation_taken_more_than_half_an_hour_from_now_is_refused() {
        // Arrange
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(Any.Species().Generate()).Generate();
        anyCulture.Culture.Advance(10);
        Observation anyStale = Any.Observation()
                                  .WithCultureId(anyCulture.Culture.Id)
                                  .WithSpecies(anyCulture.Species)
                                  .WithElapsedHours(9)
                                  .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Culture.Record(anyStale));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void An_observation_of_a_species_not_grown_here_is_refused() {
        // Arrange
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(Any.Species().Generate()).Generate();
        Observation anyOfAStranger = Any.Observation()
                                        .WithCultureId(anyCulture.Culture.Id)
                                        .WithSpecies(s => s.WithBinomialName(n => n.DifferentFrom(anyCulture.Species.BinomialName)))
                                        .WithElapsedHours(0)
                                        .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Culture.Record(anyOfAStranger));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void An_observation_as_certain_as_the_model_lands_the_estimate_halfway_and_tightens_it() {
        // Arrange
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(Any.Species().Generate(), inoculum: new Concentration(100_000)).Generate();
        Observation anyCount = Any.Observation()
                                  .WithCultureId(anyCulture.Culture.Id)
                                  .WithSpecies(anyCulture.Species)
                                  .WithElapsedHours(0)
                                  .WithMeasured(new Concentration(300_000))
                                  .WithMethod(ObservationMethod.PlateCount)
                                  .WithRelativeUncertainty(Culture.InitialRelativeUncertainty)
                                  .Generate();

        // Act
        anyCulture.Culture.Record(anyCount);

        // Assert
        EstimatedState estimate = anyCulture.Culture.Estimate(anyCulture.Species);
        Assert.Equal(200_000, estimate.Concentration.CellsPerMillilitre, 6);
        Assert.Equal(Culture.InitialRelativeUncertainty / Math.Sqrt(2), estimate.RelativeUncertainty, 9);
        Assert.Equal(1, estimate.ObservationCount);
        Assert.False(estimate.IsModelOnly);
    }

}
