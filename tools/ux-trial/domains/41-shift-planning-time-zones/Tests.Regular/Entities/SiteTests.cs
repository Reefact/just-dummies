using JustDummies;

using ShiftPlanning.Domain.Tests.Regular.Support;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.Entities;

public sealed class SiteTests {

    [Fact]
    public void A_site_remembers_its_identity_its_zone_and_its_hours() {
        // Arrange
        SiteId       anyId    = new AnySiteId().Generate();
        string       anyName  = Any.String().Alpha().WithLengthBetween(1, Site.MaxNameLength).Generate();
        SiteZone     anyZone  = new AnySiteZone().Generate();
        WorkingHours anyHours = new AnyWorkingHours().Generate();

        // Act
        Site site = new Site(anyId, anyName, anyZone, anyHours);

        // Assert
        Assert.Equal(anyId, site.Id);
        Assert.Equal(anyName, site.Name);
        Assert.Equal(anyZone, site.Zone);
        Assert.Equal(anyHours, site.WorkingHours);
    }

    [Fact]
    public void A_name_of_exactly_eighty_characters_is_accepted() {
        // Arrange
        string anyLongName = Any.String().Alpha().WithLength(Site.MaxNameLength).Generate();

        // Act & Assert
        Assert.Equal(anyLongName, new AnySite().WithName(anyLongName).Generate().Name);
    }

    [Fact]
    public void A_name_of_eighty_one_characters_is_refused() {
        // Arrange
        string anyTooLongName = Any.String().Alpha().WithLength(Site.MaxNameLength + 1).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AnySite().WithName(anyTooLongName).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new AnySite().WithName(name).Generate());
    }

    [Fact]
    public void Every_part_of_a_site_is_required() {
        // Arrange
        SiteId       anyId    = new AnySiteId().Generate();
        SiteZone     anyZone  = new AnySiteZone().Generate();
        WorkingHours anyHours = new AnyWorkingHours().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Site(null!, "Head office", anyZone, anyHours));
        Assert.ThrowsAny<ArgumentException>(() => new Site(anyId, null!, anyZone, anyHours));
        Assert.Throws<ArgumentNullException>(() => new Site(anyId, "Head office", null!, anyHours));
        Assert.Throws<ArgumentNullException>(() => new Site(anyId, "Head office", anyZone, null!));
    }

    [Theory]
    [InlineData("2026-01-12T08:00:00Z", "2026-01-12T17:00:00Z", true)]    // 09:00-18:00 in Paris (UTC+1)
    [InlineData("2026-01-12T07:59:00Z", "2026-01-12T12:00:00Z", false)]   // opens at 09:00 local
    [InlineData("2026-01-12T12:00:00Z", "2026-01-12T17:01:00Z", false)]   // closes at 18:00 local
    [InlineData("2026-07-13T07:00:00Z", "2026-07-13T16:00:00Z", true)]    // same hours, UTC+2 in summer
    [InlineData("2026-07-13T08:00:00Z", "2026-07-13T17:00:00Z", false)]   // 10:00-19:00 in summer
    [InlineData("2026-01-17T09:00:00Z", "2026-01-17T10:00:00Z", false)]   // Saturday
    [InlineData("2026-01-12T16:30:00Z", "2026-01-12T17:30:00Z", false)]   // ends after closing
    public void A_site_reads_an_interval_on_its_own_wall_clock(string start, string end, bool expected) {
        // Arrange
        Site paris = Kit.OfficeIn(Kit.Paris);

        // Act & Assert
        Assert.Equal(expected, paris.IsWithinWorkingHours(DateTimeOffset.Parse(start), DateTimeOffset.Parse(end)));
    }

    [Fact]
    public void The_same_instants_are_working_hours_in_one_zone_and_not_in_another() {
        // Arrange
        DateTimeOffset start = Kit.Utc_(2026, 1, 12, 8, 0);
        DateTimeOffset end   = Kit.Utc_(2026, 1, 12, 9, 0);

        // Act & Assert
        Assert.True(Kit.OfficeIn(Kit.Paris).IsWithinWorkingHours(start, end));
        Assert.False(Kit.OfficeIn(Kit.NewYork).IsWithinWorkingHours(start, end));
    }

    [Fact]
    public void An_interval_that_ends_before_it_starts_cannot_be_asked_about() {
        // Arrange
        Site anyOffice = Kit.OfficeIn(Kit.Paris);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => anyOffice.IsWithinWorkingHours(Kit.Utc_(2026, 1, 12, 9, 0), Kit.Utc_(2026, 1, 12, 8, 0)));
    }

}
