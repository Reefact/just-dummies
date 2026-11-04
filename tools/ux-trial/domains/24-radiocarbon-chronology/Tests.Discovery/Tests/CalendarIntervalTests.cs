namespace RadiocarbonChronology.Tests.Discovery;

public sealed class CalendarIntervalTests {

    [Theory]
    [InlineData(CalendarInterval.EarliestYear, CalendarInterval.EarliestYear)]
    [InlineData(CalendarInterval.EarliestYear, CalendarInterval.LatestYear)]
    [InlineData(CalendarInterval.LatestYear, CalendarInterval.LatestYear)]
    [InlineData(-1, 1)]
    public void An_interval_within_the_calendar_is_accepted(int startYear, int endYear) {
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
        int anyEndYear = Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarInterval(startYear, anyEndYear));

        // Assert
        Assert.Equal("startYear", refusal.ParamName);
    }

    [Theory]
    [InlineData(-55_001)]
    [InlineData(1_951)]
    public void An_end_year_outside_the_calendar_is_refused(int endYear) {
        // Arrange
        int anyStartYear = Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarInterval(anyStartYear, endYear));

        // Assert
        Assert.Equal("endYear", refusal.ParamName);
    }

    [Fact]
    public void An_interval_that_starts_after_it_ends_is_refused() {
        // Arrange
        (int anyEarlierYear, int anyLaterYear) = Any.Pair(Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear),
                                                                 Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear)).Having.Item1LessThanItem2().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CalendarInterval(anyLaterYear, anyEarlierYear));

        // Assert
        Assert.Equal("endYear", refusal.ParamName);
    }

    [Theory]
    [InlineData(100, 100, 1)]
    [InlineData(-10, 10, 21)]
    [InlineData(-1, 0, 2)]
    [InlineData(-55_000, 1_950, 56_951)]
    public void The_length_counts_both_end_years(int startYear, int endYear, int expectedLength) {
        // Arrange
        CalendarInterval interval = new CalendarInterval(startYear, endYear);

        // Assert
        Assert.Equal(expectedLength, interval.LengthInYears);
    }

    [Theory]
    [InlineData(-5, false)]
    [InlineData(-4, true)]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void An_interval_contains_its_end_years_and_what_lies_between(int year, bool expected) {
        // Arrange
        CalendarInterval interval = new CalendarInterval(-4, 7);

        // Assert
        Assert.Equal(expected, interval.Contains(year));
    }

    [Theory]
    [InlineData(10, 20, true)]    // shares its first year with the end of the other
    [InlineData(11, 20, false)]   // starts the year after
    [InlineData(-20, 0, true)]    // shares its last year with the start of the other
    [InlineData(-20, -1, false)]
    [InlineData(2, 3, true)]      // inside
    [InlineData(-50, 50, true)]   // around
    public void Two_intervals_overlap_when_they_share_at_least_one_year(int otherStart, int otherEnd, bool expected) {
        // Arrange
        CalendarInterval interval = new CalendarInterval(0, 10);
        CalendarInterval other    = new CalendarInterval(otherStart, otherEnd);

        // Assert
        Assert.Equal(expected, interval.Overlaps(other));
        Assert.Equal(expected, other.Overlaps(interval));
    }

    [Fact]
    public void An_interval_cannot_be_compared_with_nothing() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyInterval.Overlaps(null!));
    }

    [Fact]
    public void Two_intervals_with_the_same_years_are_equal() {
        // Arrange
        CalendarInterval interval      = new CalendarInterval(-300, -200);
        CalendarInterval sameInterval  = new CalendarInterval(-300, -200);
        CalendarInterval otherInterval = new CalendarInterval(-300, -199);

        // Assert
        Assert.Equal(interval, sameInterval);
        Assert.Equal(interval.GetHashCode(), sameInterval.GetHashCode());
        Assert.NotEqual(interval, otherInterval);
        Assert.Equal("[-300, -200]", interval.ToString());
    }

}
