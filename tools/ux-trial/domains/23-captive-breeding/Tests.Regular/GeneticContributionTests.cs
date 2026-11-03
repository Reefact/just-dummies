using System.Globalization;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Domain.Tests.Regular;

public sealed class GeneticContributionTests {

    [Fact]
    public void A_founder_and_a_proportion_are_kept() {
        // Arrange
        StudbookId anyFounder    = Any.StudbookId().Generate();
        double     anyProportion = Any.Double().Between(0d, 1d).Generate();

        // Act
        GeneticContribution contribution = new GeneticContribution(anyFounder, anyProportion);

        // Assert
        Assert.Same(anyFounder, contribution.FounderId);
        Assert.Equal(anyProportion, contribution.Proportion);
    }

    [Fact]
    public void A_contribution_of_zero_is_lost() {
        // Arrange
        StudbookId anyFounder = Any.StudbookId().Generate();

        // Act
        GeneticContribution contribution = new GeneticContribution(anyFounder, 0d);

        // Assert
        Assert.True(contribution.IsLost);
    }

    [Fact]
    public void A_positive_contribution_is_not_lost() {
        // Arrange
        double anyProportion = Any.Double().GreaterThan(0d).LessThanOrEqualTo(1d).Generate();

        // Act
        GeneticContribution contribution = Any.GeneticContribution().WithProportion(anyProportion).Generate();

        // Assert
        Assert.False(contribution.IsLost);
    }

    [Fact]
    public void A_contribution_of_one_is_accepted() {
        Assert.Equal(1d, Any.GeneticContribution().WithProportion(1d).Generate().Proportion);
    }

    [Fact]
    public void A_negative_proportion_is_refused() {
        // Arrange
        double anyNegative = Any.Double().Negative().Generate();

        // Act
        Action construction = () => Any.GeneticContribution().WithProportion(anyNegative).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_proportion_above_one_is_refused() {
        // Arrange
        double anyTooLarge = Any.Double().GreaterThan(1d).Generate();

        // Act
        Action construction = () => Any.GeneticContribution().WithProportion(anyTooLarge).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_proportion_that_is_not_a_number_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeneticContribution(Any.StudbookId().Generate(), double.NaN));
    }

    [Fact]
    public void A_missing_founder_is_refused() {
        // Arrange
        double anyProportion = Any.Double().Between(0d, 1d).Generate();

        // Act
        Action construction = () => new GeneticContribution(null!, anyProportion);

        // Assert
        Assert.Throws<ArgumentNullException>(construction);
    }

    [Fact]
    public void The_text_names_the_founder_and_the_share_in_percent() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try {
            GeneticContribution contribution = new GeneticContribution(new StudbookId("ZSL-01234"), 0.25d);

            // Act
            string text = contribution.ToString();

            // Assert
            Assert.StartsWith("ZSL-01234: 25.0", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
