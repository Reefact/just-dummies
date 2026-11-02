using JustDummies;

using Telecom.Domain;
using Telecom.Tests.Expert.Generators;

using Xunit;

namespace Telecom.Tests.Expert;

public sealed class BillingPeriodTests {

    private static readonly AnyDateOnly AnyDay = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1));

    [Fact]
    public void A_period_keeps_its_start_and_its_end() {
        // Act
        BillingPeriod period = new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 4, 1));

        // Assert
        Assert.Equal(new DateOnly(2025, 3, 1), period.Start);
        Assert.Equal(new DateOnly(2025, 4, 1), period.End);
    }

    [Theory]
    [InlineData(1)]     // the shortest
    [InlineData(28)]
    [InlineData(31)]    // the longest
    public void A_period_of_1_to_31_days_is_accepted(int days) {
        // Arrange
        DateOnly anyStart = AnyDay.Generate();

        // Act
        BillingPeriod period = new BillingPeriod(anyStart, anyStart.AddDays(days));

        // Assert
        Assert.Equal(days, period.End.DayNumber - period.Start.DayNumber);
    }

    [Fact]
    public void A_period_of_32_days_is_refused() {
        // Arrange
        DateOnly anyStart = AnyDay.Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BillingPeriod(anyStart, anyStart.AddDays(32)));

        // Assert
        Assert.Equal("end", refusal.ParamName);
    }

    [Fact]
    public void A_period_that_ends_where_it_starts_is_refused() {
        // Arrange
        DateOnly anyDay = AnyDay.Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BillingPeriod(anyDay, anyDay));
    }

    [Fact]
    public void A_period_that_ends_before_it_starts_is_refused() {
        // Arrange
        DateOnly anyStart = AnyDay.Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BillingPeriod(anyStart, anyStart.AddDays(-1)));
    }

    [Fact]
    public void The_first_day_is_in_the_period() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();

        // Act
        bool contained = anyPeriod.Contains(anyPeriod.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        // Assert
        Assert.True(contained);
    }

    [Fact]
    public void The_last_instant_of_the_last_day_is_in_the_period() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();

        // Act
        bool contained = anyPeriod.Contains(anyPeriod.End.AddDays(-1).ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        // Assert
        Assert.True(contained);
    }

    [Fact]
    public void The_end_day_is_not_in_the_period() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();

        // Act
        bool contained = anyPeriod.Contains(anyPeriod.End.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        // Assert
        Assert.False(contained);
    }

    [Fact]
    public void The_last_instant_before_the_start_is_not_in_the_period() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();

        // Act
        bool contained = anyPeriod.Contains(anyPeriod.Start.AddDays(-1).ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        // Assert
        Assert.False(contained);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void The_kind_of_the_instant_does_not_change_the_day_it_falls_on(DateTimeKind kind) {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();
        DateTime      noonOnTheFirstDay = anyPeriod.Start.ToDateTime(new TimeOnly(12, 0), kind);
        DateTime      noonOnTheEndDay   = anyPeriod.End.ToDateTime(new TimeOnly(12, 0), kind);

        // Act & Assert
        Assert.True(anyPeriod.Contains(noonOnTheFirstDay));
        Assert.False(anyPeriod.Contains(noonOnTheEndDay));
    }

}
