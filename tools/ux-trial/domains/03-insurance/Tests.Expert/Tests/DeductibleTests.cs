using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests;

public sealed class DeductibleTests {

    [Theory]
    [InlineData("0")]          // zero is accepted: a policy without deductible
    [InlineData("0.01")]       // one cent
    [InlineData("250")]
    [InlineData("4999.99")]
    [InlineData("5000")]       // the ceiling is accepted
    [InlineData("10.100")]     // a trailing zero is not a third decimal place
    public void An_amount_between_zero_and_the_ceiling_in_cents_is_accepted_ends_included(string text) {
        // Arrange
        decimal amount = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        Deductible deductible = new Deductible(amount);

        // Assert
        Assert.Equal(amount, deductible.Amount);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-1000")]
    [InlineData("5000.01")]
    [InlineData("1000000")]
    public void An_amount_outside_zero_and_the_ceiling_is_out_of_range(string text) {
        // Arrange
        decimal amount = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act + Assert
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Deductible(amount));
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("10.005")]
    [InlineData("4999.999")]
    public void An_amount_with_a_third_decimal_place_is_refused_as_a_plain_argument_error(string text) {
        // Arrange
        decimal amount = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act + Assert (the exact type: it is not a range problem)
        Assert.Throws<ArgumentException>(() => new Deductible(amount));
    }

    [Fact]
    public void The_ceiling_is_five_thousand() {
        Assert.Equal(5_000m, Deductible.Ceiling);
    }

}
