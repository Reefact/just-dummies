using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Discovery;

public sealed class RosterTests {

    private static readonly SiteZone Paris   = new SiteZone("Europe/Paris");
    private static readonly SiteZone NewYork = new SiteZone("America/New_York");
    private static readonly SiteZone Tokyo   = new SiteZone("Asia/Tokyo");
    private static readonly SiteZone Troll   = new SiteZone("Antarctica/Troll");

    // Spring period: Monday 2026-03-16 to Sunday 2026-06-14 (13 weeks), holding the Paris spring-forward of 29 March.
    private static PlanningPeriod SpringPeriod() => new PlanningPeriod(new IsoWeek(2026, 12), 13);

    // Autumn period: Monday 2026-09-28 to Sunday 2026-12-27, holding the Paris fall-back of 25 October and the New York one of 1 November.
    private static PlanningPeriod AutumnPeriod() => new PlanningPeriod(new IsoWeek(2026, 40), 13);

    // Ids of sites (and of employees) registered in one roster must differ: say so rather than rely on chance.
    private static Site SiteIn(SiteZone zone, params Site[] distinctFrom) {
        return new AnySite().WithZone(zone).WithId(i => i.WithValue(v => v.Except(distinctFrom.Select(s => s.Id.Value)))).Generate();
    }

    private static Employee EmployeeAt(Site site, params Employee[] distinctFrom) {
        return new AnyEmployee().WithHomeSite(site).WithId(i => i.WithValue(v => v.Except(distinctFrom.Select(e => e.Id.Value)))).Generate();
    }

    private static Roster RosterFor(PlanningPeriod period, params Site[] sites) {
        Roster roster = new Roster(period);
        foreach (Site site in sites) {
            roster.RegisterSite(site);
        }

        return roster;
    }

    private static ShiftRequest Request(Site site, DateOnly date, TimeOnly start, TimeOnly end, OffsetChoice startChoice = OffsetChoice.Unspecified, OffsetChoice endChoice = OffsetChoice.Unspecified) {
        TimeSpan length = end - start;
        if (length < TimeSpan.Zero) { length += TimeSpan.FromDays(1); }

        return new AnyShiftRequest().WithSite(site.Id)
                                    .WithStartDate(date)
                                    .WithStartTime(start)
                                    .WithWallClockLength(length)
                                    .WithStartChoice(startChoice)
                                    .WithEndChoice(endChoice)
                                    .Generate();
    }

    private static Shift Plan(Roster roster, Site site, DateOnly date, TimeOnly start, TimeOnly end) {
        return roster.PlanShift(Request(site, date, start, end));
    }

    private static RefusalReason ReasonOf(Action action) {
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(action);
        return refusal.Reason;
    }

    private static DateOnly D(int month, int day) => new DateOnly(2026, month, day);

    private static TimeOnly T(int hour, int minute = 0) => new TimeOnly(hour, minute);

    // ---------------------------------------------------------------- construction and registration

    [Fact]
    public void A_new_roster_is_a_draft_with_nothing_in_it() {
        // Arrange
        PlanningPeriod period = new AnyPlanningPeriod().Generate();

        // Act
        Roster roster = new Roster(period);

        // Assert
        Assert.Same(period, roster.Period);
        Assert.Equal(RosterStatus.Draft, roster.Status);
        Assert.Empty(roster.Sites);
        Assert.Empty(roster.Employees);
        Assert.Empty(roster.Shifts);
        Assert.Empty(roster.Meetings);
    }

    [Fact]
    public void A_roster_needs_a_period() {
        Assert.Throws<ArgumentNullException>(() => new Roster(null!));
    }

