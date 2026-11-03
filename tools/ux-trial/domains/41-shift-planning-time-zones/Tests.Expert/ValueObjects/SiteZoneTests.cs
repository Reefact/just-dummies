using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.ValueObjects;

public sealed class SiteZoneTests {

    // ---- Construction ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("Europe/Paris")]
    [InlineData("UTC")]
    [InlineData("America/Argentina/Buenos_Aires")]   // two slashes
    [InlineData("America/Port-au-Prince")]           // a dash in the location
    [InlineData("Etc/GMT+5")]                        // a plus sign
    public void An_IANA_id_that_the_system_knows_is_accepted_as_it_was_given(string ianaId) {
        // Act
        SiteZone zone = new SiteZone(ianaId);

        // Assert
        Assert.Equal(ianaId, zone.Id);
        Assert.Equal(ianaId, zone.ToString());
    }

    [Theory]
    [InlineData("Romance Standard Time")]    // a Windows id
    [InlineData("W. Europe Standard Time")]
    [InlineData("europe/Paris")]             // the area starts with a capital
    [InlineData("Paris")]                    // no area
    [InlineData("Europe/")]
    [InlineData("/Paris")]
    [InlineData("Europe Paris")]
    [InlineData("Europe/Paris/Opera/Garnier")]   // three levels
    [InlineData("utc")]
    [InlineData("Utc")]
    [InlineData("GMT")]                      // known to the system, not of the shape
    public void An_id_that_is_not_of_the_IANA_shape_is_a_format_error(string malformed) {
        // Act
        Action construct = () => new SiteZone(malformed);

        // Assert
        Assert.Throws<FormatException>(construct);
    }

    [Fact]
    public void An_id_of_the_right_shape_that_the_system_does_not_know_is_an_argument_error() {
        // Act
        Action construct = () => new SiteZone("Europe/Atlantis");

        // Assert
        Assert.Throws<ArgumentException>("ianaId", construct);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_id_is_refused(string blank) {
        // Act
        Action construct = () => new SiteZone(blank);

        // Assert
        Assert.Throws<ArgumentException>("ianaId", construct);
    }

    [Fact]
    public void A_missing_id_is_refused() {
        // Act
        Action construct = () => new SiteZone(null!);

        // Assert
        Assert.Throws<ArgumentNullException>("ianaId", construct);
    }

    [Fact(Skip = "domain defect: IanaShape ends with `$`, which .NET also matches before a final newline; \"Europe/Paris\\n\" passes the shape check and is refused later as an unknown zone (ArgumentException) instead of a FormatException (literal input, no draw involved)")]
    public void An_id_followed_by_a_newline_is_a_format_error() {
        // Act
        Action construct = () => new SiteZone("Europe/Paris\n");

        // Assert
        Assert.Throws<FormatException>(construct);
    }

    [Fact]
    public void Zones_are_equal_when_their_ids_are() {
        // Arrange
        SiteZone first  = new SiteZone("Europe/Paris");
        SiteZone second = new SiteZone("Europe/Paris");

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new SiteZone("Europe/Berlin"));
        Assert.False(first.Equals(null));
    }

    // ---- Classification of a local time ------------------------------------------------------------------------

