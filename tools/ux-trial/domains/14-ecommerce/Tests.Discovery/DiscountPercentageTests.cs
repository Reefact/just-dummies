using System.Globalization;

using Xunit;

namespace Shop.Domain;

public sealed class DiscountPercentageTests {

    [Theory]
    [InlineData("0")]
    [InlineData("0.1")]
    [InlineData("12.5")]
    [InlineData("99.9")]
    [InlineData("100")]
    [InlineData("100.0")]
    public void A_percentage_between_zero_and_a_hundred_with_one_decimal_is_accepted(string value) {
        decimal expected = decimal.Parse(value, CultureInfo.InvariantCulture);

        DiscountPercentage discount = new DiscountPercentage(expected);

        Assert.Equal(expected, discount.Value);
    }

    [Theory]
    [InlineData("-0.1")]
    [InlineData("-1")]
    [InlineData("-100")]
    public void A_negative_percentage_is_refused(string value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiscountPercentage(decimal.Parse(value, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("100.1")]
    [InlineData("101")]
    [InlineData("1000")]
    public void A_percentage_above_a_hundred_is_refused(string value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiscountPercentage(decimal.Parse(value, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("12.25")]
    [InlineData("0.01")]
    [InlineData("99.99")]
    public void A_percentage_with_two_decimal_places_is_refused(string value) {
        Assert.Throws<ArgumentException>(() => new DiscountPercentage(decimal.Parse(value, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void The_range_is_checked_before_the_precision() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiscountPercentage(-0.05m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiscountPercentage(100.05m));
    }

    [Fact]
    public void None_is_zero_and_is_not_full() {
        Assert.Equal(0m, DiscountPercentage.None.Value);
        Assert.False(DiscountPercentage.None.IsFull);
    }

    [Theory]
    [InlineData("100", true)]
    [InlineData("100.0", true)]
    [InlineData("99.9", false)]
    [InlineData("0", false)]
    public void Only_a_hundred_percent_is_full(string value, bool expected) {
        DiscountPercentage discount = new DiscountPercentage(decimal.Parse(value, CultureInfo.InvariantCulture));

        Assert.Equal(expected, discount.IsFull);
    }

}
