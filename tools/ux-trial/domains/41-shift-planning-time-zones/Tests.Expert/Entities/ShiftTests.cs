using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.Entities;

public sealed class ShiftTests {

    private static Site SiteIn(string zoneId) {
        return Any.Site().WithZone(zone => zone.WithIanaId(zoneId)).Generate();
    }

    private static Shift ShiftAt(Site site, string date, string start, string end, OffsetChoice startChoice = OffsetChoice.Unspecified, OffsetChoice endChoice = OffsetChoice.Unspecified) {
        return new Shift(1, site, new ShiftRequest(site.Id, Clock.Day(date), Clock.At(start), Clock.At(end), startChoice, endChoice));
    }

    // ---- An ordinary day ---------------------------------------------------------------------------------------

    [Fact]
    public void A_shift_is_stored_as_the_UTC_instants_of_its_local_ends() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Shift shift = ShiftAt(paris, "2027-01-12", "09:00", "17:00");

        // Assert
        Assert.Equal(Clock.Utc("2027-01-12 08:00"), shift.StartUtc);
        Assert.Equal(Clock.Utc("2027-01-12 16:00"), shift.EndUtc);
        Assert.Equal(TimeSpan.Zero, shift.StartUtc.Offset);
        Assert.Equal(TimeSpan.Zero, shift.EndUtc.Offset);
        Assert.Equal(TimeSpan.FromHours(8), shift.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(8), shift.WallClockLength);
        Assert.Equal(TimeSpan.Zero, shift.DaylightSavingAdjustment);
        Assert.False(shift.CrossesTransition);
    }

    [Fact]
    public void A_shift_reads_back_on_the_site_clock_with_the_offset_then_in_force() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Shift shift = ShiftAt(paris, "2027-07-13", "09:00", "17:00");

        // Assert
        Assert.Equal(new DateTime(2027, 7, 13, 9, 0, 0), shift.LocalStart.DateTime);
        Assert.Equal(new DateTime(2027, 7, 13, 17, 0, 0), shift.LocalEnd.DateTime);
        Assert.Equal(TimeSpan.FromHours(2), shift.LocalStart.Offset);
        Assert.Equal(TimeSpan.FromHours(2), shift.LocalEnd.Offset);
    }

    [Fact]
    public void A_new_shift_has_nobody_assigned() {
        // Arrange
        Site anySite = Any.Site().Generate();
        ShiftRequest anyRequest = QuietPeriod.AnyRequestAt(anySite.Id);

        // Act
        Shift shift = new Shift(1, anySite, anyRequest);

        // Assert
        Assert.Null(shift.AssignedTo);
        Assert.False(shift.IsStaffed);
        Assert.Same(anyRequest, shift.Request);
        Assert.Same(anySite, shift.Site);
    }

    // ---- Across a transition -----------------------------------------------------------------------------------

    [Fact]
    public void An_eight_hour_night_across_spring_forward_lasts_seven_hours() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");   // 2027-03-28, 02:00 becomes 03:00

        // Act
        Shift shift = ShiftAt(paris, "2027-03-27", "22:00", "06:00");

        // Assert
        Assert.Equal(Clock.Utc("2027-03-27 21:00"), shift.StartUtc);   // 22:00 CET
        Assert.Equal(Clock.Utc("2027-03-28 04:00"), shift.EndUtc);     // 06:00 CEST
        Assert.Equal(TimeSpan.FromHours(7), shift.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(8), shift.WallClockLength);
        Assert.Equal(TimeSpan.FromHours(-1), shift.DaylightSavingAdjustment);
        Assert.True(shift.CrossesTransition);
    }

    [Fact]
    public void An_eight_hour_night_across_fall_back_lasts_nine_hours() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");   // 2027-10-31, 03:00 becomes 02:00

        // Act
        Shift shift = ShiftAt(paris, "2027-10-30", "22:00", "06:00");

        // Assert
        Assert.Equal(Clock.Utc("2027-10-30 20:00"), shift.StartUtc);   // 22:00 CEST
        Assert.Equal(Clock.Utc("2027-10-31 05:00"), shift.EndUtc);     // 06:00 CET
        Assert.Equal(TimeSpan.FromHours(9), shift.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(8), shift.WallClockLength);
        Assert.Equal(TimeSpan.FromHours(1), shift.DaylightSavingAdjustment);
        Assert.True(shift.CrossesTransition);
    }

    [Fact]
    public void A_day_shift_on_the_day_of_the_transition_that_does_not_span_it_is_unchanged() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Shift shift = ShiftAt(paris, "2027-03-28", "08:00", "16:00");

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), shift.ElapsedDuration);
        Assert.False(shift.CrossesTransition);
    }

    [Fact]
    public void A_shift_that_ends_right_after_the_gap_loses_the_hour_of_the_gap() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Shift shift = ShiftAt(paris, "2027-03-28", "01:00", "03:00");   // 01:00 CET to 03:00 CEST

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), shift.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(2), shift.WallClockLength);
        Assert.True(shift.CrossesTransition);
    }

    [Fact]
    public void A_night_in_a_zone_without_daylight_saving_never_crosses_a_transition() {
        // Arrange
        Site tokyo = SiteIn("Asia/Tokyo");

        // Act
        Shift marchNight   = ShiftAt(tokyo, "2027-03-27", "22:00", "06:00");
        Shift octoberNight = ShiftAt(tokyo, "2027-10-30", "22:00", "06:00");

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), marchNight.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(8), octoberNight.ElapsedDuration);
        Assert.False(marchNight.CrossesTransition);
        Assert.False(octoberNight.CrossesTransition);
    }

    [Fact]
    public void The_same_wall_clock_night_is_longer_or_shorter_depending_on_the_hemisphere() {
        // Arrange
        Site sydney = SiteIn("Australia/Sydney");   // clocks go back on 2027-04-04 and forward on 2027-10-03

        // Act
        Shift autumnNight = ShiftAt(sydney, "2027-04-03", "22:00", "06:00");
        Shift springNight = ShiftAt(sydney, "2027-10-02", "22:00", "06:00");

        // Assert
        Assert.Equal(TimeSpan.FromHours(9), autumnNight.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(7), springNight.ElapsedDuration);
    }

    // ---- Local times that cannot be placed on the timeline -----------------------------------------------------

    [Fact]
    public void A_start_in_the_gap_is_refused() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Action plan = () => ShiftAt(paris, "2027-03-28", "02:30", "08:30");

        // Assert
        Assert.Throws<ArgumentException>("time", plan);
    }

    [Fact]
    public void An_end_in_the_gap_is_refused() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Action plan = () => ShiftAt(paris, "2027-03-27", "22:30", "02:30");

        // Assert
        Assert.Throws<ArgumentException>("time", plan);
    }

    [Fact]
    public void An_ambiguous_start_needs_a_choice() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Action plan = () => ShiftAt(paris, "2027-10-31", "02:30", "08:30");

        // Assert
        Assert.Throws<ArgumentException>("choice", plan);
    }

    [Fact]
    public void An_ambiguous_start_taken_earlier_makes_a_longer_shift_than_taken_later() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Shift fromFirst02h30  = ShiftAt(paris, "2027-10-31", "02:30", "08:30", startChoice: OffsetChoice.Earlier);
        Shift fromSecond02h30 = ShiftAt(paris, "2027-10-31", "02:30", "08:30", startChoice: OffsetChoice.Later);

        // Assert
        Assert.Equal(Clock.Utc("2027-10-31 00:30"), fromFirst02h30.StartUtc);
        Assert.Equal(TimeSpan.FromHours(7), fromFirst02h30.ElapsedDuration);
        Assert.Equal(Clock.Utc("2027-10-31 01:30"), fromSecond02h30.StartUtc);
        Assert.Equal(TimeSpan.FromHours(6), fromSecond02h30.ElapsedDuration);
    }

    [Fact]
    public void An_ambiguous_end_taken_later_makes_a_longer_shift_than_taken_earlier() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Shift untilFirst02h30  = ShiftAt(paris, "2027-10-31", "00:30", "02:30", endChoice: OffsetChoice.Earlier);
        Shift untilSecond02h30 = ShiftAt(paris, "2027-10-31", "00:30", "02:30", endChoice: OffsetChoice.Later);

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), untilFirst02h30.ElapsedDuration);
        Assert.Equal(TimeSpan.FromHours(3), untilSecond02h30.ElapsedDuration);
    }

    [Fact]
    public void An_ambiguous_end_needs_a_choice() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");

        // Act
        Action plan = () => ShiftAt(paris, "2027-10-31", "00:30", "02:30");

        // Assert
        Assert.Throws<ArgumentException>("choice", plan);
    }

    [Fact]
    public void Choices_that_turn_the_shift_back_on_itself_are_refused() {
        // Arrange
        // Troll station falls back two hours at 2027-10-31 01:00 UTC: the wall clock shows 01:00 to 02:59 twice,
        // so a one-hour shift 01:00-02:00 can be made to end before it starts.
        Site troll = SiteIn("Antarctica/Troll");

        // Act
        Action plan = () => ShiftAt(troll, "2027-10-31", "01:00", "02:00", startChoice: OffsetChoice.Later, endChoice: OffsetChoice.Earlier);

        // Assert
        Assert.Throws<ArgumentException>("request", plan);
    }

    // ---- Arguments ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_request_for_another_site_is_refused() {
        // Arrange
        Site anySite = Any.Site().Generate();
        SiteId otherSite = Any.SiteId().DifferentFrom(anySite.Id).Generate();
        ShiftRequest requestElsewhere = QuietPeriod.AnyRequestAt(otherSite);

        // Act
        Action plan = () => new Shift(1, anySite, requestElsewhere);

        // Assert
        Assert.Throws<ArgumentException>("site", plan);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_shift_number_starts_at_one(int number) {
        // Arrange
        Site anySite = Any.Site().Generate();
        ShiftRequest anyRequest = QuietPeriod.AnyRequestAt(anySite.Id);

        // Act
        Action plan = () => new Shift(number, anySite, anyRequest);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("number", plan);
    }

    [Fact]
    public void A_missing_site_or_request_is_refused() {
        // Arrange
        Site anySite = Any.Site().Generate();
        ShiftRequest anyRequest = QuietPeriod.AnyRequestAt(anySite.Id);

        // Assert
        Assert.Throws<ArgumentNullException>("site", () => new Shift(1, null!, anyRequest));
        Assert.Throws<ArgumentNullException>("request", () => new Shift(1, anySite, null!));
    }

    // ---- Overlap and rest, on the one timeline -----------------------------------------------------------------

    [Fact]
    public void Shifts_on_two_sites_overlap_when_their_instants_do_not_their_wall_clocks() {
        // Arrange
        Shift inParis    = ShiftAt(SiteIn("Europe/Paris"), "2027-01-12", "08:00", "16:00");       // 07:00Z to 15:00Z
        Shift inNewYork  = ShiftAt(SiteIn("America/New_York"), "2027-01-12", "09:00", "17:00");   // 14:00Z to 22:00Z

        // Assert
        Assert.True(inParis.Overlaps(inNewYork));
        Assert.True(inNewYork.Overlaps(inParis));
        Assert.Equal(TimeSpan.Zero, inParis.RestBetween(inNewYork));
        Assert.Equal(TimeSpan.Zero, inNewYork.RestBetween(inParis));
    }

    [Fact]
    public void Shifts_that_touch_do_not_overlap_and_leave_no_rest() {
        // Arrange
        Site paris = SiteIn("Europe/Paris");
        Shift morning   = ShiftAt(paris, "2027-01-12", "06:00", "14:00");
        Shift afternoon = ShiftAt(paris, "2027-01-12", "14:00", "22:00");

        // Assert
        Assert.False(morning.Overlaps(afternoon));
        Assert.False(afternoon.Overlaps(morning));
        Assert.Equal(TimeSpan.Zero, morning.RestBetween(afternoon));
    }

    [Fact]
    public void Rest_between_shifts_is_the_elapsed_time_from_the_end_of_the_earlier_to_the_start_of_the_later_in_either_order() {
        // Arrange
        Shift inParis = ShiftAt(SiteIn("Europe/Paris"), "2027-01-12", "08:00", "16:00");   // ends 15:00Z
        Shift inTokyo = ShiftAt(SiteIn("Asia/Tokyo"), "2027-01-13", "08:00", "16:00");     // starts 2027-01-12 23:00Z

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), inParis.RestBetween(inTokyo));
        Assert.Equal(TimeSpan.FromHours(8), inTokyo.RestBetween(inParis));
    }

    [Fact]
    public void Comparing_with_a_missing_shift_is_refused() {
        // Arrange
        Site anySite = Any.Site().Generate();
        Shift anyShift = QuietPeriod.AnyShiftAt(anySite);

        // Assert
        Assert.Throws<ArgumentNullException>("other", () => anyShift.Overlaps(null!));
        Assert.Throws<ArgumentNullException>("other", () => anyShift.RestBetween(null!));
    }

}
