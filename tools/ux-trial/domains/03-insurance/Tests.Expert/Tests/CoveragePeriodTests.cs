using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests;

public sealed class CoveragePeriodTests {

    public static TheoryData<DateOnly, DateOnly, int> Lengths => new() {
        { new DateOnly(2026, 3, 1),  new DateOnly(2026, 3, 2),  2 },     // the shortest period: two days
        { new DateOnly(2026, 3, 1),  new DateOnly(2026, 3, 31), 31 },
        { new DateOnly(2026, 1, 1),  new DateOnly(2026, 12, 31), 365 },
        { new DateOnly(2026, 1, 1),  new DateOnly(2027, 1, 1),  366 },   // exactly one calendar year
        { new DateOnly(2028, 1, 1),  new DateOnly(2029, 1, 1),  367 },   // one calendar year over a leap day
        { new DateOnly(2024, 2, 29), new DateOnly(2025, 2, 28), 366 },   // a year from a leap day ends the day before its anniversary
    };

    [Theory]
    [MemberData(nameof(Lengths))]
    public void A_period_counts_both_its_first_and_its_last_day(DateOnly start, DateOnly end, int expectedDays) {
        // Act
        CoveragePeriod period = new CoveragePeriod(start, end);

        // Assert
        Assert.Equal(start, period.Start);
        Assert.Equal(end, period.End);
        Assert.Equal(expectedDays, period.Days);
    }

    [Fact]
    public void A_period_cannot_end_the_day_it_starts() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CoveragePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 1)));

        // Assert
        Assert.Equal("end", refusal.ParamName);
    }

    [Fact]
    public void A_period_cannot_end_before_it_starts() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CoveragePeriod(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 1)));

        // Assert
        Assert.Equal("end", refusal.ParamName);
    }

    [Theory]
    [InlineData(2026, 1, 1, 2027, 1, 2)]    // one day past the year
    [InlineData(2024, 2, 29, 2025, 3, 1)]   // the year of a leap day ends on 28 February
    [InlineData(2026, 1, 1, 2030, 1, 1)]
    public void A_period_cannot_run_past_one_calendar_year(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CoveragePeriod(new DateOnly(startYear, startMonth, startDay), new DateOnly(endYear, endMonth, endDay)));

        // Assert
        Assert.Equal("end", refusal.ParamName);
    }

    [Theory]
    [InlineData("2026-02-28", false)]   // the day before
    [InlineData("2026-03-01", true)]    // the first day
    [InlineData("2026-03-15", true)]
    [InlineData("2026-03-31", true)]    // the last day
    [InlineData("2026-04-01", false)]   // the day after
    [InlineData("2025-03-15", false)]   // a year earlier
    public void A_period_contains_the_days_from_its_start_to_its_end_both_included(string day, bool expected) {
        // Arrange
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        // Act
        bool contained = period.Contains(DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(expected, contained);
    }

    [Fact]
    public void The_next_period_of_a_whole_year_is_the_following_whole_year() {
        // Arrange
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        // Act
        CoveragePeriod next = period.Next();

        // Assert
        Assert.Equal(new DateOnly(2027, 1, 1), next.Start);
        Assert.Equal(new DateOnly(2027, 12, 31), next.End);
    }

    [Fact]
    public void The_next_period_starts_the_day_after_the_end_and_lasts_as_many_days() {
        // Arrange
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        // Act
        CoveragePeriod next = period.Next();

        // Assert
        Assert.Equal(new DateOnly(2026, 4, 1), next.Start);
        Assert.Equal(new DateOnly(2026, 5, 1), next.End);
        Assert.Equal(31, next.Days);
    }

    [Fact]
    public void The_next_period_keeps_the_number_of_days_not_the_calendar_year() {
        // Arrange
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2027, 3, 1), new DateOnly(2028, 2, 29));

        // Act
        CoveragePeriod next = period.Next();

        // Assert
        Assert.Equal(new DateOnly(2028, 3, 1), next.Start);
        Assert.Equal(new DateOnly(2029, 3, 1), next.End);
        Assert.Equal(period.Days, next.Days);
    }

    [Fact]
    public void The_next_period_is_refused_when_a_period_over_a_leap_day_is_too_long_to_repeat() {
        // Arrange: a calendar year over 29 February 2028 is accepted, 367 days long...
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2028, 1, 1), new DateOnly(2029, 1, 1));

        // Act + Assert: ...but 367 days from 2029-01-02 run past 2030-01-02
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => period.Next());
        Assert.Equal("end", refusal.ParamName);
    }

    [Fact]
    public void Asking_for_the_next_period_leaves_the_original_period_untouched() {
        // Arrange
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        DateOnly       start     = anyPeriod.Start;
        DateOnly       end       = anyPeriod.End;

        // Act
        CoveragePeriod next = anyPeriod.Next();

        // Assert
        Assert.NotSame(anyPeriod, next);
        Assert.Equal(start, anyPeriod.Start);
        Assert.Equal(end, anyPeriod.End);
    }

}
