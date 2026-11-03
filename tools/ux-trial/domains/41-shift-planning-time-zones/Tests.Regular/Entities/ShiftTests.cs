using JustDummies;

using ShiftPlanning.Domain.Tests.Regular.Support;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.Entities;

public sealed class ShiftTests {

    private static readonly IAny<DateOnly> AnyDay = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));

    [Fact]
    public void A_number_below_one_is_refused() {
        // Arrange
        Site         anySite    = Kit.SiteIn(Kit.Tokyo);
        ShiftRequest anyRequest = new AnyShiftRequest().WithSite(anySite.Id).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Shift(0, anySite, anyRequest));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Shift(-3, anySite, anyRequest));
    }

    [Fact]
    public void A_request_for_another_site_is_refused() {
        // Arrange
        List<Site> anyTwoSites = Kit.OfficesIn(Kit.Tokyo, Kit.Tokyo);
        ShiftRequest requestForTheSecond = new AnyShiftRequest().WithSite(anyTwoSites[1].Id).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Shift(1, anyTwoSites[0], requestForTheSecond));
    }

    [Fact]
    public void Site_and_request_are_required() {
        // Arrange
        Site anySite = Kit.SiteIn(Kit.Tokyo);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Shift(1, null!, new AnyShiftRequest().Generate()));
        Assert.Throws<ArgumentNullException>(() => new Shift(1, anySite, null!));
    }

    [Fact]
    public void In_a_zone_without_daylight_saving_elapsed_time_is_wall_clock_time() {
        // Arrange
        Site         tokyo   = Kit.SiteIn(Kit.Tokyo);
        ShiftRequest request = new AnyShiftRequest().WithSite(tokyo.Id)
                                                    .WithStartDate(d => d.Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 30)))
                                                    .Generate();

        // Act
        Shift shift = new Shift(1, tokyo, request);

        // Assert
        Assert.Equal(request.WallClockLength, shift.ElapsedDuration);
        Assert.Equal(TimeSpan.Zero, shift.DaylightSavingAdjustment);
        Assert.False(shift.CrossesTransition);
    }

    [Fact]
    public void A_shift_is_stored_as_utc_instants_and_read_back_on_the_local_clock() {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);

        // Act
        Shift shift = new Shift(4, paris, Kit.Request(paris, new DateOnly(2026, 1, 15), "09:00", "17:00"));

        // Assert
        Assert.Equal(4, shift.Number);
        Assert.Same(paris, shift.Site);
        Assert.Equal(Kit.Utc_(2026, 1, 15, 8, 0), shift.StartUtc);
        Assert.Equal(Kit.Utc_(2026, 1, 15, 16, 0), shift.EndUtc);
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.FromHours(1)), shift.LocalStart);
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 17, 0, 0, TimeSpan.FromHours(1)), shift.LocalEnd);
        Assert.Null(shift.AssignedTo);
        Assert.False(shift.IsStaffed);
    }

    [Fact]
    public void An_eight_hour_night_across_spring_forward_lasts_seven_hours() {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);

        // Act
        Shift shift = new Shift(1, paris, Kit.Request(paris, new DateOnly(2026, 3, 28), "22:00", "06:00"));

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), shift.WallClockLength);
        Assert.Equal(TimeSpan.FromHours(7), shift.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(-1), shift.DaylightSavingAdjustment);
        Assert.True(shift.CrossesTransition);
        Assert.Equal(TimeSpan.FromHours(1), shift.LocalStart.Offset);
        Assert.Equal(TimeSpan.FromHours(2), shift.LocalEnd.Offset);
    }

    [Fact]
    public void An_eight_hour_night_across_fall_back_lasts_nine_hours() {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);

        // Act
        Shift shift = new Shift(1, paris, Kit.Request(paris, new DateOnly(2026, 10, 24), "22:00", "06:00"));

        // Assert
        Assert.Equal(TimeSpan.FromHours(9), shift.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(1), shift.DaylightSavingAdjustment);
        Assert.True(shift.CrossesTransition);
    }

    [Fact]
    public void A_day_shift_on_the_day_of_the_transition_is_not_adjusted() {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);

        // Act
        Shift springDay = new Shift(1, paris, Kit.Request(paris, new DateOnly(2026, 3, 29), "08:00", "16:00"));
        Shift autumnDay = new Shift(2, paris, Kit.Request(paris, new DateOnly(2026, 10, 25), "08:00", "16:00"));

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), springDay.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(8), autumnDay.ElapsedDuration);
        Assert.False(springDay.CrossesTransition);
        Assert.False(autumnDay.CrossesTransition);
    }

    [Fact]
    public void A_shift_that_starts_in_the_skipped_hour_is_refused() {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Shift(1, paris, Kit.Request(paris, new DateOnly(2026, 3, 29), "02:30", "10:30", OffsetChoice.Earlier)));
    }

    [Fact]
    public void A_shift_that_ends_in_the_skipped_hour_is_refused() {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Shift(1, paris, Kit.Request(paris, new DateOnly(2026, 3, 28), "20:00", "02:30", OffsetChoice.Earlier, OffsetChoice.Later)));
    }

    [Fact]
    public void A_shift_that_starts_in_the_repeated_hour_needs_a_choice() {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Shift(1, paris, Kit.Request(paris, new DateOnly(2026, 10, 25), "02:30", "08:00")));
    }

    [Fact]
    public void A_shift_that_ends_in_the_repeated_hour_needs_a_choice() {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Shift(1, paris, Kit.Request(paris, new DateOnly(2026, 10, 24), "20:00", "02:30")));
    }

    [Theory]
    [InlineData(OffsetChoice.Earlier, 6.5)]
    [InlineData(OffsetChoice.Later, 5.5)]
    public void The_choice_for_a_repeated_start_decides_how_long_the_shift_really_lasts(OffsetChoice startChoice, double expectedHours) {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);

        // Act
        Shift shift = new Shift(1, paris, Kit.Request(paris, new DateOnly(2026, 10, 25), "02:30", "08:00", startChoice));

        // Assert: wall clock says 5 h 30 whichever occurrence of 02:30 was meant
        Assert.Equal(TimeSpan.FromHours(5.5), shift.WallClockLength);
        Assert.Equal(TimeSpan.FromHours(expectedHours), shift.ElapsedDuration);
    }

    [Fact]
    public void Choices_that_put_the_end_before_the_start_are_refused() {
        // Arrange: Troll station turns its clocks back two hours, so 01:00 to 02:00 can be read as ending before it starts
        Site troll = Kit.SiteIn(new SiteZone("Antarctica/Troll"));
        ShiftRequest request = Kit.Request(troll, new DateOnly(2026, 10, 25), "01:00", "02:00", OffsetChoice.Later, OffsetChoice.Earlier);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Shift(1, troll, request));
    }

    [Fact]
    public void Two_shifts_overlap_when_they_share_time_but_not_when_one_ends_as_the_other_starts() {
        // Arrange
        Site  tokyo    = Kit.SiteIn(Kit.Tokyo);
        Shift morning  = new Shift(1, tokyo, Kit.Request(tokyo, new DateOnly(2026, 1, 12), "08:00", "12:00"));
        Shift crossing = new Shift(2, tokyo, Kit.Request(tokyo, new DateOnly(2026, 1, 12), "11:59", "15:00"));
        Shift touching = new Shift(3, tokyo, Kit.Request(tokyo, new DateOnly(2026, 1, 12), "12:00", "16:00"));

        // Assert
        Assert.True(morning.Overlaps(crossing));
        Assert.True(crossing.Overlaps(morning));
        Assert.False(morning.Overlaps(touching));
        Assert.False(touching.Overlaps(morning));
    }

    [Fact]
    public void Overlap_is_judged_on_instants_not_on_local_times() {
        // Arrange: 09:00-17:00 in Paris is 08:00-16:00 UTC; 17:00-22:00 in Tokyo is 08:00-13:00 UTC
        List<Site> sites = Kit.OfficesIn(Kit.Paris, Kit.Tokyo);
        Shift inParis    = new Shift(1, sites[0], Kit.Request(sites[0], new DateOnly(2026, 1, 12), "09:00", "17:00"));
        Shift inTokyo    = new Shift(2, sites[1], Kit.Request(sites[1], new DateOnly(2026, 1, 12), "17:00", "22:00"));

        // Assert
        Assert.True(inParis.Overlaps(inTokyo));
    }

    [Fact]
    public void Rest_between_two_shifts_is_the_elapsed_time_from_the_end_of_the_earlier_to_the_start_of_the_later() {
        // Arrange
        Site  tokyo  = Kit.SiteIn(Kit.Tokyo);
        Shift first  = new Shift(1, tokyo, Kit.Request(tokyo, new DateOnly(2026, 1, 12), "08:00", "16:00"));
        Shift second = new Shift(2, tokyo, Kit.Request(tokyo, new DateOnly(2026, 1, 13), "03:30", "11:30"));

        // Assert
        Assert.Equal(TimeSpan.FromHours(11.5), first.RestBetween(second));
        Assert.Equal(TimeSpan.FromHours(11.5), second.RestBetween(first));
    }

    [Fact]
    public void Rest_between_shifts_that_overlap_is_zero_and_between_shifts_that_touch_is_zero_too() {
        // Arrange
        Site  tokyo    = Kit.SiteIn(Kit.Tokyo);
        Shift first    = new Shift(1, tokyo, Kit.Request(tokyo, new DateOnly(2026, 1, 12), "08:00", "12:00"));
        Shift crossing = new Shift(2, tokyo, Kit.Request(tokyo, new DateOnly(2026, 1, 12), "10:00", "14:00"));
        Shift touching = new Shift(3, tokyo, Kit.Request(tokyo, new DateOnly(2026, 1, 12), "12:00", "14:00"));

        // Assert
        Assert.Equal(TimeSpan.Zero, first.RestBetween(crossing));
        Assert.Equal(TimeSpan.Zero, first.RestBetween(touching));
    }

    [Fact]
    public void Rest_is_measured_in_elapsed_time_when_a_transition_falls_between_the_shifts() {
        // Arrange: 18:00 to 05:00 is 11 h on the wall clock, 10 h elapsed across spring-forward
        Site  paris = Kit.SiteIn(Kit.Paris);
        Shift first  = new Shift(1, paris, Kit.Request(paris, new DateOnly(2026, 3, 28), "10:00", "18:00"));
        Shift second = new Shift(2, paris, Kit.Request(paris, new DateOnly(2026, 3, 29), "05:00", "13:00"));

        // Assert
        Assert.Equal(TimeSpan.FromHours(10), first.RestBetween(second));
    }

    [Fact]
    public void Another_shift_is_required_to_compare_with() {
        // Arrange
        Site  tokyo = Kit.SiteIn(Kit.Tokyo);
        Shift shift = new Shift(1, tokyo, Kit.Request(tokyo, new DateOnly(2026, 1, 12), "08:00", "12:00"));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => shift.Overlaps(null!));
        Assert.Throws<ArgumentNullException>(() => shift.RestBetween(null!));
    }

    [Fact]
    public void A_shift_prints_its_number_its_request_and_its_elapsed_hours() {
        // Arrange
        Site paris = Kit.SiteIn(Kit.Paris);
        Shift shift = new Shift(7, paris, Kit.Request(paris, new DateOnly(2026, 3, 28), "22:00", "06:00"));

        // Assert
        Assert.Equal($"#7 {paris.Id} 2026-03-28 22:00-06:00 (7 h elapsed)", shift.ToString());
    }

}
