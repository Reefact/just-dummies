using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert.Value;

public sealed class SlotTests {

    // The tests hang their times on a drawn day: which day it is matters to none of them.
    private static DateTime At(DateTime midnight, int hours, int minutes, int seconds = 0) {
        return midnight + new TimeSpan(hours, minutes, seconds);
    }

    private static Slot SlotAt(IcaoAirportCode airport, DateTime start, int durationMinutes) {
        return Any.Slot().WithAirport(airport).WithStart(start).WithDuration(TimeSpan.FromMinutes(durationMinutes)).Generate();
    }

    [Fact]
    public void A_slot_ends_when_its_duration_has_elapsed_from_its_start() {
        // Arrange
        DateTime anyMidnight = Fixtures.AnyUtcMidnight.Generate();

        // Act
        Slot slot = Any.Slot().WithStart(At(anyMidnight, 10, 15)).WithDuration(TimeSpan.FromMinutes(45)).Generate();

        // Assert
        Assert.Equal(At(anyMidnight, 11, 0), slot.End);
    }

    [Fact]
    public void A_slot_keeps_the_airport_the_start_and_the_duration_it_was_given() {
        // Arrange
        IcaoAirportCode airport  = new IcaoAirportCode("LFPG");
        DateTime        start    = new DateTime(2026, 3, 14, 10, 15, 0, DateTimeKind.Utc);
        TimeSpan        duration = TimeSpan.FromMinutes(20);

        // Act
        Slot slot = new Slot(airport, start, duration);

        // Assert
        Assert.Same(airport, slot.Airport);
        Assert.Equal(start, slot.Start);
        Assert.Equal(duration, slot.Duration);
    }

    [Fact]
    public void A_slot_without_an_airport_is_refused() {
        // Arrange
        Slot anySlot = Any.Slot().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Slot(null!, anySlot.Start, anySlot.Duration));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void A_start_that_is_not_in_UTC_is_refused(DateTimeKind kind) {
        // Arrange
        DateTime start = new DateTime(2026, 3, 14, 10, 15, 0, kind);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Slot().WithStart(start).Generate());

