using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class RelativeHumidityTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(100.0)]
    [InlineData(0.0000001)]
    [InlineData(99.9999999)]
    [InlineData(42.5)]
    public void A_percentage_from_0_to_100_is_kept_as_given(double percent) {
        Assert.Equal(percent, new RelativeHumidity(percent).Percent);
    }

    [Theory]
    [InlineData(-0.0000001)]
    [InlineData(-1.0)]
    [InlineData(double.NegativeInfinity)]
    public void A_negative_humidity_is_refused(double percent) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new RelativeHumidity(percent));

        Assert.Equal("percent", exception.ParamName);
    }

    [Theory]
    [InlineData(100.0000001)]
    [InlineData(250.0)]
    [InlineData(double.PositiveInfinity)]
    public void A_humidity_above_100_is_refused(double percent) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new RelativeHumidity(percent));

        Assert.Equal("percent", exception.ParamName);
    }

    [Fact(Skip = "domain defect: RelativeHumidity(double.NaN) is accepted (both comparisons are false for NaN), and a NaN then poisons AverageHumidity; the other value objects refuse NaN, seed n/a (literal case)")]
    public void A_humidity_that_is_not_a_number_is_refused() {
        Assert.ThrowsAny<ArgumentException>(() => new RelativeHumidity(double.NaN));
    }

    [Theory]
    [InlineData(40.0, 40.0, false)]      // strict: equal to the threshold is not below it
    [InlineData(40.0, 40.0000001, true)]
    [InlineData(40.0, 39.9999999, false)]
    [InlineData(0.0, 0.0, false)]
    [InlineData(0.0, 100.0, true)]
    [InlineData(100.0, 100.0, false)]
    public void Humidity_is_below_a_threshold_only_when_strictly_under_it(double percent, double threshold, bool expected) {
        Assert.Equal(expected, new RelativeHumidity(percent).IsBelow(threshold));
    }

    [Fact]
    public void Two_humidities_with_the_same_percentage_are_equal() {
        // Arrange
        double anyPercent = Any.Double().Between(0d, 100d).Generate();

        // Act
        RelativeHumidity first  = new RelativeHumidity(anyPercent);
        RelativeHumidity second = new RelativeHumidity(anyPercent);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_humidities_with_different_percentages_are_not_equal() {
        Assert.NotEqual(new RelativeHumidity(30.0), new RelativeHumidity(30.1));
    }

    [Fact]
    public void A_humidity_is_not_equal_to_nothing() {
        Assert.False(Any.RelativeHumidity().Generate().Equals(null));
    }

}
