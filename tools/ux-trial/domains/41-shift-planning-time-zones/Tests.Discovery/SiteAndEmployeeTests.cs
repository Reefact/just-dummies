using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Discovery;

public sealed class SiteTests {

    private static readonly SiteZone Paris = new SiteZone("Europe/Paris");

    private static WorkingHours NineToFiveWeekdays() {
        return new WorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0), [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]);
    }

    [Fact]
    public void A_site_keeps_what_it_was_given() {
        // Arrange
        SiteId       id    = new AnySiteId().Generate();
        WorkingHours hours = NineToFiveWeekdays();

        // Act
        Site site = new Site(id, "Paris head office", Paris, hours);

        // Assert
        Assert.Equal(id, site.Id);
        Assert.Equal("Paris head office", site.Name);
        Assert.Equal(Paris, site.Zone);
        Assert.Equal(hours, site.WorkingHours);
    }

    [Fact]
    public void A_name_of_80_characters_is_accepted_and_one_of_81_is_refused() {
        // Assert
        Assert.Equal(80, new AnySite().WithName(new string('x', 80)).Generate().Name.Length);
        Assert.Throws<ArgumentException>(() => new AnySite().WithName(new string('x', 81)).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new AnySite().WithName(name).Generate());
    }

    [Fact]
    public void A_null_argument_is_refused() {
        // Arrange
        SiteId id = new AnySiteId().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Site(null!, "Paris", Paris, NineToFiveWeekdays()));
        Assert.Throws<ArgumentNullException>(() => new Site(id, null!, Paris, NineToFiveWeekdays()));
        Assert.Throws<ArgumentNullException>(() => new Site(id, "Paris", null!, NineToFiveWeekdays()));
        Assert.Throws<ArgumentNullException>(() => new Site(id, "Paris", Paris, null!));
    }

    [Fact]
    public void Working_hours_are_read_on_the_sites_own_clock_in_summer() {
        // Arrange
        Site paris = new AnySite().WithZone(Paris).WithWorkingHours(NineToFiveWeekdays()).Generate();

        // Act: Wednesday 2026-06-17, Paris is UTC+2
        bool opening = paris.IsWithinWorkingHours(new DateTimeOffset(2026, 6, 17, 7, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 17, 8, 0, 0, TimeSpan.Zero));
        bool tooEarly = paris.IsWithinWorkingHours(new DateTimeOffset(2026, 6, 17, 6, 59, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 17, 8, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.True(opening);
        Assert.False(tooEarly);
    }

    [Fact]
    public void The_same_instants_fall_outside_the_hours_in_winter_when_the_offset_is_one_hour_less() {
        // Arrange
        Site paris = new AnySite().WithZone(Paris).WithWorkingHours(NineToFiveWeekdays()).Generate();

        // Act: Wednesday 2026-01-14, Paris is UTC+1, so 07:00Z is 08:00 local
        bool inWinter = paris.IsWithinWorkingHours(new DateTimeOffset(2026, 1, 14, 7, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 14, 8, 0, 0, TimeSpan.Zero));
        bool shifted  = paris.IsWithinWorkingHours(new DateTimeOffset(2026, 1, 14, 8, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 14, 9, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.False(inWinter);
        Assert.True(shifted);
    }

    [Fact]
    public void The_closing_time_is_included() {
        // Arrange
        Site paris = new AnySite().WithZone(Paris).WithWorkingHours(NineToFiveWeekdays()).Generate();

        // Act: 17:00 local in summer is 15:00Z
        bool atClosing = paris.IsWithinWorkingHours(new DateTimeOffset(2026, 6, 17, 14, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 17, 15, 0, 0, TimeSpan.Zero));
        bool past      = paris.IsWithinWorkingHours(new DateTimeOffset(2026, 6, 17, 14, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 17, 15, 1, 0, TimeSpan.Zero));

        // Assert
        Assert.True(atClosing);
        Assert.False(past);
    }

    [Fact]
    public void Utc_day_and_local_day_can_differ() {
        // Arrange
        SiteZone     tokyo = new SiteZone("Asia/Tokyo");
        Site         site  = new AnySite().WithZone(tokyo).WithWorkingHours(NineToFiveWeekdays()).Generate();

        // Act: Wednesday 2026-06-17 00:00Z is already 09:00 on the same Wednesday in Tokyo; Sunday 2026-06-14 23:00Z is Monday 08:00 there
        bool openingWednesday = site.IsWithinWorkingHours(new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 17, 1, 0, 0, TimeSpan.Zero));
        bool beforeMonday     = site.IsWithinWorkingHours(new DateTimeOffset(2026, 6, 14, 23, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.True(openingWednesday);
        Assert.False(beforeMonday);
    }

    [Fact]
    public void A_non_working_local_day_is_outside_the_hours() {
        // Arrange
        Site paris = new AnySite().WithZone(Paris).WithWorkingHours(NineToFiveWeekdays()).Generate();

        // Act: Saturday 2026-06-20 10:00Z
        bool saturday = paris.IsWithinWorkingHours(new DateTimeOffset(2026, 6, 20, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 20, 11, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.False(saturday);
    }

    [Fact]
    public void An_interval_that_ends_before_it_starts_is_refused() {
        // Arrange
        Site anySite = new AnySite().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => anySite.IsWithinWorkingHours(new DateTimeOffset(2026, 6, 17, 8, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 17, 7, 0, 0, TimeSpan.Zero)));
    }

}

public sealed class EmployeeTests {

    [Fact]
    public void An_employee_keeps_what_it_was_given() {
        // Arrange
        EmployeeId id   = new AnyEmployeeId().Generate();
        Site       site = new AnySite().Generate();

        // Act
        Employee employee = new Employee(id, "Ada Lovelace", site);

        // Assert
        Assert.Equal(id, employee.Id);
        Assert.Equal("Ada Lovelace", employee.FullName);
        Assert.Same(site, employee.HomeSite);
    }

    [Fact]
    public void A_name_of_120_characters_is_accepted_and_one_of_121_is_refused() {
        Assert.Equal(120, new AnyEmployee().WithFullName(new string('x', 120)).Generate().FullName.Length);
        Assert.Throws<ArgumentException>(() => new AnyEmployee().WithFullName(new string('x', 121)).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new AnyEmployee().WithFullName(name).Generate());
    }

    [Theory]
    [InlineData(" Ada Lovelace")]
    [InlineData("Ada Lovelace ")]
    [InlineData("\tAda Lovelace")]
    [InlineData("Ada Lovelace\n")]
    public void A_name_with_leading_or_trailing_whitespace_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new AnyEmployee().WithFullName(name).Generate());
    }

    [Fact]
    public void Inner_spaces_are_kept() {
        Assert.Equal("Jean  de La   Fontaine", new AnyEmployee().WithFullName("Jean  de La   Fontaine").Generate().FullName);
    }

    [Fact]
    public void A_null_argument_is_refused() {
        // Arrange
        EmployeeId id   = new AnyEmployeeId().Generate();
        Site       site = new AnySite().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Employee(null!, "Ada Lovelace", site));
        Assert.Throws<ArgumentNullException>(() => new Employee(id, null!, site));
        Assert.Throws<ArgumentNullException>(() => new Employee(id, "Ada Lovelace", null!));
    }

}