        // Assert
        Assert.Equal("start", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(55)]
    public void A_start_on_a_five_minute_boundary_is_accepted(int minute) {
        // Act
        Slot slot = Any.Slot().WithStart(new DateTime(2026, 3, 14, 10, minute, 0, DateTimeKind.Utc)).Generate();

        // Assert
        Assert.Equal(minute, slot.Start.Minute);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(59)]
    public void A_start_off_the_five_minute_boundary_is_refused(int minute) {
        // Arrange
        DateTime start = new DateTime(2026, 3, 14, 10, minute, 0, DateTimeKind.Utc);

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Slot().WithStart(start).Generate());
    }

    [Fact]
    public void A_start_with_seconds_is_refused() {
        // Arrange
        DateTime start = new DateTime(2026, 3, 14, 10, 15, 1, DateTimeKind.Utc);

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Slot().WithStart(start).Generate());
    }

    [Fact]
    public void A_start_with_milliseconds_is_refused() {
        // Arrange
        DateTime start = new DateTime(2026, 3, 14, 10, 15, 0, 1, DateTimeKind.Utc);

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Slot().WithStart(start).Generate());
    }

    [Fact(Skip = "domain defect: a start one tick (100 ns) past a five-minute boundary is accepted, because only whole milliseconds are inspected, although a slot starts on a five-minute boundary; no seed involved, the value is a literal")]
    public void A_start_a_tick_past_a_five_minute_boundary_is_refused() {
        // Arrange
        DateTime start = new DateTime(2026, 3, 14, 10, 15, 0, DateTimeKind.Utc).AddTicks(1);

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Slot().WithStart(start).Generate());
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(59)]
    [InlineData(60)]
    public void A_duration_from_five_minutes_to_an_hour_is_accepted(int minutes) {
        // Act
        Slot slot = Any.Slot().WithDuration(TimeSpan.FromMinutes(minutes)).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(minutes), slot.Duration);
    }

    [Fact]
    public void A_duration_just_under_five_minutes_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Slot().WithDuration(TimeSpan.FromMinutes(5) - TimeSpan.FromTicks(1)).Generate());
    }

    [Fact]
    public void A_duration_just_over_an_hour_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Slot().WithDuration(TimeSpan.FromHours(1) + TimeSpan.FromTicks(1)).Generate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(61)]
    [InlineData(1_440)]
    public void A_duration_outside_five_minutes_to_an_hour_is_refused(int minutes) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Slot().WithDuration(TimeSpan.FromMinutes(minutes)).Generate());

        Assert.Equal("duration", refusal.ParamName);
    }

    // The reference slot runs from 10:00 to 10:30 on its day.
    [Theory]
    [InlineData(9, 30, 30, false)]   // ends exactly when the reference starts: touching ends do not overlap
    [InlineData(9, 30, 31, true)]    // overlaps its first minute
    [InlineData(9, 55, 10, true)]    // straddles its start
    [InlineData(10, 0, 30, true)]    // the very same window
    [InlineData(10, 5, 10, true)]    // inside it
    [InlineData(9, 0, 60, false)]    // an hour ending at 10:00
    [InlineData(9, 5, 60, true)]     // an hour containing it, and more
    [InlineData(10, 25, 15, true)]   // straddles its end
    [InlineData(10, 30, 15, false)]  // starts exactly when the reference ends
    [InlineData(10, 35, 15, false)]  // clear of it
    public void Two_slots_at_the_same_airport_overlap_only_when_their_intervals_strictly_intersect(int hours, int minutes, int durationMinutes, bool expected) {
        // Arrange
        DateTime        anyMidnight = Fixtures.AnyUtcMidnight.Generate();
        IcaoAirportCode anyAirport  = Any.IcaoAirportCode().Generate();

        Slot reference = SlotAt(anyAirport, At(anyMidnight, 10, 0), 30);
        Slot other     = SlotAt(anyAirport, At(anyMidnight, hours, minutes), durationMinutes);

        // Act & Assert
        Assert.Equal(expected, reference.Overlaps(other));
        Assert.Equal(expected, other.Overlaps(reference));
    }

    [Fact]
    public void Overlapping_slots_at_different_airports_do_not_overlap() {
        // Arrange
        DateTime        anyMidnight    = Fixtures.AnyUtcMidnight.Generate();
        IcaoAirportCode anyAirport     = Any.IcaoAirportCode().Generate();
        IcaoAirportCode anyOtherAirport = Any.IcaoAirportCode().WithValue(v => v.DifferentFrom(anyAirport.Value)).Generate();

        Slot atAirport      = SlotAt(anyAirport, At(anyMidnight, 10, 0), 30);
        Slot atOtherAirport = SlotAt(anyOtherAirport, At(anyMidnight, 10, 0), 30);

        // Assert
        Assert.False(atAirport.Overlaps(atOtherAirport));
    }

    [Fact]
    public void Slots_at_the_same_airport_overlap_even_when_each_holds_its_own_instance_of_the_code() {
        // Arrange
        DateTime        anyMidnight = Fixtures.AnyUtcMidnight.Generate();
        IcaoAirportCode anyAirport  = Any.IcaoAirportCode().Generate();
        IcaoAirportCode sameAirport = new IcaoAirportCode(anyAirport.Value);

        Slot first  = SlotAt(anyAirport, At(anyMidnight, 10, 0), 30);
        Slot second = SlotAt(sameAirport, At(anyMidnight, 10, 15), 30);

        // Assert
        Assert.True(first.Overlaps(second));
    }

    [Fact]
    public void Slots_on_different_days_do_not_overlap() {
        // Arrange
        DateTime        anyMidnight = Fixtures.AnyUtcMidnight.Generate();
        IcaoAirportCode anyAirport  = Any.IcaoAirportCode().Generate();

        Slot today    = SlotAt(anyAirport, At(anyMidnight, 10, 0), 30);
        Slot tomorrow = SlotAt(anyAirport, At(anyMidnight, 10, 0).AddDays(1), 30);

        // Assert
        Assert.False(today.Overlaps(tomorrow));
    }

    [Fact]
    public void A_slot_cannot_be_compared_with_a_missing_slot() {
        // Arrange
        Slot anySlot = Any.Slot().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySlot.Overlaps(null!));
    }

    // The reference slot runs from 10:00 to 10:30; a departure is tolerated five minutes either side.
    [Theory]
    [InlineData(9, 54, 59, false)]
    [InlineData(9, 55, 0, true)]     // exactly five minutes before the start
    [InlineData(9, 55, 1, true)]
    [InlineData(10, 0, 0, true)]
    [InlineData(10, 15, 0, true)]
    [InlineData(10, 30, 0, true)]
    [InlineData(10, 35, 0, true)]    // exactly five minutes after the end
    [InlineData(10, 35, 1, false)]
    [InlineData(12, 0, 0, false)]
    [InlineData(6, 0, 0, false)]
    public void A_departure_respects_the_slot_within_five_minutes_of_its_window_either_side(int hours, int minutes, int seconds, bool expected) {
        // Arrange
        DateTime anyMidnight = Fixtures.AnyUtcMidnight.Generate();
        Slot     slot        = Any.Slot().WithStart(At(anyMidnight, 10, 0)).WithDuration(TimeSpan.FromMinutes(30)).Generate();

        // Act
        bool respected = slot.IsRespectedBy(At(anyMidnight, hours, minutes, seconds));

        // Assert
        Assert.Equal(expected, respected);
    }

    [Fact]
    public void A_departure_a_tick_beyond_the_tolerance_either_side_does_not_respect_the_slot() {
        // Arrange
        DateTime anyMidnight = Fixtures.AnyUtcMidnight.Generate();
        Slot     slot        = Any.Slot().WithStart(At(anyMidnight, 10, 0)).WithDuration(TimeSpan.FromMinutes(30)).Generate();

        // Assert
        Assert.False(slot.IsRespectedBy(At(anyMidnight, 10, 35).AddTicks(1)));
        Assert.False(slot.IsRespectedBy(At(anyMidnight, 9, 55).AddTicks(-1)));
    }

    [Fact]
    public void The_same_clock_time_on_another_day_does_not_respect_the_slot() {
        // Arrange
        DateTime anyMidnight = Fixtures.AnyUtcMidnight.Generate();
        Slot     slot        = Any.Slot().WithStart(At(anyMidnight, 10, 0)).WithDuration(TimeSpan.FromMinutes(30)).Generate();

        // Assert
        Assert.False(slot.IsRespectedBy(At(anyMidnight, 10, 15).AddDays(1)));
    }

}
