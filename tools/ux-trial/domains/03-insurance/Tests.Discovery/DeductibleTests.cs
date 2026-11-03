using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests.Discovery;

public sealed class DeductibleTests {

    [Theory]
    [InlineData("0")]
    [InlineData("0.01")]
    [InlineData("250.75")]
    [InlineData("4999.99")]
    [InlineData("5000")]
    [InlineData("5000.00")]
    public void An_amount_in_cents_within_the_ceiling_is_kept(string text) {
        // Arrange
        decimal amount = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(amount, new Deductible(amount).Amount);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-100")]
    [InlineData("5000.01")]
    [InlineData("1000000")]
    public void An_amount_outside_the_range_is_refused(string text) {
        // Arrange
        decimal amount = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("amount", () => new Deductible(amount));
    }

    [Theory]
    [InlineData("10.001")]
    [InlineData("0.005")]
    [InlineData("4999.999")]
    public void An_amount_finer_than_a_cent_is_refused(string text) {
        // Arrange
        decimal amount = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.Throws<ArgumentException>("amount", () => new Deductible(amount));
    }

    [Fact]
    public void An_amount_above_the_ceiling_and_finer_than_a_cent_is_refused_for_its_range() {
        Assert.Throws<ArgumentOutOfRangeException>("amount", () => new Deductible(5000.001m));
    }

    [Fact]
    public void The_scaffolded_generator_only_draws_acceptable_deductibles() {
        // Act
        Deductible anyDeductible = new AnyDeductible().Generate();

        // Assert
        Assert.InRange(anyDeductible.Amount, 0m, Deductible.Ceiling);
    }

}
