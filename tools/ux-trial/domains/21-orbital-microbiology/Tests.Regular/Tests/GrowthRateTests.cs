using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests;

public sealed class GrowthRateTests {

    [Theory]
    [InlineData(0)]
    [InlineData(0.001)]
    [InlineData(5)]
    public void A_rate_inside_the_range_is_kept(double perHour) {
        // Act
        GrowthRate rate = new GrowthRate(perHour);

        // Assert
        Assert.Equal(perHour, rate.PerHour);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(5.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_rate_outside_the_range_is_refused(double perHour) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new GrowthRate(perHour));

        // Assert
        Assert.Equal("perHour", refusal.ParamName);
    }

    [Fact]
    public void The_zero_rate_is_a_rate_of_zero() {
        Assert.Equal(0, GrowthRate.Zero.PerHour);
    }

    [Fact]
    public void A_rate_of_ln2_doubles_the_population_every_hour() {
        // Arrange
        GrowthRate rate = new GrowthRate(Math.Log(2));

        // Act / Assert
        Assert.Equal(1, rate.GenerationTimeHours, precision: 12);
        Assert.Equal(3, rate.GenerationsIn(3), precision: 12);
    }

    [Fact]
    public void A_slower_rate_takes_proportionally_longer_to_double() {
        // Arrange
        GrowthRate rate = new GrowthRate(Math.Log(2) / 4);

        // Assert
        Assert.Equal(4, rate.GenerationTimeHours, precision: 12);
    }

    [Fact]
    public void A_zero_rate_has_no_generation_time() {
        Assert.Throws<InvalidOperationException>(() => GrowthRate.Zero.GenerationTimeHours);
    }

    [Fact]
    public void No_time_elapsed_means_no_generation() {
        // Arrange
        GrowthRate anyRate = Any.GrowthRate().Generate();

        // Act
        double generations = anyRate.GenerationsIn(0);

        // Assert
        Assert.Equal(0, generations);
    }

    [Fact]
    public void A_zero_rate_never_completes_a_generation() {
        // Arrange
        double anyHours = Any.Double().Between(0, 10_000).Generate();

        // Act
        double generations = GrowthRate.Zero.GenerationsIn(anyHours);

        // Assert
        Assert.Equal(0, generations);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_duration_must_be_finite_and_non_negative(double hours) {
        // Arrange
        GrowthRate anyRate = Any.GrowthRate().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyRate.GenerationsIn(hours));
    }

    [Fact]
    public void Two_rates_with_the_same_value_are_equal() {
        // Act
        GrowthRate first  = new GrowthRate(0.75);
        GrowthRate second = new GrowthRate(0.75);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_rates_with_different_values_are_not_equal() {
        // Arrange
        GrowthRate rate = new GrowthRate(0.75);

        // Assert
        Assert.NotEqual(rate, new GrowthRate(0.7500001));
        Assert.False(rate.Equals(null));
    }

}
