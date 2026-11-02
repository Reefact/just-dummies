using JustDummies;

using Xunit;

namespace Booking.Domain.Tests;

public sealed class CancellationPolicyTests {

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(365)]
    public void A_free_cancellation_window_up_to_a_year_is_accepted(int days) {
        // Arrange
        decimal anyRate = Any.Decimal().Between(0m, 1m).Generate();

        // Act
        CancellationPolicy policy = new CancellationPolicy(days, anyRate);

        // Assert
        Assert.Equal(days, policy.FreeCancellationDays);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_free_cancellation_window_outside_zero_to_a_year_is_refused(int days) {
        // Arrange
        decimal anyRate = Any.Decimal().Between(0m, 1m).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CancellationPolicy(days, anyRate));

        // Assert
        Assert.Equal("freeCancellationDays", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.25")]
    [InlineData("1")]
    public void A_penalty_rate_from_zero_to_one_is_accepted(string rate) {
        // Arrange
        int anyDays = Any.Int32().Between(0, 365).Generate();
        decimal expected = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        CancellationPolicy policy = new CancellationPolicy(anyDays, expected);

        // Assert
        Assert.Equal(expected, policy.PenaltyRate);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.01")]
    [InlineData("-1")]
    [InlineData("100")]
    public void A_penalty_rate_outside_zero_to_one_is_refused(string rate) {
        // Arrange
        int anyDays = Any.Int32().Between(0, 365).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CancellationPolicy(anyDays, decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)));

        // Assert
        Assert.Equal("penaltyRate", refusal.ParamName);
    }

    // Every test below is about where the free window ends, so the window, the rate and the total are literals;
    // the check-in date and the currency are arbitrary.

    [Theory]
    [InlineData(30)]   // well before
    [InlineData(8)]    // one day more than the window
    [InlineData(7)]    // exactly at the edge of the window
    public void Cancelling_at_least_the_free_window_before_check_in_costs_nothing(int daysBefore) {
        // Arrange
        CancellationPolicy policy = new CancellationPolicy(7, 0.5m);
        DateOnly anyCheckIn       = AnyStayPeriod.ValidCheckIn().Generate();
        Money total               = Any.Money().WithAmount(200m).Generate();

        // Act
        Money penalty = policy.PenaltyFor(total, anyCheckIn.AddDays(-daysBefore), anyCheckIn);

        // Assert
        Assert.Equal(0m, penalty.Amount);
    }

    [Theory]
    [InlineData(6)]    // one day inside the window
    [InlineData(1)]
    [InlineData(0)]    // on the day of check-in
    [InlineData(-3)]   // after check-in
    public void Cancelling_inside_the_free_window_keeps_the_penalty_rate_of_the_total(int daysBefore) {
        // Arrange
        CancellationPolicy policy = new CancellationPolicy(7, 0.5m);
        DateOnly anyCheckIn       = AnyStayPeriod.ValidCheckIn().Generate();
        Money total               = Any.Money().WithAmount(200m).Generate();

        // Act
        Money penalty = policy.PenaltyFor(total, anyCheckIn.AddDays(-daysBefore), anyCheckIn);

        // Assert
        Assert.Equal(100m, penalty.Amount);
    }

    [Fact]
    public void A_policy_without_free_window_is_free_on_the_day_of_check_in_and_not_after() {
        // Arrange
        CancellationPolicy policy = new CancellationPolicy(0, 0.5m);
        DateOnly anyCheckIn       = AnyStayPeriod.ValidCheckIn().Generate();
        Money total               = Any.Money().WithAmount(200m).Generate();

        // Act
        Money onTheDay   = policy.PenaltyFor(total, anyCheckIn, anyCheckIn);
        Money theDayAfter = policy.PenaltyFor(total, anyCheckIn.AddDays(1), anyCheckIn);

        // Assert
        Assert.Equal(0m, onTheDay.Amount);
        Assert.Equal(100m, theDayAfter.Amount);
    }

    [Fact]
    public void A_penalty_rate_of_zero_never_costs_anything() {
        // Arrange
        CancellationPolicy policy = new CancellationPolicy(7, 0m);
        DateOnly anyCheckIn       = AnyStayPeriod.ValidCheckIn().Generate();
        Money total               = Any.Money().WithAmount(200m).Generate();

        // Act
        Money penalty = policy.PenaltyFor(total, anyCheckIn, anyCheckIn);

        // Assert
        Assert.Equal(0m, penalty.Amount);
    }

    [Fact]
    public void A_penalty_rate_of_one_keeps_the_whole_total() {
        // Arrange
        CancellationPolicy policy = new CancellationPolicy(7, 1m);
        DateOnly anyCheckIn       = AnyStayPeriod.ValidCheckIn().Generate();
        Money total               = Any.Money().WithAmount(200.40m).Generate();

        // Act
        Money penalty = policy.PenaltyFor(total, anyCheckIn, anyCheckIn);

        // Assert
        Assert.Equal(200.40m, penalty.Amount);
    }

    [Fact]
    public void The_penalty_is_rounded_to_cents_half_away_from_zero() {
        // Arrange
        CancellationPolicy policy = new CancellationPolicy(7, 0.5m);
        DateOnly anyCheckIn       = AnyStayPeriod.ValidCheckIn().Generate();
        Money total               = Any.Money().WithAmount(33.33m).Generate();

        // Act
        Money penalty = policy.PenaltyFor(total, anyCheckIn, anyCheckIn);

        // Assert
        Assert.Equal(16.67m, penalty.Amount);
    }

    [Fact]
    public void A_free_cancellation_is_free_in_the_currency_of_the_total() {
        // Arrange
        CancellationPolicy policy = new CancellationPolicy(7, 0.5m);
        DateOnly anyCheckIn       = AnyStayPeriod.ValidCheckIn().Generate();
        Money total               = new Money(200m, "CHF");

        // Act
        Money penalty = policy.PenaltyFor(total, anyCheckIn.AddDays(-30), anyCheckIn);

        // Assert
        Assert.Equal(new Money(0m, "CHF"), penalty);
    }

    [Fact]
    public void A_penalty_is_charged_in_the_currency_of_the_total() {
        // Arrange
        CancellationPolicy policy = new CancellationPolicy(7, 0.5m);
        DateOnly anyCheckIn       = AnyStayPeriod.ValidCheckIn().Generate();
        Money total               = new Money(200m, "CHF");

        // Act
        Money penalty = policy.PenaltyFor(total, anyCheckIn, anyCheckIn);

        // Assert
        Assert.Equal(new Money(100m, "CHF"), penalty);
    }

}
