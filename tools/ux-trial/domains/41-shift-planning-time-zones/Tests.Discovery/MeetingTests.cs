using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Discovery;

public sealed class MeetingTests {

    private static List<EmployeeId> AnyParticipants(int count) {
        return Any.ListOf(new AnyEmployeeId()).WithCount(count).Distinct().Generate();
    }

    [Fact]
    public void A_meeting_keeps_its_title_participants_and_duration_and_stores_UTC() {
        // Arrange
        List<EmployeeId> participants = AnyParticipants(3);
        DateTimeOffset   start        = new DateTimeOffset(2026, 6, 17, 9, 30, 0, TimeSpan.FromHours(2));

        // Act
        Meeting meeting = new AnyMeeting().WithTitle("Quarterly planning")
                                          .WithStart(start)
                                          .WithDuration(TimeSpan.FromMinutes(90))
                                          .WithParticipants(participants)
                                          .Generate();

        // Assert
        Assert.Equal("Quarterly planning", meeting.Title);
        Assert.Equal(new DateTimeOffset(2026, 6, 17, 7, 30, 0, TimeSpan.Zero), meeting.StartUtc);
        Assert.Equal(TimeSpan.Zero, meeting.StartUtc.Offset);
        Assert.Equal(TimeSpan.FromMinutes(90), meeting.Duration);
        Assert.Equal(new DateTimeOffset(2026, 6, 17, 9, 0, 0, TimeSpan.Zero), meeting.EndUtc);
        Assert.Equal(participants, meeting.Participants);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(240)]
    public void A_duration_from_15_minutes_to_4_hours_is_accepted(int minutes) {
        Assert.Equal(TimeSpan.FromMinutes(minutes), new AnyMeeting().WithDuration(TimeSpan.FromMinutes(minutes)).Generate().Duration);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(241)]
    [InlineData(0)]
    [InlineData(-30)]
    public void A_duration_outside_15_minutes_to_4_hours_is_refused(int minutes) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyMeeting().WithDuration(TimeSpan.FromMinutes(minutes)).Generate());
    }

    [Fact]
    public void A_duration_with_a_fractional_minute_is_refused() {
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithDuration(TimeSpan.FromSeconds(15 * 60 + 30)).Generate());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    public void Between_2_and_12_participants_are_accepted(int count) {
        Assert.Equal(count, new AnyMeeting().WithParticipants(AnyParticipants(count)).Generate().Participants.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(13)]
    public void Fewer_than_2_or_more_than_12_participants_are_refused(int count) {
        // Arrange
        List<EmployeeId> participants = AnyParticipants(count);

        // Assert
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithParticipants(participants).Generate());
    }

    [Fact]
    public void A_participant_listed_twice_is_refused() {
        // Arrange
        EmployeeId anyParticipant = new AnyEmployeeId().Generate();
        EmployeeId anyOther       = new AnyEmployeeId().WithValue(v => v.DifferentFrom(anyParticipant.Value)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithParticipants([anyParticipant, anyOther, new EmployeeId(anyParticipant.Value)]).Generate());
    }

    [Fact]
    public void A_null_participant_is_refused() {
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithParticipants([new AnyEmployeeId().Generate(), null!]).Generate());
    }

    [Fact]
    public void A_null_list_of_participants_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Meeting("Sync", new DateTimeOffset(2026, 6, 17, 9, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(1), null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_title_is_refused(string title) {
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithTitle(title).Generate());
    }

    [Fact]
    public void A_title_of_120_characters_is_accepted_and_one_of_121_is_refused() {
        Assert.Equal(120, new AnyMeeting().WithTitle(new string('x', 120)).Generate().Title.Length);
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithTitle(new string('x', 121)).Generate());
    }

    [Fact]
    public void A_null_title_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AnyMeeting().WithTitle((string)null!).Generate());
    }

    [Fact]
    public void A_meeting_that_would_end_past_the_last_representable_instant_is_refused() {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyMeeting().WithStart(DateTimeOffset.MaxValue.AddMinutes(-10)).WithDuration(TimeSpan.FromMinutes(15)).Generate());
        Assert.Equal(DateTimeOffset.MaxValue, new AnyMeeting().WithStart(DateTimeOffset.MaxValue.AddMinutes(-15)).WithDuration(TimeSpan.FromMinutes(15)).Generate().EndUtc);
    }

}
