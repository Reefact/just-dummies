using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class PolicyTermsTests {

    [Fact]
    public void A_negative_deductible_is_refused() {
        // Arrange
        decimal anyLimit = Any.Decimal().Between(1m, 1_000_000m).WithScale(2).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new PolicyTerms(-0.01m, anyLimit));

        // Assert
        Assert.Equal("deductible", error.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void A_limit_that_is_not_positive_is_refused(string limit) {
        // Arrange
        decimal anyDeductible = Any.Decimal().Between(0m, 1_000_000m).WithScale(2).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new PolicyTerms(anyDeductible, decimal.Parse(limit)));

        // Assert
        Assert.Equal("limit", error.ParamName);
    }

    [Fact]
    public void A_zero_deductible_is_accepted() {
        // Arrange
        decimal anyLimit = Any.Decimal().Between(1m, 1_000_000m).WithScale(2).Generate();

        // Act
        PolicyTerms terms = new PolicyTerms(0m, anyLimit);

        // Assert
        Assert.Equal(0m, terms.Deductible);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1000", "0")]
    [InlineData("1000.01", "0.01")]
    [InlineData("4000", "3000")]
    [InlineData("11000", "10000")]
    [InlineData("1000000", "10000")]
    public void The_insurer_pays_above_the_deductible_up_to_the_limit(string groundUp, string expected) {
        // Arrange
        PolicyTerms terms = new PolicyTerms(1_000m, 10_000m);

        // Act
        decimal paid = terms.Apply(decimal.Parse(groundUp));

        // Assert
        Assert.Equal(decimal.Parse(expected), paid);
    }

    [Fact]
    public void A_negative_ground_up_loss_is_refused() {
        // Arrange
        PolicyTerms anyTerms = Any.PolicyTerms().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyTerms.Apply(-0.01m));
    }

    [Fact]
    public void Terms_without_deductible_pay_the_whole_loss_below_the_limit() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(0m, 500m);

        // Act & Assert
        Assert.Equal(123.45m, terms.Apply(123.45m));
    }

}
