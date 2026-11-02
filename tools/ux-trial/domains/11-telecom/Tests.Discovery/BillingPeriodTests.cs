using JustDummies;

using Xunit;

namespace Telecom.Domain.Tests;

public sealed class BillingPeriodTests {

    [Fact]
    public void Keeps_its_start_and_its_end() {
        // Act
        BillingPeriod period = new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 31));

        // Assert
        Assert.Equal(new DateOnly(2025, 3, 1), period.Start);
        Assert.Equal(new DateOnly(2025, 3, 31), period.End);
    }

    [Fact]
    public void Lasts_at_most_31_days() {
        // Arrange
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act
        BillingPeriod period = new BillingPeriod(anyStart, anyStart.AddDays(31));

        // Assert
        Assert.Equal(anyStart.AddDays(31), period.End);
    }

    [Fact]
    public void Refuses_to_last_32_days() {
        // Arrange
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new BillingPeriod(anyStart, anyStart.AddDays(32)));

        // Assert
        Assert.Equal("end", exception.ParamName);
    }

    [Fact]
    public void Refuses_to_last_longer_than_a_month_by_far() {
        // Arrange
        DateOnly anyStart  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();
        int      anyLength = Any.Int32().Between(33, 5_000).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new BillingPeriod(anyStart, anyStart.AddDays(anyLength)));
    }

    [Fact]
    public void Refuses_an_end_equal_to_its_start() {
        // Arrange
        DateOnly anyDay = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new BillingPeriod(anyDay, anyDay));
    }

    [Fact]
    public void Refuses_an_end_before_its_start() {
        // Arrange
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();
        int      anyGap   = Any.Int32().Between(1, 400).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new BillingPeriod(anyStart, anyStart.AddDays(-anyGap)));
    }

    [Fact]
    public void Accepts_a_one_day_period() {
        // Arrange
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act
        BillingPeriod period = new BillingPeriod(anyStart, anyStart.AddDays(1));

        // Assert
        Assert.Equal(anyStart.AddDays(1), period.End);
    }

    [Fact]
    public void Contains_the_first_instant_of_its_start_day() {
        // Arrange
        BillingPeriod period = new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 4, 1));

        // Act
        bool contains = period.Contains(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void Contains_the_last_instant_before_its_end_day() {
        // Arrange
        BillingPeriod period = new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 4, 1));

        // Act
        bool contains = period.Contains(new DateTime(2025, 3, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999));

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void Does_not_contain_the_first_instant_of_its_end_day() {
        // Arrange
        BillingPeriod period = new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 4, 1));

        // Act
        bool contains = period.Contains(new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        Assert.False(contains);
    }

    [Fact]
    public void Does_not_contain_the_last_instant_before_its_start_day() {
        // Arrange
        BillingPeriod period = new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 4, 1));

        // Act
        bool contains = period.Contains(new DateTime(2025, 2, 28, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999));

        // Assert
        Assert.False(contains);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Reads_the_date_part_of_an_instant_whatever_its_kind(DateTimeKind kind) {
        // Arrange
        BillingPeriod period = new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 15));

        // Act
        bool containsInside  = period.Contains(new DateTime(2025, 3, 14, 23, 30, 0, kind));
        bool containsOutside = period.Contains(new DateTime(2025, 3, 15, 0, 30, 0, kind));

        // Assert
        Assert.True(containsInside);
        Assert.False(containsOutside);
    }

    [Fact]
    public void Contains_an_instant_on_any_day_of_a_period_drawn_whole() {
        // Arrange: an arbitrary period, and an instant 3 days after its start (every period lasts 4 days at least)
        BillingPeriod anyPeriod = Any.BillingPeriod().WithLengthDays(l => l.GreaterThanOrEqualTo(4)).Generate();
        DateTime      instant   = anyPeriod.Start.AddDays(3).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);

        // Act
        bool contains = anyPeriod.Contains(instant);

        // Assert
        Assert.True(contains);
    }

}
