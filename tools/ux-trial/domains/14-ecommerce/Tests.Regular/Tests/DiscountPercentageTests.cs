using System.Globalization;

using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class DiscountPercentageTests {

    [Theory]
    [InlineData("0")]
    [InlineData("0.1")]
    [InlineData("12.5")]
    [InlineData("99.9")]
    [InlineData("100")]
    public void A_percentage_from_0_to_100_with_at_most_one_decimal_is_accepted(string value) {
        // Act
        DiscountPercentage discount = new DiscountPercentage(decimal.Parse(value, CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(decimal.Parse(value, CultureInfo.InvariantCulture), discount.Value);
    }

    [Theory]
    [InlineData("-0.1")]
    [InlineData("-50")]
    [InlineData("100.1")]
    [InlineData("101")]
    public void A_percentage_outside_0_to_100_is_refused(string value) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiscountPercentage(decimal.Parse(value, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("12.25")]
    [InlineData("0.01")]
    [InlineData("99.99")]
    public void A_percentage_with_more_than_one_decimal_is_refused(string value) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DiscountPercentage(decimal.Parse(value, CultureInfo.InvariantCulture)));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void None_is_no_discount_at_all() {
        // Act & Assert
        Assert.Equal(0m, DiscountPercentage.None.Value);
        Assert.False(DiscountPercentage.None.IsFull);
    }

    [Fact]
    public void Only_a_hundred_percent_is_full() {
        // Act & Assert
        Assert.True(new DiscountPercentage(100m).IsFull);
        Assert.False(new DiscountPercentage(99.9m).IsFull);
    }

}
