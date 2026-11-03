using HealthcareClaims.Domain;
using JustDummies;
using Xunit;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class ReimbursementRateTests {

    public static TheoryData<decimal> AcceptedPercents => new() { 0.1m, 33.3m, 50m, 99.9m, 100m };

    public static TheoryData<decimal> PercentsOutsideTheRange => new() { 0m, -0.1m, -50m, 100.1m, 1_000m };

    public static TheoryData<decimal> PercentsBeyondOneDecimal => new() { 12.34m, 0.05m, 99.99m, 50.01m };

    [Theory]
    [MemberData(nameof(AcceptedPercents))]
    public void A_rate_within_zero_exclusive_and_one_hundred_with_one_decimal_is_kept(decimal percent) {
        ReimbursementRate rate = new(percent);

        Assert.Equal(percent, rate.Percent);
    }

    [Theory]
    [MemberData(nameof(PercentsOutsideTheRange))]
    public void A_rate_outside_zero_exclusive_and_one_hundred_is_refused(decimal percent) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementRate(percent));
    }

    [Theory]
    [MemberData(nameof(PercentsBeyondOneDecimal))]
    public void A_rate_with_more_than_one_decimal_is_refused(decimal percent) {
        Assert.Throws<ArgumentException>(() => new ReimbursementRate(percent));
    }

    [Fact]
    public void A_rate_written_with_a_trailing_zero_is_still_one_decimal() {
        ReimbursementRate rate = new(33.30m);

        Assert.Equal(33.3m, rate.Percent);
    }

    public static TheoryData<decimal, decimal, decimal> RoundedDownToTheCent => new() {
        { 33.3m, 10.00m, 3.33m },     // 3.330, exact
        { 33.3m, 10.01m, 3.33m },     // 3.33333, the rest dropped
        { 12.5m, 0.07m, 0.00m },      // 0.00875 never rounds up to a cent
        { 12.5m, 0.08m, 0.01m },      // 0.0100, exact
        { 50m, 10.01m, 5.00m },       // 5.005 rounds down: not to even, not up
        { 50m, 10.03m, 5.01m },       // 5.015 rounds down to the odd cent
        { 100m, 42.42m, 42.42m },
        { 0.1m, 999.99m, 0.99m }      // 0.99999
    };

    [Theory]
    [MemberData(nameof(RoundedDownToTheCent))]
    public void A_rate_applied_to_an_amount_is_rounded_down_to_the_cent(decimal percent, decimal amount, decimal expected) {
        ReimbursementRate rate = new(percent);

        decimal reimbursed = rate.Of(amount);

        Assert.Equal(expected, reimbursed);
    }

    [Fact]
    public void A_rate_applied_to_nothing_reimburses_nothing() {
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();

        Assert.Equal(0m, anyRate.Of(0m));
    }

    [Fact]
    public void A_rate_cannot_be_applied_to_a_negative_amount() {
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyRate.Of(-0.01m));
    }

    [Fact]
    public void Two_rates_with_the_same_percent_are_equal() {
        ReimbursementRate anyRate     = Any.ReimbursementRate().Generate();
        ReimbursementRate sameRate    = new(anyRate.Percent);

        Assert.Equal(anyRate, sameRate);
        Assert.Equal(anyRate.GetHashCode(), sameRate.GetHashCode());
    }

    [Fact]
    public void Two_rates_with_different_percents_are_not_equal() {
        ReimbursementRate anyRate       = Any.ReimbursementRate().Generate();
        ReimbursementRate anyOtherRate  = Any.ReimbursementRate().WithPercent(percent => percent.DifferentFrom(anyRate.Percent)).Generate();

        Assert.NotEqual(anyRate, anyOtherRate);
    }

    [Fact]
    public void A_rate_is_not_equal_to_nothing() {
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();

        Assert.False(anyRate.Equals(null));
    }

}
