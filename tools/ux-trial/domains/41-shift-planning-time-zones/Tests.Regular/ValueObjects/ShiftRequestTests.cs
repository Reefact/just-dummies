using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.ValueObjects;

public sealed class ShiftRequestTests {

    private static readonly IAny<DateOnly> AnyDay = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));

    [Fact]
    public void A_request_remembers_what_the_planner_wrote() {
        // Arrange
        SiteId   anySite = new AnySiteId().Generate();
        DateOnly anyDate = AnyDay.Generate();

        // Act
        ShiftRequest request = new ShiftRequest(anySite, anyDate, new TimeOnly(8, 0), new TimeOnly(16, 30), OffsetChoice.Earlier, OffsetChoice.Later);

        // Assert
        Assert.Equal(anySite, request.Site);
        Assert.Equal(anyDate, request.StartDate);
        Assert.Equal(new TimeOnly(8, 0), request.StartTime);
        Assert.Equal(new TimeOnly(16, 30), request.EndTime);
        Assert.Equal(OffsetChoice.Earlier, request.StartChoice);
        Assert.Equal(OffsetChoice.Later, request.EndChoice);
    }

    [Fact]
    public void The_choices_default_to_unspecified() {
        // Act
        ShiftRequest request = new ShiftRequest(new AnySiteId().Generate(), AnyDay.Generate(), new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.Equal(OffsetChoice.Unspecified, request.StartChoice);
        Assert.Equal(OffsetChoice.Unspecified, request.EndChoice);
    }

    [Fact]
    public void A_shift_that_ends_later_the_same_day_is_not_overnight() {
        // Arrange
        ShiftRequest request = new AnyShiftRequest().WithStartTime(new TimeOnly(9, 0)).WithWallClockLength(TimeSpan.FromHours(8)).Generate();

        // Assert
        Assert.False(request.IsOvernight);
        Assert.Equal(request.StartDate, request.EndDate);
        Assert.Equal(TimeSpan.FromHours(8), request.WallClockLength);
    }

    [Fact]
    public void A_shift_that_ends_before_its_start_time_ends_the_next_day() {
        // Arrange
        ShiftRequest request = new AnyShiftRequest().WithStartTime(new TimeOnly(22, 0)).WithWallClockLength(TimeSpan.FromHours(8)).Generate();

        // Assert
        Assert.True(request.IsOvernight);
        Assert.Equal(new TimeOnly(6, 0), request.EndTime);
        Assert.Equal(request.StartDate.AddDays(1), request.EndDate);
        Assert.Equal(TimeSpan.FromHours(8), request.WallClockLength);
    }

    [Fact]
    public void A_shift_that_ends_at_midnight_is_overnight() {
        // Arrange
        ShiftRequest request = new AnyShiftRequest().WithStartTime(new TimeOnly(20, 0)).WithWallClockLength(TimeSpan.FromHours(4)).Generate();

        // Assert
        Assert.Equal(TimeOnly.MinValue, request.EndTime);
        Assert.True(request.IsOvernight);
        Assert.Equal(request.StartDate.AddDays(1), request.EndDate);
    }

    [Fact]
    public void Any_drawn_request_has_a_wall_clock_length_between_one_and_twelve_hours() {
        // Arrange
        ShiftRequest anyRequest = new AnyShiftRequest().Generate();

        // Assert
        Assert.InRange(anyRequest.WallClockLength, ShiftRequest.MinWallClockLength, ShiftRequest.MaxWallClockLength);
    }

    [Theory]
    [InlineData("08:00", "09:00")]
    [InlineData("08:00", "20:00")]
    [InlineData("23:30", "00:30")]
    [InlineData("12:00", "00:00")]
    [InlineData("20:00", "08:00")]
    public void The_wall_clock_length_may_be_exactly_one_hour_or_exactly_twelve(string start, string end) {
        // Act
        ShiftRequest request = new ShiftRequest(new AnySiteId().Generate(), AnyDay.Generate(), TimeOnly.Parse(start), TimeOnly.Parse(end));

        // Assert
        Assert.InRange(request.WallClockLength, ShiftRequest.MinWallClockLength, ShiftRequest.MaxWallClockLength);
    }

    [Theory]
    [InlineData("08:00", "08:59")]
    [InlineData("08:00", "20:01")]
    [InlineData("23:30", "00:29")]
    [InlineData("20:00", "08:01")]
    [InlineData("08:00", "08:01")]
    [InlineData("08:00", "07:59")]
    [InlineData("08:00", "08:00")]
    public void A_wall_clock_length_outside_one_to_twelve_hours_is_refused(string start, string end) {
        Assert.Throws<ArgumentException>(() => new ShiftRequest(new AnySiteId().Generate(), AnyDay.Generate(), TimeOnly.Parse(start), TimeOnly.Parse(end)));
    }

    [Fact]
    public void A_start_time_with_seconds_is_refused() {
        Assert.Throws<ArgumentException>(() => new ShiftRequest(new AnySiteId().Generate(), AnyDay.Generate(), new TimeOnly(8, 0, 30), new TimeOnly(16, 0)));
    }

    [Fact]
    public void An_end_time_with_a_fraction_of_a_second_is_refused() {
        Assert.Throws<ArgumentException>(() => new ShiftRequest(new AnySiteId().Generate(), AnyDay.Generate(), new TimeOnly(8, 0), new TimeOnly(16, 0).Add(TimeSpan.FromMilliseconds(1))));
    }

    [Fact]
    public void An_undefined_start_choice_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftRequest(new AnySiteId().Generate(), AnyDay.Generate(), new TimeOnly(8, 0), new TimeOnly(16, 0), (OffsetChoice)7));
    }

    [Fact]
    public void An_undefined_end_choice_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftRequest(new AnySiteId().Generate(), AnyDay.Generate(), new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, (OffsetChoice)7));
    }

    [Fact]
    public void A_request_needs_a_site() {
        Assert.Throws<ArgumentNullException>(() => new ShiftRequest(null!, AnyDay.Generate(), new TimeOnly(8, 0), new TimeOnly(16, 0)));
    }

    [Fact]
    public void A_shift_may_end_on_the_last_representable_date() {
        // Act
        ShiftRequest request = new ShiftRequest(new AnySiteId().Generate(), DateOnly.MaxValue, new TimeOnly(8, 0), new TimeOnly(16, 0));

        // Assert
        Assert.Equal(DateOnly.MaxValue, request.EndDate);
    }

    [Fact]
    public void An_overnight_shift_cannot_start_on_the_last_representable_date() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftRequest(new AnySiteId().Generate(), DateOnly.MaxValue, new TimeOnly(22, 0), new TimeOnly(6, 0)));
    }

    [Fact]
    public void Requests_are_equal_when_all_six_fields_are_equal() {
        // Arrange
        SiteId   anySite = new AnySiteId().Generate();
        DateOnly anyDate = AnyDay.Generate();

        ShiftRequest request = new ShiftRequest(anySite, anyDate, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later);
        ShiftRequest same    = new ShiftRequest(new SiteId(anySite.Value), anyDate, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later);

        // Assert
        Assert.Equal(request, same);
        Assert.Equal(request.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Requests_differ_as_soon_as_one_field_differs() {
        // Arrange
        List<SiteId> anyTwoSites = Any.ListOf(new AnySiteId()).WithCount(2).Distinct().Generate();
        SiteId       anySite     = anyTwoSites[0];
        DateOnly     date        = AnyDay.Generate();
        ShiftRequest request = new ShiftRequest(anySite, date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later);

        // Assert
        Assert.NotEqual(request, new ShiftRequest(anySite, date.AddDays(1), new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later));
        Assert.NotEqual(request, new ShiftRequest(anySite, date, new TimeOnly(8, 1), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later));
        Assert.NotEqual(request, new ShiftRequest(anySite, date, new TimeOnly(8, 0), new TimeOnly(16, 1), OffsetChoice.Earlier, OffsetChoice.Later));
        Assert.NotEqual(request, new ShiftRequest(anySite, date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Later, OffsetChoice.Later));
        Assert.NotEqual(request, new ShiftRequest(anySite, date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Earlier));
        Assert.NotEqual(request, new ShiftRequest(anyTwoSites[1], date, new TimeOnly(8, 0), new TimeOnly(16, 0), OffsetChoice.Earlier, OffsetChoice.Later));
    }

    [Fact]
    public void A_request_prints_its_site_its_date_and_its_local_times() {
        // Act
        ShiftRequest request = new ShiftRequest(new SiteId("PAR-01"), new DateOnly(2026, 3, 28), new TimeOnly(22, 0), new TimeOnly(6, 0));

        // Assert
        Assert.Equal("PAR-01 2026-03-28 22:00-06:00", request.ToString());
    }

}
