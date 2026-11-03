using JustDummies;

using ShiftPlanning.Domain.Tests.Regular.Support;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.ValueObjects;

public sealed class WorkingHoursTests {

    private static readonly DayOfWeek[] WorkDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

    private static readonly IAny<TimeOnly> AnyOpening = Any.TimeOnly().Between(new TimeOnly(0, 0), new TimeOnly(20, 0)).WithGranularity(TimeSpan.FromMinutes(1));

    [Fact]
    public void Hours_remember_their_window_and_their_days() {
        // Arrange
        TimeOnly anyOpening = AnyOpening.Generate();
        TimeOnly closing    = anyOpening.Add(TimeSpan.FromHours(3));

        // Act
        WorkingHours hours = new WorkingHours(anyOpening, closing, WorkDays);

        // Assert
        Assert.Equal(anyOpening, hours.Opens);
        Assert.Equal(closing, hours.Closes);
        Assert.Equal(WorkDays.OrderBy(d => d), hours.Days.OrderBy(d => d));
    }

    [Fact]
    public void A_window_of_exactly_one_hour_is_accepted() {
        // Arrange
        TimeOnly anyOpening = AnyOpening.Generate();

        // Act & Assert
        Assert.Equal(anyOpening.Add(TimeSpan.FromHours(1)), new WorkingHours(anyOpening, anyOpening.Add(TimeSpan.FromHours(1)), WorkDays).Closes);
    }

    [Fact]
    public void A_window_one_minute_short_of_an_hour_is_refused() {
        // Arrange
        TimeOnly anyOpening = AnyOpening.Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new WorkingHours(anyOpening, anyOpening.Add(TimeSpan.FromMinutes(59)), WorkDays));
    }

    [Fact]
    public void Closing_when_opening_is_refused() {
        // Arrange
        TimeOnly anyOpening = AnyOpening.Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new WorkingHours(anyOpening, anyOpening, WorkDays));
    }

    [Fact]
    public void Closing_before_opening_is_refused_because_a_window_does_not_cross_midnight() {
        // Arrange
        TimeOnly anyClosing = Any.TimeOnly().Between(new TimeOnly(0, 0), new TimeOnly(20, 0)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new WorkingHours(anyClosing.Add(TimeSpan.FromHours(3)), anyClosing, WorkDays));
        Assert.Throws<ArgumentException>(() => new WorkingHours(new TimeOnly(22, 0), new TimeOnly(6, 0), WorkDays));
    }

    [Fact]
    public void At_least_one_day_is_required() {
        Assert.Throws<ArgumentException>(() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), Array.Empty<DayOfWeek>()));
    }

    [Fact]
    public void A_day_listed_twice_is_refused() {
        Assert.Throws<ArgumentException>(() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Monday }));
    }

    [Fact]
    public void A_day_that_is_not_a_day_of_the_week_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), new[] { DayOfWeek.Monday, (DayOfWeek)7 }));
    }

    [Fact]
    public void Missing_days_are_refused() {
        Assert.Throws<ArgumentNullException>(() => new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), null!));
    }

    [Theory]
    [InlineData("2026-01-12 09:00", "2026-01-12 18:00", true)]
    [InlineData("2026-01-12 09:00", "2026-01-12 09:00", true)]
    [InlineData("2026-01-12 12:30", "2026-01-12 13:15", true)]
    [InlineData("2026-01-12 08:59", "2026-01-12 12:00", false)]
    [InlineData("2026-01-12 12:00", "2026-01-12 18:01", false)]
    [InlineData("2026-01-12 06:00", "2026-01-12 07:00", false)]
    [InlineData("2026-01-16 09:00", "2026-01-16 18:00", true)]
    [InlineData("2026-01-17 10:00", "2026-01-17 11:00", false)]
    [InlineData("2026-01-18 10:00", "2026-01-18 11:00", false)]
    [InlineData("2026-01-12 17:00", "2026-01-13 10:00", false)]
    public void Hours_cover_an_interval_that_stays_on_one_working_day_inside_the_window(string start, string end, bool expected) {
        // Arrange
        WorkingHours hours = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), WorkDays);

        // Act & Assert
        Assert.Equal(expected, hours.Covers(DateTime.Parse(start), DateTime.Parse(end)));
    }

    [Fact]
    public void An_interval_that_ends_before_it_starts_cannot_be_asked_about() {
        Assert.Throws<ArgumentException>(() => Kit.OfficeHours.Covers(new DateTime(2026, 1, 12, 10, 0, 0), new DateTime(2026, 1, 12, 9, 0, 0)));
    }

    [Fact]
    public void Hours_are_equal_when_window_and_days_are_the_same_whatever_the_order_of_the_days() {
        // Arrange
        WorkingHours one     = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), new[] { DayOfWeek.Monday, DayOfWeek.Friday });
        WorkingHours another = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), new[] { DayOfWeek.Friday, DayOfWeek.Monday });

        // Assert
        Assert.Equal(one, another);
        Assert.Equal(one.GetHashCode(), another.GetHashCode());
    }

    [Fact]
    public void Hours_differ_by_window_or_by_days() {
        // Arrange
        WorkingHours hours = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), WorkDays);

        // Assert
        Assert.NotEqual(hours, new WorkingHours(new TimeOnly(9, 30), new TimeOnly(18, 0), WorkDays));
        Assert.NotEqual(hours, new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), WorkDays));
        Assert.NotEqual(hours, new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), WorkDays.Append(DayOfWeek.Saturday)));
    }

}
