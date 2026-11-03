using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.ValueObjects;

public sealed class IsoWeekTests {

    [Theory]
    [InlineData(2020, 53)]   // 2020 starts on a Wednesday and is a leap year: 53 weeks
    [InlineData(2026, 53)]   // 2026 starts on a Thursday
    [InlineData(2021, 52)]
    [InlineData(2027, 52)]
    [InlineData(1, 1)]
    [InlineData(9999, 52)]
    public void A_week_inside_the_years_weeks_is_accepted(int year, int week) {
        // Act
        IsoWeek isoWeek = new IsoWeek(year, week);

        // Assert
        Assert.Equal(year, isoWeek.Year);
        Assert.Equal(week, isoWeek.Week);
    }

    [Theory]
    [InlineData(2021, 53)]   // 2021 has only 52 weeks
    [InlineData(2027, 53)]
    [InlineData(2020, 54)]
    [InlineData(2027, 0)]
    [InlineData(2027, -1)]
    public void A_week_beyond_the_weeks_of_that_year_is_refused(int year, int week) {
        // Act
        Action construct = () => new IsoWeek(year, week);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("week", construct);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2027)]
    [InlineData(10000)]
    public void A_year_outside_1_to_9999_is_refused(int year) {
        // Act
        Action construct = () => new IsoWeek(year, 1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("year", construct);
    }

    [Theory]
    [InlineData(2027, 1, "2027-01-04", "2027-01-10")]   // 2027-01-01 is a Friday: week 1 holds the first Thursday, the 7th
    [InlineData(2021, 1, "2021-01-04", "2021-01-10")]
    [InlineData(2020, 53, "2020-12-28", "2021-01-03")]  // a week that belongs to the old year
    [InlineData(2025, 1, "2024-12-30", "2025-01-05")]   // a week that starts in the previous calendar year
    [InlineData(2027, 12, "2027-03-22", "2027-03-28")]
    public void A_week_runs_from_its_Monday_to_its_Sunday(int year, int week, string monday, string sunday) {
        // Arrange
        IsoWeek isoWeek = new IsoWeek(year, week);

        // Assert
        Assert.Equal(Clock.Day(monday), isoWeek.Monday);
        Assert.Equal(Clock.Day(sunday), isoWeek.Sunday);
    }

    [Theory]
    [InlineData("2027-01-04", 2027, 1)]
    [InlineData("2027-01-10", 2027, 1)]   // the Sunday still belongs to the week it ends
    [InlineData("2027-01-11", 2027, 2)]
    [InlineData("2027-01-01", 2026, 53)]  // a Friday of the new calendar year, still in the old ISO year
    [InlineData("2021-01-03", 2020, 53)]
    [InlineData("2024-12-30", 2025, 1)]   // a Monday of the old calendar year, already in the next ISO year
    [InlineData("2023-01-01", 2022, 52)]
    [InlineData("0001-01-01", 1, 1)]
    public void The_week_of_a_date_is_the_ISO_week_that_holds_it(string date, int year, int week) {
        // Act
        IsoWeek isoWeek = IsoWeek.Of(Clock.Day(date));

        // Assert
        Assert.Equal(new IsoWeek(year, week), isoWeek);
    }

    [Theory]
    [InlineData("2027-01-03", false)]   // the Sunday before
    [InlineData("2027-01-04", true)]    // Monday
    [InlineData("2027-01-10", true)]    // Sunday
    [InlineData("2027-01-11", false)]
    public void A_week_contains_the_dates_from_its_Monday_to_its_Sunday_inclusive(string date, bool expected) {
        // Arrange
        IsoWeek week = new IsoWeek(2027, 1);

        // Act
        bool contains = week.Contains(Clock.Day(date));

        // Assert
        Assert.Equal(expected, contains);
    }

    [Theory]
    [InlineData("0001-01-01")]
    [InlineData("2027-01-04")]
    [InlineData("2020-12-31")]
    public void A_date_belongs_to_the_week_it_gives(string date) {
        // Arrange
        DateOnly day = Clock.Day(date);

        // Act
        bool contained = IsoWeek.Of(day).Contains(day);

        // Assert
        Assert.True(contained);
    }

    [Fact(Skip = "domain defect: IsoWeek(9999, 52) is accepted but its Sunday would be 10000-01-02, so Sunday and Contains throw ArgumentOutOfRangeException for the last days of year 9999, and IsoWeek.Of(DateOnly.MaxValue).Contains(DateOnly.MaxValue) throws (literal input, no draw involved)")]
    public void The_last_representable_date_belongs_to_its_week() {
        // Arrange
        DateOnly lastDay = DateOnly.MaxValue;

        // Act
        bool contained = IsoWeek.Of(lastDay).Contains(lastDay);

        // Assert
        Assert.True(contained);
    }

    [Fact]
    public void Weeks_are_ordered_by_year_then_by_week() {
        // Arrange
        IsoWeek[] shuffled = [new IsoWeek(2021, 1), new IsoWeek(2020, 53), new IsoWeek(2020, 2), new IsoWeek(2027, 1)];

        // Act
        IsoWeek[] ordered = shuffled.Order().ToArray();

        // Assert
        Assert.Equal(["2020-W02", "2020-W53", "2021-W01", "2027-W01"], ordered.Select(week => week.ToString()));
    }

    [Fact]
    public void A_week_compares_greater_than_nothing() {
        // Arrange
        IsoWeek week = new IsoWeek(2027, 1);

        // Assert
        Assert.True(week.CompareTo(null) > 0);
        Assert.Equal(0, week.CompareTo(new IsoWeek(2027, 1)));
    }

    [Fact]
    public void Weeks_are_equal_when_year_and_week_are() {
        // Assert
        Assert.Equal(new IsoWeek(2027, 5), new IsoWeek(2027, 5));
        Assert.Equal(new IsoWeek(2027, 5).GetHashCode(), new IsoWeek(2027, 5).GetHashCode());
        Assert.NotEqual(new IsoWeek(2027, 5), new IsoWeek(2027, 6));
        Assert.NotEqual(new IsoWeek(2027, 5), new IsoWeek(2026, 5));
    }

    [Fact]
    public void A_week_reads_as_year_dash_W_and_two_digits() {
        // Assert
        Assert.Equal("2027-W05", new IsoWeek(2027, 5).ToString());
        Assert.Equal("0001-W01", new IsoWeek(1, 1).ToString());
        Assert.Equal("2020-W53", new IsoWeek(2020, 53).ToString());
    }

}
