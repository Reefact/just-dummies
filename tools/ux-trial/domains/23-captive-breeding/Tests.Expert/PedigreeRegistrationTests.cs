using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Scenarios;

namespace CaptiveBreeding.Tests.Expert;

public sealed class PedigreeRegistrationTests {

    [Fact]
    public void An_offspring_whose_parents_are_not_in_the_studbook_is_refused() {
        // Arrange
        Studbook   studbook          = new();
        Individual anyUnrecordedSire = Any.Individual().WithSex(Sex.Male).Generate();
        Individual anyDam            = studbook.Female();
        Individual anyOffspring      = Any.Individual().AsOffspringOf(anyUnrecordedSire, anyDam).Generate();

        // Act
        Action register = () => studbook.Pedigree.Register(anyOffspring);

        // Assert
        Assert.Throws<KeyNotFoundException>(register);
    }

    [Fact]
    public void An_offspring_born_the_same_day_as_its_dam_is_refused() {
        // Arrange
        Studbook   studbook     = new();
        Individual anySire      = studbook.Male();
        Individual anyDam       = studbook.Female();
        Individual anyOffspring = Any.Individual().AsOffspringOf(anySire, anyDam).WithBirthDate(anyDam.BirthDate).Generate();

        // Act
        Action register = () => studbook.Pedigree.Register(anyOffspring);

        // Assert
        Assert.Throws<InvalidOperationException>(register);
    }

    [Fact]
    public void An_offspring_born_after_its_dam_died_is_refused() {
        // Arrange
        Studbook   studbook     = new();
        Individual anySire      = studbook.Male();
        Individual anyDam       = studbook.Female();
        Individual anyOffspring = Any.Individual().AsOffspringOf(anySire, anyDam).Generate();
        anyDam.RecordDeath(anyOffspring.BirthDate.AddDays(-1));

        // Act
        Action register = () => studbook.Pedigree.Register(anyOffspring);

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(register);
        Assert.Contains("died before", refusal.Message);
    }

    [Fact]
    public void A_founder_is_generation_zero_and_an_offspring_counts_its_deepest_parental_line() {
        // Arrange: the sire's line is two generations deep, the dam is a founder.
        Studbook   studbook      = new();
        Individual anyGrandsire  = studbook.Male();
        Individual anyGranddam   = studbook.Female();
        Individual anySire       = studbook.SonOf(anyGrandsire, anyGranddam);
        Individual anyFounderDam = studbook.Female();
        Individual anyOffspring  = studbook.OffspringOf(anySire, anyFounderDam);

        // Act
        int founderGeneration   = studbook.Pedigree.GenerationOf(anyFounderDam.Id);
        int offspringGeneration = studbook.Pedigree.GenerationOf(anyOffspring.Id);

        // Assert
        Assert.Equal(0, founderGeneration);
        Assert.Equal(2, offspringGeneration);
    }

}
