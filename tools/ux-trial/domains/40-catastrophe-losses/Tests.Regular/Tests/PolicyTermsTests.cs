namespace CatastropheLosses.Tests.Regular.Tests;

public sealed class PolicyTermsTests {

    [Fact]
    public void A_ground_up_loss_within_the_deductible_pays_nothing() {
        // Arrange
        decimal     anyGroundUpLoss = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate();
        PolicyTerms terms           = Arbitrary.PolicyTerms().WithDeductible(10_000m).Generate();

        // Act
        decimal insuredLoss = terms.Apply(anyGroundUpLoss);

        // Assert
        Assert.Equal(0m, insuredLoss);
    }

    [Fact]
    public void The_insurer_pays_the_ground_up_loss_less_the_deductible_below_the_limit() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(deductible: 5_000m, limit: 100_000m);

        // Act
        decimal insuredLoss = terms.Apply(42_500m);

        // Assert
        Assert.Equal(37_500m, insuredLoss);
    }

    [Fact]
    public void The_insured_loss_is_capped_at_the_limit() {
        // Arrange
        decimal     anyHugeGroundUpLoss = Any.Decimal().GreaterThan(1_000_000m).WithScale(2).Generate();
        PolicyTerms terms               = Arbitrary.PolicyTerms()
                                                   .WithDeductible(d => d.LessThanOrEqualTo(10_000m))
                                                   .WithLimit(250_000m)
                                                   .Generate();

        // Act
        decimal insuredLoss = terms.Apply(anyHugeGroundUpLoss);

        // Assert
        Assert.Equal(250_000m, insuredLoss);
    }

    [Fact]
    public void A_negative_deductible_is_refused() {
        // Arrange
        decimal anyLimit = Any.Decimal().Positive().Generate();

        // Act
        Exception? error = Record.Exception(() => new PolicyTerms(-1m, anyLimit));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

}
