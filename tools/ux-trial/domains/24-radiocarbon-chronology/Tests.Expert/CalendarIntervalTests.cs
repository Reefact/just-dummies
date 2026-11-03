using JustDummies;

using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Expert.Support;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class CalendarIntervalTests {

    [Theory]
    [InlineData(-55_000, 1_950)]
    [InlineData(-55_000, -55_000)]
    [InlineData(1_950, 1_950)]
    public void An_interval_at_the_limits_of_the_calendar_is_accepted(int startYear, int endYear) {
        // Act
        CalendarInterval interval = new CalendarInterval(startYear, endYear);

        // Assert
        Assert.Equal(startYear, interval.StartYear);
        Assert.Equal(endYear, interval.EndYear);
    }

    [Theory]
    [InlineData(-55_001)]
    [InlineData(1_951)]
    public void A_start_year_outside_the_calendar_is_refused(int startYear) {
        // Arrange
        int anyEndYear = Years.InTheCalendar().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CalendarInterval(startYear, anyEndYear));

        // Assert
        Assert.Equal("startYear", refusal.ParamName);
    }

    [Theory]
    [InlineData(-55_001)]
    [InlineData(1_951)]
    public void An_end_year_outside_the_calendar_is_refused(int endYear) {
        // Arrange
        int anyStartYear = Years.InTheCalendar().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CalendarInterval(anyStartYear, endYear));

        // Assert
        Assert.Equal("endYear", refusal.ParamName);
    }

    [Fact]
    public void An_interval_that_starts_one_year_after_its_end_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CalendarInterval(-499, -500));

        // Assert
        Assert.Equal("endYear", refusal.ParamName);
    }

    [Theory]
    [InlineData(-500, -500, 1)]
    [InlineData(-500, -499, 2)]
    [InlineData(-10, 10, 21)]
    [InlineData(1, 100, 100)]
    [InlineData(-55_000, 1_950, 56_951)]
    public void The_length_counts_both_ends_and_the_year_zero(int startYear, int endYear, int expectedLength) {
        // Arrange
        CalendarInterval interval = new CalendarInterval(startYear, endYear);

        // Assert
        Assert.Equal(expectedLength, interval.LengthInYears);
    }

    [Theory]
    [InlineData(-300, false)]
    [InlineData(-201, false)]
    [InlineData(-200, true)]
    [InlineData(-100, true)]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    public void An_interval_contains_its_two_end_years_and_what_lies_between(int year, bool expected) {
        // Arrange
        CalendarInterval interval = new CalendarInterval(-200, -1);

        // Assert
        Assert.Equal(expected, interval.Contains(year));
    }

    [Theory]
    [InlineData(100, 200, true)]     // identical
    [InlineData(150, 250, true)]     // overlapping at the end
    [InlineData(50, 150, true)]      // overlapping at the start
    [InlineData(120, 180, true)]     // contained
    [InlineData(0, 300, true)]       // containing
    [InlineData(200, 300, true)]     // sharing only its last year
    [InlineData(0, 100, true)]       // sharing only its first year
    [InlineData(201, 300, false)]    // one year after
    [InlineData(0, 99, false)]       // one year before
    public void Two_intervals_overlap_when_they_share_at_least_one_year(int otherStart, int otherEnd, bool expected) {
        // Arrange
        CalendarInterval interval = new CalendarInterval(100, 200);
        CalendarInterval other    = new CalendarInterval(otherStart, otherEnd);

        // Assert
        Assert.Equal(expected, interval.Overlaps(other));
        Assert.Equal(expected, other.Overlaps(interval));
    }

    [Fact]
    public void An_interval_cannot_be_compared_for_overlap_with_nothing() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyInterval.Overlaps(null!));
    }

    [Fact]
    public void Two_intervals_with_the_same_years_are_equal() {
        // Arrange
        CalendarInterval interval     = new CalendarInterval(-2_000, -1_500);
        CalendarInterval sameInterval = new CalendarInterval(-2_000, -1_500);

        // Assert
        Assert.Equal(interval, sameInterval);
        Assert.Equal(interval.GetHashCode(), sameInterval.GetHashCode());
    }

    [Theory]
    [InlineData(-2_001, -1_500)]
    [InlineData(-2_000, -1_501)]
    public void Intervals_differing_by_one_end_are_not_equal(int startYear, int endYear) {
        // Arrange
        CalendarInterval interval = new CalendarInterval(-2_000, -1_500);

        // Assert
        Assert.NotEqual(interval, new CalendarInterval(startYear, endYear));
    }

    [Fact]
    public void An_interval_reads_as_its_two_years_in_brackets() {
        // Assert
        Assert.Equal("[-2000, -1500]", new CalendarInterval(-2_000, -1_500).ToString());
    }

}
