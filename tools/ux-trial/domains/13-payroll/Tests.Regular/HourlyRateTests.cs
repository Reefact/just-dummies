using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Regular;

public sealed class HourlyRateTests {

    public static TheoryData<decimal> AcceptedRates => new TheoryData<decimal> { 0.01m, 12.5m, 499.99m, 500m, 500.00m };

    public static TheoryData<decimal> RatesNotInWholeCents => new TheoryData<decimal> { 12.345m, 0.001m, 499.999m, 20.0000001m };

    [Theory]
    [MemberData(nameof(AcceptedRates))]
    public void A_rate_inside_the_range_and_in_whole_cents_is_kept(decimal value) {
        // Act
        HourlyRate rate = new HourlyRate(value);

        // Assert
        Assert.Equal(value, rate.Value);
    }

    [Fact]
    public void The_ceiling_is_five_hundred_euros() {
        Assert.Equal(500m, HourlyRate.Ceiling);
    }

    [Fact]
    public void A_zero_rate_is_refused() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(0m));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_negative_rate_is_refused() {
        // Arrange
        decimal anyNegativeRate = Any.Decimal().Negative().WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(anyNegativeRate));
    }

    [Fact]
    public void A_rate_just_above_the_ceiling_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(500.01m));
    }

    [Fact]
    public void A_rate_above_the_ceiling_is_refused() {
        // Arrange
        decimal anyExcessiveRate = Any.Decimal().GreaterThan(HourlyRate.Ceiling).WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(anyExcessiveRate));
    }

    [Theory]
    [MemberData(nameof(RatesNotInWholeCents))]
    public void A_rate_that_is_not_in_whole_cents_is_refused(decimal value) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new HourlyRate(value));

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("value", exception.ParamName);
    }

}
