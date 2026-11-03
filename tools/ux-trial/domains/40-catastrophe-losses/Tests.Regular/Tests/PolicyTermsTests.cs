using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Regular;

public sealed class PolicyTermsTests {

    [Fact]
    public void A_deductible_cannot_be_negative() {
        decimal anyLimit = Any.Decimal().Positive().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new PolicyTerms(-0.01m, anyLimit));

        Assert.Equal("deductible", error.ParamName);
    }

    [Fact]
    public void A_zero_deductible_is_allowed() {
        decimal anyLimit = Any.Decimal().Positive().Generate();

        PolicyTerms terms = new(0m, anyLimit);

        Assert.Equal(0m, terms.Deductible);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void A_limit_must_be_strictly_positive(string limit) {
        decimal anyDeductible = Any.Decimal().GreaterThanOrEqualTo(0m).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new PolicyTerms(anyDeductible, Fixtures.D(limit)));

        Assert.Equal("limit", error.ParamName);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("99.99", "0")]
    [InlineData("100", "0")]
    [InlineData("100.01", "0.01")]
    [InlineData("300", "200")]
    [InlineData("600", "500")]
    [InlineData("600.01", "500")]
    [InlineData("1000000", "500")]
    public void The_insurer_pays_above_the_deductible_up_to_the_limit(string groundUp, string expected) {
        PolicyTerms terms = Any.PolicyTerms().WithDeductible(100m).WithLimit(500m).Generate();

        decimal insured = terms.Apply(Fixtures.D(groundUp));

        Assert.Equal(Fixtures.D(expected), insured);
    }

    [Fact]
    public void Without_a_deductible_the_insurer_pays_the_loss_up_to_the_limit() {
        PolicyTerms terms = Any.PolicyTerms().WithDeductible(0m).WithLimit(500m).Generate();

        Assert.Equal(250m, terms.Apply(250m));
        Assert.Equal(500m, terms.Apply(750m));
    }

    [Fact]
    public void A_negative_ground_up_loss_is_refused() {
        PolicyTerms anyTerms = Any.PolicyTerms().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyTerms.Apply(-0.01m));

        Assert.Equal("groundUpLoss", error.ParamName);
    }

}