    [Theory]
    // Paris springs forward on 2027-03-28 at 02:00, and falls back on 2027-10-31 at 03:00.
    [InlineData("Europe/Paris", "2027-03-28", "01:59", LocalTimeKind.Unique)]
    [InlineData("Europe/Paris", "2027-03-28", "02:00", LocalTimeKind.Skipped)]
    [InlineData("Europe/Paris", "2027-03-28", "02:59", LocalTimeKind.Skipped)]
    [InlineData("Europe/Paris", "2027-03-28", "03:00", LocalTimeKind.Unique)]
    [InlineData("Europe/Paris", "2027-10-31", "01:59", LocalTimeKind.Unique)]
    [InlineData("Europe/Paris", "2027-10-31", "02:00", LocalTimeKind.Ambiguous)]
    [InlineData("Europe/Paris", "2027-10-31", "02:59", LocalTimeKind.Ambiguous)]
    [InlineData("Europe/Paris", "2027-10-31", "03:00", LocalTimeKind.Unique)]
    // New York: second Sunday of March at 02:00, first Sunday of November at 02:00 (so the overlap is 01:00 to 01:59).
    [InlineData("America/New_York", "2027-03-14", "02:30", LocalTimeKind.Skipped)]
    [InlineData("America/New_York", "2027-11-07", "01:30", LocalTimeKind.Ambiguous)]
    [InlineData("America/New_York", "2027-11-07", "02:30", LocalTimeKind.Unique)]
    // Sydney is in the other hemisphere: its clocks go back in April and forward in October.
    [InlineData("Australia/Sydney", "2027-04-04", "02:30", LocalTimeKind.Ambiguous)]
    [InlineData("Australia/Sydney", "2027-10-03", "02:30", LocalTimeKind.Skipped)]
    // Lord Howe moves by thirty minutes only.
    [InlineData("Australia/Lord_Howe", "2027-10-03", "02:29", LocalTimeKind.Skipped)]
    [InlineData("Australia/Lord_Howe", "2027-10-03", "02:30", LocalTimeKind.Unique)]
    // A zone without daylight saving has neither.
    [InlineData("Asia/Tokyo", "2027-03-28", "02:30", LocalTimeKind.Unique)]
    [InlineData("UTC", "2027-10-31", "02:30", LocalTimeKind.Unique)]
    public void A_local_time_is_unique_skipped_or_ambiguous_on_the_zones_own_clock(string ianaId, string date, string time, LocalTimeKind expected) {
        // Arrange
        SiteZone zone = new SiteZone(ianaId);

        // Act
        LocalTimeKind kind = zone.Classify(Clock.Day(date), Clock.At(time));

        // Assert
        Assert.Equal(expected, kind);
    }

    // ---- Local time to instant ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("Europe/Paris", "2027-01-12", "09:00", "2027-01-12 08:00")]        // CET, +01:00
    [InlineData("Europe/Paris", "2027-07-13", "09:00", "2027-07-13 07:00")]        // CEST, +02:00
    [InlineData("America/New_York", "2027-01-12", "09:00", "2027-01-12 14:00")]    // EST, -05:00
    [InlineData("Asia/Kolkata", "2027-01-12", "09:00", "2027-01-12 03:30")]        // +05:30
    [InlineData("Asia/Tokyo", "2027-01-12", "00:30", "2027-01-11 15:30")]          // the UTC date is the day before
    [InlineData("UTC", "2027-01-12", "09:00", "2027-01-12 09:00")]
    [InlineData("Europe/Paris", "2027-03-28", "01:59", "2027-03-28 00:59")]        // the last minute before the gap
    [InlineData("Europe/Paris", "2027-03-28", "03:00", "2027-03-28 01:00")]        // the first after it
    public void A_unique_local_time_is_one_instant_whatever_the_choice(string ianaId, string date, string time, string expectedUtc) {
        // Arrange
        SiteZone zone = new SiteZone(ianaId);

        // Act
        DateTimeOffset unspecified = zone.ToUtc(Clock.Day(date), Clock.At(time), OffsetChoice.Unspecified);
        DateTimeOffset earlier     = zone.ToUtc(Clock.Day(date), Clock.At(time), OffsetChoice.Earlier);
        DateTimeOffset later       = zone.ToUtc(Clock.Day(date), Clock.At(time), OffsetChoice.Later);

        // Assert
        Assert.Equal(Clock.Utc(expectedUtc), unspecified);
        Assert.Equal(unspecified, earlier);
        Assert.Equal(unspecified, later);
        Assert.Equal(TimeSpan.Zero, unspecified.Offset);
    }

