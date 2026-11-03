using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace CatastropheLosses.Tests.Expert;

public sealed class PolicyTermsTests {

    [Fact]
    public void Terms_keep_the_deductible_and_the_limit_they_were_built_from() {
        // Act
        PolicyTerms terms = new PolicyTerms(1_000m, 50_000m);

        // Assert
        Assert.Equal(1_000m, terms.Deductible);
        Assert.Equal(50_000m, terms.Limit);
    }

    [Fact]
    public void A_policy_without_deductible_is_accepted() {
        // Arrange
        decimal anyLimit = Any.Decimal().Positive().Generate();

        // Act
        PolicyTerms terms = new PolicyTerms(0m, anyLimit);

        // Assert
        Assert.Equal(0m, terms.Deductible);
    }

    [Fact]
    public void A_negative_deductible_is_refused() {
        // Arrange
        decimal anyNegativeDeductible = Any.Decimal().Negative().Generate();
        decimal anyLimit              = Any.Decimal().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PolicyTerms(anyNegativeDeductible, anyLimit));

        // Assert
        Assert.Equal("deductible", refusal.ParamName);
    }

    [Fact]
    public void A_deductible_just_below_zero_is_refused() {
        // Arrange
        decimal anyLimit = Any.Decimal().Positive().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new PolicyTerms(-0.01m, anyLimit));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_policy_that_pays_nothing_at_most_is_refused() {
        // Arrange
        decimal anyDeductible = Any.Decimal().GreaterThanOrEqualTo(0m).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PolicyTerms(anyDeductible, 0m));

        // Assert
        Assert.Equal("limit", refusal.ParamName);
    }

    [Fact]
    public void A_negative_limit_is_refused() {
        // Arrange
        decimal anyDeductible    = Any.Decimal().GreaterThanOrEqualTo(0m).Generate();
        decimal anyNegativeLimit = Any.Decimal().Negative().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new PolicyTerms(anyDeductible, anyNegativeLimit));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_loss_within_the_deductible_is_not_paid() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(1_000m, 50_000m);

        // Act
        decimal paid = terms.Apply(999.99m);

        // Assert
        Assert.Equal(0m, paid);
    }

    [Fact]
    public void A_loss_equal_to_the_deductible_is_not_paid() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(1_000m, 50_000m);

        // Act
        decimal paid = terms.Apply(1_000m);

        // Assert
        Assert.Equal(0m, paid);
    }

    [Fact]
    public void A_loss_above_the_deductible_is_paid_for_the_excess() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(1_000m, 50_000m);

        // Act
        decimal paid = terms.Apply(11_000m);

        // Assert
        Assert.Equal(10_000m, paid);
    }

    [Fact]
    public void An_excess_equal_to_the_limit_is_paid_in_full() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(1_000m, 50_000m);

        // Act
        decimal paid = terms.Apply(51_000m);

        // Assert
        Assert.Equal(50_000m, paid);
    }

    [Fact]
    public void An_excess_beyond_the_limit_is_capped_at_the_limit() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(1_000m, 50_000m);

        // Act
        decimal paid = terms.Apply(51_000.01m);

        // Assert
        Assert.Equal(50_000m, paid);
    }

    [Fact]
    public void Without_a_deductible_a_loss_is_paid_up_to_the_limit() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(0m, 50_000m);

        // Act
        decimal withinLimit  = terms.Apply(20_000m);
        decimal beyondLimit  = terms.Apply(80_000m);

        // Assert
        Assert.Equal(20_000m, withinLimit);
        Assert.Equal(50_000m, beyondLimit);
    }

    [Fact]
    public void No_loss_is_paid_nothing() {
        // Arrange
        PolicyTerms anyTerms = Any.PolicyTerms().Generate();

        // Act
        decimal paid = anyTerms.Apply(0m);

        // Assert
        Assert.Equal(0m, paid);
    }

    [Fact]
    public void A_negative_ground_up_loss_is_refused() {
        // Arrange
        PolicyTerms anyTerms          = Any.PolicyTerms().Generate();
        decimal     anyNegativeLoss   = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyTerms.Apply(anyNegativeLoss));

        // Assert
        Assert.Equal("groundUpLoss", refusal.ParamName);
    }

}
