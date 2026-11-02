using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Discovery;

public sealed class HourlyRateTests {

    public static TheoryData<decimal> AcceptedRates => new TheoryData<decimal> { 0.01m, 12.50m, 12.5m, 499.99m, 500m, 500.00m };

    [Theory]
    [MemberData(nameof(AcceptedRates))]
    public void A_rate_within_the_ceiling_and_in_whole_cents_is_kept(decimal value) {
        // Act
        HourlyRate rate = new HourlyRate(value);

        // Assert
        Assert.Equal(value, rate.Value);
    }

    [Fact]
    public void The_ceiling_is_five_hundred_euros() {
        Assert.Equal(500m, HourlyRate.Ceiling);
    }

    public static TheoryData<decimal> NonPositiveRates => new TheoryData<decimal> { 0m, -0.01m, -12.50m };

    [Theory]
    [MemberData(nameof(NonPositiveRates))]
    public void A_rate_that_is_not_positive_is_refused(decimal value) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(value));

        Assert.Equal("value", exception.ParamName);
    }

    public static TheoryData<decimal> RatesAboveTheCeiling => new TheoryData<decimal> { 500.01m, 1000m };

    [Theory]
    [MemberData(nameof(RatesAboveTheCeiling))]
    public void A_rate_above_the_ceiling_is_refused(decimal value) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(value));

        Assert.Equal("value", exception.ParamName);
    }

    public static TheoryData<decimal> RatesBelowTheCent => new TheoryData<decimal> { 12.345m, 0.001m, 499.999m };

    [Theory]
    [MemberData(nameof(RatesBelowTheCent))]
    public void A_rate_finer_than_a_cent_is_refused(decimal value) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new HourlyRate(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_rate_above_the_ceiling_is_reported_as_out_of_range_before_the_cent_is_looked_at() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(500.001m));
    }

}
