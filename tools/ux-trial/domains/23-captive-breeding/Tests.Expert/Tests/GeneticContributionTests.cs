using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Expert.Tests;

public sealed class GeneticContributionTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.375)]
    [InlineData(1.0)]
    public void A_proportion_within_zero_and_one_is_kept_with_its_founder(double proportion) {
        // Arrange
        StudbookId anyFounderId = Any.StudbookId().Generate();

        // Act
        GeneticContribution contribution = new GeneticContribution(anyFounderId, proportion);

        // Assert
        Assert.Equal(proportion, contribution.Proportion);
        Assert.Same(anyFounderId, contribution.FounderId);
    }

    [Theory]
    [InlineData(-0.0000001)]
    [InlineData(1.0000001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_proportion_outside_zero_and_one_is_refused(double proportion) {
        // Arrange
        StudbookId anyFounderId = Any.StudbookId().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeneticContribution(anyFounderId, proportion));
    }

    [Fact]
    public void A_contribution_needs_a_founder() {
        Assert.Throws<ArgumentNullException>(() => new GeneticContribution(null!, 0.5));
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.0001, false)]
    [InlineData(1.0, false)]
    public void A_founder_is_lost_only_when_nothing_of_it_remains(double proportion, bool expected) {
        // Arrange
        GeneticContribution contribution = new GeneticContribution(Any.StudbookId().Generate(), proportion);

        // Assert
        Assert.Equal(expected, contribution.IsLost);
    }

}
