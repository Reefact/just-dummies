using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class GrowthRateTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.693)]
    [InlineData(5.0)]
    public void A_rate_between_zero_and_five_per_hour_is_accepted(double perHour) {
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
    public void A_rate_outside_zero_to_five_or_not_finite_is_refused(double perHour) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GrowthRate(perHour));
    }

    [Fact]
    public void The_zero_rate_is_zero() {
        // Assert
        Assert.Equal(0, GrowthRate.Zero.PerHour);
    }

    [Fact]
    public void A_rate_of_ln2_per_hour_doubles_every_hour() {
        // Arrange
        GrowthRate rate = new GrowthRate(Math.Log(2));

        // Act
        double generationTime = rate.GenerationTimeHours;

        // Assert
        Assert.Equal(1.0, generationTime, precision: 12);
    }

    [Fact]
    public void A_rate_of_one_per_hour_has_a_generation_time_of_ln2_hours() {
        // Arrange
        GrowthRate rate = new GrowthRate(1.0);

        // Act / Assert
        Assert.Equal(Math.Log(2), rate.GenerationTimeHours, precision: 12);
    }

    [Fact]
    public void A_zero_rate_has_no_generation_time() {
        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => GrowthRate.Zero.GenerationTimeHours);
    }

    [Fact]
    public void Ten_hours_at_ln2_per_hour_is_ten_generations() {
        // Arrange
        GrowthRate rate = new GrowthRate(Math.Log(2));

        // Act
        double generations = rate.GenerationsIn(10);

        // Assert
        Assert.Equal(10.0, generations, precision: 12);
    }

    [Fact]
    public void No_time_elapsed_means_no_generation_whatever_the_rate() {
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
        double anyDuration = Any.Double().Between(0d, 720d).Generate();

        // Act
        double generations = GrowthRate.Zero.GenerationsIn(anyDuration);

        // Assert
        Assert.Equal(0, generations);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_duration_that_is_negative_or_not_finite_is_refused(double hours) {
        // Arrange
        GrowthRate anyRate = Any.GrowthRate().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyRate.GenerationsIn(hours));
    }

    [Fact]
    public void Two_rates_with_the_same_value_are_equal() {
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
    public void Two_rates_with_different_values_are_not_equal() {
        // Arrange
        GrowthRate anyRate      = Any.GrowthRate().Generate();
        GrowthRate anyOtherRate = Any.GrowthRate().WithPerHour(r => r.DifferentFrom(anyRate.PerHour)).Generate();

        // Act / Assert
        Assert.NotEqual(anyRate, anyOtherRate);
    }

    [Fact]
    public void A_rate_prints_with_its_unit() {
        // Arrange
        GrowthRate rate = new GrowthRate(0.5);

        // Act / Assert
        Assert.Equal("0.5 h⁻¹", rate.ToString());
    }

}
