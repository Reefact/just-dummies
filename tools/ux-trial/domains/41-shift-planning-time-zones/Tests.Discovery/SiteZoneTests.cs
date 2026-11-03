using Xunit;

namespace ShiftPlanning.Domain.Tests.Discovery;

public sealed class SiteZoneTests {

    private static readonly SiteZone Paris   = new SiteZone("Europe/Paris");
    private static readonly SiteZone NewYork = new SiteZone("America/New_York");
    private static readonly SiteZone Sydney  = new SiteZone("Australia/Sydney");

    [Theory]
    [InlineData("Europe/Paris")]
    [InlineData("America/New_York")]
    [InlineData("America/Argentina/Buenos_Aires")]
    [InlineData("Asia/Kolkata")]
    [InlineData("UTC")]
    public void A_known_IANA_id_is_accepted(string id) {
        // Act
        SiteZone zone = new SiteZone(id);

        // Assert
        Assert.Equal(id, zone.Id);
        Assert.Equal(id, zone.ToString());
    }

    [Theory]
    [InlineData("Romance Standard Time")]
    [InlineData("europe/Paris")]
    [InlineData("Paris")]
    [InlineData("Europe/")]
    [InlineData("/Paris")]
    [InlineData("Europe/Paris/Extra/Deep")]
    [InlineData("utc")]
    [InlineData("GMT+1")]
    public void An_id_that_is_not_shaped_like_an_IANA_id_is_refused(string id) {
        Assert.Throws<FormatException>(() => new SiteZone(id));
    }

