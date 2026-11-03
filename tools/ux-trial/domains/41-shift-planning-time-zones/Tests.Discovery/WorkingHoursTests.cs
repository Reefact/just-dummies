using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Discovery;

public sealed class WorkingHoursTests {

    private static readonly DayOfWeek[] Weekdays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private static WorkingHours NineToFive() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), Weekdays);

    // 2026-06-17 is a Wednesday, 2026-06-20 a Saturday.
    private static DateTime Wednesday(int hour, int minute) => new DateTime(2026, 6, 17, hour, minute, 0);

    [Fact]
    public void A_window_keeps_its_hours_and_days() {
        // Act
        WorkingHours hours = NineToFive();

        // Assert
        Assert.Equal(new TimeOnly(9, 0), hours.Opens);
        Assert.Equal(new TimeOnly(17, 0), hours.Closes);
        Assert.Equal(Weekdays.OrderBy(d => d), hours.Days.OrderBy(d => d));
    }

    [Fact]
    public void A_window_of_exactly_one_hour_is_accepted() {
        // Arrange
        TimeOnly anyOpening = Any.TimeOnly().Between(new TimeOnly(0, 0), new TimeOnly(22, 59)).WithGranularity(TimeSpan.FromMinutes(1)).Generate();

        // Act
        WorkingHours hours = new WorkingHours(anyOpening, anyOpening.AddHours(1), Weekdays);

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), hours.Closes - hours.Opens);
    }

    [Fact]
    public void A_window_a_minute_short_of_an_hour_is_refused() {
        // Arrange
        TimeOnly anyOpening = Any.TimeOnly().Between(new TimeOnly(0, 0), new TimeOnly(22, 59)).WithGranularity(TimeSpan.FromMinutes(1)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new WorkingHours(anyOpening, anyOpening.AddMinutes(59), Weekdays));
    }

    [Fact]
    public void A_window_that_closes_when_it_opens_is_refused() {
        Assert.Throws<ArgumentException>(() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(9, 0), Weekdays));
    }

    [Fact]
    public void An_overnight_window_is_refused() {
        Assert.Throws<ArgumentException>(() => new WorkingHours(new TimeOnly(22, 0), new TimeOnly(6, 0), Weekdays));
    }

    [Fact]
    public void A_window_needs_at_least_one_day() {
        Assert.Throws<ArgumentException>(() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), []));
    }

    [Fact]
    public void A_day_listed_twice_is_refused() {
        Assert.Throws<ArgumentException>(() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), [DayOfWeek.Monday, DayOfWeek.Monday]));
    }

    [Fact]
    public void An_undefined_day_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), [(DayOfWeek)7]));
    }

    [Fact]
    public void A_null_list_of_days_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), null!));
    }

    [Theory]
    [InlineData(9, 0, 17, 0, true)]
    [InlineData(9, 0, 9, 0, true)]
    [InlineData(17, 0, 17, 0, true)]
    [InlineData(12, 0, 13, 0, true)]
    [InlineData(8, 59, 12, 0, false)]
    [InlineData(12, 0, 17, 1, false)]
    [InlineData(6, 0, 8, 0, false)]
    [InlineData(18, 0, 19, 0, false)]
    public void Both_ends_must_fall_inside_the_window_and_the_bounds_are_inclusive(int startHour, int startMinute, int endHour, int endMinute, bool covered) {
        // Act
        bool covers = NineToFive().Covers(Wednesday(startHour, startMinute), Wednesday(endHour, endMinute));

        // Assert
        Assert.Equal(covered, covers);
    }

    [Fact]
    public void A_day_that_is_not_a_working_day_is_not_covered() {
        // Act: a Saturday
        bool covers = NineToFive().Covers(new DateTime(2026, 6, 20, 10, 0, 0), new DateTime(2026, 6, 20, 11, 0, 0));

        // Assert
        Assert.False(covers);
    }

    [Fact]
    public void An_interval_that_spans_two_dates_is_not_covered_even_inside_the_hours() {
        // Act: Wednesday 16:00 to Thursday 10:00 never stays on one local date
        bool covers = NineToFive().Covers(Wednesday(16, 0), new DateTime(2026, 6, 18, 10, 0, 0));

        // Assert
        Assert.False(covers);
    }

    [Fact]
    public void An_end_before_the_start_is_refused() {
        Assert.Throws<ArgumentException>(() => NineToFive().Covers(Wednesday(12, 0), Wednesday(11, 0)));
    }

    [Fact]
    public void Windows_are_equal_whatever_order_the_days_were_listed_in() {
        // Arrange
        WorkingHours first  = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), [DayOfWeek.Monday, DayOfWeek.Friday]);
        WorkingHours second = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), [DayOfWeek.Friday, DayOfWeek.Monday]);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Windows_that_differ_by_opening_closing_or_days_are_not_equal() {
        // Arrange
        WorkingHours reference = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), [DayOfWeek.Monday]);

        // Assert
        Assert.NotEqual(reference, new WorkingHours(new TimeOnly(9, 30), new TimeOnly(17, 0), [DayOfWeek.Monday]));
        Assert.NotEqual(reference, new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), [DayOfWeek.Monday]));
        Assert.NotEqual(reference, new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), [DayOfWeek.Tuesday]));
        Assert.NotEqual(reference, new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), [DayOfWeek.Monday, DayOfWeek.Tuesday]));
        Assert.False(reference.Equals(null));
    }

}
