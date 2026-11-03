using JustDummies;

using ShiftPlanning.Domain.Tests.Regular.Support;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.RosterTests;

public sealed class RosterAssignmentTests {

    // Paris clocks: forward on Sunday 2026-03-29 (W13), back on Sunday 2026-10-25 (W43)
    private static readonly PlanningPeriod SpringPeriod = new PlanningPeriod(new IsoWeek(2026, 12), 3);
    private static readonly PlanningPeriod AutumnPeriod = new PlanningPeriod(new IsoWeek(2026, 42), 3);
    // Tokyo and New York, away from any change of the clocks
    private static readonly PlanningPeriod WinterPeriod = new PlanningPeriod(new IsoWeek(2026, 3), 3);

    private static RefusalReason AssignRefusal(Roster roster, int shiftNumber, Employee employee) {
        return Assert.Throws<RosterRefusedException>(() => roster.Assign(shiftNumber, employee.Id)).Reason;
    }

    [Fact]
    public void Assigning_a_shift_staffs_it() {
        // Arrange
        Scene scene = new Scene();
        Shift shift = scene.Plan(0, "08:00", "16:00");

        // Act
        scene.Roster.Assign(shift.Number, scene.First.Id);

        // Assert
        Assert.True(shift.IsStaffed);
        Assert.Equal(scene.First.Id, shift.AssignedTo);
        Assert.Same(shift, Assert.Single(scene.Roster.ShiftsOf(scene.First.Id)));
    }

    [Fact]
    public void Checking_an_allowed_assignment_answers_none_and_changes_nothing() {
        // Arrange
        Scene scene = new Scene();
        Shift shift = scene.Plan(0, "08:00", "16:00");

        // Act
        RefusalReason answer = scene.Roster.Check(shift.Number, scene.First.Id);

        // Assert
        Assert.Equal(RefusalReason.None, answer);
        Assert.False(shift.IsStaffed);
    }

    [Fact]
    public void Checking_and_assigning_answer_the_same_reason() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift overlapping = scene.Plan(0, "12:00", "20:00");

        // Act
        RefusalReason checkedReason  = scene.Roster.Check(overlapping.Number, scene.First.Id);
        RefusalReason assignedReason = AssignRefusal(scene.Roster, overlapping.Number, scene.First);

