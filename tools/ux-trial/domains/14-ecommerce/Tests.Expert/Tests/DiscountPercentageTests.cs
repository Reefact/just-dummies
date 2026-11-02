using JustDummies;

using Xunit;

namespace Shop.Domain.Tests;

public sealed class DiscountPercentageTests {

    public static TheoryData<decimal> AcceptedValues => new TheoryData<decimal> { 0m, 0.1m, 12.5m, 12.50m, 99.9m, 100m };

    [Theory]
    [MemberData(nameof(AcceptedValues))]
    public void A_percentage_from_zero_to_one_hundred_with_one_decimal_is_kept(decimal value) {
        // Act
        DiscountPercentage discount = new DiscountPercentage(value);

        // Assert
        Assert.Equal(value, discount.Value);
    }

    public static TheoryData<decimal> NegativeValues => new TheoryData<decimal> { -0.1m, -1m, -100m };

    [Theory]
    [MemberData(nameof(NegativeValues))]
    public void A_negative_percentage_is_refused(decimal value) {
        // Act / Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DiscountPercentage(value));
        Assert.Equal("value", error.ParamName);
    }

    public static TheoryData<decimal> ValuesAboveOneHundred => new TheoryData<decimal> { 100.1m, 101m, 1000m };

    [Theory]
    [MemberData(nameof(ValuesAboveOneHundred))]
    public void A_percentage_above_one_hundred_is_refused(decimal value) {
        // Act / Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DiscountPercentage(value));
        Assert.Equal("value", error.ParamName);
    }

    public static TheoryData<decimal> TooPreciseValues => new TheoryData<decimal> { 12.25m, 0.05m, 99.99m };

    [Theory]
    [MemberData(nameof(TooPreciseValues))]
    public void A_percentage_with_two_decimals_is_refused(decimal value) {
        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new DiscountPercentage(value));
        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void The_range_is_checked_before_the_precision() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiscountPercentage(100.05m));
    }

    [Fact]
    public void None_is_a_zero_percentage() {
        // Act
        DiscountPercentage none = DiscountPercentage.None;

        // Assert
        Assert.Equal(0m, none.Value);
        Assert.False(none.IsFull);
    }

    [Fact]
    public void One_hundred_percent_is_full() {
        // Arrange
        DiscountPercentage hundred = new DiscountPercentage(100m);

        // Act / Assert
        Assert.True(hundred.IsFull);
    }

    [Fact]
    public void Ninety_nine_point_nine_percent_is_not_full() {
        // Arrange
        DiscountPercentage almostFull = new DiscountPercentage(99.9m);

        // Act / Assert
        Assert.False(almostFull.IsFull);
    }

}
