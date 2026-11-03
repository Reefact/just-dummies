using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Generators;
using JustDummies;
using Xunit;

namespace CaptiveBreeding.Tests;

public sealed class GeneticContributionTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.375)]
    [InlineData(1.0)]
    public void A_proportion_inside_the_unit_interval_is_kept(double proportion) {
        // Arrange
        StudbookId anyFounderId = Any.StudbookId().Generate();

        // Act
        GeneticContribution contribution = new GeneticContribution(anyFounderId, proportion);

        // Assert
        Assert.Equal(proportion, contribution.Proportion);
    }

    [Fact]
    public void A_contribution_remembers_its_founder() {
        // Arrange
        StudbookId founderId = new StudbookId("ZSL-01234");

        // Act
        GeneticContribution contribution = new GeneticContribution(founderId, 0.5);

        // Assert
        Assert.Equal(founderId, contribution.FounderId);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(1.001)]
    [InlineData(double.NaN)]
    public void A_proportion_outside_the_unit_interval_is_refused(double proportion) {
        // Arrange
        StudbookId anyFounderId = Any.StudbookId().Generate();

        // Act
        Action act = () => new GeneticContribution(anyFounderId, proportion);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_contribution_without_founder_is_refused() {
        // Act
        Action act = () => new GeneticContribution(null!, 0.5);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void A_contribution_of_zero_is_lost() {
        // Arrange
        GeneticContribution contribution = Any.GeneticContribution().WithProportion(0.0).Generate();

        // Assert
        Assert.True(contribution.IsLost);
    }

    [Fact]
    public void A_contribution_above_zero_is_not_lost() {
        // Arrange
        GeneticContribution contribution = Any.GeneticContribution().WithProportion(0.000001).Generate();

        // Assert
        Assert.False(contribution.IsLost);
    }

}