        // Assert
        Assert.Equal(RefusalReason.OverlapsAnotherShift, checkedReason);
        Assert.Equal(checkedReason, assignedReason);
        Assert.False(overlapping.IsStaffed);
    }

    [Fact]
    public void An_employee_is_required_to_check_or_assign() {
        // Arrange
        Scene scene = new Scene();
        Shift shift = scene.Plan(0, "08:00", "16:00");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => scene.Roster.Check(shift.Number, null!));
        Assert.Throws<ArgumentNullException>(() => scene.Roster.Assign(shift.Number, null!));
    }

    [Fact]
    public void A_shift_that_does_not_exist_cannot_be_assigned() {
        // Arrange
        Scene scene = new Scene();

        // Act & Assert
        Assert.Equal(RefusalReason.UnknownShift, AssignRefusal(scene.Roster, 1, scene.First));
        Assert.Equal(RefusalReason.UnknownShift, scene.Roster.Check(1, scene.First.Id));
    }

    [Fact]
    public void An_employee_who_is_not_registered_cannot_be_assigned() {
        // Arrange
        Scene    scene    = new Scene();
        Shift    shift    = scene.Plan(0, "08:00", "16:00");
        Employee stranger = new AnyEmployee().WithHomeSite(scene.Site).Generate();

        // Act & Assert
        Assert.Equal(RefusalReason.UnknownEmployee, AssignRefusal(scene.Roster, shift.Number, stranger));
    }

    [Fact]
    public void The_shift_is_checked_before_the_employee() {
        // Arrange
        Scene    scene    = new Scene();
        Employee stranger = new AnyEmployee().WithHomeSite(scene.Site).Generate();

        // Act & Assert
        Assert.Equal(RefusalReason.UnknownShift, AssignRefusal(scene.Roster, 42, stranger));
    }

    [Fact]
    public void A_shift_that_already_has_somebody_cannot_be_assigned_again() {
        // Arrange
        Scene scene = new Scene();
        Shift shift = scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");

        // Act & Assert
        Assert.Equal(RefusalReason.ShiftAlreadyAssigned, AssignRefusal(scene.Roster, shift.Number, scene.Second));
        Assert.Equal(RefusalReason.ShiftAlreadyAssigned, AssignRefusal(scene.Roster, shift.Number, scene.First));
        Assert.Equal(scene.First.Id, shift.AssignedTo);
    }

    [Fact]
    public void An_employee_cannot_work_two_shifts_that_overlap() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift overlapping = scene.Plan(0, "15:59", "20:00");

        // Act & Assert
        Assert.Equal(RefusalReason.OverlapsAnotherShift, AssignRefusal(scene.Roster, overlapping.Number, scene.First));
    }

    [Fact]
    public void Somebody_else_can_work_the_overlapping_shift() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift overlapping = scene.Plan(0, "12:00", "20:00");

        // Act
        scene.Roster.Assign(overlapping.Number, scene.Second.Id);

        // Assert
        Assert.Equal(scene.Second.Id, overlapping.AssignedTo);
    }

    [Fact]
    public void Overlap_is_detected_between_two_sites_by_their_instants() {
        // Arrange: 09:00-17:00 in Paris is 08:00-16:00 UTC, 17:00-22:00 in Tokyo is 08:00-13:00 UTC
        List<Site> offices  = Kit.OfficesIn(Kit.Paris, Kit.Tokyo);
        Roster     roster   = Kit.RosterWith(WinterPeriod, offices.ToArray());
        Employee   employee = Kit.EmployeesAt(offices[0], 1)[0];
        roster.RegisterEmployee(employee);
        Shift inParis = Kit.Plan(roster, offices[0], new DateOnly(2026, 1, 13), "09:00", "17:00");
        Shift inTokyo = Kit.Plan(roster, offices[1], new DateOnly(2026, 1, 13), "17:00", "22:00");
        roster.Assign(inParis.Number, employee.Id);

        // Act & Assert
        Assert.Equal(RefusalReason.OverlapsAnotherShift, AssignRefusal(roster, inTokyo.Number, employee));
    }

    [Fact]
    public void Overlap_is_reported_rather_than_a_lack_of_rest() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift overlapping = scene.Plan(0, "10:00", "14:00");

        // Act & Assert
        Assert.Equal(RefusalReason.OverlapsAnotherShift, scene.Roster.Check(overlapping.Number, scene.First.Id));
    }

    [Fact]
    public void Exactly_eleven_hours_of_rest_are_allowed() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift next = scene.Plan(1, "03:00", "11:00");

        // Act
        scene.Roster.Assign(next.Number, scene.First.Id);

        // Assert
        Assert.Equal(TimeSpan.FromHours(11), scene.Roster.ShortestRest(scene.First.Id));
    }

    [Fact]
    public void One_minute_short_of_eleven_hours_of_rest_is_refused() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift next = scene.Plan(1, "02:59", "11:00");

        // Act & Assert
        Assert.Equal(RefusalReason.InsufficientRest, AssignRefusal(scene.Roster, next.Number, scene.First));
    }

    [Fact]
    public void Back_to_back_shifts_leave_no_rest() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift next = scene.Plan(0, "16:00", "20:00");

        // Act & Assert
        Assert.Equal(RefusalReason.InsufficientRest, scene.Roster.Check(next.Number, scene.First.Id));
    }

    [Fact]
    public void Rest_is_required_after_a_shift_as_well_as_before_it() {
        // Arrange: the later shift is assigned first, it starts at 08:00 the next day
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 1, "08:00", "16:00");
        Shift endsElevenHoursBefore   = scene.Plan(0, "10:00", "21:00");
        Shift endsTenHours59Before    = scene.Plan(0, "10:00", "21:01");

        // Act & Assert
        Assert.Equal(RefusalReason.None, scene.Roster.Check(endsElevenHoursBefore.Number, scene.First.Id));
        Assert.Equal(RefusalReason.InsufficientRest, scene.Roster.Check(endsTenHours59Before.Number, scene.First.Id));
    }

    [Fact]
    public void Rest_is_required_between_shifts_of_different_sites() {
        // Arrange
        List<Site> twoSites = Kit.OfficesIn(Kit.Tokyo, Kit.Tokyo);
        Roster     roster   = Kit.RosterWith(WinterPeriod, twoSites.ToArray());
        Employee   employee = Kit.EmployeesAt(twoSites[0], 1)[0];
        roster.RegisterEmployee(employee);
        Shift here  = Kit.Plan(roster, twoSites[0], new DateOnly(2026, 1, 13), "08:00", "16:00");
        Shift there = Kit.Plan(roster, twoSites[1], new DateOnly(2026, 1, 13), "18:00", "23:00");
        roster.Assign(here.Number, employee.Id);

        // Act & Assert
        Assert.Equal(RefusalReason.InsufficientRest, AssignRefusal(roster, there.Number, employee));
    }

    [Fact]
    public void The_rest_of_a_colleague_does_not_count() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift next = scene.Plan(0, "16:30", "20:00");

        // Act & Assert
        Assert.Equal(RefusalReason.None, scene.Roster.Check(next.Number, scene.Second.Id));
    }

    [Fact]
    public void Rest_is_elapsed_time_so_spring_forward_takes_an_hour_of_it() {
        // Arrange: 18:00 Saturday to 05:00 Sunday is 11 h on the wall clock and 10 h elapsed
        Site     paris    = Kit.SiteIn(Kit.Paris);
        Roster   roster   = Kit.RosterWith(SpringPeriod, paris);
        Employee employee = Kit.EmployeesAt(paris, 1)[0];
        roster.RegisterEmployee(employee);
        Shift saturday = Kit.Plan(roster, paris, new DateOnly(2026, 3, 28), "10:00", "18:00");
        Shift sunday   = Kit.Plan(roster, paris, new DateOnly(2026, 3, 29), "05:00", "13:00");
        roster.Assign(saturday.Number, employee.Id);

        // Act & Assert
        Assert.Equal(RefusalReason.InsufficientRest, AssignRefusal(roster, sunday.Number, employee));
    }

    [Fact]
    public void Rest_is_elapsed_time_so_fall_back_gives_an_hour_of_it() {
        // Arrange: 18:00 Saturday to 04:00 Sunday is 10 h on the wall clock and 11 h elapsed
        Site     paris    = Kit.SiteIn(Kit.Paris);
        Roster   roster   = Kit.RosterWith(AutumnPeriod, paris);
        Employee employee = Kit.EmployeesAt(paris, 1)[0];
        roster.RegisterEmployee(employee);
        Shift saturday = Kit.Plan(roster, paris, new DateOnly(2026, 10, 24), "10:00", "18:00");
        Shift sunday   = Kit.Plan(roster, paris, new DateOnly(2026, 10, 25), "04:00", "12:00");

        // Act
        roster.Assign(saturday.Number, employee.Id);
        roster.Assign(sunday.Number, employee.Id);

        // Assert
        Assert.Equal(TimeSpan.FromHours(11), roster.ShortestRest(employee.Id));
    }

    [Fact]
    public void Forty_eight_hours_in_a_week_are_allowed() {
        // Arrange: six days of 8 h, a day apart
        Scene scene = new Scene();
        for (int day = 0; day < 6; day++) { scene.PlanAndAssign(scene.First, day, "08:00", "16:00"); }

        // Assert
        Assert.Equal(TimeSpan.FromHours(48), scene.Roster.ElapsedTimeIn(scene.First.Id, scene.Period.FirstWeek));
    }

    [Fact]
    public void A_seventh_day_in_the_same_week_goes_over_the_limit() {
        // Arrange
        Scene scene = new Scene();
        for (int day = 0; day < 6; day++) { scene.PlanAndAssign(scene.First, day, "08:00", "16:00"); }
        Shift sunday = scene.Plan(6, "08:00", "09:00");

        // Act & Assert
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, AssignRefusal(scene.Roster, sunday.Number, scene.First));
    }

    [Fact]
    public void One_minute_over_forty_eight_hours_is_refused() {
        // Arrange
        Scene scene = new Scene();
        for (int day = 0; day < 5; day++) { scene.PlanAndAssign(scene.First, day, "08:00", "16:00"); }
        Shift saturday = scene.Plan(5, "08:00", "16:01");

        // Act & Assert
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, scene.Roster.Check(saturday.Number, scene.First.Id));
    }

    [Fact]
    public void The_week_after_starts_again_from_zero() {
        // Arrange
        Scene scene = new Scene(minimumWeeks: 2);
        for (int day = 0; day < 6; day++) { scene.PlanAndAssign(scene.First, day, "08:00", "16:00"); }
        Shift monday = scene.Plan(7, "08:00", "16:00");

        // Act
        scene.Roster.Assign(monday.Number, scene.First.Id);

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), scene.Roster.ElapsedTimeIn(scene.First.Id, scene.Period.Weeks()[1]));
    }

    [Fact]
    public void A_night_shift_counts_wholly_toward_the_week_it_starts_in() {
        // Arrange: Sunday 22:00 to Monday 06:00 after five 8 h days
        Scene scene = new Scene(minimumWeeks: 2);
        for (int day = 0; day < 5; day++) { scene.PlanAndAssign(scene.First, day, "08:00", "16:00"); }

        // Act
        scene.PlanAndAssign(scene.First, 6, "22:00", "06:00");

        // Assert
        Assert.Equal(TimeSpan.FromHours(48), scene.Roster.ElapsedTimeIn(scene.First.Id, scene.Period.Weeks()[0]));
        Assert.Equal(TimeSpan.Zero, scene.Roster.ElapsedTimeIn(scene.First.Id, scene.Period.Weeks()[1]));
    }

    [Fact]
    public void Insufficient_rest_is_reported_before_the_weekly_limit() {
        // Arrange: a shift that breaks both rules
        Scene scene = new Scene();
        for (int day = 0; day < 6; day++) { scene.PlanAndAssign(scene.First, day, "08:00", "16:00"); }
        Shift sunday = scene.Plan(5, "20:00", "23:00");

        // Act & Assert
        Assert.Equal(RefusalReason.InsufficientRest, scene.Roster.Check(sunday.Number, scene.First.Id));
    }

    [Fact]
    public void Spring_forward_makes_a_week_one_hour_shorter() {
        // Arrange: five 8 h days, then Saturday night 22:00 to Sunday 06:00 (7 h elapsed): 47 h
        Site     paris    = Kit.SiteIn(Kit.Paris);
        Roster   roster   = Kit.RosterWith(SpringPeriod, paris);
        Employee employee = Kit.EmployeesAt(paris, 1)[0];
        roster.RegisterEmployee(employee);
        for (int day = 23; day <= 27; day++) { roster.Assign(Kit.Plan(roster, paris, new DateOnly(2026, 3, day), "08:00", "16:00").Number, employee.Id); }
        roster.Assign(Kit.Plan(roster, paris, new DateOnly(2026, 3, 28), "22:00", "06:00").Number, employee.Id);
        Shift exactly = Kit.Plan(roster, paris, new DateOnly(2026, 3, 29), "17:30", "18:30");
        Shift over    = Kit.Plan(roster, paris, new DateOnly(2026, 3, 29), "17:30", "18:31");

        // Act & Assert: 47 h + 1 h is exactly the limit, 47 h + 1 h 01 is over it
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, roster.Check(over.Number, employee.Id));
        roster.Assign(exactly.Number, employee.Id);
        Assert.Equal(TimeSpan.FromHours(48), roster.ElapsedTimeIn(employee.Id, new IsoWeek(2026, 13)));
    }

    [Fact]
    public void The_week_a_shift_counts_toward_is_read_in_the_home_zone_of_the_employee() {
        // Arrange: Sunday 2026-01-18 20:00 in New York is Monday 10:00 in Tokyo, so it counts toward Tokyo's week 4
        List<Site> offices  = Kit.OfficesIn(Kit.NewYork, Kit.Tokyo);
        Site       newYork  = offices[0];
        Site       tokyo    = offices[1];
        Roster     roster   = Kit.RosterWith(WinterPeriod, newYork, tokyo);
        Employee   inTokyo  = Kit.EmployeesAt(tokyo, 1)[0];
        roster.RegisterEmployee(inTokyo);

        roster.Assign(Kit.Plan(roster, newYork, new DateOnly(2026, 1, 18), "20:00", "04:00").Number, inTokyo.Id);
        for (int day = 20; day <= 24; day++) { roster.Assign(Kit.Plan(roster, tokyo, new DateOnly(2026, 1, day), "08:00", "16:00").Number, inTokyo.Id); }
        Shift sunday = Kit.Plan(roster, tokyo, new DateOnly(2026, 1, 25), "09:00", "10:00");

        // Act & Assert: 8 h + 5 x 8 h = 48 h in week 4, one more hour is too many
        Assert.Equal(TimeSpan.FromHours(48), roster.ElapsedTimeIn(inTokyo.Id, new IsoWeek(2026, 4)));
        Assert.Equal(TimeSpan.Zero, roster.ElapsedTimeIn(inTokyo.Id, new IsoWeek(2026, 3)));
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, roster.Check(sunday.Number, inTokyo.Id));
    }

    [Fact]
    public void The_same_shift_counts_toward_another_week_for_an_employee_of_another_zone() {
        // Arrange
        List<Site> offices  = Kit.OfficesIn(Kit.NewYork, Kit.Tokyo);
        Roster     roster   = Kit.RosterWith(WinterPeriod, offices.ToArray());
        Employee   inNewYork = Kit.EmployeesAt(offices[0], 1)[0];
        roster.RegisterEmployee(inNewYork);

        // Act
        roster.Assign(Kit.Plan(roster, offices[0], new DateOnly(2026, 1, 18), "20:00", "04:00").Number, inNewYork.Id);

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), roster.ElapsedTimeIn(inNewYork.Id, new IsoWeek(2026, 3)));
        Assert.Equal(TimeSpan.Zero, roster.ElapsedTimeIn(inNewYork.Id, new IsoWeek(2026, 4)));
    }

    [Fact]
    public void Unassigning_a_shift_frees_it_for_somebody_else() {
        // Arrange
        Scene scene = new Scene();
        Shift shift = scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");

        // Act
        scene.Roster.Unassign(shift.Number);
        scene.Roster.Assign(shift.Number, scene.Second.Id);

        // Assert
        Assert.Equal(scene.Second.Id, shift.AssignedTo);
        Assert.Empty(scene.Roster.ShiftsOf(scene.First.Id));
    }

    [Fact]
    public void A_shift_with_nobody_on_it_cannot_be_unassigned() {
        // Arrange
        Scene scene = new Scene();
        Shift shift = scene.Plan(0, "08:00", "16:00");

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => scene.Roster.Unassign(shift.Number));

        // Assert
        Assert.Equal(RefusalReason.ShiftNotAssigned, refusal.Reason);
    }

    [Fact]
    public void A_shift_that_does_not_exist_cannot_be_unassigned() {
        Assert.Equal(RefusalReason.UnknownShift, Assert.Throws<RosterRefusedException>(() => new Scene().Roster.Unassign(1)).Reason);
    }

    [Fact]
    public void Nothing_can_be_assigned_checked_or_unassigned_once_the_roster_is_published() {
        // Arrange
        Scene scene = new Scene();
        scene.Publish();

        // Act & Assert: even for a shift and an employee that do not exist
        Assert.Equal(RefusalReason.RosterPublished, scene.Roster.Check(99, new AnyEmployeeId().Generate()));
        Assert.Equal(RefusalReason.RosterPublished, Assert.Throws<RosterRefusedException>(() => scene.Roster.Assign(99, new AnyEmployeeId().Generate())).Reason);
        Assert.Equal(RefusalReason.RosterPublished, Assert.Throws<RosterRefusedException>(() => scene.Roster.Unassign(99)).Reason);
    }

}
