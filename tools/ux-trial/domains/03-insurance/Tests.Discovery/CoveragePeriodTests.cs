using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests.Discovery;

public sealed class CoveragePeriodTests {

    [Fact]
    public void A_period_keeps_its_two_days() {
        // Act
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 1), period.Start);
        Assert.Equal(new DateOnly(2026, 3, 31), period.End);
    }

    [Fact]
    public void A_period_cannot_end_on_the_day_it_starts() {
        Assert.Throws<ArgumentException>("end", () => new CoveragePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void A_period_cannot_end_before_it_starts() {
        Assert.Throws<ArgumentException>("end", () => new CoveragePeriod(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void Two_consecutive_days_make_the_shortest_period() {
        // Act
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 2));

        // Assert
        Assert.Equal(2, period.Days);
    }

    [Theory]
    [InlineData(2026, 1, 1, 2026, 12, 31, 365)]
    [InlineData(2026, 1, 1, 2027, 1, 1, 366)]       // exactly one calendar year is accepted
    [InlineData(2028, 1, 1, 2029, 1, 1, 367)]       // across a leap day
    [InlineData(2028, 2, 29, 2029, 2, 28, 366)]     // 29 February + one year = 28 February
    public void A_period_of_up_to_one_calendar_year_is_accepted(int sy, int sm, int sd, int ey, int em, int ed, int expectedDays) {
        // Act
        CoveragePeriod period = new CoveragePeriod(new DateOnly(sy, sm, sd), new DateOnly(ey, em, ed));

        // Assert
        Assert.Equal(expectedDays, period.Days);
    }

    [Theory]
    [InlineData(2026, 1, 1, 2027, 1, 2)]
    [InlineData(2028, 2, 29, 2029, 3, 1)]
    [InlineData(2026, 1, 1, 2030, 1, 1)]
    public void A_period_longer_than_one_calendar_year_is_refused(int sy, int sm, int sd, int ey, int em, int ed) {
        Assert.Throws<ArgumentException>("end", () => new CoveragePeriod(new DateOnly(sy, sm, sd), new DateOnly(ey, em, ed)));
    }

    [Fact]
    public void Both_boundary_days_belong_to_the_period() {
        // Arrange
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        // Assert
        Assert.True(period.Contains(new DateOnly(2026, 3, 1)));
        Assert.True(period.Contains(new DateOnly(2026, 3, 31)));
        Assert.True(period.Contains(new DateOnly(2026, 3, 15)));
    }

    [Fact]
    public void The_days_around_the_period_do_not_belong_to_it() {
        // Arrange
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        // Assert
        Assert.False(period.Contains(new DateOnly(2026, 2, 28)));
        Assert.False(period.Contains(new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public void The_next_period_starts_the_day_after_and_lasts_as_many_days() {
        // Arrange
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));

        // Act
        CoveragePeriod next = period.Next();

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 11), next.Start);
        Assert.Equal(new DateOnly(2026, 1, 20), next.End);
        Assert.Equal(10, next.Days);
    }

    [Fact]
    public void The_next_period_of_a_calendar_year_is_the_following_year() {
        // Arrange
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        // Act
        CoveragePeriod next = period.Next();

        // Assert
        Assert.Equal(new DateOnly(2027, 1, 1), next.Start);
        Assert.Equal(new DateOnly(2027, 12, 31), next.End);
    }

    [Fact]
    public void The_next_period_of_a_leap_year_runs_a_day_short_of_the_year_after() {
        // Arrange
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2028, 1, 1), new DateOnly(2028, 12, 31));

        // Act
        CoveragePeriod next = period.Next();

        // Assert
        Assert.Equal(new DateOnly(2029, 1, 1), next.Start);
        Assert.Equal(new DateOnly(2030, 1, 1), next.End);   // 366 days: one calendar year to the day
    }

    [Fact]
    public void The_next_period_is_refused_when_it_would_pass_one_year_from_its_new_start() {
        // Arrange: 367 days, accepted; its successor would end past 2030-01-02
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2028, 1, 1), new DateOnly(2029, 1, 1));

        // Assert
        Assert.Throws<ArgumentException>("end", () => period.Next());
    }

}
