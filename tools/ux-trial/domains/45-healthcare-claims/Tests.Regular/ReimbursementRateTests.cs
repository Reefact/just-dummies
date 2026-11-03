using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ReimbursementRateTests {

    public static TheoryData<decimal> AcceptedPercents => new() { 0.1m, 33.3m, 65m, 99.9m, 100m };

    [Theory]
    [MemberData(nameof(AcceptedPercents))]
    public void A_rate_above_zero_and_up_to_one_hundred_with_one_decimal_is_accepted(decimal percent) {
        // Act
        ReimbursementRate rate = new(percent);

        // Assert
        Assert.Equal(percent, rate.Percent);
    }

    public static TheoryData<decimal> OutOfRangePercents => new() { 0m, -0.1m, -50m, 100.1m, 1000m };

    [Theory]
    [MemberData(nameof(OutOfRangePercents))]
    public void A_rate_outside_zero_exclusive_and_one_hundred_inclusive_is_refused(decimal percent) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementRate(percent));

        // Assert
        Assert.Equal("percent", refusal.ParamName);
    }

    public static TheoryData<decimal> TooPrecisePercents => new() { 12.34m, 0.05m, 99.99m, 65.01m };

    [Theory]
    [MemberData(nameof(TooPrecisePercents))]
    public void A_rate_with_more_than_one_decimal_is_refused(decimal percent) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReimbursementRate(percent));

        // Assert
        Assert.Equal("percent", refusal.ParamName);
    }

    [Fact]
    public void A_trailing_zero_beyond_one_decimal_does_not_count_as_a_second_decimal() {
        // Act
        ReimbursementRate rate = new(12.50m);

        // Assert
        Assert.Equal(12.5m, rate.Percent);
    }

    public static TheoryData<decimal, decimal, decimal> RoundedDownReimbursements => new() {
        { 100m, 65m, 65.00m },
        { 10.01m, 33.3m, 3.33m },    // 3.33333 rounds down
        { 0.05m, 50m, 0.02m },       // 2.5 cents rounds down, not to the nearest even cent
        { 0.03m, 50m, 0.01m },       // 1.5 cents rounds down, not up
        { 19.99m, 99.9m, 19.97m },   // 19.97001 rounds down
        { 12.34m, 100m, 12.34m }
    };

    [Theory]
    [MemberData(nameof(RoundedDownReimbursements))]
    public void A_rate_applies_to_an_amount_rounded_down_to_the_cent(decimal amount, decimal percent, decimal expected) {
        // Arrange
        ReimbursementRate rate = new(percent);

        // Act
        decimal reimbursed = rate.Of(amount);

        // Assert
        Assert.Equal(expected, reimbursed);
    }

    [Fact]
    public void A_rate_of_nothing_is_nothing() {
        // Arrange
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();

        // Act
        decimal reimbursed = anyRate.Of(0m);

        // Assert
        Assert.Equal(0m, reimbursed);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyRate.Of(-0.01m));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void Two_rates_with_the_same_percent_are_equal() {
        // Arrange
        ReimbursementRate first  = new(65m);
        ReimbursementRate second = new(65.0m);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_rates_with_different_percents_are_not_equal() {
        // Assert
        Assert.NotEqual(new ReimbursementRate(65m), new ReimbursementRate(65.1m));
    }

}
