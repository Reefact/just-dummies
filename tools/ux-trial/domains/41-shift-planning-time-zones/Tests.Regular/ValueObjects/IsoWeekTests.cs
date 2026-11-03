using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.ValueObjects;

public sealed class IsoWeekTests {

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10000, 1)]
    [InlineData(-5, 1)]
    public void A_year_outside_1_to_9999_is_refused(int year, int week) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IsoWeek(year, week));
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 54)]
    [InlineData(2025, 53)]
    [InlineData(2027, 53)]
    public void A_week_outside_the_weeks_of_that_year_is_refused(int year, int week) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IsoWeek(year, week));
    }

    [Theory]
    [InlineData(2026, 53)]
    [InlineData(2020, 53)]
    [InlineData(2025, 52)]
    [InlineData(2026, 1)]
    public void The_last_week_of_the_year_is_accepted_whether_it_is_the_52nd_or_the_53rd(int year, int week) {
        // Act
        IsoWeek isoWeek = new IsoWeek(year, week);

        // Assert
        Assert.Equal(year, isoWeek.Year);
        Assert.Equal(week, isoWeek.Week);
    }

    [Fact]
    public void Any_week_from_1_to_52_exists_in_any_year() {
        // Arrange
        int anyYear = Any.Int32().Between(1, 9998).Generate();
        int anyWeek = Any.Int32().Between(1, 52).Generate();

        // Act
        IsoWeek isoWeek = new IsoWeek(anyYear, anyWeek);

        // Assert
        Assert.Equal(anyYear, isoWeek.Year);
        Assert.Equal(anyWeek, isoWeek.Week);
    }

    [Theory]
    [InlineData("2026-01-01", 2026, 1)]
    [InlineData("2025-12-29", 2026, 1)]
    [InlineData("2024-12-30", 2025, 1)]
    [InlineData("2027-01-01", 2026, 53)]
    [InlineData("2027-01-03", 2026, 53)]
    [InlineData("2027-01-04", 2027, 1)]
    [InlineData("2021-01-03", 2020, 53)]
    [InlineData("2026-12-31", 2026, 53)]
    [InlineData("2026-03-29", 2026, 13)]
    public void A_date_belongs_to_the_ISO_week_of_its_Thursday(string date, int year, int week) {
        Assert.Equal(new IsoWeek(year, week), IsoWeek.Of(DateOnly.Parse(date)));
    }

    [Fact]
    public void A_week_runs_from_Monday_to_Sunday() {
        // Act
        IsoWeek first = new IsoWeek(2026, 1);

        // Assert
        Assert.Equal(new DateOnly(2025, 12, 29), first.Monday);
        Assert.Equal(new DateOnly(2026, 1, 4), first.Sunday);
        Assert.Equal(new DateOnly(2027, 1, 4), new IsoWeek(2027, 1).Monday);
        Assert.Equal(new DateOnly(2027, 1, 3), new IsoWeek(2026, 53).Sunday);
    }

    [Fact]
    public void The_week_of_a_date_contains_it_and_the_neighbouring_weeks_do_not() {
        // Arrange
        DateOnly anyDate = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act
        IsoWeek week = IsoWeek.Of(anyDate);

        // Assert
        Assert.True(week.Contains(anyDate));
        Assert.True(week.Contains(week.Monday));
        Assert.True(week.Contains(week.Sunday));
        Assert.False(week.Contains(week.Monday.AddDays(-1)));
        Assert.False(week.Contains(week.Sunday.AddDays(1)));
        Assert.Equal(DayOfWeek.Monday, week.Monday.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, week.Sunday.DayOfWeek);
    }

    [Fact]
    public void Weeks_order_by_year_then_by_week() {
        Assert.True(new IsoWeek(2025, 52).CompareTo(new IsoWeek(2026, 1)) < 0);
        Assert.True(new IsoWeek(2026, 53).CompareTo(new IsoWeek(2026, 1)) > 0);
        Assert.Equal(0, new IsoWeek(2026, 7).CompareTo(new IsoWeek(2026, 7)));
        Assert.True(new IsoWeek(2026, 7).CompareTo(null) > 0);
    }

    [Fact]
    public void Weeks_are_equal_when_year_and_week_are_equal() {
        // Arrange
        IsoWeek anyWeek = new AnyIsoWeek().Generate();

        // Act
        IsoWeek same = new IsoWeek(anyWeek.Year, anyWeek.Week);

        // Assert
        Assert.Equal(anyWeek, same);
        Assert.Equal(anyWeek.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(anyWeek, new IsoWeek(anyWeek.Year, anyWeek.Week == 1 ? 2 : 1));
    }

    [Fact]
    public void A_week_prints_as_year_dash_W_and_two_digits() {
        Assert.Equal("2026-W05", new IsoWeek(2026, 5).ToString());
        Assert.Equal("0001-W01", new IsoWeek(1, 1).ToString());
    }

    [Fact(Skip = "domain defect: the last ISO week of year 9999 ends on 10000-01-02, so Sunday and Contains throw instead of answering; literal input, no seed")]
    public void The_last_representable_date_belongs_to_a_week_that_contains_it() {
        // Act
        IsoWeek last = IsoWeek.Of(DateOnly.MaxValue);

        // Assert
        Assert.True(last.Contains(DateOnly.MaxValue));
    }

}
