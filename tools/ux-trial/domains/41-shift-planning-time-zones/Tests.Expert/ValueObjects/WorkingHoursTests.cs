using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.ValueObjects;

public sealed class WorkingHoursTests {

    // ---- Construction ------------------------------------------------------------------------------------------

    [Fact]
    public void Working_hours_keep_their_window_and_their_days() {
        // Act
        WorkingHours hours = new WorkingHours(Clock.At("09:00"), Clock.At("17:30"), [DayOfWeek.Tuesday, DayOfWeek.Saturday]);

        // Assert
        Assert.Equal(Clock.At("09:00"), hours.Opens);
        Assert.Equal(Clock.At("17:30"), hours.Closes);
        Assert.Equal(new HashSet<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Saturday }, hours.Days.ToHashSet());
    }

    [Fact]
    public void A_window_of_exactly_one_hour_is_the_shortest_allowed() {
        // Act
        WorkingHours hours = new WorkingHours(Clock.At("09:00"), Clock.At("10:00"), Hours.Weekdays);

        // Assert
        Assert.Equal(Clock.At("10:00"), hours.Closes);
    }

    [Fact]
    public void A_window_of_less_than_one_hour_is_refused() {
        // Act
        Action construct = () => new WorkingHours(Clock.At("09:00"), Clock.At("09:59"), Hours.Weekdays);

        // Assert
        Assert.Throws<ArgumentException>("closes", construct);
    }

    [Theory]
    [InlineData("09:00", "09:00")]   // an empty window
    [InlineData("17:00", "09:00")]   // an overnight window is not a working window
    [InlineData("22:00", "06:00")]
    public void Working_hours_close_after_they_open_on_the_same_day(string opens, string closes) {
        // Act
        Action construct = () => new WorkingHours(Clock.At(opens), Clock.At(closes), Hours.Weekdays);

        // Assert
        Assert.Throws<ArgumentException>("closes", construct);
    }

    [Fact]
    public void Working_hours_may_run_from_midnight_to_the_last_tick_of_the_day() {
        // Act
        WorkingHours hours = new WorkingHours(TimeOnly.MinValue, TimeOnly.MaxValue, Hours.Weekdays);

        // Assert
        Assert.Equal(TimeOnly.MaxValue, hours.Closes);
    }

    [Fact]
    public void Working_hours_need_at_least_one_day() {
        // Act
        Action construct = () => new WorkingHours(Clock.At("09:00"), Clock.At("17:00"), []);

        // Assert
        Assert.Throws<ArgumentException>("days", construct);
    }

    [Fact]
    public void A_day_listed_twice_is_refused() {
        // Act
        Action construct = () => new WorkingHours(Clock.At("09:00"), Clock.At("17:00"), [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Monday]);

        // Assert
        Assert.Throws<ArgumentException>("days", construct);
    }

    [Fact]
    public void A_day_the_enum_does_not_declare_is_refused() {
        // Act
        Action construct = () => new WorkingHours(Clock.At("09:00"), Clock.At("17:00"), [DayOfWeek.Monday, (DayOfWeek)7]);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("days", construct);
    }

    [Fact]
    public void Missing_days_are_refused() {
        // Act
        Action construct = () => new WorkingHours(Clock.At("09:00"), Clock.At("17:00"), null!);

        // Assert
        Assert.Throws<ArgumentNullException>("days", construct);
    }

    // ---- Coverage of an interval -------------------------------------------------------------------------------

    // 2027-01-11 is a Monday.

    [Theory]
    [InlineData("2027-01-11 09:00", "2027-01-11 17:00")]   // both ends are inclusive
    [InlineData("2027-01-11 09:00", "2027-01-11 09:00")]   // an empty interval is covered too
    [InlineData("2027-01-11 12:00", "2027-01-11 13:00")]
    [InlineData("2027-01-15 16:59", "2027-01-15 17:00")]   // Friday, the last minute
    public void An_interval_on_a_working_day_between_opening_and_closing_is_covered(string start, string end) {
        // Arrange
        WorkingHours hours = Hours.NineToFive();

        // Act
        bool covered = hours.Covers(LocalDateTime(start), LocalDateTime(end));

        // Assert
        Assert.True(covered);
    }

    [Theory]
    [InlineData("2027-01-11 08:59", "2027-01-11 12:00")]   // starts a minute before opening
    [InlineData("2027-01-11 12:00", "2027-01-11 17:01")]   // ends a minute after closing
    [InlineData("2027-01-16 10:00", "2027-01-16 11:00")]   // a Saturday
    [InlineData("2027-01-17 10:00", "2027-01-17 11:00")]   // a Sunday
    [InlineData("2027-01-11 16:00", "2027-01-12 10:00")]   // not the same date
    [InlineData("2027-01-11 10:00", "2027-01-12 00:00")]   // ends at midnight of the next date
    public void An_interval_outside_the_hours_the_days_or_the_date_is_not_covered(string start, string end) {
        // Arrange
        WorkingHours hours = Hours.NineToFive();

        // Act
        bool covered = hours.Covers(LocalDateTime(start), LocalDateTime(end));

        // Assert
        Assert.False(covered);
    }

    [Fact]
    public void A_day_off_is_not_covered_even_inside_the_window() {
        // Arrange
        WorkingHours tuesdayToThursday = new WorkingHours(Clock.At("09:00"), Clock.At("17:00"), [DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday]);

        // Act
        bool mondayCovered  = tuesdayToThursday.Covers(LocalDateTime("2027-01-11 10:00"), LocalDateTime("2027-01-11 11:00"));
        bool tuesdayCovered = tuesdayToThursday.Covers(LocalDateTime("2027-01-12 10:00"), LocalDateTime("2027-01-12 11:00"));

        // Assert
        Assert.False(mondayCovered);
        Assert.True(tuesdayCovered);
    }

    [Fact]
    public void An_interval_that_ends_before_it_starts_is_refused() {
        // Arrange
        WorkingHours hours = Hours.NineToFive();

        // Act
        Action cover = () => hours.Covers(LocalDateTime("2027-01-11 12:00"), LocalDateTime("2027-01-11 11:59"));

        // Assert
        Assert.Throws<ArgumentException>("localEnd", cover);
    }

    // ---- Equality ----------------------------------------------------------------------------------------------

    [Fact]
    public void Working_hours_are_equal_when_window_and_day_set_are_equal_whatever_the_listing_order() {
        // Arrange
        WorkingHours first  = new WorkingHours(Clock.At("09:00"), Clock.At("17:00"), [DayOfWeek.Monday, DayOfWeek.Friday, DayOfWeek.Wednesday]);
        WorkingHours second = new WorkingHours(Clock.At("09:00"), Clock.At("17:00"), [DayOfWeek.Wednesday, DayOfWeek.Monday, DayOfWeek.Friday]);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData("09:01", "17:00", DayOfWeek.Monday)]
    [InlineData("09:00", "17:01", DayOfWeek.Monday)]
    [InlineData("09:00", "17:00", DayOfWeek.Tuesday)]
    public void Working_hours_differing_by_opening_closing_or_day_are_not_equal(string opens, string closes, DayOfWeek day) {
        // Arrange
        WorkingHours reference = new WorkingHours(Clock.At("09:00"), Clock.At("17:00"), [DayOfWeek.Monday]);

        // Act
        WorkingHours other = new WorkingHours(Clock.At(opens), Clock.At(closes), [day]);

        // Assert
        Assert.NotEqual(reference, other);
    }

    private static DateTime LocalDateTime(string dateAndTime) {
        return DateTime.ParseExact(dateAndTime, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

}
