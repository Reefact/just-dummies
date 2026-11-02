using Aviation.Domain;

using JustDummies;

using Xunit;

namespace Aviation.Tests.Discovery;

public sealed class SlotTests {

    private static readonly DateTime NineOClock = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_slot_keeps_its_airport_start_and_duration_and_ends_after_its_duration() {
        // Arrange
        IcaoAirportCode paris = new IcaoAirportCode("LFPG");

        // Act
        Slot slot = new Slot(paris, NineOClock, TimeSpan.FromMinutes(20));

        // Assert
        Assert.Same(paris, slot.Airport);
        Assert.Equal(NineOClock, slot.Start);
        Assert.Equal(TimeSpan.FromMinutes(20), slot.Duration);
        Assert.Equal(new DateTime(2026, 6, 1, 9, 20, 0, DateTimeKind.Utc), slot.End);
    }

    [Fact]
    public void A_null_airport_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Slot(null!, NineOClock, TimeSpan.FromMinutes(20)));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void A_start_that_is_not_UTC_is_refused(DateTimeKind kind) {
        // Arrange
        IcaoAirportCode anyAirport = Any.IcaoAirportCode().Generate();
        DateTime        notUtc     = new DateTime(2026, 6, 1, 9, 0, 0, kind);

        // Assert
        Assert.Throws<ArgumentException>(() => new Slot(anyAirport, notUtc, TimeSpan.FromMinutes(20)));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(5, 0, 0)]
    [InlineData(55, 0, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(4, 0, 0)]
    [InlineData(59, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(5, 30, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(10, 0, 999)]
    public void A_start_is_accepted_only_on_a_five_minute_boundary(int minute, int second, int millisecond) {
        // Arrange
        IcaoAirportCode anyAirport = Any.IcaoAirportCode().Generate();
        DateTime        start      = new DateTime(2026, 6, 1, 9, minute, second, millisecond, DateTimeKind.Utc);
        bool            onBoundary = minute % 5 == 0 && second == 0 && millisecond == 0;

        // Act
        Exception? refusal = Record.Exception(() => new Slot(anyAirport, start, TimeSpan.FromMinutes(20)));

        // Assert
        if (onBoundary) {
            Assert.Null(refusal);
        } else {
            Assert.IsType<ArgumentException>(refusal);
        }
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(30)]
    [InlineData(60)]
    public void A_duration_from_five_minutes_to_one_hour_is_accepted(int minutes) {
        // Arrange
        IcaoAirportCode anyAirport = Any.IcaoAirportCode().Generate();

        // Act
        Slot slot = new Slot(anyAirport, NineOClock, TimeSpan.FromMinutes(minutes));

        // Assert
        Assert.Equal(NineOClock.AddMinutes(minutes), slot.End);
    }

    [Fact]
    public void A_duration_a_tick_short_of_five_minutes_or_a_tick_over_an_hour_is_refused() {
        // Arrange
        IcaoAirportCode anyAirport = Any.IcaoAirportCode().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slot(anyAirport, NineOClock, TimeSpan.FromMinutes(5) - TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slot(anyAirport, NineOClock, TimeSpan.FromHours(1) + TimeSpan.FromTicks(1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-20)]
    [InlineData(4)]
    [InlineData(61)]
    [InlineData(1_440)]
    public void A_duration_outside_five_minutes_to_one_hour_is_refused(int minutes) {
        // Arrange
        IcaoAirportCode anyAirport = Any.IcaoAirportCode().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slot(anyAirport, NineOClock, TimeSpan.FromMinutes(minutes)));
    }

    [Fact]
    public void A_start_not_on_the_boundary_is_refused_before_the_duration_is_looked_at() {
        // Arrange
        IcaoAirportCode anyAirport = Any.IcaoAirportCode().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Slot(anyAirport, NineOClock.AddMinutes(1), TimeSpan.FromHours(3)));
    }

    [Fact]
    public void Two_slots_on_the_same_airport_overlap_when_their_intervals_cross() {
        // Arrange
        Slot nineToTen   = new Slot(new IcaoAirportCode("LFPG"), NineOClock, TimeSpan.FromHours(1));
        Slot halfPastNine = new Slot(new IcaoAirportCode("LFPG"), NineOClock.AddMinutes(30), TimeSpan.FromHours(1));

        // Assert
        Assert.True(nineToTen.Overlaps(halfPastNine));
        Assert.True(halfPastNine.Overlaps(nineToTen));
    }

    [Fact]
    public void A_slot_inside_another_on_the_same_airport_overlaps_it() {
        // Arrange
        Slot outer = new Slot(new IcaoAirportCode("LFPG"), NineOClock, TimeSpan.FromHours(1));
        Slot inner = new Slot(new IcaoAirportCode("LFPG"), NineOClock.AddMinutes(20), TimeSpan.FromMinutes(10));

        // Assert
        Assert.True(outer.Overlaps(inner));
        Assert.True(inner.Overlaps(outer));
    }

    [Fact]
    public void A_slot_overlaps_one_with_the_same_interval() {
        // Arrange
        Slot first  = new Slot(new IcaoAirportCode("LFPG"), NineOClock, TimeSpan.FromMinutes(30));
        Slot second = new Slot(new IcaoAirportCode("LFPG"), NineOClock, TimeSpan.FromMinutes(30));

        // Assert
        Assert.True(first.Overlaps(second));
    }

    [Fact]
    public void Slots_that_only_touch_do_not_overlap() {
        // Arrange
        Slot nineToHalfPast = new Slot(new IcaoAirportCode("LFPG"), NineOClock, TimeSpan.FromMinutes(30));
        Slot halfPastToTen  = new Slot(new IcaoAirportCode("LFPG"), NineOClock.AddMinutes(30), TimeSpan.FromMinutes(30));

        // Assert
        Assert.False(nineToHalfPast.Overlaps(halfPastToTen));
        Assert.False(halfPastToTen.Overlaps(nineToHalfPast));
    }

    [Fact]
    public void Slots_apart_in_time_do_not_overlap() {
        // Arrange
        Slot morning   = new Slot(new IcaoAirportCode("LFPG"), NineOClock, TimeSpan.FromMinutes(30));
        Slot afternoon = new Slot(new IcaoAirportCode("LFPG"), NineOClock.AddHours(6), TimeSpan.FromMinutes(30));

        // Assert
        Assert.False(morning.Overlaps(afternoon));
    }

    [Fact]
    public void Slots_at_different_airports_do_not_overlap_even_at_the_same_time() {
        // Arrange
        Slot paris  = new Slot(new IcaoAirportCode("LFPG"), NineOClock, TimeSpan.FromHours(1));
        Slot london = new Slot(new IcaoAirportCode("EGLL"), NineOClock, TimeSpan.FromHours(1));

        // Assert
        Assert.False(paris.Overlaps(london));
    }

    [Fact]
    public void Overlap_compares_airports_by_value_not_by_instance() {
        // Arrange
        IcaoAirportCode sameCode = new IcaoAirportCode("LFPG");
        Slot            first    = new Slot(sameCode, NineOClock, TimeSpan.FromHours(1));
        Slot            second   = new Slot(new IcaoAirportCode("LFPG"), NineOClock, TimeSpan.FromHours(1));

        // Assert
        Assert.True(first.Overlaps(second));
    }

    [Fact]
    public void A_null_slot_cannot_be_overlapped() {
        // Arrange
        Slot anySlot = Any.Slot().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySlot.Overlaps(null!));
    }

    [Theory]
    [InlineData(-6, false)]
    [InlineData(-5, true)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(15, true)]
    [InlineData(30, true)]
    [InlineData(34, true)]
    [InlineData(35, true)]
    [InlineData(36, false)]
    public void A_departure_respects_the_slot_within_five_minutes_either_side(int minutesFromStart, bool respected) {
        // Arrange: a half-hour slot
        Slot slot = new Slot(Any.IcaoAirportCode().Generate(), NineOClock, TimeSpan.FromMinutes(30));

        // Act
        bool result = slot.IsRespectedBy(NineOClock.AddMinutes(minutesFromStart));

        // Assert
        Assert.Equal(respected, result);
    }

    [Fact]
    public void The_tolerance_is_inclusive_to_the_tick() {
        // Arrange
        Slot slot = new Slot(Any.IcaoAirportCode().Generate(), NineOClock, TimeSpan.FromMinutes(30));

        // Assert
        Assert.True(slot.IsRespectedBy(NineOClock.AddMinutes(-5)));
        Assert.False(slot.IsRespectedBy(NineOClock.AddMinutes(-5).AddTicks(-1)));
        Assert.True(slot.IsRespectedBy(slot.End.AddMinutes(5)));
        Assert.False(slot.IsRespectedBy(slot.End.AddMinutes(5).AddTicks(1)));
    }

}
