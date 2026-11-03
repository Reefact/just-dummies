using JustDummies;

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
    public void A_positive_amount_to_the_cent_up_to_one_hundred_thousand_is_accepted(string text) {
        // Arrange
        decimal amount = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ReimbursementPrice price = new ReimbursementPrice(amount);

        // Assert
        Assert.Equal(amount, price.Amount);
    }

    [Fact]
    public void A_drawn_price_keeps_its_amount() {
        // Arrange
        ReimbursementPrice anyPrice = new AnyReimbursementPrice().Generate();

        // Act
        ReimbursementPrice sameAmount = new ReimbursementPrice(anyPrice.Amount);

        // Assert
        Assert.Equal(anyPrice, sameAmount);
    }

    [Fact]
    public void Zero_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementPrice(0m));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        decimal anyNegative = Any.Decimal().Negative().WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementPrice(anyNegative));
    }

    [Fact]
    public void An_amount_above_one_hundred_thousand_is_refused() {
        // Arrange
        decimal anyTooMuch = Any.Decimal().GreaterThan(100_000m).LessThanOrEqualTo(1_000_000m).WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementPrice(anyTooMuch));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementPrice(100_000.01m));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("12.345")]
    [InlineData("1.005")]
    public void A_fraction_of_a_cent_is_refused(string text) {
        // Arrange
        decimal amount = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReimbursementPrice(amount));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void The_range_is_checked_before_the_scale() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementPrice(100_000.001m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementPrice(-0.001m));
    }

    [Fact]
    public void Prices_are_equal_by_amount_whatever_their_trailing_zeros() {
        // Assert
        Assert.Equal(new ReimbursementPrice(12.5m), new ReimbursementPrice(12.50m));
        Assert.Equal(new ReimbursementPrice(12.5m).GetHashCode(), new ReimbursementPrice(12.50m).GetHashCode());
        Assert.NotEqual(new ReimbursementPrice(12.5m), new ReimbursementPrice(12.51m));
    }

}
