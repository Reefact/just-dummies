using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Discovery;

public sealed class ShiftTests {

    private static readonly SiteZone Paris = new SiteZone("Europe/Paris");
    private static readonly SiteZone Troll = new SiteZone("Antarctica/Troll");

    private static Site SiteIn(SiteZone zone) => new AnySite().WithZone(zone).Generate();

    private static ShiftRequest Request(Site site, DateOnly date, TimeOnly start, TimeOnly end, OffsetChoice startChoice = OffsetChoice.Unspecified, OffsetChoice endChoice = OffsetChoice.Unspecified) {
        return new AnyShiftRequest().WithSite(site.Id)
                                    .WithStartDate(date)
                                    .WithStartTime(start)
                                    .WithWallClockLength(end - start < TimeSpan.Zero ? end - start + TimeSpan.FromDays(1) : end - start)
                                    .WithStartChoice(startChoice)
                                    .WithEndChoice(endChoice)
                                    .Generate();
    }

    private static Shift Shift(Site site, DateOnly date, TimeOnly start, TimeOnly end) {
        return new Shift(Any.Int32().Positive().Generate(), site, Request(site, date, start, end));
    }

    [Fact]
    public void A_summer_shift_is_stored_as_UTC_instants() {
        // Arrange
        Site site = SiteIn(Paris);

        // Act: 08:00-16:00 Paris on 10 June is UTC+2
        Shift shift = Shift(site, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 6, 10, 6, 0, 0, TimeSpan.Zero), shift.StartUtc);
        Assert.Equal(new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero), shift.EndUtc);
        Assert.Equal(TimeSpan.FromHours(8), shift.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(8), shift.WallClockLength);
        Assert.Equal(TimeSpan.Zero, shift.DaylightSavingAdjustment);
        Assert.False(shift.CrossesTransition);
    }

    [Fact]
    public void The_local_start_and_end_come_back_with_the_offset_in_force() {
        // Arrange
        Site site = SiteIn(Paris);

        // Act
        Shift shift = Shift(site, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.Equal(new DateTime(2026, 6, 10, 8, 0, 0), shift.LocalStart.DateTime);
        Assert.Equal(TimeSpan.FromHours(2), shift.LocalStart.Offset);
        Assert.Equal(new DateTime(2026, 6, 10, 16, 0, 0), shift.LocalEnd.DateTime);
    }

    [Fact]
    public void A_night_shift_across_spring_forward_lasts_seven_hours() {
        // Arrange
        Site site = SiteIn(Paris);

        // Act: 28 March 22:00 (UTC+1) to 29 March 06:00 (UTC+2)
        Shift shift = Shift(site, new DateOnly(2026, 3, 28), new TimeOnly(22, 0), new TimeOnly(6, 0));

        // Assert
        Assert.Equal(TimeSpan.FromHours(7), shift.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(8), shift.WallClockLength);
        Assert.Equal(TimeSpan.FromHours(-1), shift.DaylightSavingAdjustment);
        Assert.True(shift.CrossesTransition);
    }

    [Fact]
    public void A_night_shift_across_fall_back_lasts_nine_hours() {
        // Arrange
        Site site = SiteIn(Paris);

        // Act: 24 October 22:00 (UTC+2) to 25 October 06:00 (UTC+1)
        Shift shift = Shift(site, new DateOnly(2026, 10, 24), new TimeOnly(22, 0), new TimeOnly(6, 0));

        // Assert
        Assert.Equal(TimeSpan.FromHours(9), shift.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(1), shift.DaylightSavingAdjustment);
        Assert.True(shift.CrossesTransition);
    }

    [Fact]
    public void A_shift_on_the_transition_day_but_clear_of_the_transition_is_not_adjusted() {
        // Arrange
        Site site = SiteIn(Paris);

        // Act: Sunday 29 March, 08:00-16:00 is entirely after the gap
        Shift shift = Shift(site, new DateOnly(2026, 3, 29), new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.False(shift.CrossesTransition);
        Assert.Equal(TimeSpan.FromHours(8), shift.ElapsedDuration);
    }

    [Fact]
    public void A_shift_starting_inside_the_spring_forward_gap_is_refused() {
        // Arrange
        Site site = SiteIn(Paris);

        // Assert
        Assert.Throws<ArgumentException>(() => Shift(site, new DateOnly(2026, 3, 29), new TimeOnly(2, 30), new TimeOnly(10, 0)));
    }

    [Fact]
    public void A_shift_ending_inside_the_spring_forward_gap_is_refused() {
        // Arrange
        Site site = SiteIn(Paris);

        // Assert
        Assert.Throws<ArgumentException>(() => Shift(site, new DateOnly(2026, 3, 28), new TimeOnly(22, 0), new TimeOnly(2, 30)));
    }

    [Fact]
    public void An_ambiguous_start_needs_a_choice() {
        // Arrange
        Site site = SiteIn(Paris);

        // Assert
        Assert.Throws<ArgumentException>(() => Shift(site, new DateOnly(2026, 10, 25), new TimeOnly(2, 30), new TimeOnly(10, 0)));
    }

    [Fact]
    public void The_start_choice_decides_which_occurrence_of_an_ambiguous_time_is_meant() {
        // Arrange
        Site site = SiteIn(Paris);
        DateOnly date = new DateOnly(2026, 10, 25);

        // Act: 02:30 to 10:00 wall clock, starting at either occurrence of 02:30
        Shift first  = new Shift(1, site, Request(site, date, new TimeOnly(2, 30), new TimeOnly(10, 0), OffsetChoice.Earlier));
        Shift second = new Shift(2, site, Request(site, date, new TimeOnly(2, 30), new TimeOnly(10, 0), OffsetChoice.Later));

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero), first.StartUtc);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero), second.StartUtc);
        Assert.Equal(first.EndUtc, second.EndUtc);
        Assert.Equal(TimeSpan.FromHours(8.5), first.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(7.5), second.ElapsedDuration);
    }

    [Fact]
    public void Offset_choices_that_put_the_end_before_the_start_are_refused() {
        // Arrange: Antarctica/Troll turns its clocks back two hours on 25 Oct 2026, so 01:00 to 03:00 local occurs twice
        Site troll = SiteIn(Troll);

        // Assert: starting at the second occurrence of 01:00 and ending at the first occurrence of 02:00 runs backwards
        ShiftRequest backwards = Request(troll, new DateOnly(2026, 10, 25), new TimeOnly(1, 0), new TimeOnly(2, 0), OffsetChoice.Later, OffsetChoice.Earlier);
        Assert.Throws<ArgumentException>(() => new Shift(1, troll, backwards));
    }

    [Fact]
    public void A_request_for_another_site_is_refused() {
        // Arrange
        Site site      = SiteIn(Paris);
        Site otherSite = new AnySite().WithZone(Paris).WithId(i => i.WithValue(v => v.DifferentFrom(site.Id.Value))).Generate();
        ShiftRequest request = Request(otherSite, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.Throws<ArgumentException>(() => new Shift(1, site, request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_shift_number_starts_at_one(int number) {
        // Arrange
        Site site = SiteIn(Paris);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Shift(number, site, Request(site, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0))));
    }

    [Fact]
    public void A_null_site_or_request_is_refused() {
        // Arrange
        Site site = SiteIn(Paris);

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Shift(1, null!, Request(site, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0))));
        Assert.Throws<ArgumentNullException>(() => new Shift(1, site, null!));
    }

    [Fact]
    public void A_new_shift_is_not_staffed() {
        // Act
        Shift shift = Shift(SiteIn(Paris), new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.Null(shift.AssignedTo);
        Assert.False(shift.IsStaffed);
    }

    [Fact]
    public void Shifts_overlap_when_their_instants_intersect() {
        // Arrange
        Site  site  = SiteIn(Paris);
        Shift morning = Shift(site, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));
        Shift partly  = Shift(site, new DateOnly(2026, 6, 10), new TimeOnly(15, 0), new TimeOnly(23, 0));
        Shift after   = Shift(site, new DateOnly(2026, 6, 10), new TimeOnly(16, 0), new TimeOnly(23, 0));

        // Assert
        Assert.True(morning.Overlaps(partly));
        Assert.True(partly.Overlaps(morning));
        Assert.False(morning.Overlaps(after));
        Assert.False(after.Overlaps(morning));
    }

    [Fact]
    public void Shifts_at_different_sites_overlap_on_their_instants_not_on_their_wall_clocks() {
        // Arrange: Tokyo 08:00-16:00 is 23:00Z-07:00Z the day before; Paris 08:00-16:00 is 06:00Z-14:00Z
        Site  tokyo = SiteIn(new SiteZone("Asia/Tokyo"));
        Site  paris = SiteIn(Paris);
        Shift inTokyo = Shift(tokyo, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));
        Shift inParis = Shift(paris, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.True(inTokyo.Overlaps(inParis));
    }

    [Fact]
    public void Rest_is_the_elapsed_gap_between_the_end_of_one_shift_and_the_start_of_the_other_in_either_order() {
        // Arrange
        Site  site = SiteIn(Paris);
        Shift day1 = Shift(site, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));
        Shift day2 = Shift(site, new DateOnly(2026, 6, 11), new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.Equal(TimeSpan.FromHours(16), day1.RestBetween(day2));
        Assert.Equal(TimeSpan.FromHours(16), day2.RestBetween(day1));
    }

    [Fact]
    public void Rest_is_measured_in_elapsed_time_across_a_transition() {
        // Arrange
        Site  site = SiteIn(Paris);
        Shift saturday = Shift(site, new DateOnly(2026, 3, 28), new TimeOnly(14, 0), new TimeOnly(22, 0));
        Shift sunday   = Shift(site, new DateOnly(2026, 3, 29), new TimeOnly(9, 0), new TimeOnly(17, 0));

        // Assert: 22:00 to 09:00 on the clock is 11 h, but spring forward took one away
        Assert.Equal(TimeSpan.FromHours(10), saturday.RestBetween(sunday));
    }

    [Fact]
    public void Rest_is_zero_when_shifts_touch_or_overlap() {
        // Arrange
        Site  site = SiteIn(Paris);
        Shift first   = Shift(site, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));
        Shift touches = Shift(site, new DateOnly(2026, 6, 10), new TimeOnly(16, 0), new TimeOnly(22, 0));
        Shift overlap = Shift(site, new DateOnly(2026, 6, 10), new TimeOnly(12, 0), new TimeOnly(20, 0));

        // Assert
        Assert.Equal(TimeSpan.Zero, first.RestBetween(touches));
        Assert.Equal(TimeSpan.Zero, first.RestBetween(overlap));
        Assert.Equal(TimeSpan.Zero, overlap.RestBetween(first));
    }

    [Fact]
    public void Overlap_and_rest_refuse_a_null_other_shift() {
        // Arrange
        Shift shift = Shift(SiteIn(Paris), new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.Throws<ArgumentNullException>(() => shift.Overlaps(null!));
        Assert.Throws<ArgumentNullException>(() => shift.RestBetween(null!));
    }

}
