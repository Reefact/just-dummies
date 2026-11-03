using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Discovery;

public sealed class ShiftRequestTests {

    private static ShiftRequest Request(int startHour, int startMinute, int endHour, int endMinute) {
        return new ShiftRequest(new AnySiteId().Generate(),
                                Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate(),
                                new TimeOnly(startHour, startMinute),
                                new TimeOnly(endHour, endMinute));
    }

    [Fact]
    public void A_day_shift_ends_on_the_day_it_starts() {
        // Arrange
        DateOnly startDate = new DateOnly(2026, 6, 10);

        // Act
        ShiftRequest request = new AnyShiftRequest().WithStartDate(startDate)
                                                    .WithStartTime(new TimeOnly(8, 0))
                                                    .WithWallClockLength(TimeSpan.FromHours(8))
                                                    .Generate();

        // Assert
        Assert.Equal(new TimeOnly(16, 0), request.EndTime);
        Assert.False(request.IsOvernight);
        Assert.Equal(startDate, request.EndDate);
        Assert.Equal(TimeSpan.FromHours(8), request.WallClockLength);
    }

    [Fact]
    public void A_night_shift_ends_on_the_next_day() {
        // Arrange
        DateOnly startDate = new DateOnly(2026, 6, 10);

        // Act
        ShiftRequest request = new AnyShiftRequest().WithStartDate(startDate)
                                                    .WithStartTime(new TimeOnly(22, 0))
                                                    .WithWallClockLength(TimeSpan.FromHours(8))
                                                    .Generate();

        // Assert
        Assert.Equal(new TimeOnly(6, 0), request.EndTime);
        Assert.True(request.IsOvernight);
        Assert.Equal(new DateOnly(2026, 6, 11), request.EndDate);
        Assert.Equal(TimeSpan.FromHours(8), request.WallClockLength);
    }

    [Fact]
    public void A_shift_ending_exactly_at_midnight_ends_on_the_next_day() {
        // Act
        ShiftRequest request = Request(20, 0, 0, 0);

        // Assert
        Assert.True(request.IsOvernight);
        Assert.Equal(request.StartDate.AddDays(1), request.EndDate);
        Assert.Equal(TimeSpan.FromHours(4), request.WallClockLength);
    }

    [Theory]
    [InlineData(8, 0, 9, 0)]
    [InlineData(8, 0, 20, 0)]
    [InlineData(23, 30, 0, 30)]
    [InlineData(12, 0, 0, 0)]
    [InlineData(22, 0, 10, 0)]
    public void A_wall_clock_length_from_1_to_12_hours_is_accepted_ends_included(int startHour, int startMinute, int endHour, int endMinute) {
        // Act
        ShiftRequest request = Request(startHour, startMinute, endHour, endMinute);

        // Assert
        Assert.InRange(request.WallClockLength, ShiftRequest.MinWallClockLength, ShiftRequest.MaxWallClockLength);
    }

    [Theory]
    [InlineData(8, 0, 8, 59)]
    [InlineData(8, 0, 20, 1)]
    [InlineData(23, 30, 0, 29)]
    [InlineData(22, 0, 10, 1)]
    [InlineData(8, 0, 7, 59)]
    public void A_wall_clock_length_outside_1_to_12_hours_is_refused(int startHour, int startMinute, int endHour, int endMinute) {
        Assert.Throws<ArgumentException>(() => Request(startHour, startMinute, endHour, endMinute));
    }

    [Fact]
    public void A_shift_cannot_end_when_it_starts() {
        Assert.Throws<ArgumentException>(() => Request(8, 0, 8, 0));
    }

    [Fact]
    public void Times_with_seconds_are_refused() {
        // Arrange
        SiteId anySite = new AnySiteId().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ShiftRequest(anySite, new DateOnly(2026, 6, 10), new TimeOnly(8, 0, 30), new TimeOnly(16, 0)));
        Assert.Throws<ArgumentException>(() => new ShiftRequest(anySite, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0, 1)));
    }

    [Fact]
    public void An_undefined_offset_choice_is_refused_on_either_end() {
        // Arrange
        SiteId anySite = new AnySiteId().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftRequest(anySite, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0), (OffsetChoice)9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftRequest(anySite, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, (OffsetChoice)9));
    }

    [Fact]
    public void A_null_site_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ShiftRequest(null!, new DateOnly(2026, 6, 10), new TimeOnly(8, 0), new TimeOnly(16, 0)));
    }

    [Fact]
    public void A_night_shift_cannot_start_on_the_last_representable_date() {
        // Arrange
        SiteId anySite = new AnySiteId().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftRequest(anySite, DateOnly.MaxValue, new TimeOnly(22, 0), new TimeOnly(6, 0)));
    }

    [Fact]
    public void A_day_shift_may_start_on_the_last_representable_date() {
        // Arrange
        SiteId anySite = new AnySiteId().Generate();

        // Act
        ShiftRequest request = new ShiftRequest(anySite, DateOnly.MaxValue, new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.Equal(DateOnly.MaxValue, request.EndDate);
    }

    [Fact]
    public void Requests_are_equal_when_all_six_fields_are_equal() {
        // Arrange
        SiteId   site = new AnySiteId().Generate();
        DateOnly date = new DateOnly(2026, 6, 10);

        ShiftRequest reference = new ShiftRequest(site, date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later);

        // Assert
        Assert.Equal(reference, new ShiftRequest(new SiteId(site.Value), date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later));
        Assert.Equal(reference.GetHashCode(), new ShiftRequest(site, date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later).GetHashCode());
        Assert.NotEqual(reference, new ShiftRequest(new AnySiteId().WithValue(v => v.DifferentFrom(site.Value)).Generate(), date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later));
        Assert.NotEqual(reference, new ShiftRequest(site, date.AddDays(1), new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later));
        Assert.NotEqual(reference, new ShiftRequest(site, date, new TimeOnly(8, 1), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later));
        Assert.NotEqual(reference, new ShiftRequest(site, date, new TimeOnly(8, 0), new TimeOnly(16, 1), OffsetChoice.Earlier, OffsetChoice.Later));
        Assert.NotEqual(reference, new ShiftRequest(site, date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Later, OffsetChoice.Later));
        Assert.NotEqual(reference, new ShiftRequest(site, date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Earlier));
        Assert.False(reference.Equals(null));
    }

}
