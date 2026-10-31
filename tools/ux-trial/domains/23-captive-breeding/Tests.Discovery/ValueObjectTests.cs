using CaptiveBreeding.Domain;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Discovery;

public sealed class ValueObjectTests {

    [Fact]
    public void A_studbook_id_names_its_institution_by_its_prefix() {
        // Arrange
        StudbookId id = new StudbookId("ZSL-01234");

        // Act
        string institution = id.Institution;

        // Assert
        Assert.Equal("ZSL", institution);
    }

    [Fact]
    public void A_studbook_id_in_lower_case_is_refused() {
        // Arrange
        // Act
        Exception? error = Record.Exception(() => new StudbookId("zsl-01234"));

        // Assert
        Assert.IsType<FormatException>(error);
    }

    [Fact]
    public void A_kinship_above_the_limit_exceeds_it_but_one_equal_to_it_does_not() {
        // Arrange
        KinshipCoefficient limit = new KinshipCoefficient(0.125);

        // Act
        bool aboveExceeds = new KinshipCoefficient(0.25).Exceeds(limit);
        bool equalExceeds = new KinshipCoefficient(0.125).Exceeds(limit);

        // Assert
        Assert.True(aboveExceeds);
        Assert.False(equalExceeds);
    }

    [Fact]
    public void A_pair_cannot_join_an_individual_with_itself() {
        // Arrange
        StudbookId         anyId      = Any.StudbookId().Generate();
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.5)).Generate();

        // Act
        Exception? error = Record.Exception(() => new BreedingPair(anyId, anyId, anyKinship));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

}