    [Theory]
    [InlineData("Europe/Atlantis")]
    [InlineData("Mars/Olympus_Mons")]
    public void A_well_shaped_id_the_system_does_not_know_is_refused(string id) {
        Assert.Throws<ArgumentException>(() => new SiteZone(id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_id_is_refused(string id) {
        Assert.ThrowsAny<ArgumentException>(() => new SiteZone(id));
    }

    [Fact]
    public void A_null_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new SiteZone(null!));
    }

    [Fact]
    public void Equality_is_by_id() {
        Assert.Equal(Paris, new SiteZone("Europe/Paris"));
        Assert.Equal(Paris.GetHashCode(), new SiteZone("Europe/Paris").GetHashCode());
        Assert.NotEqual(Paris, NewYork);
    }

    [Theory]
    [InlineData("Europe/Paris", 2026, 7, 1, 12, 0, LocalTimeKind.Unique)]
    [InlineData("Europe/Paris", 2026, 3, 29, 1, 59, LocalTimeKind.Unique)]
    [InlineData("Europe/Paris", 2026, 3, 29, 2, 0, LocalTimeKind.Skipped)]
    [InlineData("Europe/Paris", 2026, 3, 29, 2, 59, LocalTimeKind.Skipped)]
    [InlineData("Europe/Paris", 2026, 3, 29, 3, 0, LocalTimeKind.Unique)]
    [InlineData("Europe/Paris", 2026, 10, 25, 1, 59, LocalTimeKind.Unique)]
    [InlineData("Europe/Paris", 2026, 10, 25, 2, 0, LocalTimeKind.Ambiguous)]
    [InlineData("Europe/Paris", 2026, 10, 25, 2, 59, LocalTimeKind.Ambiguous)]
    [InlineData("Europe/Paris", 2026, 10, 25, 3, 0, LocalTimeKind.Unique)]
    [InlineData("America/New_York", 2026, 3, 8, 2, 30, LocalTimeKind.Skipped)]
    [InlineData("America/New_York", 2026, 11, 1, 1, 30, LocalTimeKind.Ambiguous)]
    [InlineData("Australia/Sydney", 2026, 10, 4, 2, 30, LocalTimeKind.Skipped)]
    [InlineData("Australia/Sydney", 2026, 4, 5, 2, 30, LocalTimeKind.Ambiguous)]
    [InlineData("UTC", 2026, 3, 29, 2, 30, LocalTimeKind.Unique)]
    [InlineData("Asia/Kolkata", 2026, 3, 29, 2, 30, LocalTimeKind.Unique)]
    public void A_local_time_is_unique_skipped_or_ambiguous(string id, int year, int month, int day, int hour, int minute, LocalTimeKind expected) {
        // Arrange
        SiteZone zone = new SiteZone(id);

        // Act
        LocalTimeKind kind = zone.Classify(new DateOnly(year, month, day), new TimeOnly(hour, minute));

        // Assert
        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData(OffsetChoice.Unspecified)]
    [InlineData(OffsetChoice.Earlier)]
    [InlineData(OffsetChoice.Later)]
    public void The_choice_is_ignored_for_a_time_that_occurs_once(OffsetChoice choice) {
        // Act: noon in Paris in July is UTC+2
        DateTimeOffset instant = Paris.ToUtc(new DateOnly(2026, 7, 1), new TimeOnly(12, 0), choice);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero), instant);
        Assert.Equal(TimeSpan.Zero, instant.Offset);
    }

    [Fact]
    public void Winter_and_summer_offsets_differ_for_the_same_wall_clock() {
        // Act
        DateTimeOffset winter = Paris.ToUtc(new DateOnly(2026, 1, 15), new TimeOnly(9, 0), OffsetChoice.Unspecified);
        DateTimeOffset summer = Paris.ToUtc(new DateOnly(2026, 7, 15), new TimeOnly(9, 0), OffsetChoice.Unspecified);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero), winter);
        Assert.Equal(new DateTimeOffset(2026, 7, 15, 7, 0, 0, TimeSpan.Zero), summer);
    }

    [Fact]
    public void A_time_in_the_spring_forward_gap_does_not_exist() {
        Assert.Throws<ArgumentException>(() => Paris.ToUtc(new DateOnly(2026, 3, 29), new TimeOnly(2, 30), OffsetChoice.Earlier));
    }

    [Fact]
    public void An_ambiguous_time_needs_a_choice() {
        Assert.Throws<ArgumentException>(() => Paris.ToUtc(new DateOnly(2026, 10, 25), new TimeOnly(2, 30), OffsetChoice.Unspecified));
    }

    [Fact]
    public void The_earlier_choice_is_the_first_occurrence_and_the_later_one_the_second() {
        // Act: at 02:30 on 25 Oct 2026 Paris first runs on CEST (UTC+2) then on CET (UTC+1)
        DateTimeOffset earlier = Paris.ToUtc(new DateOnly(2026, 10, 25), new TimeOnly(2, 30), OffsetChoice.Earlier);
        DateTimeOffset later   = Paris.ToUtc(new DateOnly(2026, 10, 25), new TimeOnly(2, 30), OffsetChoice.Later);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero), earlier);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero), later);
    }

    [Fact]
    public void The_earlier_and_later_choices_in_the_southern_hemisphere_follow_the_same_rule() {
        // Act: 5 Apr 2026 02:30 in Sydney is first AEDT (UTC+11) then AEST (UTC+10)
        DateTimeOffset earlier = Sydney.ToUtc(new DateOnly(2026, 4, 5), new TimeOnly(2, 30), OffsetChoice.Earlier);
        DateTimeOffset later   = Sydney.ToUtc(new DateOnly(2026, 4, 5), new TimeOnly(2, 30), OffsetChoice.Later);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 4, 4, 15, 30, 0, TimeSpan.Zero), earlier);
        Assert.Equal(new DateTimeOffset(2026, 4, 4, 16, 30, 0, TimeSpan.Zero), later);
    }

    [Fact]
    public void An_undefined_choice_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Paris.ToUtc(new DateOnly(2026, 7, 1), new TimeOnly(12, 0), (OffsetChoice)42));
    }

    [Fact]
    public void An_instant_is_read_back_on_the_local_clock_with_the_offset_in_force() {
        // Act
        DateTimeOffset local = NewYork.ToLocal(new DateTimeOffset(2026, 1, 15, 17, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Equal(new DateTime(2026, 1, 15, 12, 0, 0), local.DateTime);
        Assert.Equal(TimeSpan.FromHours(-5), local.Offset);
    }

    [Fact]
    public void The_offset_at_an_instant_follows_daylight_saving() {
        Assert.Equal(TimeSpan.FromHours(1), Paris.OffsetAt(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero)));
        Assert.Equal(TimeSpan.FromHours(2), Paris.OffsetAt(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero)));
        Assert.Equal(TimeSpan.FromHours(1), Paris.OffsetAt(new DateTimeOffset(2026, 3, 29, 0, 59, 0, TimeSpan.Zero)));
        Assert.Equal(TimeSpan.FromHours(2), Paris.OffsetAt(new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.Zero)));
    }

}
