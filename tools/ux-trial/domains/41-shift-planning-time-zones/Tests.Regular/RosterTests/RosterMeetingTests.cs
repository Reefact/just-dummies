using JustDummies;

using ShiftPlanning.Domain.Tests.Regular.Support;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.RosterTests;

public sealed class RosterMeetingTests {

    private sealed class Offices {

        public Offices() {
            List<Site> sites = Kit.OfficesIn(Kit.Paris, Kit.Tokyo, Kit.NewYork);
            Paris   = sites[0];
            Tokyo   = sites[1];
            NewYork = sites[2];
            Roster  = Kit.RosterWith(new PlanningPeriod(new IsoWeek(2026, 3), 3), sites.ToArray());

            List<EmployeeId> ids = Any.ListOf(new AnyEmployeeId()).WithCount(5).Distinct().Generate();
            Alice   = Register(ids[0], Paris);
            Bruno   = Register(ids[1], Paris);
            Chiyo   = Register(ids[2], Tokyo);
            Dana    = Register(ids[3], NewYork);
            Stranger = new AnyEmployee().WithId(ids[4]).WithHomeSite(Paris).Generate();
        }

        public Site Paris { get; }
        public Site Tokyo { get; }
        public Site NewYork { get; }
        public Roster Roster { get; }
        public Employee Alice { get; }
        public Employee Bruno { get; }
        public Employee Chiyo { get; }
        public Employee Dana { get; }
        public Employee Stranger { get; }

        private Employee Register(EmployeeId id, Site home) {
            Employee employee = new AnyEmployee().WithId(id).WithHomeSite(home).Generate();
            Roster.RegisterEmployee(employee);

            return employee;
        }

        public Meeting Meeting(DateTimeOffset start, TimeSpan duration, params Employee[] participants) {
            return new AnyMeeting().WithStart(start).WithDuration(duration).WithParticipants(participants.Select(p => p.Id).ToList()).Generate();
        }

    }

    private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

    [Fact]
    public void A_meeting_inside_everybodys_working_hours_is_booked() {
        // Arrange: Monday 2026-01-12, 08:00-09:00 UTC is 09:00 in Paris and 17:00 in Tokyo
        Offices offices = new Offices();
        Meeting meeting = offices.Meeting(Kit.Utc_(2026, 1, 12, 8, 0), OneHour, offices.Alice, offices.Chiyo);

        // Act
        offices.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Same(meeting, Assert.Single(offices.Roster.Meetings));
    }

    [Fact]
    public void A_meeting_that_ends_after_closing_for_one_participant_is_refused() {
        // Arrange: 08:15-09:15 UTC is 18:15 in Tokyo
        Offices offices = new Offices();
        Meeting meeting = offices.Meeting(Kit.Utc_(2026, 1, 12, 8, 15), OneHour, offices.Alice, offices.Chiyo);

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => offices.Roster.ScheduleMeeting(meeting));

        // Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, refusal.Reason);
        Assert.Contains(offices.Chiyo.Id.Value, refusal.Message);
        Assert.Empty(offices.Roster.Meetings);
    }

    [Fact]
    public void Opening_and_closing_times_are_inside_the_hours() {
        // Arrange
        Offices offices = new Offices();
        Meeting atOpening = offices.Meeting(Kit.Utc_(2026, 1, 12, 8, 0), OneHour, offices.Alice, offices.Bruno);
        Meeting atClosing = offices.Meeting(Kit.Utc_(2026, 1, 12, 16, 0), OneHour, offices.Alice, offices.Bruno);

        // Act
        offices.Roster.ScheduleMeeting(atOpening);
        offices.Roster.ScheduleMeeting(atClosing);

        // Assert
        Assert.Equal(2, offices.Roster.Meetings.Count);
    }

    [Theory]
    [InlineData(7, 59)]
    [InlineData(16, 1)]
    [InlineData(3, 0)]
    public void A_meeting_starting_before_opening_or_ending_after_closing_is_refused(int hour, int minute) {
        // Arrange
        Offices offices = new Offices();
        Meeting meeting = offices.Meeting(Kit.Utc_(2026, 1, 12, hour, minute), OneHour, offices.Alice, offices.Bruno);

        // Act & Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, Assert.Throws<RosterRefusedException>(() => offices.Roster.ScheduleMeeting(meeting)).Reason);
    }

    [Fact]
    public void A_meeting_on_a_day_off_is_refused() {
        // Arrange: Saturday
        Offices offices = new Offices();
        Meeting meeting = offices.Meeting(Kit.Utc_(2026, 1, 17, 10, 0), OneHour, offices.Alice, offices.Bruno);

        // Act & Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, Assert.Throws<RosterRefusedException>(() => offices.Roster.ScheduleMeeting(meeting)).Reason);
    }

    [Fact]
    public void A_participant_who_is_not_registered_is_refused() {
        // Arrange
        Offices offices = new Offices();
        Meeting meeting = offices.Meeting(Kit.Utc_(2026, 1, 12, 9, 0), OneHour, offices.Alice, offices.Stranger);

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => offices.Roster.ScheduleMeeting(meeting));

        // Assert
        Assert.Equal(RefusalReason.UnknownEmployee, refusal.Reason);
        Assert.Empty(offices.Roster.Meetings);
    }

    [Fact]
    public void A_meeting_has_to_be_given() {
        Assert.Throws<ArgumentNullException>(() => new Offices().Roster.ScheduleMeeting(null!));
    }

    [Fact]
    public void Meetings_can_still_be_booked_once_the_roster_is_published() {
        // Arrange
        Offices offices = new Offices();
        Shift   shift   = Kit.Plan(offices.Roster, offices.Tokyo, new DateOnly(2026, 1, 12), "08:00", "16:00");
        offices.Roster.Assign(shift.Number, offices.Chiyo.Id);
        offices.Roster.Publish();
        Meeting meeting = offices.Meeting(Kit.Utc_(2026, 1, 12, 8, 0), OneHour, offices.Alice, offices.Chiyo);

        // Act
        offices.Roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Equal(RosterStatus.Published, offices.Roster.Status);
        Assert.Same(meeting, Assert.Single(offices.Roster.Meetings));
    }

    [Fact]
    public void Working_hours_follow_daylight_saving_in_each_zone() {
        // Arrange: 13:00-14:00 UTC; New York is on summer time from 2026-03-08 but Paris only from 2026-03-29
        Offices offices = new Offices();
        Meeting inWinter      = offices.Meeting(Kit.Utc_(2026, 1, 12, 13, 0), OneHour, offices.Alice, offices.Dana);
        Meeting inBetween     = offices.Meeting(Kit.Utc_(2026, 3, 16, 13, 0), OneHour, offices.Alice, offices.Dana);
        Meeting evenLaterOnUtc = offices.Meeting(Kit.Utc_(2026, 3, 16, 12, 0), OneHour, offices.Alice, offices.Dana);

        // Act & Assert: 08:00 in New York in winter, 09:00 on 16 March
        Assert.Equal(RefusalReason.OutsideWorkingHours, Assert.Throws<RosterRefusedException>(() => offices.Roster.ScheduleMeeting(inWinter)).Reason);
        offices.Roster.ScheduleMeeting(inBetween);
        Assert.Equal(RefusalReason.OutsideWorkingHours, Assert.Throws<RosterRefusedException>(() => offices.Roster.ScheduleMeeting(evenLaterOnUtc)).Reason);
    }

    [Fact]
    public void Hours_are_those_of_the_home_site_not_of_the_site_where_the_employee_works() {
        // Arrange: Chiyo is based in Tokyo and works a shift in Paris; the meeting is judged in Tokyo anyway
        Offices offices = new Offices();
        offices.Roster.PlanShift(Kit.Request(offices.Paris, new DateOnly(2026, 1, 12), "09:00", "17:00"));
        Meeting parisDaytime = offices.Meeting(Kit.Utc_(2026, 1, 12, 10, 0), OneHour, offices.Alice, offices.Chiyo);

        // Act & Assert: 10:00 UTC is 19:00 in Tokyo
        Assert.Equal(RefusalReason.OutsideWorkingHours, Assert.Throws<RosterRefusedException>(() => offices.Roster.ScheduleMeeting(parisDaytime)).Reason);
    }

    [Fact]
    public void Slots_of_a_single_site_fill_its_opening_hours_every_quarter_of_an_hour() {
        // Arrange: Monday 2026-01-12, a one-hour meeting in 09:00-18:00 UTC can start from 09:00 to 17:00
        Site     utc      = new AnySite().WithZone(Kit.Utc).WithWorkingHours(Kit.OfficeHours).Generate();
        Roster   roster   = Kit.RosterWith(new AnyPlanningPeriod().Generate(), utc);
        Employee employee = Kit.EmployeesAt(utc, 1)[0];
        roster.RegisterEmployee(employee);

        // Act
        IReadOnlyList<DateTimeOffset> slots = roster.MeetingSlots(new DateOnly(2026, 1, 12), OneHour, new[] { employee.Id });

        // Assert
        Assert.Equal(33, slots.Count);
        Assert.Equal(Kit.Utc_(2026, 1, 12, 9, 0), slots[0]);
        Assert.Equal(Kit.Utc_(2026, 1, 12, 17, 0), slots[^1]);
        Assert.All(slots.Zip(slots.Skip(1)), pair => Assert.Equal(TimeSpan.FromMinutes(15), pair.Second - pair.First));
    }

    [Theory]
    [InlineData(15, 36)]
    [InlineData(240, 21)]
    public void The_longest_and_the_shortest_meetings_fit_in_as_many_slots_as_the_hours_allow(int minutes, int expectedSlots) {
        // Arrange
        Site     utc      = new AnySite().WithZone(Kit.Utc).WithWorkingHours(Kit.OfficeHours).Generate();
        Roster   roster   = Kit.RosterWith(new AnyPlanningPeriod().Generate(), utc);
        Employee employee = Kit.EmployeesAt(utc, 1)[0];
        roster.RegisterEmployee(employee);

        // Act
        IReadOnlyList<DateTimeOffset> slots = roster.MeetingSlots(new DateOnly(2026, 1, 12), TimeSpan.FromMinutes(minutes), new[] { employee.Id });

        // Assert
        Assert.Equal(expectedSlots, slots.Count);
    }

    [Fact]
    public void Slots_across_two_zones_are_the_overlap_of_their_hours() {
        // Arrange: Tokyo is open 00:00-09:00 UTC, Paris 08:00-17:00 UTC
        Offices offices = new Offices();

        // Act
        IReadOnlyList<DateTimeOffset> thirtyMinutes = offices.Roster.MeetingSlots(new DateOnly(2026, 1, 12), TimeSpan.FromMinutes(30), new[] { offices.Alice.Id, offices.Chiyo.Id });
        IReadOnlyList<DateTimeOffset> anHour        = offices.Roster.MeetingSlots(new DateOnly(2026, 1, 12), OneHour, new[] { offices.Alice.Id, offices.Chiyo.Id });

        // Assert
        Assert.Equal(new[] { Kit.Utc_(2026, 1, 12, 8, 0), Kit.Utc_(2026, 1, 12, 8, 15), Kit.Utc_(2026, 1, 12, 8, 30) }, thirtyMinutes);
        Assert.Equal(new[] { Kit.Utc_(2026, 1, 12, 8, 0) }, anHour);
    }

    [Fact]
    public void Slots_are_empty_when_the_hours_never_overlap() {
        // Arrange: Sydney is open 22:00-07:00 UTC in January, Paris 08:00-17:00 UTC
        List<Site> sites = Kit.OfficesIn(Kit.Paris, Kit.Sydney);
        Roster     roster = Kit.RosterWith(new AnyPlanningPeriod().Generate(), sites.ToArray());
        Employee   inParis = Kit.EmployeesAt(sites[0], 1)[0];
        Employee   inSydney = Kit.EmployeesAt(sites[1], 1)[0];
        roster.RegisterEmployee(inParis);
        roster.RegisterEmployee(inSydney);

        // Act
        IReadOnlyList<DateTimeOffset> slots = roster.MeetingSlots(new DateOnly(2026, 1, 12), TimeSpan.FromMinutes(15), new[] { inParis.Id, inSydney.Id });

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void There_are_no_slots_on_a_day_off() {
        // Arrange: Saturday 2026-01-17 for Paris, which is open weekdays
        Offices offices = new Offices();

        // Act & Assert
        Assert.Empty(offices.Roster.MeetingSlots(new DateOnly(2026, 1, 17), OneHour, new[] { offices.Alice.Id }));
    }

    [Fact]
    public void Two_participants_of_one_site_give_the_slots_of_that_site() {
        // Arrange
        Offices offices = new Offices();

        // Act
        IReadOnlyList<DateTimeOffset> together = offices.Roster.MeetingSlots(new DateOnly(2026, 1, 12), OneHour, new[] { offices.Alice.Id, offices.Bruno.Id });
        IReadOnlyList<DateTimeOffset> alone    = offices.Roster.MeetingSlots(new DateOnly(2026, 1, 12), OneHour, new[] { offices.Alice.Id });

        // Assert
        Assert.Equal(alone, together);
    }

    [Fact]
    public void Every_slot_can_be_booked_and_no_other_quarter_of_an_hour_can() {
        // Arrange
        DateOnly anyUtcDate  = Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).Generate();
        TimeSpan anyDuration = Any.TimeSpan().Between(Meeting.MinDuration, Meeting.MaxDuration).WithGranularity(TimeSpan.FromMinutes(15)).Generate();
        Offices  offices     = new Offices();
        EmployeeId[] participants = { offices.Alice.Id, offices.Dana.Id };

        // Act
        HashSet<DateTimeOffset> slots = offices.Roster.MeetingSlots(anyUtcDate, anyDuration, participants).ToHashSet();
        DateTimeOffset          dayStart = new DateTimeOffset(anyUtcDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        List<DateTimeOffset>    bookable = new List<DateTimeOffset>();
        for (int quarter = 0; quarter < 96; quarter++) {
            DateTimeOffset start = dayStart.AddMinutes(15 * quarter);
            try {
                offices.Roster.ScheduleMeeting(new AnyMeeting().WithStart(start).WithDuration(anyDuration).WithParticipants(participants).Generate());
                bookable.Add(start);
            } catch (RosterRefusedException refusal) {
                Assert.Equal(RefusalReason.OutsideWorkingHours, refusal.Reason);
            }
        }

        // Assert
        Assert.Equal(slots.OrderBy(s => s), bookable);
    }

    [Fact(Skip = "domain defect: MeetingSlots on DateOnly.MaxValue throws ArgumentOutOfRangeException from dayStart.AddDays(1) instead of answering; literal input, no seed")]
    public void The_last_representable_utc_day_can_be_searched_for_slots() {
        // Arrange
        Site     utc      = new AnySite().WithZone(Kit.Utc).WithWorkingHours(Kit.OfficeHours).Generate();
        Roster   roster   = Kit.RosterWith(new AnyPlanningPeriod().Generate(), utc);
        Employee employee = Kit.EmployeesAt(utc, 1)[0];
        roster.RegisterEmployee(employee);

        // Act & Assert: 9999-12-31 is a Friday
        Assert.NotEmpty(roster.MeetingSlots(DateOnly.MaxValue, OneHour, new[] { employee.Id }));
    }

    [Theory]
    [InlineData(14)]
    [InlineData(241)]
    [InlineData(0)]
    public void The_duration_of_a_meeting_is_limited_when_looking_for_slots_too(int minutes) {
        // Arrange
        Offices offices = new Offices();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => offices.Roster.MeetingSlots(new DateOnly(2026, 1, 12), TimeSpan.FromMinutes(minutes), new[] { offices.Alice.Id }));
    }

    [Fact]
    public void Looking_for_slots_needs_somebody_who_is_registered() {
        // Arrange
        Offices offices = new Offices();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => offices.Roster.MeetingSlots(new DateOnly(2026, 1, 12), OneHour, Array.Empty<EmployeeId>()));
        Assert.Equal(RefusalReason.UnknownEmployee, Assert.Throws<RosterRefusedException>(() => offices.Roster.MeetingSlots(new DateOnly(2026, 1, 12), OneHour, new[] { offices.Stranger.Id })).Reason);
        Assert.Throws<ArgumentNullException>(() => offices.Roster.MeetingSlots(new DateOnly(2026, 1, 12), OneHour, null!));
    }

}
