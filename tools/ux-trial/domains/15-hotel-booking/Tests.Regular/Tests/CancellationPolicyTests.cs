using JustDummies;

using Xunit;

namespace Booking.Domain.Tests.Regular;

public sealed class CancellationPolicyTests {

    private static AnyDateOnly AnyDate() => Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2040, 12, 31));

    [Fact]
    public void A_policy_keeps_its_free_days_and_its_rate() {
        // Act
        CancellationPolicy policy = new CancellationPolicy(7, 0.4m);

        // Assert
        Assert.Equal(7, policy.FreeCancellationDays);
        Assert.Equal(0.4m, policy.PenaltyRate);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(365, "1")]
    [InlineData(1, "0.5")]
    public void The_bounds_of_the_days_and_of_the_rate_are_accepted(int days, string rate) {
        // Act & Assert
        Assert.NotNull(new CancellationPolicy(days, decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Free_days_outside_zero_to_365_are_refused(int days) {
        // Arrange
        decimal anyRate = Any.Decimal().Between(0m, 1m).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new CancellationPolicy(days, anyRate));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.01")]
    [InlineData("-1")]
    [InlineData("100")]
    public void A_rate_outside_zero_to_one_is_refused(string rate) {
        // Arrange
        int anyDays = Any.Int32().Between(0, 365).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new CancellationPolicy(anyDays, decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)));

        // Assert
        Assert.Equal("penaltyRate", exception.ParamName);
    }

    [Fact]
    public void Cancelling_exactly_on_the_last_free_day_costs_nothing() {
        // Arrange
        DateOnly           anyCheckIn = AnyDate().Generate();
        CancellationPolicy policy     = new CancellationPolicy(7, 0.5m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(200m, "EUR"), anyCheckIn.AddDays(-7), anyCheckIn);

        // Assert
        Assert.Equal(new Money(0m, "EUR"), penalty);
    }

    [Fact]
    public void Cancelling_a_day_too_late_costs_the_penalty_share() {
        // Arrange
        DateOnly           anyCheckIn = AnyDate().Generate();
        CancellationPolicy policy     = new CancellationPolicy(7, 0.5m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(200m, "EUR"), anyCheckIn.AddDays(-6), anyCheckIn);

        // Assert
        Assert.Equal(new Money(100m, "EUR"), penalty);
    }

    [Fact]
    public void Cancelling_well_before_the_free_limit_costs_nothing() {
        // Arrange
        DateOnly           anyCheckIn = AnyDate().Generate();
        int                anyAdvance = Any.Int32().Between(8, 300).Generate();
        CancellationPolicy policy     = new CancellationPolicy(7, 1m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(200m, "EUR"), anyCheckIn.AddDays(-anyAdvance), anyCheckIn);

        // Assert
        Assert.Equal(0m, penalty.Amount);
    }

    [Fact]
    public void Cancelling_on_the_check_in_day_costs_the_penalty_when_there_is_a_free_period() {
        // Arrange
        DateOnly           anyCheckIn = AnyDate().Generate();
        CancellationPolicy policy     = new CancellationPolicy(1, 0.3m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(100m, "EUR"), anyCheckIn, anyCheckIn);

        // Assert
        Assert.Equal(new Money(30m, "EUR"), penalty);
    }

    [Fact]
    public void Cancelling_after_the_check_in_costs_the_penalty() {
        // Arrange
        DateOnly           anyCheckIn = AnyDate().Generate();
        int                anyDelay   = Any.Int32().Between(1, 30).Generate();
        CancellationPolicy policy     = new CancellationPolicy(0, 0.3m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(100m, "EUR"), anyCheckIn.AddDays(anyDelay), anyCheckIn);

        // Assert
        Assert.Equal(new Money(30m, "EUR"), penalty);
    }

    [Fact]
    public void Without_a_free_period_cancelling_on_the_check_in_day_is_still_free() {
        // Arrange: daysBefore (0) >= FreeCancellationDays (0)
        DateOnly           anyCheckIn = AnyDate().Generate();
        CancellationPolicy policy     = new CancellationPolicy(0, 0.3m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(100m, "EUR"), anyCheckIn, anyCheckIn);

        // Assert
        Assert.Equal(0m, penalty.Amount);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("JPY")]
    public void A_free_cancellation_is_zero_in_the_currency_of_the_total(string currency) {
        // Arrange
        DateOnly           anyCheckIn = AnyDate().Generate();
        CancellationPolicy policy     = new CancellationPolicy(3, 0.5m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(80m, currency), anyCheckIn.AddDays(-10), anyCheckIn);

        // Assert
        Assert.Equal(new Money(0m, currency), penalty);
    }

    [Fact]
    public void A_penalty_is_rounded_to_cents() {
        // Arrange
        DateOnly           anyCheckIn = AnyDate().Generate();
        CancellationPolicy policy     = new CancellationPolicy(5, 0.333m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(10m, "EUR"), anyCheckIn, anyCheckIn);

        // Assert: 3.33
        Assert.Equal(new Money(3.33m, "EUR"), penalty);
    }

    [Fact]
    public void A_full_rate_keeps_the_whole_total() {
        // Arrange
        DateOnly           anyCheckIn = AnyDate().Generate();
        CancellationPolicy policy     = new CancellationPolicy(5, 1m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(123.45m, "EUR"), anyCheckIn, anyCheckIn);

        // Assert
        Assert.Equal(new Money(123.45m, "EUR"), penalty);
    }

    [Fact]
    public void A_zero_rate_never_costs_anything() {
        // Arrange
        DateOnly           anyCheckIn = AnyDate().Generate();
        CancellationPolicy policy     = new CancellationPolicy(5, 0m);

        // Act
        Money penalty = policy.PenaltyFor(new Money(123.45m, "EUR"), anyCheckIn.AddDays(1), anyCheckIn);

        // Assert
        Assert.Equal(0m, penalty.Amount);
    }

    [Fact]
    public void The_generator_only_draws_valid_policies() {
        // Act
        for (int i = 0; i < 200; i++) {
            CancellationPolicy anyPolicy = Arbitrary.CancellationPolicy().Generate();

            Assert.InRange(anyPolicy.FreeCancellationDays, 0, 365);
            Assert.InRange(anyPolicy.PenaltyRate, 0m, 1m);
        }
    }

}
