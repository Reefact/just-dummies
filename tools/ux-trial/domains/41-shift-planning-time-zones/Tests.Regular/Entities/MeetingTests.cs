using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.Entities;

public sealed class MeetingTests {

    [Fact]
    public void A_meeting_is_stored_as_a_utc_instant_and_a_duration() {
        // Arrange
        string           anyTitle        = Any.String().Alpha().WithLengthBetween(1, 120).Generate();
        TimeSpan         anyDuration     = Any.TimeSpan().Between(Meeting.MinDuration, Meeting.MaxDuration).WithGranularity(TimeSpan.FromMinutes(1)).Generate();
        List<EmployeeId> anyParticipants = Any.ListOf(new AnyEmployeeId()).WithCountBetween(2, Meeting.MaxParticipants).Distinct().Generate();
        DateTimeOffset   parisNoon       = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.FromHours(2));

        // Act
        Meeting meeting = new Meeting(anyTitle, parisNoon, anyDuration, anyParticipants);

        // Assert
        Assert.Equal(anyTitle, meeting.Title);
        Assert.Equal(TimeSpan.Zero, meeting.StartUtc.Offset);
        Assert.Equal(Support.Kit.Utc_(2026, 7, 1, 10, 0), meeting.StartUtc);
        Assert.Equal(anyDuration, meeting.Duration);
        Assert.Equal(parisNoon + anyDuration, meeting.EndUtc);
        Assert.Equal(anyParticipants, meeting.Participants);
    }

    [Fact]
    public void A_drawn_meeting_is_a_valid_one() {
        // Arrange
        Meeting anyMeeting = new AnyMeeting().Generate();

        // Assert
        Assert.InRange(anyMeeting.Duration, Meeting.MinDuration, Meeting.MaxDuration);
        Assert.InRange(anyMeeting.Participants.Count, 2, Meeting.MaxParticipants);
        Assert.Equal(anyMeeting.StartUtc + anyMeeting.Duration, anyMeeting.EndUtc);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(240)]
    public void A_duration_from_fifteen_minutes_to_four_hours_is_accepted(int minutes) {
        // Act
        Meeting meeting = new AnyMeeting().WithDuration(TimeSpan.FromMinutes(minutes)).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(minutes), meeting.Duration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(241)]
    [InlineData(-30)]
    public void A_shorter_or_longer_duration_is_refused(int minutes) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyMeeting().WithDuration(TimeSpan.FromMinutes(minutes)).Generate());
    }

    [Fact]
    public void A_duration_that_is_not_a_whole_number_of_minutes_is_refused() {
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithDuration(TimeSpan.FromSeconds(1801)).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_title_is_refused(string title) {
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithTitle(title).Generate());
    }

    [Fact]
    public void A_title_of_120_characters_is_accepted_and_one_of_121_is_refused() {
        // Arrange
        string anyLongTitle    = Any.String().Alpha().WithLength(120).Generate();
        string anyTooLongTitle = Any.String().Alpha().WithLength(121).Generate();

        // Act & Assert
        Assert.Equal(anyLongTitle, new AnyMeeting().WithTitle(anyLongTitle).Generate().Title);
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithTitle(anyTooLongTitle).Generate());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    public void Between_two_and_twelve_participants_are_accepted(int count) {
        // Arrange
        List<EmployeeId> anyParticipants = Any.ListOf(new AnyEmployeeId()).WithCount(count).Distinct().Generate();

        // Act
        Meeting meeting = new AnyMeeting().WithParticipants(anyParticipants).Generate();

        // Assert
        Assert.Equal(count, meeting.Participants.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(13)]
    public void Fewer_than_two_or_more_than_twelve_participants_are_refused(int count) {
        // Arrange
        List<EmployeeId> anyParticipants = Any.ListOf(new AnyEmployeeId()).WithCount(count).Distinct().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithParticipants(anyParticipants).Generate());
    }

    [Fact]
    public void A_participant_listed_twice_is_refused() {
        // Arrange
        List<EmployeeId> anyTwo = Any.ListOf(new AnyEmployeeId()).WithCount(2).Distinct().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithParticipants(new[] { anyTwo[0], anyTwo[1], new EmployeeId(anyTwo[0].Value) }).Generate());
    }

    [Fact]
    public void A_null_participant_is_refused() {
        // Arrange
        EmployeeId anyParticipant = new AnyEmployeeId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AnyMeeting().WithParticipants(new EmployeeId[] { anyParticipant, null! }).Generate());
    }

    [Fact]
    public void A_missing_participant_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Meeting("Sync", DateTimeOffset.UnixEpoch, TimeSpan.FromHours(1), null!));
    }

    [Fact]
    public void A_meeting_cannot_end_past_the_last_representable_instant() {
        // Arrange
        TimeSpan duration = TimeSpan.FromHours(1);

        // Act & Assert
        Assert.Equal(DateTimeOffset.MaxValue, new AnyMeeting().WithDuration(duration).WithStart(DateTimeOffset.MaxValue - duration).Generate().EndUtc);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyMeeting().WithDuration(duration).WithStart(DateTimeOffset.MaxValue - duration + TimeSpan.FromTicks(1)).Generate());
    }

}