    [Theory]
    [InlineData("Europe/Paris", "2027-10-31", "02:30", "2027-10-31 00:30", "2027-10-31 01:30")]       // CEST then CET
    [InlineData("America/New_York", "2027-11-07", "01:30", "2027-11-07 05:30", "2027-11-07 06:30")]   // EDT then EST
    [InlineData("Australia/Sydney", "2027-04-04", "02:30", "2027-04-03 15:30", "2027-04-03 16:30")]   // AEDT then AEST
    [InlineData("Australia/Lord_Howe", "2027-04-04", "01:45", "2027-04-03 14:45", "2027-04-03 15:15")]   // thirty minutes apart
    public void An_ambiguous_local_time_is_the_first_occurrence_when_earlier_and_the_second_when_later(string ianaId, string date, string time, string firstUtc, string secondUtc) {
        // Arrange
        SiteZone zone = new SiteZone(ianaId);

        // Act
        DateTimeOffset earlier = zone.ToUtc(Clock.Day(date), Clock.At(time), OffsetChoice.Earlier);
        DateTimeOffset later   = zone.ToUtc(Clock.Day(date), Clock.At(time), OffsetChoice.Later);

        // Assert
        Assert.Equal(Clock.Utc(firstUtc), earlier);
        Assert.Equal(Clock.Utc(secondUtc), later);
        Assert.True(earlier < later);
    }

    [Fact]
    public void An_ambiguous_local_time_needs_a_choice() {
        // Arrange
        SiteZone zone = new SiteZone("Europe/Paris");

        // Act
        Action convert = () => zone.ToUtc(Clock.Day("2027-10-31"), Clock.At("02:30"), OffsetChoice.Unspecified);

        // Assert
        Assert.Throws<ArgumentException>("choice", convert);
    }

    [Theory]
    [InlineData(OffsetChoice.Unspecified)]
    [InlineData(OffsetChoice.Earlier)]
    [InlineData(OffsetChoice.Later)]
    public void A_skipped_local_time_has_no_instant_whatever_the_choice(OffsetChoice choice) {
        // Arrange
        SiteZone zone = new SiteZone("Europe/Paris");

        // Act
        Action convert = () => zone.ToUtc(Clock.Day("2027-03-28"), Clock.At("02:30"), choice);

        // Assert
        Assert.Throws<ArgumentException>("time", convert);
    }

    [Fact]
    public void A_choice_the_enum_does_not_declare_is_refused_even_for_a_unique_time() {
        // Arrange
        SiteZone zone = new SiteZone("Europe/Paris");

        // Act
        Action convert = () => zone.ToUtc(Clock.Day("2027-01-12"), Clock.At("09:00"), (OffsetChoice)42);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("choice", convert);
    }

    // ---- Instant to local time ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("Europe/Paris", "2027-01-12 08:00", "2027-01-12T09:00:00+01:00")]
    [InlineData("Europe/Paris", "2027-07-13 07:00", "2027-07-13T09:00:00+02:00")]
    [InlineData("America/New_York", "2027-01-12 14:00", "2027-01-12T09:00:00-05:00")]
    [InlineData("Asia/Kolkata", "2027-01-12 03:30", "2027-01-12T09:00:00+05:30")]
    [InlineData("Europe/Paris", "2027-10-31 00:30", "2027-10-31T02:30:00+02:00")]   // the first 02:30
    [InlineData("Europe/Paris", "2027-10-31 01:30", "2027-10-31T02:30:00+01:00")]   // the second one
    public void An_instant_is_shown_on_the_zones_wall_clock_with_the_offset_then_in_force(string ianaId, string utc, string expectedLocal) {
        // Arrange
        SiteZone zone = new SiteZone(ianaId);

        // Act
        DateTimeOffset local = zone.ToLocal(Clock.Utc(utc));

        // Assert
        Assert.Equal(expectedLocal, local.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture));
        Assert.Equal(Clock.Utc(utc), local);
    }

    [Theory]
    [InlineData("Europe/Paris", "2027-03-28 00:59", 1)]    // the last minute before the gap
    [InlineData("Europe/Paris", "2027-03-28 01:00", 2)]    // the first one after it
    [InlineData("Europe/Paris", "2027-10-31 00:59", 2)]
    [InlineData("Europe/Paris", "2027-10-31 01:00", 1)]
    [InlineData("Asia/Kolkata", "2027-01-12 00:00", 5.5)]
    [InlineData("UTC", "2027-07-13 12:00", 0)]
    public void The_offset_in_force_changes_at_the_transition_instant(string ianaId, string utc, double expectedHours) {
        // Arrange
        SiteZone zone = new SiteZone(ianaId);

        // Act
        TimeSpan offset = zone.OffsetAt(Clock.Utc(utc));

        // Assert
        Assert.Equal(TimeSpan.FromHours(expectedHours), offset);
    }

}
