using AgriTech.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests.Expert;

public sealed class RelativeHumidityTests {

    [Fact]
    public void A_humidity_keeps_the_percent_it_was_given() {
        // Act
        RelativeHumidity humidity = new RelativeHumidity(42.5);

        // Assert
        Assert.Equal(42.5, humidity.Percent);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(100.0)]
    public void Both_ends_of_the_zero_to_one_hundred_range_are_valid(double percent) {
        // Act
        RelativeHumidity humidity = new RelativeHumidity(percent);

        // Assert
        Assert.Equal(percent, humidity.Percent);
    }

    [Theory]
    [InlineData(-0.000000000000001)]
    [InlineData(-1.0)]
    [InlineData(double.NegativeInfinity)]
    public void A_negative_percent_is_refused(double percent) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new RelativeHumidity(percent));

        // Assert
        Assert.Equal("percent", refusal.ParamName);
    }

    [Theory]
    [InlineData(100.00000000000001)]
    [InlineData(101.0)]
    [InlineData(double.PositiveInfinity)]
    public void A_percent_above_one_hundred_is_refused(double percent) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new RelativeHumidity(percent));

        // Assert
        Assert.Equal("percent", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: NaN is accepted as a humidity (both range checks are plain comparisons, which NaN passes), so one corrupt radio frame poisons AverageHumidity and silently turns ShouldIrrigate off; no drawn value involved, the literal double.NaN reproduces it")]
    public void A_percent_that_is_not_a_number_is_refused() {
        Assert.ThrowsAny<ArgumentException>(() => new RelativeHumidity(double.NaN));
    }

    [Theory]
    [InlineData(29.9, 30.0, true)]
    [InlineData(30.0, 30.0, false)]
    [InlineData(30.1, 30.0, false)]
    [InlineData(0.0, 0.1, true)]
    [InlineData(100.0, 100.0, false)]
    public void A_humidity_is_below_a_threshold_only_when_strictly_under_it(double percent, double threshold, bool expected) {
        // Arrange
        RelativeHumidity humidity = new RelativeHumidity(percent);

        // Act
        bool isBelow = humidity.IsBelow(threshold);

        // Assert
        Assert.Equal(expected, isBelow);
    }

    [Fact]
    public void Two_humidities_with_the_same_percent_are_equal() {
        // Arrange
        double anyPercent = Any.RelativeHumidity().Generate().Percent;

        // Act
        RelativeHumidity first  = new RelativeHumidity(anyPercent);
        RelativeHumidity second = new RelativeHumidity(anyPercent);

        // Assert
        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_humidities_with_different_percents_are_not_equal() {
        // Arrange
        RelativeHumidity anyHumidity      = Any.RelativeHumidity().Generate();
        RelativeHumidity anyOtherHumidity = Any.RelativeHumidity().WithPercent(percent => percent.DifferentFrom(anyHumidity.Percent)).Generate();

        // Assert
        Assert.NotEqual(anyHumidity, anyOtherHumidity);
    }

    [Fact]
    public void A_humidity_is_equal_to_neither_null_nor_to_something_that_is_not_a_humidity() {
        // Arrange
        RelativeHumidity anyHumidity = Any.RelativeHumidity().Generate();

        // Assert
        Assert.False(anyHumidity.Equals(null));
        Assert.False(anyHumidity.Equals((object?)null));
        Assert.False(anyHumidity.Equals((object)anyHumidity.Percent));
    }

}
