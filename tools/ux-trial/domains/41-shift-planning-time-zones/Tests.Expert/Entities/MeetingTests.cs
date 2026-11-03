using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.Entities;

public sealed class MeetingTests {

    // ---- What a meeting keeps ----------------------------------------------------------------------------------

    [Fact]
    public void A_meeting_is_stored_as_a_UTC_instant_and_a_duration() {
        // Arrange
        DateTimeOffset startInParis = new DateTimeOffset(2027, 1, 12, 10, 0, 0, TimeSpan.FromHours(1));

        // Act
        Meeting meeting = Any.Meeting().WithStart(startInParis).WithDuration(TimeSpan.FromMinutes(90)).Generate();

        // Assert
        Assert.Equal(Clock.Utc("2027-01-12 09:00"), meeting.StartUtc);
        Assert.Equal(TimeSpan.Zero, meeting.StartUtc.Offset);
        Assert.Equal(TimeSpan.FromMinutes(90), meeting.Duration);
        Assert.Equal(Clock.Utc("2027-01-12 10:30"), meeting.EndUtc);
    }

    [Fact]
    public void A_meeting_keeps_its_participants_in_the_order_given() {
        // Arrange
        List<EmployeeId> inOrder = Any.ListOf(Any.EmployeeId()).WithCount(4).Distinct().Generate();

        // Act
        Meeting meeting = Any.Meeting().WithParticipants(inOrder).Generate();

        // Assert
        Assert.Equal(inOrder, meeting.Participants);
    }

    // ---- Title -------------------------------------------------------------------------------------------------

    [Fact]
    public void A_title_of_one_hundred_and_twenty_characters_is_the_longest_allowed() {
        // Arrange
        string longestTitle = new string('t', 120);

        // Act
        Meeting meeting = Any.Meeting().WithTitle(longestTitle).Generate();

        // Assert
        Assert.Equal(120, meeting.Title.Length);
    }

    [Fact]
    public void A_title_of_more_than_one_hundred_and_twenty_characters_is_refused() {
        // Arrange
        string tooLongTitle = new string('t', 121);

        // Act
        Action construct = () => Any.Meeting().WithTitle(tooLongTitle).Generate();

        // Assert
        Assert.Throws<ArgumentException>("title", construct);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_title_is_refused(string blank) {
        // Act
        Action construct = () => Any.Meeting().WithTitle(blank).Generate();

        // Assert
        Assert.Throws<ArgumentException>("title", construct);
    }

    // ---- Duration ----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(15)]
    [InlineData(240)]
    public void A_meeting_lasts_from_fifteen_minutes_to_four_hours(int minutes) {
        // Act
        Meeting meeting = Any.Meeting().WithDuration(TimeSpan.FromMinutes(minutes)).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(minutes), meeting.Duration);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(241)]
    [InlineData(0)]
    [InlineData(-30)]
    public void A_meeting_shorter_than_fifteen_minutes_or_longer_than_four_hours_is_refused(int minutes) {
        // Act
        Action construct = () => Any.Meeting().WithDuration(TimeSpan.FromMinutes(minutes)).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("duration", construct);
    }

    [Fact]
    public void A_duration_must_be_a_whole_number_of_minutes() {
        // Act
        Action construct = () => Any.Meeting().WithDuration(TimeSpan.FromSeconds(30 * 60 + 1)).Generate();

        // Assert
        Assert.Throws<ArgumentException>("duration", construct);
    }

    // ---- The end of the calendar -------------------------------------------------------------------------------

    [Fact]
    public void A_meeting_may_end_on_the_last_representable_instant() {
        // Arrange
        TimeSpan duration = TimeSpan.FromMinutes(15);
        DateTimeOffset lastPossibleStart = DateTimeOffset.MaxValue - duration;

        // Act
        Meeting meeting = Any.Meeting().WithStart(lastPossibleStart).WithDuration(duration).Generate();

        // Assert
        Assert.Equal(DateTimeOffset.MaxValue, meeting.EndUtc);
    }

    [Fact]
    public void A_meeting_cannot_end_past_the_last_representable_instant() {
        // Arrange
        TimeSpan duration = TimeSpan.FromMinutes(15);
        DateTimeOffset startOneTickTooLate = DateTimeOffset.MaxValue - duration + TimeSpan.FromTicks(1);

        // Act
        Action construct = () => Any.Meeting().WithStart(startOneTickTooLate).WithDuration(duration).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("start", construct);
    }

    // ---- Participants ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    public void A_meeting_has_from_two_to_twelve_participants(int count) {
        // Arrange
        List<EmployeeId> participants = Any.ListOf(Any.EmployeeId()).WithCount(count).Distinct().Generate();

        // Act
        Meeting meeting = Any.Meeting().WithParticipants(participants).Generate();

        // Assert
        Assert.Equal(count, meeting.Participants.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(13)]
    public void A_meeting_with_fewer_than_two_or_more_than_twelve_participants_is_refused(int count) {
        // Arrange
        List<EmployeeId> participants = Any.ListOf(Any.EmployeeId()).WithCount(count).Distinct().Generate();

        // Act
        Action construct = () => Any.Meeting().WithParticipants(participants).Generate();

        // Assert
        Assert.Throws<ArgumentException>("participants", construct);
    }

    [Fact]
    public void A_participant_listed_twice_is_refused() {
        // Arrange
        EmployeeId anyParticipant = Any.EmployeeId().Generate();
        EmployeeId anotherParticipant = Any.EmployeeId().DifferentFrom(anyParticipant).Generate();
        List<EmployeeId> listedTwice = [anyParticipant, anotherParticipant, new EmployeeId(anyParticipant.Value)];

        // Act
        Action construct = () => Any.Meeting().WithParticipants(listedTwice).Generate();

        // Assert
        Assert.Throws<ArgumentException>("participants", construct);
    }

    [Fact]
    public void A_missing_participant_is_refused() {
        // Arrange
        EmployeeId[] withAGap = [Any.EmployeeId().Generate(), null!];

        // Act
        Action construct = () => Any.Meeting().WithParticipants(withAGap).Generate();

        // Assert
        Assert.Throws<ArgumentException>("participants", construct);
    }

    [Fact]
    public void A_missing_list_of_participants_is_refused() {
        // Act
        Action construct = () => Any.Meeting().WithParticipants((IEnumerable<EmployeeId>)null!).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("participants", construct);
    }

}
