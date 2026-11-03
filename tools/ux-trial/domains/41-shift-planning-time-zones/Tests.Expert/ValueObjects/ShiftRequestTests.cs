using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.ValueObjects;

public sealed class ShiftRequestTests {

    // ---- What a request keeps ----------------------------------------------------------------------------------

    [Fact]
    public void A_day_shift_ends_on_the_day_it_starts() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        ShiftRequest request = new ShiftRequest(anySite, Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("16:30"));

        // Assert
        Assert.False(request.IsOvernight);
        Assert.Equal(Clock.Day("2027-01-12"), request.EndDate);
        Assert.Equal(TimeSpan.FromMinutes(8 * 60 + 30), request.WallClockLength);
        Assert.Equal(OffsetChoice.Unspecified, request.StartChoice);
        Assert.Equal(OffsetChoice.Unspecified, request.EndChoice);
    }

    [Fact]
    public void An_end_before_the_start_time_means_the_next_day() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        ShiftRequest request = new ShiftRequest(anySite, Clock.Day("2027-01-12"), Clock.At("22:00"), Clock.At("06:00"));

        // Assert
        Assert.True(request.IsOvernight);
        Assert.Equal(Clock.Day("2027-01-13"), request.EndDate);
        Assert.Equal(TimeSpan.FromHours(8), request.WallClockLength);
    }

    [Fact]
    public void An_end_at_midnight_is_the_start_of_the_next_day() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        ShiftRequest request = new ShiftRequest(anySite, Clock.Day("2027-01-12"), Clock.At("22:00"), Clock.At("00:00"));

        // Assert
        Assert.True(request.IsOvernight);
        Assert.Equal(Clock.Day("2027-01-13"), request.EndDate);
        Assert.Equal(TimeSpan.FromHours(2), request.WallClockLength);
    }

    [Fact]
    public void The_choices_given_for_the_two_ends_are_kept_apart() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        ShiftRequest request = new ShiftRequest(anySite, Clock.Day("2027-10-31"), Clock.At("00:30"), Clock.At("02:30"), OffsetChoice.Earlier, OffsetChoice.Later);

        // Assert
        Assert.Equal(OffsetChoice.Earlier, request.StartChoice);
        Assert.Equal(OffsetChoice.Later, request.EndChoice);
    }

    // ---- Wall-clock length -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("08:00", "09:00")]   // one hour, the shortest
    [InlineData("08:00", "20:00")]   // twelve hours, the longest
    [InlineData("20:00", "08:00")]   // twelve hours across midnight
    [InlineData("23:30", "00:30")]   // one hour across midnight
    [InlineData("00:00", "12:00")]
    public void A_shift_of_one_to_twelve_wall_clock_hours_is_accepted(string start, string end) {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        ShiftRequest request = new ShiftRequest(anySite, Clock.Day("2027-01-12"), Clock.At(start), Clock.At(end));

        // Assert
        Assert.InRange(request.WallClockLength, TimeSpan.FromHours(1), TimeSpan.FromHours(12));
    }

    [Theory]
    [InlineData("08:00", "08:59")]   // a minute short of one hour
    [InlineData("08:00", "20:01")]   // a minute over twelve hours
    [InlineData("20:00", "08:01")]
    [InlineData("23:30", "00:29")]
    [InlineData("08:00", "08:01")]
    public void A_shift_shorter_than_one_hour_or_longer_than_twelve_is_refused(string start, string end) {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        Action construct = () => new ShiftRequest(anySite, Clock.Day("2027-01-12"), Clock.At(start), Clock.At(end));

        // Assert
        Assert.Throws<ArgumentException>("endTime", construct);
    }

    [Fact]
    public void A_shift_cannot_end_at_the_time_it_starts() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        Action construct = () => new ShiftRequest(anySite, Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("08:00"));

        // Assert
        Assert.Throws<ArgumentException>("endTime", construct);
    }

    [Theory]
    [InlineData(8, 0, 30, 16, 0, 0, "startTime")]
    [InlineData(8, 0, 0, 16, 0, 30, "endTime")]
    public void Times_are_whole_minutes(int startHour, int startMinute, int startSecond, int endHour, int endMinute, int endSecond, string offender) {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        Action construct = () => new ShiftRequest(anySite, Clock.Day("2027-01-12"), new TimeOnly(startHour, startMinute, startSecond), new TimeOnly(endHour, endMinute, endSecond));

        // Assert
        Assert.Throws<ArgumentException>(offender, construct);
    }

    [Fact]
    public void Times_are_refused_below_the_second_too() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        Action construct = () => new ShiftRequest(anySite, Clock.Day("2027-01-12"), new TimeOnly(8, 0, 0, 500), Clock.At("16:00"));

        // Assert
        Assert.Throws<ArgumentException>("startTime", construct);
    }

    // ---- The last representable date ---------------------------------------------------------------------------

    [Fact]
    public void An_overnight_shift_cannot_start_on_the_last_representable_date() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        Action construct = () => new ShiftRequest(anySite, DateOnly.MaxValue, Clock.At("22:00"), Clock.At("06:00"));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("startDate", construct);
    }

    [Fact]
    public void A_day_shift_may_start_on_the_last_representable_date() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        ShiftRequest request = new ShiftRequest(anySite, DateOnly.MaxValue, Clock.At("08:00"), Clock.At("16:00"));

        // Assert
        Assert.Equal(DateOnly.MaxValue, request.EndDate);
    }

    // ---- Arguments ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_missing_site_is_refused() {
        // Act
        Action construct = () => new ShiftRequest(null!, Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("16:00"));

        // Assert
        Assert.Throws<ArgumentNullException>("site", construct);
    }

    [Fact]
    public void A_start_choice_the_enum_does_not_declare_is_refused() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        Action construct = () => new ShiftRequest(anySite, Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("16:00"), (OffsetChoice)3);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("startChoice", construct);
    }

    [Fact]
    public void An_end_choice_the_enum_does_not_declare_is_refused() {
        // Arrange
        SiteId anySite = Any.SiteId().Generate();

        // Act
        Action construct = () => new ShiftRequest(anySite, Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("16:00"), OffsetChoice.Unspecified, (OffsetChoice)(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("endChoice", construct);
    }

    // ---- Equality ----------------------------------------------------------------------------------------------

    private static ShiftRequest Reference() {
        return new ShiftRequest(new SiteId("PAR-01"), Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("16:00"), OffsetChoice.Earlier, OffsetChoice.Later);
    }

    [Fact]
    public void Requests_are_equal_when_all_six_fields_are() {
        // Arrange
        ShiftRequest first  = Reference();
        ShiftRequest second = Reference();

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    public static TheoryData<string, ShiftRequest> RequestsDifferingByOneField => new TheoryData<string, ShiftRequest> {
        { "site", new ShiftRequest(new SiteId("PAR-02"), Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("16:00"), OffsetChoice.Earlier, OffsetChoice.Later) },
        { "start date", new ShiftRequest(new SiteId("PAR-01"), Clock.Day("2027-01-13"), Clock.At("08:00"), Clock.At("16:00"), OffsetChoice.Earlier, OffsetChoice.Later) },
        { "start time", new ShiftRequest(new SiteId("PAR-01"), Clock.Day("2027-01-12"), Clock.At("08:01"), Clock.At("16:00"), OffsetChoice.Earlier, OffsetChoice.Later) },
        { "end time", new ShiftRequest(new SiteId("PAR-01"), Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("16:01"), OffsetChoice.Earlier, OffsetChoice.Later) },
        { "start choice", new ShiftRequest(new SiteId("PAR-01"), Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("16:00"), OffsetChoice.Later, OffsetChoice.Later) },
        { "end choice", new ShiftRequest(new SiteId("PAR-01"), Clock.Day("2027-01-12"), Clock.At("08:00"), Clock.At("16:00"), OffsetChoice.Earlier, OffsetChoice.Earlier) },
    };

    [Theory]
    [MemberData(nameof(RequestsDifferingByOneField))]
    public void A_request_differing_in_one_field_is_not_equal(string field, ShiftRequest other) {
        // Assert
        Assert.NotEqual(Reference(), other);
        Assert.False(Reference().Equals(null), field);
    }

    [Fact]
    public void A_request_reads_as_site_date_and_times() {
        // Assert
        Assert.Equal("PAR-01 2027-01-12 08:00-16:00", Reference().ToString());
    }

}
