using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.ValueObjects;

public sealed class GrowthRateTests {

    [Theory]
    [InlineData(0d)]
    [InlineData(5d)]
    [InlineData(0.693)]
    public void A_rate_within_zero_and_five_per_hour_is_kept(double perHour) {
        Assert.Equal(perHour, new GrowthRate(perHour).PerHour);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(5.001)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void A_rate_outside_zero_and_five_per_hour_or_not_finite_is_refused(double perHour) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GrowthRate(perHour));
    }

    [Fact]
    public void The_zero_rate_is_a_rate_of_zero() {
        Assert.Equal(0d, GrowthRate.Zero.PerHour);
        Assert.Equal(new GrowthRate(0d), GrowthRate.Zero);
    }

    [Theory]
    [InlineData(0.6931471805599453, 1d)]
    [InlineData(0.34657359027997264, 2d)]
    [InlineData(1.3862943611198906, 0.5)]
    public void The_generation_time_is_ln_2_over_the_rate(double perHour, double expectedHours) {
        // Act
        double hours = new GrowthRate(perHour).GenerationTimeHours;

        // Assert
        Assert.Equal(expectedHours, hours, precision: 10);
    }

    [Fact]
    public void A_population_that_does_not_grow_has_no_generation_time() {
        Assert.Throws<InvalidOperationException>(() => GrowthRate.Zero.GenerationTimeHours);
    }

    [Theory]
    [InlineData(0.6931471805599453, 3d, 3d)]
    [InlineData(0.6931471805599453, 0d, 0d)]
    [InlineData(0.34657359027997264, 10d, 5d)]
    public void The_generations_elapsed_follow_the_rate_and_the_duration(double perHour, double hours, double expectedGenerations) {
        // Act
        double generations = new GrowthRate(perHour).GenerationsIn(hours);

        // Assert
        Assert.Equal(expectedGenerations, generations, precision: 10);
    }

    [Fact]
    public void A_zero_rate_goes_through_no_generation_whatever_the_duration() {
        // Arrange
        double anyHours = Any.Double().Between(0d, 1_000_000d).Generate();

        // Assert
        Assert.Equal(0d, GrowthRate.Zero.GenerationsIn(anyHours));
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void A_duration_that_is_negative_or_not_finite_is_refused(double hours) {
        // Arrange
        GrowthRate anyRate = Any.GrowthRate().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyRate.GenerationsIn(hours));
    }

    [Fact]
    public void Two_rates_of_the_same_value_are_equal() {
        // Arrange
        double anyPerHour = Any.GrowthRate().Generate().PerHour;

        // Act
        GrowthRate first  = new GrowthRate(anyPerHour);
        GrowthRate second = new GrowthRate(anyPerHour);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_rates_of_different_values_are_not_equal() {
        // Arrange
        GrowthRate anyRate      = Any.GrowthRate().Generate();
        GrowthRate anyOtherRate = Any.GrowthRate().WithPerHour(perHour => perHour.DifferentFrom(anyRate.PerHour)).Generate();

        // Assert
        Assert.NotEqual(anyRate, anyOtherRate);
    }

}
