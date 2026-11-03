using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class ReimbursementPriceTests {

    [Theory]
    [InlineData("0.01")]
    [InlineData("12.5")]
    [InlineData("12.50")]
    [InlineData("99999.99")]
    [InlineData("100000")]
    [InlineData("100000.00")]
    public void An_amount_up_to_a_hundred_thousand_to_the_cent_is_accepted(string amount) {
        // Arrange
        decimal value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ReimbursementPrice price = new ReimbursementPrice(value);

        // Assert
        Assert.Equal(value, price.Amount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-100")]
    [InlineData("100000.01")]
    [InlineData("100001")]
    public void An_amount_outside_zero_exclusive_to_a_hundred_thousand_is_refused_as_out_of_range(string amount) {
        // Arrange
        decimal value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementPrice(value));
    }

    [Theory]
    [InlineData("1.005")]
    [InlineData("0.001")]
    [InlineData("99.999")]
    public void An_amount_with_more_than_two_decimals_is_refused(string amount) {
        // Arrange
        decimal value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ReimbursementPrice(value));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.001")]
    [InlineData("100000.001")]
    public void The_range_is_checked_before_the_decimals(string amount) {
        // Arrange
        decimal value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementPrice(value));
    }

    [Fact]
    public void Prices_with_the_same_amount_are_equal_whatever_their_trailing_zeros() {
        // Arrange
        ReimbursementPrice price      = new ReimbursementPrice(12.5m);
        ReimbursementPrice samePrice  = new ReimbursementPrice(12.50m);

        // Assert
        Assert.Equal(price, samePrice);
        Assert.Equal(price.GetHashCode(), samePrice.GetHashCode());
    }

    [Fact]
    public void Two_prices_of_the_same_drawn_amount_are_equal() {
        // Arrange
        ReimbursementPrice anyPrice = new AnyReimbursementPrice().Generate();

        // Act
        ReimbursementPrice samePrice = new ReimbursementPrice(anyPrice.Amount);

        // Assert
        Assert.Equal(anyPrice, samePrice);
        Assert.Equal(anyPrice.GetHashCode(), samePrice.GetHashCode());
    }

    [Fact]
    public void Prices_of_different_amounts_are_not_equal() {
        Assert.NotEqual(new ReimbursementPrice(12.50m), new ReimbursementPrice(12.51m));
    }

}
