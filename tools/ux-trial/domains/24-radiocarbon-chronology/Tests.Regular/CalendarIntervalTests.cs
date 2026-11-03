using JustDummies;
using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Regular.Generators;
using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class CalendarIntervalTests {

    [Fact]
    public void A_generated_interval_keeps_its_bounds_in_order() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().Generate();

        // Act
        int length = anyInterval.LengthInYears;

        // Assert
        Assert.True(anyInterval.StartYear <= anyInterval.EndYear);
        Assert.Equal(anyInterval.EndYear - anyInterval.StartYear + 1, length);
    }

    [Fact]
    public void Both_bounds_count_in_the_length() {
        // Arrange
        CalendarInterval interval = new CalendarInterval(-100, -91);

        // Act
        int length = interval.LengthInYears;

        // Assert
        Assert.Equal(10, length);
    }

    [Fact]
    public void An_interval_of_a_single_year_is_accepted() {
        // Arrange
        int anyYear = Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear).Generate();

        // Act
        CalendarInterval interval = new CalendarInterval(anyYear, anyYear);

        // Assert
        Assert.Equal(1, interval.LengthInYears);
    }

    [Fact]
    public void An_interval_may_straddle_the_year_zero() {
        // Arrange
        CalendarInterval interval = new CalendarInterval(-5, 5);

        // Act
        int length = interval.LengthInYears;

        // Assert
        Assert.Equal(11, length);
        Assert.True(interval.Contains(0));
    }

    [Fact]
    public void An_interval_starting_after_its_end_is_refused() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().WithStartYear(r => r.GreaterThan(CalendarInterval.EarliestYear)).Generate();
        int              anyEarlierEnd = Any.Int32().Between(CalendarInterval.EarliestYear, anyInterval.StartYear - 1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CalendarInterval(anyInterval.StartYear, anyEarlierEnd));
    }

    [Fact]
    public void A_start_year_outside_the_calendar_range_is_refused() {
        // Arrange
        int anyYearBeforeTheRange = Any.Int32().LessThan(CalendarInterval.EarliestYear).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarInterval(anyYearBeforeTheRange, 0));

        // Assert
        Assert.Equal("startYear", error.ParamName);
    }

    [Fact]
    public void An_end_year_after_the_calendar_range_is_refused() {
        // Arrange
        int anyYearAfterTheRange = Any.Int32().GreaterThan(CalendarInterval.LatestYear).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarInterval(0, anyYearAfterTheRange));

        // Assert
        Assert.Equal("endYear", error.ParamName);
    }

    [Fact]
    public void An_end_year_before_the_calendar_range_is_refused() {
        // Arrange
        int anyYearBeforeTheRange = Any.Int32().LessThan(CalendarInterval.EarliestYear).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarInterval(CalendarInterval.EarliestYear, anyYearBeforeTheRange));

        // Assert
        Assert.Equal("endYear", error.ParamName);
    }

    [Fact]
    public void The_whole_calendar_range_is_a_valid_interval() {
        // Arrange / Act
        CalendarInterval interval = new CalendarInterval(CalendarInterval.EarliestYear, CalendarInterval.LatestYear);

        // Assert
        Assert.Equal(56_951, interval.LengthInYears);
    }

    [Fact]
    public void An_interval_contains_every_year_between_its_bounds_included() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().Generate();
        int              anyYearInside = Any.Int32().Between(anyInterval.StartYear, anyInterval.EndYear).Generate();

        // Act
        bool contained = anyInterval.Contains(anyYearInside);

        // Assert
        Assert.True(contained);
    }

    [Fact]
    public void An_interval_contains_its_two_bounds() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().Generate();

        // Act / Assert
        Assert.True(anyInterval.Contains(anyInterval.StartYear));
        Assert.True(anyInterval.Contains(anyInterval.EndYear));
    }

    [Fact]
    public void An_interval_does_not_contain_the_year_before_its_start() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().WithStartYear(r => r.GreaterThan(CalendarInterval.EarliestYear)).Generate();

        // Act
        bool contained = anyInterval.Contains(anyInterval.StartYear - 1);

        // Assert
        Assert.False(contained);
    }

    [Fact]
    public void An_interval_does_not_contain_the_year_after_its_end() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().WithEndYear(r => r.LessThan(CalendarInterval.LatestYear)).Generate();

        // Act
        bool contained = anyInterval.Contains(anyInterval.EndYear + 1);

        // Assert
        Assert.False(contained);
    }

    [Theory]
    [InlineData(0, 10, 5, 15, true)]
    [InlineData(0, 10, 10, 20, true)]
    [InlineData(0, 10, 11, 20, false)]
    [InlineData(0, 10, 2, 8, true)]
    [InlineData(2, 8, 0, 10, true)]
    [InlineData(-50, -40, -39, -30, false)]
    public void Two_intervals_overlap_when_they_share_a_year(int startA, int endA, int startB, int endB, bool expected) {
        // Arrange
        CalendarInterval first  = new CalendarInterval(startA, endA);
        CalendarInterval second = new CalendarInterval(startB, endB);

        // Act
        bool overlaps = first.Overlaps(second);

        // Assert
        Assert.Equal(expected, overlaps);
        Assert.Equal(expected, second.Overlaps(first));
    }

    [Fact]
    public void An_interval_overlaps_itself() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().Generate();

        // Act
        bool overlaps = anyInterval.Overlaps(anyInterval);

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void Overlapping_with_nothing_is_refused() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyInterval.Overlaps(null!));
    }

    [Fact]
    public void Two_intervals_with_the_same_bounds_are_equal() {
        // Arrange
        CalendarInterval anyInterval = Any.CalendarInterval().Generate();

        // Act
        CalendarInterval copy = new CalendarInterval(anyInterval.StartYear, anyInterval.EndYear);

        // Assert
        Assert.Equal(anyInterval, copy);
        Assert.Equal(anyInterval.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void An_interval_is_written_between_brackets() {
        // Arrange
        CalendarInterval interval = new CalendarInterval(-1050, -987);

        // Act
        string text = interval.ToString();

        // Assert
        Assert.Equal("[-1050, -987]", text);
    }

}