    [Fact]
    public void A_registered_site_and_employee_are_listed() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);

        // Act
        roster.RegisterEmployee(employee);

        // Assert
        Assert.Same(site, Assert.Single(roster.Sites));
        Assert.Same(employee, Assert.Single(roster.Employees));
    }

    [Fact]
    public void A_site_registered_twice_is_a_duplicate() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);

        // Assert
        Assert.Equal(RefusalReason.DuplicateSite, ReasonOf(() => roster.RegisterSite(site)));
    }

    [Fact]
    public void A_different_site_with_the_same_id_is_a_duplicate_too() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Site   twin   = new AnySite().WithId(site.Id).WithZone(Tokyo).Generate();
        Roster roster = RosterFor(SpringPeriod(), site);

        // Assert
        Assert.Equal(RefusalReason.DuplicateSite, ReasonOf(() => roster.RegisterSite(twin)));
    }

    [Fact]
    public void An_employee_whose_home_site_is_not_registered_is_refused() {
        // Arrange
        Roster roster = RosterFor(SpringPeriod(), SiteIn(Paris));

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, ReasonOf(() => roster.RegisterEmployee(EmployeeAt(SiteIn(Tokyo)))));
    }

    [Fact]
    public void An_employee_registered_twice_is_a_duplicate() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);

        // Assert
        Assert.Equal(RefusalReason.DuplicateEmployee, ReasonOf(() => roster.RegisterEmployee(employee)));
    }

    [Fact]
    public void Null_arguments_are_refused_with_argument_null_exceptions() {
        // Arrange
        Roster     roster = RosterFor(SpringPeriod(), SiteIn(Paris));
        EmployeeId anyId  = new AnyEmployeeId().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => roster.RegisterSite(null!));
        Assert.Throws<ArgumentNullException>(() => roster.RegisterEmployee(null!));
        Assert.Throws<ArgumentNullException>(() => roster.PlanShift(null!));
        Assert.Throws<ArgumentNullException>(() => roster.Assign(1, null!));
        Assert.Throws<ArgumentNullException>(() => roster.Check(1, null!));
        Assert.Throws<ArgumentNullException>(() => roster.ShiftsOf(null!));
        Assert.Throws<ArgumentNullException>(() => roster.ShortestRest(null!));
        Assert.Throws<ArgumentNullException>(() => roster.ElapsedTimeIn(null!, new IsoWeek(2026, 13)));
        Assert.Throws<ArgumentNullException>(() => roster.ElapsedTimeIn(anyId, null!));
        Assert.Throws<ArgumentNullException>(() => roster.ScheduleMeeting(null!));
        Assert.Throws<ArgumentNullException>(() => roster.MeetingSlots(D(6, 17), TimeSpan.FromHours(1), null!));
    }

    [Fact(Skip = "domain defect: RegisterEmployee only compares the home site's id, so an employee whose HomeSite is another Site (same id, other zone and hours) is accepted and its own zone then drives weeks and meetings, not the registered site's. Arguable against MODEL.md ('home site not registered'); no drawn value decides it (ids are pinned equal)")]
    public void An_employee_whose_home_site_only_shares_the_id_of_a_registered_site_is_refused() {
        // Arrange
        Site     registered = SiteIn(Paris);
        Site     twin       = new AnySite().WithId(registered.Id).WithZone(Tokyo).Generate();
        Roster   roster     = RosterFor(SpringPeriod(), registered);

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, ReasonOf(() => roster.RegisterEmployee(EmployeeAt(twin))));
    }

    // ---------------------------------------------------------------- planning

    [Fact]
    public void Planned_shifts_are_numbered_from_one_and_start_unstaffed() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);

        // Act
        Shift first  = Plan(roster, site, D(4, 6), T(8), T(16));
        Shift second = Plan(roster, site, D(4, 7), T(8), T(16));

        // Assert
        Assert.Equal(1, first.Number);
        Assert.Equal(2, second.Number);
        Assert.False(first.IsStaffed);
        Assert.Same(first, roster.FindShift(1));
        Assert.Same(second, roster.FindShift(2));
        Assert.Equal(2, roster.Shifts.Count);
    }

    [Fact]
    public void Finding_a_shift_that_does_not_exist_gives_nothing() {
        Assert.Null(RosterFor(SpringPeriod(), SiteIn(Paris)).FindShift(7));
    }

    [Fact]
    public void A_shift_for_an_unregistered_site_is_refused() {
        // Arrange
        Roster roster = RosterFor(SpringPeriod(), SiteIn(Paris));
        Site   other  = SiteIn(Tokyo);

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, ReasonOf(() => Plan(roster, other, D(4, 6), T(8), T(16))));
    }

    [Fact]
    public void A_shift_starting_on_the_first_or_last_day_of_the_period_is_accepted() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);

        // Act
        Shift onFirstDay = Plan(roster, site, new DateOnly(2026, 3, 16), T(8), T(16));
        Shift onLastDay  = Plan(roster, site, new DateOnly(2026, 6, 14), T(8), T(16));

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 16), onFirstDay.Request.StartDate);
        Assert.Equal(new DateOnly(2026, 6, 14), onLastDay.Request.StartDate);
    }

    [Fact]
    public void A_night_shift_starting_on_the_last_day_may_end_after_the_period() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);

        // Act
        Shift shift = Plan(roster, site, new DateOnly(2026, 6, 14), T(22), T(6));

        // Assert
        Assert.Equal(new DateOnly(2026, 6, 15), shift.Request.EndDate);
    }

    [Fact]
    public void A_shift_starting_the_day_before_or_after_the_period_is_outside_it() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);

        // Assert
        Assert.Equal(RefusalReason.OutsidePlanningPeriod, ReasonOf(() => Plan(roster, site, new DateOnly(2026, 3, 15), T(22), T(6))));
        Assert.Equal(RefusalReason.OutsidePlanningPeriod, ReasonOf(() => Plan(roster, site, new DateOnly(2026, 6, 15), T(8), T(16))));
    }

    [Fact]
    public void A_start_inside_the_spring_forward_gap_is_refused() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);

        // Assert
        Assert.Equal(RefusalReason.StartTimeDoesNotExist, ReasonOf(() => Plan(roster, site, D(3, 29), T(2, 30), T(10))));
    }

    [Fact]
    public void An_end_inside_the_spring_forward_gap_is_refused() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);

        // Assert
        Assert.Equal(RefusalReason.EndTimeDoesNotExist, ReasonOf(() => Plan(roster, site, D(3, 28), T(22), T(2, 30))));
    }

    [Fact]
    public void An_ambiguous_start_without_a_choice_is_refused_and_with_either_choice_accepted() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(AutumnPeriod(), site);

        // Assert
        Assert.Equal(RefusalReason.StartTimeAmbiguous, ReasonOf(() => Plan(roster, site, D(10, 25), T(2, 30), T(10))));
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero), roster.PlanShift(Request(site, D(10, 25), T(2, 30), T(10), OffsetChoice.Earlier)).StartUtc);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero), roster.PlanShift(Request(site, D(10, 25), T(2, 30), T(10), OffsetChoice.Later)).StartUtc);
    }

    [Fact]
    public void An_ambiguous_end_without_a_choice_is_refused_and_with_one_accepted() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(AutumnPeriod(), site);

        // Assert
        Assert.Equal(RefusalReason.EndTimeAmbiguous, ReasonOf(() => Plan(roster, site, D(10, 24), T(22), T(2, 30))));
        Shift shift = roster.PlanShift(Request(site, D(10, 24), T(22), T(2, 30), endChoice: OffsetChoice.Later));
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero), shift.EndUtc);
        Assert.Equal(TimeSpan.FromHours(5.5), shift.ElapsedDuration);
    }

    [Fact]
    public void Choices_that_make_the_shift_end_before_it_starts_are_refused() {
        // Arrange: see the Shift tests, Antarctica/Troll turns back two hours on 25 October 2026
        Site   troll  = SiteIn(Troll);
        Roster roster = RosterFor(AutumnPeriod(), troll);

        // Assert
        Assert.Equal(RefusalReason.EndNotAfterStart, ReasonOf(() => roster.PlanShift(Request(troll, D(10, 25), T(1), T(2), OffsetChoice.Later, OffsetChoice.Earlier))));
        Assert.Empty(roster.Shifts);
    }

    [Fact]
    public void A_cancelled_shift_leaves_the_roster_and_its_number_is_not_reused() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);
        Shift  first  = Plan(roster, site, D(4, 6), T(8), T(16));

        // Act
        roster.CancelShift(first.Number);
        Shift next = Plan(roster, site, D(4, 6), T(8), T(16));

        // Assert
        Assert.Null(roster.FindShift(first.Number));
        Assert.Equal(2, next.Number);
        Assert.Same(next, Assert.Single(roster.Shifts));
    }

    [Fact]
    public void Cancelling_an_unknown_shift_is_refused() {
        Assert.Equal(RefusalReason.UnknownShift, ReasonOf(() => RosterFor(SpringPeriod(), SiteIn(Paris)).CancelShift(1)));
    }

    [Fact]
    public void A_cancelled_staffed_shift_no_longer_counts_for_the_employee() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift shift = Plan(roster, site, D(4, 6), T(8), T(16));
        roster.Assign(shift.Number, employee.Id);

        // Act
        roster.CancelShift(shift.Number);

        // Assert
        Assert.Empty(roster.ShiftsOf(employee.Id));
        Assert.Equal(TimeSpan.Zero, roster.ElapsedTimeIn(employee.Id, IsoWeek.Of(D(4, 6))));
    }

    // ---------------------------------------------------------------- assignment

    [Fact]
    public void Assigning_staffs_the_shift_and_lists_it_for_the_employee() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift shift = Plan(roster, site, D(4, 6), T(8), T(16));

        // Act
        roster.Assign(shift.Number, employee.Id);

        // Assert
        Assert.Equal(employee.Id, shift.AssignedTo);
        Assert.True(shift.IsStaffed);
        Assert.Same(shift, Assert.Single(roster.ShiftsOf(employee.Id)));
    }

    [Fact]
    public void Check_reports_none_when_allowed_and_assigns_nothing() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift shift = Plan(roster, site, D(4, 6), T(8), T(16));

        // Assert
        Assert.Equal(RefusalReason.None, roster.Check(shift.Number, employee.Id));
        Assert.False(shift.IsStaffed);
    }

    [Fact]
    public void Assigning_an_unknown_shift_or_employee_is_refused() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift shift = Plan(roster, site, D(4, 6), T(8), T(16));

        // Assert
        Assert.Equal(RefusalReason.UnknownShift, ReasonOf(() => roster.Assign(shift.Number + 1, employee.Id)));
        Assert.Equal(RefusalReason.UnknownEmployee, ReasonOf(() => roster.Assign(shift.Number, new AnyEmployeeId().WithValue(v => v.DifferentFrom(employee.Id.Value)).Generate())));
        Assert.Equal(RefusalReason.UnknownShift, roster.Check(0, employee.Id));
    }

    [Fact]
    public void A_staffed_shift_cannot_be_assigned_again_even_to_the_same_employee() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Employee other    = EmployeeAt(site, employee);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        roster.RegisterEmployee(other);
        Shift shift = Plan(roster, site, D(4, 6), T(8), T(16));
        roster.Assign(shift.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.ShiftAlreadyAssigned, ReasonOf(() => roster.Assign(shift.Number, other.Id)));
        Assert.Equal(RefusalReason.ShiftAlreadyAssigned, ReasonOf(() => roster.Assign(shift.Number, employee.Id)));
    }

    [Fact]
    public void An_employee_cannot_work_two_overlapping_shifts_even_at_different_sites() {
        // Arrange: 08:00-16:00 in Tokyo on 7 April ends 07:00Z; 08:00-16:00 in Paris on 6 April starts 06:00Z on the 6th, so use the same instants
        Site     paris    = SiteIn(Paris);
        Site     tokyo    = SiteIn(Tokyo, paris);
        Employee employee = EmployeeAt(paris);
        Roster   roster   = RosterFor(SpringPeriod(), tokyo, paris);
        roster.RegisterEmployee(employee);
        Shift inParis = Plan(roster, paris, D(4, 6), T(8), T(16));   // 06:00Z-14:00Z
        Shift inTokyo = Plan(roster, tokyo, D(4, 6), T(15), T(23));  // 06:00Z-14:00Z
        roster.Assign(inParis.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.OverlapsAnotherShift, ReasonOf(() => roster.Assign(inTokyo.Number, employee.Id)));
    }

    [Fact]
    public void Eleven_hours_of_rest_are_enough_and_ten_hours_fifty_nine_are_not() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift first    = Plan(roster, site, D(4, 6), T(8), T(16));
        Shift exactly  = Plan(roster, site, D(4, 7), T(3), T(11));
        Shift tooClose = Plan(roster, site, D(4, 7), T(2, 59), T(10, 59));
        roster.Assign(first.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.InsufficientRest, ReasonOf(() => roster.Assign(tooClose.Number, employee.Id)));
        Assert.Equal(RefusalReason.None, roster.Check(exactly.Number, employee.Id));
        roster.Assign(exactly.Number, employee.Id);
        Assert.Equal(TimeSpan.FromHours(11), roster.ShortestRest(employee.Id));
    }

    [Fact]
    public void Rest_is_checked_against_the_shift_before_and_the_one_after() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift later  = Plan(roster, site, D(4, 7), T(8), T(16));
        Shift before = Plan(roster, site, D(4, 6), T(20), T(23));
        roster.Assign(later.Number, employee.Id);

        // Assert: 23:00 to 08:00 is only 9 hours
        Assert.Equal(RefusalReason.InsufficientRest, ReasonOf(() => roster.Assign(before.Number, employee.Id)));
    }

    [Fact]
    public void Rest_across_spring_forward_is_one_hour_shorter_than_the_clock_says() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift saturday = Plan(roster, site, D(3, 28), T(14), T(22));
        Shift sunday   = Plan(roster, site, D(3, 29), T(9), T(17));
        roster.Assign(saturday.Number, employee.Id);

        // Assert: 22:00 Saturday to 09:00 Sunday reads 11 h but lasts 10
        Assert.Equal(RefusalReason.InsufficientRest, roster.Check(sunday.Number, employee.Id));
    }

    [Fact]
    public void Rest_across_fall_back_is_one_hour_longer_than_the_clock_says() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(AutumnPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift saturday = Plan(roster, site, D(10, 24), T(14), T(22));
        Shift sunday   = Plan(roster, site, D(10, 25), T(8), T(16));
        roster.Assign(saturday.Number, employee.Id);

        // Assert: 22:00 Saturday to 08:00 Sunday reads 10 h but lasts 11
        Assert.Equal(RefusalReason.None, roster.Check(sunday.Number, employee.Id));
    }

    [Fact]
    public void Forty_eight_hours_in_a_week_are_allowed_and_a_minute_more_is_not() {
        // Arrange: four 12 h shifts on Monday to Thursday, 2026-04-06 is a Monday
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        for (int day = 6; day <= 9; day++) {
            roster.Assign(Plan(roster, site, D(4, day), T(8), T(20)).Number, employee.Id);
        }

        Shift oneMinute = roster.PlanShift(Request(site, D(4, 10), T(8), T(9)));

        // Assert
        Assert.Equal(TimeSpan.FromHours(48), roster.ElapsedTimeIn(employee.Id, IsoWeek.Of(D(4, 6))));
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, ReasonOf(() => roster.Assign(oneMinute.Number, employee.Id)));
    }

    [Fact]
    public void The_week_limit_is_exactly_48_hours_not_47() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        for (int day = 6; day <= 8; day++) {
            roster.Assign(Plan(roster, site, D(4, day), T(8), T(20)).Number, employee.Id);
        }

        Shift twelve = Plan(roster, site, D(4, 9), T(8), T(20));
        Shift thirteen = roster.PlanShift(Request(site, D(4, 10), T(8), T(20)));

        // Assert
        Assert.Equal(RefusalReason.None, roster.Check(twelve.Number, employee.Id));
        roster.Assign(twelve.Number, employee.Id);
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, roster.Check(thirteen.Number, employee.Id));
    }

    [Fact]
    public void The_weekly_limit_counts_elapsed_time_so_a_night_lost_to_spring_forward_makes_room() {
        // Arrange: week 13 of 2026 runs Monday 23 March to Sunday 29 March
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        roster.Assign(Plan(roster, site, D(3, 23), T(8), T(20)).Number, employee.Id);
        roster.Assign(Plan(roster, site, D(3, 24), T(8), T(20)).Number, employee.Id);
        roster.Assign(Plan(roster, site, D(3, 25), T(8), T(20)).Number, employee.Id);
        Shift saturdayNight = Plan(roster, site, D(3, 28), T(20), T(8)); // 12 h on the clock, 11 h elapsed
        roster.Assign(saturdayNight.Number, employee.Id);
        Shift oneHour = Plan(roster, site, D(3, 27), T(7), T(8));
        Shift twoHours = Plan(roster, site, D(3, 26), T(7), T(9));

        // Assert: 36 + 11 = 47 elapsed, so one more hour fits (48) while two do not (49)
        Assert.Equal(TimeSpan.FromHours(47), roster.ElapsedTimeIn(employee.Id, new IsoWeek(2026, 13)));
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, roster.Check(twoHours.Number, employee.Id));
        Assert.Equal(RefusalReason.None, roster.Check(oneHour.Number, employee.Id));
    }

    [Fact]
    public void A_shift_counts_wholly_toward_the_week_in_which_it_starts() {
        // Arrange: a Sunday night shift runs into the next ISO week
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift sundayNight = Plan(roster, site, D(4, 12), T(20), T(8));
        roster.Assign(sundayNight.Number, employee.Id);

        // Assert: week 15 is 6-12 April, week 16 starts on the 13th
        Assert.Equal(TimeSpan.FromHours(12), roster.ElapsedTimeIn(employee.Id, new IsoWeek(2026, 15)));
        Assert.Equal(TimeSpan.Zero, roster.ElapsedTimeIn(employee.Id, new IsoWeek(2026, 16)));
    }

    [Fact]
    public void The_week_is_read_in_the_employees_home_zone_whatever_the_site_of_the_shift() {
        // Arrange: Monday 15 June 07:00 in Tokyo is still Sunday 14 June 18:00 in New York
        Site     tokyo    = SiteIn(Tokyo);
        Site     newYork  = SiteIn(NewYork, tokyo);
        Employee tokyoer  = EmployeeAt(tokyo);
        Employee yorker   = EmployeeAt(newYork, tokyoer);
        Roster   roster   = RosterFor(new PlanningPeriod(new IsoWeek(2026, 24), 3), tokyo, newYork);
        roster.RegisterEmployee(tokyoer);
        roster.RegisterEmployee(yorker);
        Shift shift = Plan(roster, tokyo, D(6, 15), T(7), T(15));

        // Act
        roster.Assign(shift.Number, tokyoer.Id);
        Shift second = Plan(roster, tokyo, D(6, 15), T(7), T(15));
        roster.Assign(second.Number, yorker.Id);

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), roster.ElapsedTimeIn(tokyoer.Id, new IsoWeek(2026, 25)));
        Assert.Equal(TimeSpan.Zero, roster.ElapsedTimeIn(tokyoer.Id, new IsoWeek(2026, 24)));
        Assert.Equal(TimeSpan.FromHours(8), roster.ElapsedTimeIn(yorker.Id, new IsoWeek(2026, 24)));
        Assert.Equal(TimeSpan.Zero, roster.ElapsedTimeIn(yorker.Id, new IsoWeek(2026, 25)));
    }

    [Fact]
    public void The_weekly_limit_follows_the_home_zone_week_rather_than_the_site_week() {
        // Arrange: a New York employee works 4 x 12 h in Tokyo Monday to Thursday and then a Monday morning in Tokyo
        Site     tokyo   = SiteIn(Tokyo);
        Site     newYork = SiteIn(NewYork, tokyo);
        Employee yorker  = EmployeeAt(newYork);
        Roster   roster  = RosterFor(new PlanningPeriod(new IsoWeek(2026, 24), 3), tokyo, newYork);
        roster.RegisterEmployee(yorker);
        for (int day = 9; day <= 12; day++) {
            roster.Assign(Plan(roster, tokyo, D(6, day), T(10), T(22)).Number, yorker.Id);   // Tokyo 10:00 is 21:00 the evening before in New York
        }

        Shift mondayMorning = Plan(roster, tokyo, D(6, 15), T(7), T(15)); // Sunday 18:00 in New York: still week 24 there

        // Assert
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, roster.Check(mondayMorning.Number, yorker.Id));
    }

    [Fact]
    public void Checks_come_in_a_fixed_order() {
        // Arrange: a shift that is already staffed, overlaps, lacks rest and breaks the week all at once
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Employee other    = EmployeeAt(site, employee);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        roster.RegisterEmployee(other);
        Shift staffed = Plan(roster, site, D(4, 6), T(8), T(20));
        roster.Assign(staffed.Number, employee.Id);
        Shift overlaps = Plan(roster, site, D(4, 6), T(10), T(12));

        // Assert: unknown shift beats unknown employee, staffed beats overlap
        EmployeeId stranger = new AnyEmployeeId().WithValue(v => v.DifferentFrom(employee.Id.Value).DifferentFrom(other.Id.Value)).Generate();
        Assert.Equal(RefusalReason.UnknownShift, roster.Check(99, stranger));
        Assert.Equal(RefusalReason.UnknownEmployee, roster.Check(overlaps.Number, stranger));
        Assert.Equal(RefusalReason.ShiftAlreadyAssigned, roster.Check(staffed.Number, employee.Id));
        Assert.Equal(RefusalReason.OverlapsAnotherShift, roster.Check(overlaps.Number, employee.Id));
        Assert.Equal(RefusalReason.None, roster.Check(overlaps.Number, other.Id));
    }

    [Fact]
    public void Insufficient_rest_is_reported_before_the_weekly_limit() {
        // Arrange: four 12 h shifts, then a shift starting 2 h after the last one ends: both rules break
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        roster.Assign(Plan(roster, site, D(4, 6), T(8), T(20)).Number, employee.Id);
        roster.Assign(Plan(roster, site, D(4, 7), T(8), T(20)).Number, employee.Id);
        roster.Assign(Plan(roster, site, D(4, 8), T(8), T(20)).Number, employee.Id);
        roster.Assign(Plan(roster, site, D(4, 9), T(8), T(20)).Number, employee.Id);
        Shift lateThursday = Plan(roster, site, D(4, 9), T(22), T(23));

        // Assert
        Assert.Equal(RefusalReason.InsufficientRest, roster.Check(lateThursday.Number, employee.Id));
    }

    // ---------------------------------------------------------------- unassign, publish

    [Fact]
    public void Unassigning_frees_the_shift() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift shift = Plan(roster, site, D(4, 6), T(8), T(16));
        roster.Assign(shift.Number, employee.Id);

        // Act
        roster.Unassign(shift.Number);

        // Assert
        Assert.False(shift.IsStaffed);
        Assert.Null(shift.AssignedTo);
        Assert.Empty(roster.ShiftsOf(employee.Id));
    }

    [Fact]
    public void Unassigning_refuses_an_unknown_or_unstaffed_shift() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);
        Shift  shift  = Plan(roster, site, D(4, 6), T(8), T(16));

        // Assert
        Assert.Equal(RefusalReason.UnknownShift, ReasonOf(() => roster.Unassign(shift.Number + 1)));
        Assert.Equal(RefusalReason.ShiftNotAssigned, ReasonOf(() => roster.Unassign(shift.Number)));
    }

    [Fact]
    public void A_roster_with_no_shift_cannot_be_published() {
        Assert.Equal(RefusalReason.NothingToPublish, ReasonOf(() => RosterFor(SpringPeriod(), SiteIn(Paris)).Publish()));
    }

    [Fact]
    public void A_roster_with_an_unstaffed_shift_cannot_be_published() {
        // Arrange
        Site   site   = SiteIn(Paris);
        Roster roster = RosterFor(SpringPeriod(), site);
        Plan(roster, site, D(4, 6), T(8), T(16));

        // Assert
        Assert.Equal(RefusalReason.UnstaffedShift, ReasonOf(() => roster.Publish()));
        Assert.Equal(RosterStatus.Draft, roster.Status);
    }

    [Fact]
    public void A_fully_staffed_roster_is_published_and_then_frozen() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift staffed = Plan(roster, site, D(4, 6), T(8), T(16));
        Shift spare   = Plan(roster, site, D(4, 8), T(8), T(16));
        roster.Assign(staffed.Number, employee.Id);
        roster.Assign(spare.Number, employee.Id);

        // Act
        roster.Publish();

        // Assert
        Assert.Equal(RosterStatus.Published, roster.Status);
        Assert.Equal(RefusalReason.RosterPublished, ReasonOf(() => roster.Publish()));
        Assert.Equal(RefusalReason.RosterPublished, ReasonOf(() => Plan(roster, site, D(4, 9), T(8), T(16))));
        Assert.Equal(RefusalReason.RosterPublished, ReasonOf(() => roster.CancelShift(staffed.Number)));
        Assert.Equal(RefusalReason.RosterPublished, ReasonOf(() => roster.Unassign(staffed.Number)));
        Assert.Equal(RefusalReason.RosterPublished, ReasonOf(() => roster.Assign(staffed.Number, employee.Id)));
        Assert.Equal(RefusalReason.RosterPublished, roster.Check(staffed.Number, employee.Id));
    }

    [Fact]
    public void Publication_is_checked_before_the_site_is_looked_up() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        roster.Assign(Plan(roster, site, D(4, 6), T(8), T(16)).Number, employee.Id);
        roster.Publish();

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, ReasonOf(() => Plan(roster, SiteIn(Tokyo), D(4, 6), T(8), T(16))));
    }

    [Fact]
    public void A_published_roster_can_still_register_sites_and_employees() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        roster.Assign(Plan(roster, site, D(4, 6), T(8), T(16)).Number, employee.Id);
        roster.Publish();
        Site newSite = SiteIn(Tokyo, site);

        // Act
        roster.RegisterSite(newSite);
        roster.RegisterEmployee(EmployeeAt(newSite));

        // Assert
        Assert.Equal(2, roster.Sites.Count);
        Assert.Equal(2, roster.Employees.Count);
    }

    // ---------------------------------------------------------------- computations

    [Fact]
    public void An_employees_shifts_come_back_in_order_of_start() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        Shift wednesday = Plan(roster, site, D(4, 8), T(8), T(16));
        Shift monday    = Plan(roster, site, D(4, 6), T(8), T(16));
        Shift friday    = Plan(roster, site, D(4, 10), T(8), T(16));
        roster.Assign(wednesday.Number, employee.Id);
        roster.Assign(friday.Number, employee.Id);
        roster.Assign(monday.Number, employee.Id);

        // Assert
        Assert.Equal([monday, wednesday, friday], roster.ShiftsOf(employee.Id));
    }

    [Fact]
    public void An_unknown_employee_has_no_shifts() {
        Assert.Empty(RosterFor(SpringPeriod(), SiteIn(Paris)).ShiftsOf(new AnyEmployeeId().Generate()));
    }

    [Fact]
    public void The_shortest_rest_is_null_below_two_shifts() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);

        // Assert
        Assert.Null(roster.ShortestRest(employee.Id));
        roster.Assign(Plan(roster, site, D(4, 6), T(8), T(16)).Number, employee.Id);
        Assert.Null(roster.ShortestRest(employee.Id));
    }

    [Fact]
    public void The_shortest_rest_is_the_smallest_gap_between_consecutive_shifts() {
        // Arrange
        Site     site     = SiteIn(Paris);
        Employee employee = EmployeeAt(site);
        Roster   roster   = RosterFor(SpringPeriod(), site);
        roster.RegisterEmployee(employee);
        roster.Assign(Plan(roster, site, D(4, 6), T(8), T(16)).Number, employee.Id);   // gap 16 h
        roster.Assign(Plan(roster, site, D(4, 7), T(8), T(12)).Number, employee.Id);   // gap 12 h
        roster.Assign(Plan(roster, site, D(4, 8), T(0), T(8)).Number, employee.Id);    // gap 12 h

        // Assert
        Assert.Equal(TimeSpan.FromHours(12), roster.ShortestRest(employee.Id));
    }

    [Fact]
    public void Elapsed_time_in_a_week_needs_a_registered_employee() {
        // Arrange
        Roster roster = RosterFor(SpringPeriod(), SiteIn(Paris));

        // Assert
        Assert.Equal(RefusalReason.UnknownEmployee, ReasonOf(() => roster.ElapsedTimeIn(new AnyEmployeeId().Generate(), new IsoWeek(2026, 13))));
    }

    // ---------------------------------------------------------------- meetings

    // Paris and Tokyo, both 09:00-17:00 Monday to Friday on their own clocks.
    private static (Roster Roster, Employee Parisian, Employee Tokyoer) MeetingRoster() {
        WorkingHours officeHours = new WorkingHours(T(9), T(17), [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]);
        Site         paris       = new AnySite().WithZone(Paris).WithWorkingHours(officeHours).Generate();
        Site         tokyo       = new AnySite().WithZone(Tokyo).WithWorkingHours(officeHours).WithId(i => i.WithValue(v => v.DifferentFrom(paris.Id.Value))).Generate();
        Roster       roster      = RosterFor(SpringPeriod(), paris, tokyo);
        Employee     parisian    = EmployeeAt(paris);
        Employee     tokyoer     = EmployeeAt(tokyo, parisian);
        roster.RegisterEmployee(parisian);
        roster.RegisterEmployee(tokyoer);
        return (roster, parisian, tokyoer);
    }

    private static Meeting MeetingAt(DateTimeOffset start, TimeSpan duration, params Employee[] participants) {
        return new AnyMeeting().WithStart(start).WithDuration(duration).WithParticipants(participants.Select(p => p.Id).ToList()).Generate();
    }

    [Fact]
    public void A_meeting_inside_every_participants_working_hours_is_booked() {
        // Arrange: Wednesday 17 June 2026, 07:00Z-07:30Z is 09:00 in Paris (UTC+2) and 16:00 in Tokyo (UTC+9)
        (Roster roster, Employee parisian, Employee tokyoer) = MeetingRoster();
        Meeting meeting = MeetingAt(new DateTimeOffset(2026, 6, 17, 7, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30), parisian, tokyoer);

        // Act
        roster.ScheduleMeeting(meeting);

        // Assert
        Assert.Same(meeting, Assert.Single(roster.Meetings));
    }

    [Fact]
    public void A_meeting_outside_one_participants_working_hours_is_refused_and_not_booked() {
        // Arrange: 06:00Z is 08:00 in Paris, before it opens
        (Roster roster, Employee parisian, Employee tokyoer) = MeetingRoster();
        Meeting meeting = MeetingAt(new DateTimeOffset(2026, 6, 17, 6, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30), parisian, tokyoer);

        // Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, ReasonOf(() => roster.ScheduleMeeting(meeting)));
        Assert.Empty(roster.Meetings);
    }

    [Fact]
    public void The_same_instant_is_fine_in_summer_and_too_early_in_winter() {
        // Arrange: 07:00Z is 09:00 in Paris in June (UTC+2) but 08:00 in January (UTC+1)
        (Roster summer, Employee summerParisian, Employee summerTokyoer) = MeetingRoster();
        Meeting inJune = MeetingAt(new DateTimeOffset(2026, 6, 17, 7, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30), summerParisian, summerTokyoer);

        Roster winter = new Roster(new PlanningPeriod(new IsoWeek(2026, 1), 4));
        winter.RegisterSite(summerParisian.HomeSite);
        winter.RegisterSite(summerTokyoer.HomeSite);
        winter.RegisterEmployee(summerParisian);
        winter.RegisterEmployee(summerTokyoer);
        Meeting inJanuary = MeetingAt(new DateTimeOffset(2026, 1, 14, 7, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30), summerParisian, summerTokyoer);

        // Assert
        summer.ScheduleMeeting(inJune);
        Assert.Equal(RefusalReason.OutsideWorkingHours, ReasonOf(() => winter.ScheduleMeeting(inJanuary)));
    }

    [Fact]
    public void A_meeting_ending_exactly_at_closing_time_is_accepted() {
        // Arrange: 08:00Z is 17:00 in Tokyo and 10:00 in Paris
        (Roster roster, Employee parisian, Employee tokyoer) = MeetingRoster();

        // Assert
        roster.ScheduleMeeting(MeetingAt(new DateTimeOffset(2026, 6, 17, 7, 30, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30), parisian, tokyoer));
        Assert.Equal(RefusalReason.OutsideWorkingHours, ReasonOf(() => roster.ScheduleMeeting(MeetingAt(new DateTimeOffset(2026, 6, 17, 7, 45, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30), parisian, tokyoer))));
    }

    [Fact]
    public void A_meeting_with_an_unregistered_participant_is_refused() {
        // Arrange
        (Roster roster, Employee parisian, _) = MeetingRoster();
        Employee stranger = EmployeeAt(SiteIn(Paris), parisian);
        Meeting  meeting  = MeetingAt(new DateTimeOffset(2026, 6, 17, 8, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30), parisian, stranger);

        // Assert
        Assert.Equal(RefusalReason.UnknownEmployee, ReasonOf(() => roster.ScheduleMeeting(meeting)));
    }

    [Fact]
    public void A_meeting_can_be_booked_after_publication() {
        // Arrange
        (Roster roster, Employee parisian, Employee tokyoer) = MeetingRoster();
        roster.Assign(Plan(roster, parisian.HomeSite, D(4, 6), T(8), T(16)).Number, parisian.Id);
        roster.Publish();

        // Act
        roster.ScheduleMeeting(MeetingAt(new DateTimeOffset(2026, 6, 17, 7, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30), parisian, tokyoer));

        // Assert
        Assert.Single(roster.Meetings);
    }

    [Fact]
    public void A_meeting_over_a_non_working_day_is_refused() {
        // Arrange: Saturday 20 June
        (Roster roster, Employee parisian, Employee tokyoer) = MeetingRoster();

        // Assert
        Assert.Equal(RefusalReason.OutsideWorkingHours, ReasonOf(() => roster.ScheduleMeeting(MeetingAt(new DateTimeOffset(2026, 6, 20, 7, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30), parisian, tokyoer))));
    }

    [Fact]
    public void Meeting_slots_are_the_quarter_hours_where_every_site_is_open_for_the_whole_meeting() {
        // Arrange: Paris is open 07:00Z-15:00Z in summer, Tokyo 00:00Z-08:00Z, so they share 07:00Z-08:00Z
        (Roster roster, Employee parisian, Employee tokyoer) = MeetingRoster();

        // Act
        IReadOnlyList<DateTimeOffset> halfHour = roster.MeetingSlots(D(6, 17), TimeSpan.FromMinutes(30), [parisian.Id, tokyoer.Id]);
        IReadOnlyList<DateTimeOffset> hour     = roster.MeetingSlots(D(6, 17), TimeSpan.FromHours(1), [parisian.Id, tokyoer.Id]);
        IReadOnlyList<DateTimeOffset> twoHours = roster.MeetingSlots(D(6, 17), TimeSpan.FromHours(2), [parisian.Id, tokyoer.Id]);

        // Assert
        Assert.Equal([new DateTimeOffset(2026, 6, 17, 7, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 17, 7, 15, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 17, 7, 30, 0, TimeSpan.Zero)], halfHour);
        Assert.Equal([new DateTimeOffset(2026, 6, 17, 7, 0, 0, TimeSpan.Zero)], hour);
        Assert.Empty(twoHours);
    }

    [Fact]
    public void Meeting_slots_for_one_site_cover_its_whole_open_window() {
        // Arrange
        (Roster roster, Employee parisian, _) = MeetingRoster();

        // Act: Paris is open 07:00Z-15:00Z, so one-hour meetings may start from 07:00Z to 14:00Z every quarter hour
        IReadOnlyList<DateTimeOffset> slots = roster.MeetingSlots(D(6, 17), TimeSpan.FromHours(1), [parisian.Id]);

        // Assert
        Assert.Equal(29, slots.Count);
        Assert.Equal(new DateTimeOffset(2026, 6, 17, 7, 0, 0, TimeSpan.Zero), slots[0]);
        Assert.Equal(new DateTimeOffset(2026, 6, 17, 14, 0, 0, TimeSpan.Zero), slots[^1]);
    }

    [Fact]
    public void Two_employees_of_one_site_count_as_that_site() {
        // Arrange
        (Roster roster, Employee parisian, _) = MeetingRoster();
        Employee colleague = EmployeeAt(parisian.HomeSite, parisian);
        roster.RegisterEmployee(colleague);

        // Assert
        Assert.Equal(29, roster.MeetingSlots(D(6, 17), TimeSpan.FromHours(1), [parisian.Id, colleague.Id]).Count);
    }

    [Fact(Skip = "domain defect: MeetingSlots(DateOnly.MaxValue, ...) throws ArgumentOutOfRangeException from dayStart.AddDays(1), which is not among the documented refusals; the last UTC day yields no slots. Only literal inputs decide it, seed irrelevant")]
    public void The_last_representable_day_can_be_searched_for_meeting_slots() {
        // Arrange
        (Roster roster, Employee parisian, _) = MeetingRoster();

        // Assert
        Assert.NotNull(roster.MeetingSlots(DateOnly.MaxValue, TimeSpan.FromHours(1), [parisian.Id]));
    }

    [Fact]
    public void There_are_no_slots_on_a_day_nobody_works() {
        // Arrange: Sunday 21 June 2026 UTC; 00:00Z is already Sunday 09:00 in Tokyo and 02:00 in Paris
        (Roster roster, Employee parisian, Employee tokyoer) = MeetingRoster();

        // Assert
        Assert.Empty(roster.MeetingSlots(D(6, 21), TimeSpan.FromMinutes(30), [parisian.Id, tokyoer.Id]));
    }

    [Theory]
    [InlineData(14)]
    [InlineData(241)]
    public void Meeting_slots_refuse_a_duration_outside_15_minutes_to_4_hours(int minutes) {
        // Arrange
        (Roster roster, Employee parisian, _) = MeetingRoster();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => roster.MeetingSlots(D(6, 17), TimeSpan.FromMinutes(minutes), [parisian.Id]));
    }

    [Fact]
    public void Meeting_slots_need_at_least_one_known_participant() {
        // Arrange
        (Roster roster, Employee parisian, Employee tokyoer) = MeetingRoster();
        EmployeeId stranger = new AnyEmployeeId().WithValue(v => v.Except(parisian.Id.Value, tokyoer.Id.Value)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => roster.MeetingSlots(D(6, 17), TimeSpan.FromHours(1), []));
        Assert.Equal(RefusalReason.UnknownEmployee, ReasonOf(() => roster.MeetingSlots(D(6, 17), TimeSpan.FromHours(1), [stranger])));
    }

}
