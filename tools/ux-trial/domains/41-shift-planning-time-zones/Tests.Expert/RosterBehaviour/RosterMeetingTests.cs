using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.RosterBehaviour;

public sealed class RosterMeetingTests {

    // Offices open from 09:00 to 17:00 on weekdays, on their own clock. In January, Paris is at UTC+1 and New York at UTC-5,
    // so a Tuesday in Paris is open from 08:00Z to 16:00Z and in New York from 14:00Z to 22:00Z: the two share 14:00Z to 16:00Z.

    private sealed record Offices(Scene Scene, Employee InParis, Employee InNewYork, Employee InTokyo);

    private static Offices OpenOffices() {
        Scene scene = new Scene();
        Site paris = scene.OpenSite("Europe/Paris", Hours.NineToFive());
        Site newYork = scene.OpenSite("America/New_York", Hours.NineToFive());
        Site tokyo = scene.OpenSite("Asia/Tokyo", Hours.NineToFive());

        return new Offices(scene, scene.Hire(paris), scene.Hire(newYork), scene.Hire(tokyo));
    }

    private static Meeting MeetingOf(string startUtc, int minutes, params EmployeeId[] participants) {
        return Any.Meeting()
                  .WithStart(Clock.Utc(startUtc))
                  .WithDuration(TimeSpan.FromMinutes(minutes))
                  .WithParticipants(participants)
                  .Generate();
    }

    // ---- ScheduleMeeting ---------------------------------------------------------------------------------------

    [Fact]
    public void A_meeting_inside_the_working_hours_of_every_participant_is_booked() {
        // Arrange
        Offices offices = OpenOffices();
        Meeting meeting = MeetingOf("2027-01-12 14:00", 120, offices.InParis.Id, offices.InNewYork.Id);   // 15:00-17:00 in Paris, 09:00-11:00 in New York

        // Act
        offices.Scene.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Same(meeting, Assert.Single(offices.Scene.Roster.Meetings));
    }

    [Fact]
    public void A_meeting_ending_exactly_at_a_closing_time_is_booked() {
        // Arrange
        Offices offices = OpenOffices();
        Meeting meeting = MeetingOf("2027-01-12 15:00", 60, offices.InParis.Id, offices.InNewYork.Id);   // ends 17:00 in Paris, 11:00 in New York

        // Act
        offices.Scene.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Single(offices.Scene.Roster.Meetings);
    }

    [Fact]
    public void A_meeting_starting_exactly_at_an_opening_time_is_booked() {
        // Arrange
        Offices offices = OpenOffices();
        Meeting meeting = MeetingOf("2027-01-12 14:00", 30, offices.InParis.Id, offices.InNewYork.Id);   // starts 09:00 in New York

        // Act
        offices.Scene.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Single(offices.Scene.Roster.Meetings);
    }

