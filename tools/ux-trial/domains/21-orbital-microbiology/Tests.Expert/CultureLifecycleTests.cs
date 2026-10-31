namespace OrbitalMicrobiology.Tests.Expert;

public sealed class CultureLifecycleTests {

    [Fact]
    public void Station_time_is_UTC() {
        // Arrange
        CultureId              anyId         = Any.CultureId().Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        DateTimeOffset         anyLocalStart = Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(2)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Culture(anyId, anyConditions, anyLocalStart));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_culture_cannot_grow_before_it_is_inoculated() {
        // Arrange
        Culture anyCulture = Any.Culture().Generate();
        double  anyHours   = Any.Double().GreaterThan(0).LessThanOrEqualTo(Culture.MaxAdvanceHours).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Advance(anyHours));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Species_are_inoculated_at_time_zero_only() {
        // Arrange
        Species           anyFirst   = Any.Species().Generate();
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(anyFirst).Generate();
        Species           anyLate    = Any.Species().WithBinomialName(n => n.DifferentFrom(anyFirst.BinomialName)).Generate();
        Concentration     anyInoculum = Lab.AnyInoculumFor(anyCulture.Culture.Conditions, anyLate).Generate();
        anyCulture.Culture.Advance(1);

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Culture.Inoculate(anyLate, anyInoculum));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void The_same_species_is_not_inoculated_twice() {
        // Arrange
        Species           anySpecies  = Any.Species().Generate();
        InoculatedCulture anyCulture  = Lab.AnyInoculatedCulture(anySpecies).Generate();
        Concentration     anyInoculum = Lab.AnyInoculumFor(anyCulture.Culture.Conditions, anySpecies).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Culture.Inoculate(anySpecies, anyInoculum));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_culture_holds_at_most_four_species() {
        // Arrange
        List<Species> anyFiveSpecies = Any.ListOf(Any.Species()).WithCount(5).Distinct().Generate();
        Culture       anyCulture     = Any.Culture().WithConditions(c => c.Nourished()).Generate();
        foreach (Species species in anyFiveSpecies.Take(4)) {
            anyCulture.Inoculate(species, Lab.AnyInoculumFor(anyCulture.Conditions, species).Generate());
        }

        Species       anyFifth    = anyFiveSpecies[4];
        Concentration anyInoculum = Lab.AnyInoculumFor(anyCulture.Conditions, anyFifth).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Inoculate(anyFifth, anyInoculum));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void An_inoculum_must_stay_below_the_carrying_capacity() {
        // Arrange
        Species       anySpecies = Any.Species().Generate();
        Culture       anyCulture = Any.Culture().Generate();
        Concentration atCapacity = anyCulture.Conditions.CarryingCapacity(anySpecies);

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Inoculate(anySpecies, atCapacity));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_species_does_not_interact_with_itself() {
        // Arrange
        Species           anySpecies = Any.Species().Generate();
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(anySpecies).Generate();
        double            anyAlpha   = Any.Double().Between(Culture.MinInteraction, Culture.MaxInteraction).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Culture.SetInteraction(anySpecies, anySpecies, anyAlpha));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void An_interaction_needs_both_species_in_the_culture() {
        // Arrange
        Species           anySpecies = Any.Species().Generate();
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(anySpecies).Generate();
        Species           anyAbsent  = Any.Species().WithBinomialName(n => n.DifferentFrom(anySpecies.BinomialName)).Generate();
        double            anyAlpha   = Any.Double().Between(Culture.MinInteraction, Culture.MaxInteraction).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Culture.SetInteraction(anySpecies, anyAbsent, anyAlpha));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_terminated_culture_refuses_to_grow() {
        // Arrange
        InoculatedCulture anyCulture = Lab.AnyInoculatedCulture(Any.Species().Generate()).Generate();
        double            anyHours   = Any.Double().GreaterThan(0).LessThanOrEqualTo(Culture.MaxAdvanceHours).Generate();
        anyCulture.Culture.Terminate();

        // Act
        Exception? refusal = Record.Exception(() => anyCulture.Culture.Advance(anyHours));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

}
