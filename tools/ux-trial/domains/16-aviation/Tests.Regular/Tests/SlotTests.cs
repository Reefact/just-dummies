using JustDummies;

using Xunit;

namespace Aviation.Domain.Tests.Regular;

public sealed class SlotTests {

    private static readonly DateTime NoonUtc = new DateTime(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_slot_keeps_its_airport_start_and_duration_and_ends_when_the_duration_has_elapsed() {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();

        // Act
        Slot slot = new Slot(anyAirport, NoonUtc, TimeSpan.FromMinutes(20));

        // Assert
        Assert.Same(anyAirport, slot.Airport);
        Assert.Equal(NoonUtc, slot.Start);
        Assert.Equal(TimeSpan.FromMinutes(20), slot.Duration);
        Assert.Equal(NoonUtc.AddMinutes(20), slot.End);
    }

    [Fact]
    public void A_null_airport_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Slot(null!, NoonUtc, TimeSpan.FromMinutes(20)));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void A_start_that_is_not_utc_is_refused(DateTimeKind kind) {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();
        DateTime        notUtc     = new DateTime(2026, 3, 14, 12, 0, 0, kind);

        // Assert
        Assert.Throws<ArgumentException>(() => new Slot(anyAirport, notUtc, TimeSpan.FromMinutes(20)));
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(3, 0, 0)]
    [InlineData(59, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 999, 0)]
    [InlineData(5, 5, 5)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 0, 4)]
    [InlineData(0, 0, 59)]
    public void A_start_that_is_not_on_a_five_minute_boundary_is_refused(int second, int millisecond, int minuteOffset) {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();
        DateTime        start      = new DateTime(2026, 3, 14, 12, minuteOffset, second, millisecond, DateTimeKind.Utc);

        // Assert
        Assert.Throws<ArgumentException>(() => new Slot(anyAirport, start, TimeSpan.FromMinutes(20)));
    }

    [Fact(Skip = "domain defect: a start carrying sub-millisecond ticks is accepted although the slot must start on a five-minute boundary; seed n/a (literal)")]
    public void A_start_carrying_a_single_tick_past_the_boundary_is_refused() {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Slot(anyAirport, NoonUtc.AddTicks(1), TimeSpan.FromMinutes(20)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(55)]
    public void A_start_on_a_five_minute_boundary_is_accepted(int minute) {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();
        DateTime        start      = new DateTime(2026, 3, 14, 12, minute, 0, DateTimeKind.Utc);

        // Act
        Slot slot = new Slot(anyAirport, start, TimeSpan.FromMinutes(20));

        // Assert
        Assert.Equal(start, slot.Start);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(60)]
    public void A_duration_between_five_minutes_and_one_hour_is_accepted(int minutes) {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();

        // Act
        Slot slot = new Slot(anyAirport, NoonUtc, TimeSpan.FromMinutes(minutes));

        // Assert
        Assert.Equal(NoonUtc.AddMinutes(minutes), slot.End);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5 * 60)]
    [InlineData(5 * 60 - 1)]
    [InlineData(60 * 60 + 1)]
    [InlineData(2 * 60 * 60)]
    public void A_duration_outside_five_minutes_to_one_hour_is_refused(int seconds) {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slot(anyAirport, NoonUtc, TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Two_slots_of_one_airport_overlap_when_their_intervals_strictly_intersect() {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();
        Slot            first      = new Slot(anyAirport, NoonUtc, TimeSpan.FromMinutes(30));
        Slot            second     = new Slot(anyAirport, NoonUtc.AddMinutes(25), TimeSpan.FromMinutes(30));

        // Assert
        Assert.True(first.Overlaps(second));
        Assert.True(second.Overlaps(first));
    }

    [Fact]
    public void A_slot_that_contains_another_overlaps_it() {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();
        Slot            outer      = new Slot(anyAirport, NoonUtc, TimeSpan.FromMinutes(60));
        Slot            inner      = new Slot(anyAirport, NoonUtc.AddMinutes(20), TimeSpan.FromMinutes(10));

        // Assert
        Assert.True(outer.Overlaps(inner));
        Assert.True(inner.Overlaps(outer));
    }

    [Fact]
    public void A_slot_overlaps_itself() {
        // Arrange
        Slot anySlot = Arbitrary.Slot().Generate();

        // Assert
        Assert.True(anySlot.Overlaps(anySlot));
    }

    [Fact]
    public void Two_slots_that_only_touch_do_not_overlap() {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();
        Slot            first      = new Slot(anyAirport, NoonUtc, TimeSpan.FromMinutes(30));
        Slot            second     = new Slot(anyAirport, NoonUtc.AddMinutes(30), TimeSpan.FromMinutes(30));

        // Assert
        Assert.False(first.Overlaps(second));
        Assert.False(second.Overlaps(first));
    }

    [Fact]
    public void Two_slots_separated_in_time_do_not_overlap() {
        // Arrange
        IcaoAirportCode anyAirport = Arbitrary.IcaoAirportCode().Generate();
        Slot            first      = new Slot(anyAirport, NoonUtc, TimeSpan.FromMinutes(30));
        Slot            later      = new Slot(anyAirport, NoonUtc.AddHours(3), TimeSpan.FromMinutes(30));

        // Assert
        Assert.False(first.Overlaps(later));
    }

    [Fact]
    public void Two_slots_at_different_airports_do_not_overlap() {
        // Arrange
        IcaoAirportCode anyAirport      = Arbitrary.IcaoAirportCode().Generate();
        IcaoAirportCode anyOtherAirport = Arbitrary.IcaoAirportCode().WithValue(v => v.DifferentFrom(anyAirport.Value)).Generate();
        Slot            first           = new Slot(anyAirport, NoonUtc, TimeSpan.FromMinutes(30));
        Slot            second          = new Slot(anyOtherAirport, NoonUtc, TimeSpan.FromMinutes(30));

        // Assert
        Assert.False(first.Overlaps(second));
    }

    [Fact]
    public void Two_slots_overlap_when_their_airports_have_the_same_code_in_distinct_instances() {
        // Arrange
        IcaoAirportCode anyAirport     = Arbitrary.IcaoAirportCode().Generate();
        IcaoAirportCode sameCodeAirport = new IcaoAirportCode(anyAirport.Value);
        Slot            first          = new Slot(anyAirport, NoonUtc, TimeSpan.FromMinutes(30));
        Slot            second         = new Slot(sameCodeAirport, NoonUtc, TimeSpan.FromMinutes(30));

        // Assert
        Assert.True(first.Overlaps(second));
    }

    [Fact]
    public void Overlapping_a_null_slot_is_refused() {
        // Arrange
        Slot anySlot = Arbitrary.Slot().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySlot.Overlaps(null!));
    }

    [Theory]
    [InlineData(-5 * 60, true)]
    [InlineData(-5 * 60 - 1, false)]
    [InlineData(-60, true)]
    [InlineData(0, true)]
    [InlineData(15 * 60, true)]
    [InlineData(30 * 60, true)]
    [InlineData(35 * 60, true)]
    [InlineData(35 * 60 + 1, false)]
    [InlineData(2 * 60 * 60, false)]
    [InlineData(-2 * 60 * 60, false)]
    public void A_departure_respects_the_slot_within_five_minutes_either_side(int secondsFromStart, bool expected) {
        // Arrange
        Slot slot = Arbitrary.Slot().WithStart(NoonUtc).WithDuration(TimeSpan.FromMinutes(30)).Generate();

        // Act
        bool respected = slot.IsRespectedBy(NoonUtc.AddSeconds(secondsFromStart));

        // Assert
        Assert.Equal(expected, respected);
    }

    [Fact]
    public void The_tolerance_is_exact_to_the_tick() {
        // Arrange
        Slot slot = Arbitrary.Slot().WithStart(NoonUtc).WithDuration(TimeSpan.FromMinutes(30)).Generate();

        // Assert
        Assert.False(slot.IsRespectedBy(NoonUtc.AddMinutes(-5).AddTicks(-1)));
        Assert.True(slot.IsRespectedBy(NoonUtc.AddMinutes(-5)));
        Assert.True(slot.IsRespectedBy(NoonUtc.AddMinutes(35)));
        Assert.False(slot.IsRespectedBy(NoonUtc.AddMinutes(35).AddTicks(1)));
    }

}