    [Fact]
    public void A_meeting_a_minute_after_a_closing_time_is_refused() {
        // Arrange
        Offices offices = OpenOffices();
        Meeting meeting = MeetingOf("2027-01-12 15:01", 60, offices.InParis.Id, offices.InNewYork.Id);   // ends 17:01 in Paris

        // Act
        Action schedule = () => offices.Scene.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, Refusal.Of(schedule));
        Assert.Empty(offices.Scene.Roster.Meetings);
    }

    [Fact]
    public void A_meeting_a_minute_before_an_opening_time_is_refused() {
        // Arrange
        Offices offices = OpenOffices();
        Meeting meeting = MeetingOf("2027-01-12 13:59", 60, offices.InParis.Id, offices.InNewYork.Id);   // starts 08:59 in New York

        // Act
        Action schedule = () => offices.Scene.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, Refusal.Of(schedule));
    }

    [Fact]
    public void One_participant_outside_the_hours_is_enough_to_refuse_the_meeting() {
        // Arrange
        Offices offices = OpenOffices();
        Meeting meeting = MeetingOf("2027-01-12 14:00", 60, offices.InParis.Id, offices.InNewYork.Id, offices.InTokyo.Id);   // 23:00 in Tokyo

        // Act
        Action schedule = () => offices.Scene.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, Refusal.Of(schedule));
    }

    [Fact]
    public void A_weekend_is_outside_the_working_hours_wherever_the_participant_lives() {
        // Arrange
        Offices offices = OpenOffices();
        Meeting meeting = MeetingOf("2027-01-16 14:00", 60, offices.InParis.Id, offices.InNewYork.Id);   // a Saturday in both

        // Act
        Action schedule = () => offices.Scene.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, Refusal.Of(schedule));
    }

    [Fact]
    public void The_working_day_is_the_participants_local_day_not_the_UTC_day() {
        // Arrange
        Scene scene = new Scene();
        Site auckland = scene.OpenSite("Pacific/Auckland", Hours.NineToFive());   // UTC+13 in January: Monday 09:00 there is Sunday 20:00Z
        Employee first = scene.Hire(auckland);
        Employee second = scene.Hire(auckland);
        Meeting mondayMorningLocally = MeetingOf("2027-01-10 20:00", 60, first.Id, second.Id);    // a Sunday in UTC, a Monday in Auckland
        Meeting saturdayMorningLocally = MeetingOf("2027-01-15 20:00", 60, first.Id, second.Id);  // a Friday in UTC, a Saturday in Auckland

        // Act
        scene.Roster.ScheduleMeeting(mondayMorningLocally);
        Action scheduleOnTheirSaturday = () => scene.Roster.ScheduleMeeting(saturdayMorningLocally);

        // Assert
        Assert.Same(mondayMorningLocally, Assert.Single(scene.Roster.Meetings));
        Assert.Equal(RefusalReason.OutsideWorkingHours, Refusal.Of(scheduleOnTheirSaturday));
    }

    [Fact]
    public void A_participant_who_is_not_registered_refuses_the_meeting() {
        // Arrange
        Offices offices = OpenOffices();
        EmployeeId stranger = Any.EmployeeId().Generate();
        Meeting meeting = MeetingOf("2027-01-12 14:00", 60, offices.InParis.Id, stranger);

        // Act
        Action schedule = () => offices.Scene.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Equal(RefusalReason.UnknownEmployee, Refusal.Of(schedule));
        Assert.Empty(offices.Scene.Roster.Meetings);
    }

    [Fact]
    public void The_same_UTC_slot_is_fine_in_mid_March_and_too_early_for_New_York_in_January() {
        // Arrange
        Offices offices = OpenOffices();
        // Between 2027-03-14 and 2027-03-28 New York is already on summer time (UTC-4) and Paris still on winter time (UTC+1).
        Meeting inJanuary = MeetingOf("2027-01-12 13:00", 60, offices.InParis.Id, offices.InNewYork.Id);   // 08:00 in New York
        Meeting inMarch = MeetingOf("2027-03-16 13:00", 60, offices.InParis.Id, offices.InNewYork.Id);     // 09:00 in New York

        // Act
        Action scheduleInJanuary = () => offices.Scene.Roster.ScheduleMeeting(inJanuary);
        offices.Scene.Roster.ScheduleMeeting(inMarch);

        // Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, Refusal.Of(scheduleInJanuary));
        Assert.Same(inMarch, Assert.Single(offices.Scene.Roster.Meetings));
    }

    [Fact]
    public void A_meeting_can_still_be_booked_after_publication() {
        // Arrange
        Offices offices = OpenOffices();
        offices.Scene.PublishOneStaffedShift(offices.InParis, offices.InParis.HomeSite);
        Meeting meeting = MeetingOf("2027-01-12 14:00", 60, offices.InParis.Id, offices.InNewYork.Id);

        // Act
        offices.Scene.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Single(offices.Scene.Roster.Meetings);
    }

    [Fact]
    public void A_missing_meeting_is_refused() {
        // Arrange
        Offices offices = OpenOffices();

        // Act
        Action schedule = () => offices.Scene.Roster.ScheduleMeeting(null!);

        // Assert
        Assert.Throws<ArgumentNullException>("meeting", schedule);
    }

    // ---- MeetingSlots ------------------------------------------------------------------------------------------

    [Fact]
    public void Meeting_slots_are_the_quarter_hours_at_which_everybody_is_at_work() {
        // Arrange
        Offices offices = OpenOffices();

        // Act
        IReadOnlyList<DateTimeOffset> slots = offices.Scene.Roster.MeetingSlots(Clock.Day("2027-01-12"), TimeSpan.FromHours(1), [offices.InParis.Id, offices.InNewYork.Id]);

        // Assert: the shared window is 14:00Z to 16:00Z, so an hour-long meeting can start from 14:00Z to 15:00Z
        Assert.Equal(
            [Clock.Utc("2027-01-12 14:00"), Clock.Utc("2027-01-12 14:15"), Clock.Utc("2027-01-12 14:30"), Clock.Utc("2027-01-12 14:45"), Clock.Utc("2027-01-12 15:00")],
            slots);
    }

    [Fact]
    public void For_a_single_site_the_slots_run_from_its_opening_to_an_hour_before_its_closing() {
        // Arrange
        Offices offices = OpenOffices();

        // Act
        IReadOnlyList<DateTimeOffset> slots = offices.Scene.Roster.MeetingSlots(Clock.Day("2027-01-12"), TimeSpan.FromHours(1), [offices.InParis.Id]);

        // Assert
        Assert.Equal(Clock.Utc("2027-01-12 08:00"), slots.First());
        Assert.Equal(Clock.Utc("2027-01-12 15:00"), slots.Last());
        Assert.Equal(29, slots.Count);
    }

    [Fact]
    public void Two_employees_of_one_site_give_the_slots_of_that_site() {
        // Arrange
        Offices offices = OpenOffices();
        Employee anotherInParis = offices.Scene.Hire(offices.InParis.HomeSite);

        // Act
        IReadOnlyList<DateTimeOffset> slots = offices.Scene.Roster.MeetingSlots(Clock.Day("2027-01-12"), TimeSpan.FromHours(1), [offices.InParis.Id, anotherInParis.Id]);

        // Assert
        Assert.Equal(29, slots.Count);
    }

    [Fact]
    public void Sites_whose_hours_only_touch_leave_no_slot() {
        // Arrange
        Offices offices = OpenOffices();   // Tokyo closes at 08:00Z, Paris opens at 08:00Z

        // Act
        IReadOnlyList<DateTimeOffset> slots = offices.Scene.Roster.MeetingSlots(Clock.Day("2027-01-12"), TimeSpan.FromMinutes(15), [offices.InTokyo.Id, offices.InParis.Id]);

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void A_day_off_leaves_no_slot() {
        // Arrange
        Offices offices = OpenOffices();

        // Act
        IReadOnlyList<DateTimeOffset> slots = offices.Scene.Roster.MeetingSlots(Clock.Day("2027-01-16"), TimeSpan.FromHours(1), [offices.InParis.Id, offices.InNewYork.Id]);   // a Saturday

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void The_slots_follow_the_offsets_in_force_on_that_day() {
        // Arrange
        Offices offices = OpenOffices();

        // Act
        IReadOnlyList<DateTimeOffset> slots = offices.Scene.Roster.MeetingSlots(Clock.Day("2027-03-16"), TimeSpan.FromHours(1), [offices.InParis.Id, offices.InNewYork.Id]);   // Paris UTC+1, New York UTC-4

        // Assert: the shared window is 13:00Z to 16:00Z
        Assert.Equal(Clock.Utc("2027-03-16 13:00"), slots.First());
        Assert.Equal(Clock.Utc("2027-03-16 15:00"), slots.Last());
        Assert.Equal(9, slots.Count);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(241)]
    [InlineData(0)]
    public void A_duration_outside_fifteen_minutes_to_four_hours_is_refused(int minutes) {
        // Arrange
        Offices offices = OpenOffices();

        // Act
        Action slots = () => offices.Scene.Roster.MeetingSlots(Clock.Day("2027-01-12"), TimeSpan.FromMinutes(minutes), [offices.InParis.Id]);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("duration", slots);
    }

    [Fact]
    public void At_least_one_participant_is_needed() {
        // Arrange
        Offices offices = OpenOffices();

        // Act
        Action slots = () => offices.Scene.Roster.MeetingSlots(Clock.Day("2027-01-12"), TimeSpan.FromHours(1), []);

        // Assert
        Assert.Throws<ArgumentException>("participants", slots);
    }

    [Fact]
    public void An_unregistered_participant_is_refused() {
        // Arrange
        Offices offices = OpenOffices();
        EmployeeId stranger = Any.EmployeeId().Generate();

        // Act
        Action slots = () => offices.Scene.Roster.MeetingSlots(Clock.Day("2027-01-12"), TimeSpan.FromHours(1), [offices.InParis.Id, stranger]);

        // Assert
        Assert.Equal(RefusalReason.UnknownEmployee, Refusal.Of(slots));
    }

    [Fact]
    public void Missing_participants_are_refused() {
        // Arrange
        Offices offices = OpenOffices();

        // Act
        Action slots = () => offices.Scene.Roster.MeetingSlots(Clock.Day("2027-01-12"), TimeSpan.FromHours(1), null!);

        // Assert
        Assert.Throws<ArgumentNullException>("participants", slots);
    }

}
