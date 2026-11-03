using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.RosterBehaviour;

public sealed class RosterAssignmentTests {

    // ---- Assigning, unassigning --------------------------------------------------------------------------------

    [Fact]
    public void An_allowed_assignment_staffs_the_shift_and_lists_it_for_the_employee() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        Shift shift = scene.PlanAnyShiftAt(site);

        // Act
        scene.Roster.Assign(shift.Number, employee.Id);

        // Assert
        Assert.Equal(employee.Id, shift.AssignedTo);
        Assert.True(shift.IsStaffed);
        Assert.Same(shift, Assert.Single(scene.Roster.ShiftsOf(employee.Id)));
    }

    [Fact]
    public void Checking_an_allowed_assignment_says_none_and_changes_nothing() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        Shift shift = scene.PlanAnyShiftAt(site);

        // Act
        RefusalReason reason = scene.Roster.Check(shift.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.None, reason);
        Assert.False(shift.IsStaffed);
    }

    [Fact]
    public void Checking_a_refused_assignment_gives_the_reason_the_assignment_would_throw() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        int unknownShift = 99;

        // Act
        RefusalReason checkedReason = scene.Roster.Check(unknownShift, employee.Id);
        RefusalReason thrownReason  = Refusal.Of(() => scene.Roster.Assign(unknownShift, employee.Id));

        // Assert
        Assert.Equal(RefusalReason.UnknownShift, checkedReason);
        Assert.Equal(checkedReason, thrownReason);
    }

    [Fact]
    public void An_unassigned_shift_can_be_given_to_somebody_else() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee first = scene.Hire(site);
        Employee second = scene.Hire(site);
        Shift shift = scene.PlanAnyShiftAt(site);
        scene.Roster.Assign(shift.Number, first.Id);

        // Act
        scene.Roster.Unassign(shift.Number);
        scene.Roster.Assign(shift.Number, second.Id);

        // Assert
        Assert.Equal(second.Id, shift.AssignedTo);
        Assert.Empty(scene.Roster.ShiftsOf(first.Id));
    }

    [Fact]
    public void A_shift_nobody_holds_cannot_be_unassigned() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Shift shift = scene.PlanAnyShiftAt(site);

        // Act
        Action unassign = () => scene.Roster.Unassign(shift.Number);

        // Assert
        Assert.Equal(RefusalReason.ShiftNotAssigned, Refusal.Of(unassign));
    }

    [Fact]
    public void An_unknown_shift_cannot_be_unassigned() {
        // Arrange
        Scene scene = new Scene();

        // Act
        Action unassign = () => scene.Roster.Unassign(1);

        // Assert
        Assert.Equal(RefusalReason.UnknownShift, Refusal.Of(unassign));
    }

    [Fact]
    public void A_published_roster_refuses_to_unassign_even_an_unknown_shift() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        scene.PublishOneStaffedShift(employee, site);

        // Act
        Action unassign = () => scene.Roster.Unassign(99);

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, Refusal.Of(unassign));
    }

    [Fact]
    public void Missing_employees_are_refused() {
        // Arrange
        Scene scene = new Scene();

        // Assert
        Assert.Throws<ArgumentNullException>("employee", () => scene.Roster.Check(1, null!));
        Assert.Throws<ArgumentNullException>("employee", () => scene.Roster.Assign(1, null!));
        Assert.Throws<ArgumentNullException>("employee", () => scene.Roster.ShiftsOf(null!));
    }

    // ---- The order in which the rules refuse -------------------------------------------------------------------

    [Fact]
    public void A_published_roster_refuses_every_assignment_first() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        scene.PublishOneStaffedShift(employee, site);
        EmployeeId unknownEmployee = Any.EmployeeId().Generate();

        // Act
        RefusalReason reason = scene.Roster.Check(99, unknownEmployee);

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, reason);
    }

    [Fact]
    public void An_unknown_shift_is_reported_before_an_unknown_employee() {
        // Arrange
        Scene scene = new Scene();
        EmployeeId unknownEmployee = Any.EmployeeId().Generate();

        // Act
        RefusalReason reason = scene.Roster.Check(1, unknownEmployee);

        // Assert
        Assert.Equal(RefusalReason.UnknownShift, reason);
    }

    [Fact]
    public void An_unregistered_employee_cannot_take_a_shift() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Shift shift = scene.PlanAnyShiftAt(site);
        EmployeeId unknownEmployee = Any.EmployeeId().Generate();

        // Act
        RefusalReason reason = scene.Roster.Check(shift.Number, unknownEmployee);

        // Assert
        Assert.Equal(RefusalReason.UnknownEmployee, reason);
    }

    [Fact]
    public void A_staffed_shift_cannot_be_taken_again_not_even_by_its_holder() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee holder = scene.Hire(site);
        Employee other = scene.Hire(site);
        Shift shift = scene.PlanAnyShiftAt(site);
        scene.Roster.Assign(shift.Number, holder.Id);

        // Act
        RefusalReason byHolder = scene.Roster.Check(shift.Number, holder.Id);
        RefusalReason byOther = scene.Roster.Check(shift.Number, other.Id);

        // Assert
        Assert.Equal(RefusalReason.ShiftAlreadyAssigned, byHolder);
        Assert.Equal(RefusalReason.ShiftAlreadyAssigned, byOther);
    }

    // ---- Overlap -----------------------------------------------------------------------------------------------

    [Fact]
    public void Two_overlapping_shifts_on_different_sites_cannot_go_to_one_employee() {
        // Arrange
        Scene scene = new Scene();
        Site paris = scene.OpenSite("Europe/Paris");
        Site newYork = scene.OpenSite("America/New_York");
        Employee employee = scene.Hire(paris);
        scene.Staff(employee, paris, "2027-01-12", "08:00", "16:00");          // 07:00Z to 15:00Z
        Shift overlapping = scene.Plan(newYork, "2027-01-12", "09:00", "17:00");   // 14:00Z to 22:00Z

        // Act
        RefusalReason reason = scene.Roster.Check(overlapping.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.OverlapsAnotherShift, reason);
    }

    [Fact]
    public void Shifts_that_look_simultaneous_on_the_wall_clock_but_are_hours_apart_do_not_overlap() {
        // Arrange
        Scene scene = new Scene();
        Site paris = scene.OpenSite("Europe/Paris");
        Site newYork = scene.OpenSite("America/New_York");
        Employee employee = scene.Hire(paris);
        scene.Staff(employee, paris, "2027-01-12", "09:00", "13:00");              // 08:00Z to 12:00Z
        Shift sameLocalHours = scene.Plan(newYork, "2027-01-13", "09:00", "13:00");   // a day later in UTC terms: 14:00Z to 18:00Z

        // Act
        RefusalReason reason = scene.Roster.Check(sameLocalHours.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.None, reason);
    }

    [Fact]
    public void Overlap_is_reported_before_insufficient_rest() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        scene.Staff(employee, site, "2027-01-12", "08:00", "16:00");
        Shift overlapping = scene.Plan(site, "2027-01-12", "15:00", "20:00");

        // Act
        RefusalReason reason = scene.Roster.Check(overlapping.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.OverlapsAnotherShift, reason);
    }

    [Fact]
    public void An_overlap_with_a_shift_of_somebody_else_is_no_obstacle() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee busy = scene.Hire(site);
        Employee free = scene.Hire(site);
        scene.Staff(busy, site, "2027-01-12", "08:00", "16:00");
        Shift sameHours = scene.Plan(site, "2027-01-12", "08:00", "16:00");

        // Act
        RefusalReason reason = scene.Roster.Check(sameHours.Number, free.Id);

        // Assert
        Assert.Equal(RefusalReason.None, reason);
    }

    // ---- Rest: 11 hours of elapsed time ------------------------------------------------------------------------

    [Fact]
    public void Exactly_eleven_hours_of_rest_is_allowed() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        scene.Staff(employee, site, "2027-01-12", "14:00", "22:00");
        Shift next = scene.Plan(site, "2027-01-13", "09:00", "17:00");   // 11:00 after the first ends

        // Act
        RefusalReason reason = scene.Roster.Check(next.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.None, reason);
    }

    [Fact]
    public void A_minute_less_than_eleven_hours_of_rest_is_refused() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        scene.Staff(employee, site, "2027-01-12", "14:00", "22:00");
        Shift next = scene.Plan(site, "2027-01-13", "08:59", "17:00");   // 10:59 after the first ends

        // Act
        RefusalReason reason = scene.Roster.Check(next.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.InsufficientRest, reason);
    }

    [Fact]
    public void Rest_is_measured_from_the_shift_that_follows_as_well_as_from_the_one_that_precedes() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        scene.Staff(employee, site, "2027-01-13", "09:00", "17:00");
        Shift earlier = scene.Plan(site, "2027-01-12", "14:00", "22:01");   // ends 10:59 before the staffed shift starts

        // Act
        RefusalReason reason = scene.Roster.Check(earlier.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.InsufficientRest, reason);
    }

    [Fact]
    public void Shifts_that_touch_leave_no_rest_at_all() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        scene.Staff(employee, site, "2027-01-12", "06:00", "14:00");
        Shift touching = scene.Plan(site, "2027-01-12", "14:00", "22:00");

        // Act
        RefusalReason reason = scene.Roster.Check(touching.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.InsufficientRest, reason);
    }

    [Fact]
    public void Rest_is_counted_across_sites_in_elapsed_time() {
        // Arrange
        Scene scene = new Scene();
        Site paris = scene.OpenSite("Europe/Paris");
        Site tokyo = scene.OpenSite("Asia/Tokyo");
        Employee employee = scene.Hire(paris);
        scene.Staff(employee, paris, "2027-01-12", "08:00", "16:00");       // ends 15:00Z
        Shift inTokyo = scene.Plan(tokyo, "2027-01-13", "08:00", "16:00");  // starts 2027-01-12 23:00Z: eight hours later

        // Act
        RefusalReason reason = scene.Roster.Check(inTokyo.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.InsufficientRest, reason);
    }

    [Fact]
    public void Rest_is_reported_before_the_weekly_limit() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        FillWeekWithFourTwelveHourShifts(scene, employee, site);
        Shift thatBreaksBoth = scene.Plan(site, "2027-01-07", "21:00", "22:00");   // an hour after Thursday's shift, and the 49th hour

        // Act
        RefusalReason reason = scene.Roster.Check(thatBreaksBoth.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.InsufficientRest, reason);
    }

    [Fact]
    public void A_spring_forward_night_takes_an_hour_from_the_rest_that_follows_it() {
        // Arrange
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 12), 1));   // Paris springs forward on Sunday 2027-03-28
        Site paris = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(paris);
        scene.Staff(employee, paris, "2027-03-27", "14:00", "22:00");       // ends 21:00Z
        Shift elevenHoursOnTheClock = scene.Plan(paris, "2027-03-28", "09:00", "17:00");   // starts 07:00Z: ten hours of rest, not eleven

        // Act
        RefusalReason reason = scene.Roster.Check(elevenHoursOnTheClock.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.InsufficientRest, reason);
    }

    [Fact]
    public void A_fall_back_night_gives_an_hour_to_the_rest_that_follows_it() {
        // Arrange
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 43), 1));   // Paris falls back on Sunday 2027-10-31
        Site paris = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(paris);
        scene.Staff(employee, paris, "2027-10-30", "14:00", "22:00");       // ends 20:00Z
        Shift tenHoursOnTheClock = scene.Plan(paris, "2027-10-31", "08:00", "16:00");   // starts 07:00Z: exactly eleven hours of rest

        // Act
        RefusalReason reason = scene.Roster.Check(tenHoursOnTheClock.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.None, reason);
    }

    // ---- Weekly limit: 48 hours of elapsed time in an ISO week -------------------------------------------------

    /// <summary>Monday to Thursday 2027-01-04..07, 08:00 to 20:00 on the site's clock: forty-eight hours of week 2027-W01.</summary>
    private static void FillWeekWithFourTwelveHourShifts(Scene scene, Employee employee, Site site) {
        scene.Staff(employee, site, "2027-01-04", "08:00", "20:00");
        scene.Staff(employee, site, "2027-01-05", "08:00", "20:00");
        scene.Staff(employee, site, "2027-01-06", "08:00", "20:00");
        scene.Staff(employee, site, "2027-01-07", "08:00", "20:00");
    }

    [Fact]
    public void Exactly_forty_eight_hours_in_a_week_is_allowed() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        scene.Staff(employee, site, "2027-01-04", "08:00", "20:00");
        scene.Staff(employee, site, "2027-01-05", "08:00", "20:00");
        scene.Staff(employee, site, "2027-01-06", "08:00", "20:00");
        Shift fortyEighth = scene.Plan(site, "2027-01-07", "08:00", "20:00");

        // Act
        RefusalReason reason = scene.Roster.Check(fortyEighth.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.None, reason);
    }

    [Fact]
    public void One_hour_beyond_forty_eight_in_the_week_is_refused() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        FillWeekWithFourTwelveHourShifts(scene, employee, site);
        Shift fortyNinth = scene.Plan(site, "2027-01-08", "08:00", "09:00");   // Friday, twelve hours of rest after Thursday

        // Act
        RefusalReason reason = scene.Roster.Check(fortyNinth.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, reason);
    }

    [Fact]
    public void The_limit_is_per_employee() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee exhausted = scene.Hire(site);
        Employee fresh = scene.Hire(site);
        FillWeekWithFourTwelveHourShifts(scene, exhausted, site);
        Shift friday = scene.Plan(site, "2027-01-08", "08:00", "09:00");

        // Act
        RefusalReason reason = scene.Roster.Check(friday.Number, fresh.Id);

        // Assert
        Assert.Equal(RefusalReason.None, reason);
    }

    [Fact]
    public void The_limit_is_per_week() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        FillWeekWithFourTwelveHourShifts(scene, employee, site);
        Shift nextMonday = scene.Plan(site, "2027-01-11", "08:00", "20:00");

        // Act
        RefusalReason reason = scene.Roster.Check(nextMonday.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.None, reason);
    }

    [Fact]
    public void A_spring_forward_night_counts_for_seven_hours_so_it_still_fits_where_eight_would_not() {
        // Arrange
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 12), 1));   // 2027-03-22 to 2027-03-28
        Site paris = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(paris);
        scene.Staff(employee, paris, "2027-03-22", "08:00", "20:00");        // 12 h
        scene.Staff(employee, paris, "2027-03-23", "08:00", "20:00");        // 12 h
        scene.Staff(employee, paris, "2027-03-24", "08:00", "20:00");        // 12 h
        scene.Staff(employee, paris, "2027-03-25", "08:00", "13:00");        //  5 h: 41 h so far
        Shift night = scene.Plan(paris, "2027-03-27", "22:00", "06:00");     // 8 h on the clock, 7 h elapsed: 48 h

        // Act
        RefusalReason reason = scene.Roster.Check(night.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.None, reason);
    }

    [Fact]
    public void A_fall_back_night_counts_for_nine_hours_so_it_overflows_where_eight_would_not() {
        // Arrange
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 43), 1));   // 2027-10-25 to 2027-10-31
        Site paris = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(paris);
        scene.Staff(employee, paris, "2027-10-25", "08:00", "20:00");        // 12 h
        scene.Staff(employee, paris, "2027-10-26", "08:00", "20:00");        // 12 h
        scene.Staff(employee, paris, "2027-10-27", "08:00", "20:00");        // 12 h
        scene.Staff(employee, paris, "2027-10-28", "08:00", "12:00");        //  4 h: 40 h so far
        Shift night = scene.Plan(paris, "2027-10-30", "22:00", "06:00");     // 8 h on the clock, 9 h elapsed: 49 h

        // Act
        RefusalReason reason = scene.Roster.Check(night.Number, employee.Id);

        // Assert
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, reason);
    }

}
