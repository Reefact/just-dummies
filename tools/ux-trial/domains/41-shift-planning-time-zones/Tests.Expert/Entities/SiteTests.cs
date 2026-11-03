using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.Entities;

public sealed class SiteTests {

    // ---- Construction ------------------------------------------------------------------------------------------

    [Fact]
    public void A_site_keeps_what_it_was_given() {
        // Arrange
        SiteId       anyId    = Any.SiteId().Generate();
        SiteZone     anyZone  = Any.SiteZone().Generate();
        WorkingHours anyHours = Any.WorkingHours().Generate();

        // Act
        Site site = new Site(anyId, "Lyon Part-Dieu", anyZone, anyHours);

        // Assert
        Assert.Same(anyId, site.Id);
        Assert.Same(anyZone, site.Zone);
        Assert.Same(anyHours, site.WorkingHours);
        Assert.Equal("Lyon Part-Dieu", site.Name);
    }

    [Fact]
    public void A_name_of_eighty_characters_is_the_longest_allowed() {
        // Arrange
        string longestName = new string('n', 80);

        // Act
        Site site = Any.Site().WithName(longestName).Generate();

        // Assert
        Assert.Equal(80, site.Name.Length);
    }

    [Fact]
    public void A_name_of_more_than_eighty_characters_is_refused() {
        // Arrange
        string tooLongName = new string('n', 81);

        // Act
        Action construct = () => Any.Site().WithName(tooLongName).Generate();

        // Assert
        Assert.Throws<ArgumentException>("name", construct);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_name_is_refused(string blank) {
        // Act
        Action construct = () => Any.Site().WithName(blank).Generate();

        // Assert
        Assert.Throws<ArgumentException>("name", construct);
    }

    [Fact]
    public void A_missing_part_is_refused() {
        // Arrange
        SiteId       anyId    = Any.SiteId().Generate();
        SiteZone     anyZone  = Any.SiteZone().Generate();
        WorkingHours anyHours = Any.WorkingHours().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("id", () => new Site(null!, "Lyon", anyZone, anyHours));
        Assert.Throws<ArgumentNullException>("name", () => new Site(anyId, null!, anyZone, anyHours));
        Assert.Throws<ArgumentNullException>("zone", () => new Site(anyId, "Lyon", null!, anyHours));
        Assert.Throws<ArgumentNullException>("workingHours", () => new Site(anyId, "Lyon", anyZone, null!));
    }

    // ---- Working hours read on the site's own clock ------------------------------------------------------------

    private static Site ParisOffice() {
        return Any.Site().WithZone(zone => zone.WithIanaId("Europe/Paris")).WithWorkingHours(Hours.NineToFive()).Generate();
    }

    [Theory]
    [InlineData("2027-01-12 08:00", "2027-01-12 16:00", true)]    // Tuesday, 09:00 to 17:00 in Paris (UTC+1): the whole opening, ends included
    [InlineData("2027-01-12 07:59", "2027-01-12 16:00", false)]   // opens a minute early
    [InlineData("2027-01-12 08:00", "2027-01-12 16:01", false)]   // closes a minute late
    [InlineData("2027-01-16 08:00", "2027-01-16 16:00", false)]   // a Saturday
    [InlineData("2027-07-13 07:00", "2027-07-13 15:00", true)]    // in summer (UTC+2) the same wall clock is two hours earlier on the UTC clock
    [InlineData("2027-07-13 08:00", "2027-07-13 16:00", false)]   // so the winter instants are an hour late
    public void An_interval_is_within_working_hours_when_its_ends_read_inside_them_on_the_site_clock(string startUtc, string endUtc, bool expected) {
        // Arrange
        Site paris = ParisOffice();

        // Act
        bool within = paris.IsWithinWorkingHours(Clock.Utc(startUtc), Clock.Utc(endUtc));

        // Assert
        Assert.Equal(expected, within);
    }

    [Fact]
    public void The_same_instants_fall_inside_the_hours_of_one_site_and_outside_the_hours_of_another() {
        // Arrange
        Site paris = ParisOffice();
        Site tokyo = Any.Site().WithZone(zone => zone.WithIanaId("Asia/Tokyo")).WithWorkingHours(Hours.NineToFive()).Generate();
        DateTimeOffset start = Clock.Utc("2027-01-12 00:00");   // 01:00 in Paris, 09:00 in Tokyo
        DateTimeOffset end   = Clock.Utc("2027-01-12 08:00");

        // Act
        bool parisWithin = paris.IsWithinWorkingHours(start, end);
        bool tokyoWithin = tokyo.IsWithinWorkingHours(start, end);

        // Assert
        Assert.False(parisWithin);
        Assert.True(tokyoWithin);
    }

    [Fact]
    public void An_interval_ending_before_it_starts_is_refused() {
        // Arrange
        Site paris = ParisOffice();

        // Act
        Action check = () => paris.IsWithinWorkingHours(Clock.Utc("2027-01-12 10:00"), Clock.Utc("2027-01-12 09:59"));

        // Assert
        Assert.Throws<ArgumentException>("end", check);
    }

}
