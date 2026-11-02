using JustDummies;

using Xunit;

namespace Booking.Domain;

public sealed class CancellationPolicyTests {

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(365, 1.0)]
    [InlineData(7, 0.25)]
    public void A_policy_within_the_bounds_is_accepted(int days, double rate) {
        // Act
        CancellationPolicy policy = new CancellationPolicy(days, (decimal)rate);

        // Assert
        Assert.Equal(days, policy.FreeCancellationDays);
        Assert.Equal((decimal)rate, policy.PenaltyRate);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(366)]
    public void Free_days_outside_zero_to_365_are_refused(int days) {
        // Arrange
        decimal anyRate = Any.Decimal().Between(0m, 1m).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CancellationPolicy(days, anyRate));

        // Assert
        Assert.Equal("freeCancellationDays", error.ParamName);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void A_rate_outside_zero_to_one_is_refused(double rate) {
        // Arrange
        int anyDays = Any.Int32().Between(0, 365).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CancellationPolicy(anyDays, (decimal)rate));

        // Assert
        Assert.Equal("penaltyRate", error.ParamName);
    }

    [Fact]
    public void Cancelling_exactly_the_free_period_before_check_in_costs_nothing() {
        // Arrange
        DateOnly           anyCheckIn = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        string             anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        CancellationPolicy policy     = new CancellationPolicy(7, 0.25m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(200m, anyCurrency), anyCheckIn.AddDays(-7), anyCheckIn);

        // Assert
        Assert.Equal(new Money(0m, anyCurrency), penalty);
    }

    [Fact]
    public void Cancelling_earlier_than_the_free_period_costs_nothing() {
        // Arrange
        DateOnly           anyCheckIn  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        string             anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        CancellationPolicy policy      = new CancellationPolicy(7, 0.25m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(200m, anyCurrency), anyCheckIn.AddDays(-30), anyCheckIn);

        // Assert
        Assert.Equal(new Money(0m, anyCurrency), penalty);
    }

    [Fact]
    public void Cancelling_one_day_inside_the_free_period_keeps_the_penalty_share() {
        // Arrange
        DateOnly           anyCheckIn  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        string             anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        CancellationPolicy policy      = new CancellationPolicy(7, 0.25m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(200m, anyCurrency), anyCheckIn.AddDays(-6), anyCheckIn);

        // Assert
        Assert.Equal(new Money(50m, anyCurrency), penalty);
    }

    [Fact]
    public void Cancelling_on_the_check_in_day_keeps_the_penalty_share() {
        // Arrange
        DateOnly           anyCheckIn  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        string             anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        CancellationPolicy policy      = new CancellationPolicy(7, 0.25m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(200m, anyCurrency), anyCheckIn, anyCheckIn);

        // Assert
        Assert.Equal(new Money(50m, anyCurrency), penalty);
    }

    [Fact]
    public void Cancelling_after_the_check_in_keeps_the_penalty_share() {
        // Arrange
        DateOnly           anyCheckIn  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        string             anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        CancellationPolicy policy      = new CancellationPolicy(7, 0.25m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(200m, anyCurrency), anyCheckIn.AddDays(3), anyCheckIn);

        // Assert
        Assert.Equal(new Money(50m, anyCurrency), penalty);
    }

    [Fact]
    public void With_no_free_period_cancelling_on_the_check_in_day_is_still_free() {
        // Arrange
        DateOnly           anyCheckIn  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        string             anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        CancellationPolicy policy      = new CancellationPolicy(0, 0.5m);

        // Act
        Money onTheDay = policy.PenaltyFor(new Money(200m, anyCurrency), anyCheckIn, anyCheckIn);
        Money afterwards = policy.PenaltyFor(new Money(200m, anyCurrency), anyCheckIn.AddDays(1), anyCheckIn);

        // Assert
        Assert.Equal(new Money(0m, anyCurrency), onTheDay);
        Assert.Equal(new Money(100m, anyCurrency), afterwards);
    }

    [Fact]
    public void A_full_penalty_rate_keeps_the_whole_total() {
        // Arrange
        DateOnly           anyCheckIn  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        string             anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        CancellationPolicy policy      = new CancellationPolicy(7, 1m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(199.99m, anyCurrency), anyCheckIn.AddDays(-1), anyCheckIn);

        // Assert
        Assert.Equal(new Money(199.99m, anyCurrency), penalty);
    }

    [Fact]
    public void The_penalty_is_rounded_to_two_decimals() {
        // Arrange
        DateOnly           anyCheckIn  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        string             anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        CancellationPolicy policy      = new CancellationPolicy(7, 0.333m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(100m, anyCurrency), anyCheckIn.AddDays(-1), anyCheckIn);

        // Assert
        Assert.Equal(new Money(33.30m, anyCurrency), penalty);
    }

}
