using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.RosterBehaviour;

public sealed class RosterPlanningTests {

    // ---- Numbering ---------------------------------------------------------------------------------------------

    [Fact]
    public void Planned_shifts_are_unstaffed_and_numbered_from_one() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();

        // Act
        Shift first  = scene.PlanAnyShiftAt(site);
        Shift second = scene.PlanAnyShiftAt(site);
        Shift third  = scene.PlanAnyShiftAt(site);

        // Assert
        Assert.Equal([1, 2, 3], [first.Number, second.Number, third.Number]);
        Assert.All(new[] { first, second, third }, shift => Assert.False(shift.IsStaffed));
        Assert.Equal(3, scene.Roster.Shifts.Count);
        Assert.Same(second, scene.Roster.FindShift(2));
    }

    [Fact]
    public void A_refused_request_does_not_use_up_a_number() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Assert.Throws<RosterRefusedException>(() => scene.Plan(site, "2027-02-14", "08:00", "16:00"));   // outside the period

        // Act
        Shift next = scene.PlanAnyShiftAt(site);

        // Assert
        Assert.Equal(1, next.Number);
    }

    [Fact]
    public void A_cancelled_shifts_number_is_not_reused() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Shift cancelled = scene.PlanAnyShiftAt(site);
        scene.Roster.CancelShift(cancelled.Number);

        // Act
        Shift next = scene.PlanAnyShiftAt(site);

        // Assert
        Assert.Equal(2, next.Number);
        Assert.Null(scene.Roster.FindShift(cancelled.Number));
    }

    [Fact]
    public void An_unknown_shift_number_finds_nothing() {
        // Arrange
        Scene scene = new Scene();

        // Assert
        Assert.Null(scene.Roster.FindShift(1));
        Assert.Null(scene.Roster.FindShift(0));
        Assert.Null(scene.Roster.FindShift(-1));
    }

    [Fact]
    public void The_shift_is_resolved_in_the_zone_of_its_site() {
        // Arrange
        Scene scene = new Scene();
        Site tokyo = scene.OpenSite("Asia/Tokyo");

        // Act
        Shift shift = scene.Plan(tokyo, "2027-01-12", "09:00", "17:00");

        // Assert
        Assert.Equal(Clock.Utc("2027-01-12 00:00"), shift.StartUtc);
        Assert.Equal(Clock.Utc("2027-01-12 08:00"), shift.EndUtc);
    }

    [Fact]
    public void A_missing_request_is_refused() {
        // Arrange
        Scene scene = new Scene();

        // Act
        Action plan = () => scene.Roster.PlanShift(null!);

        // Assert
        Assert.Throws<ArgumentNullException>("request", plan);
    }

    // ---- Site and period ---------------------------------------------------------------------------------------

    [Fact]
    public void A_shift_at_a_site_that_is_not_registered_is_refused() {
        // Arrange
        Scene scene = new Scene();
        SiteId unregisteredSite = Any.SiteId().Generate();
        ShiftRequest request = QuietPeriod.AnyRequestAt(unregisteredSite);

        // Act
        Action plan = () => scene.Roster.PlanShift(request);

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, Refusal.Of(plan));
    }

    [Theory]
    [InlineData("2027-01-04")]   // its first day
    [InlineData("2027-01-31")]   // its last day
    public void A_shift_starting_on_the_first_or_the_last_day_of_the_planning_period_is_planned(string startDate) {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");

        // Act
        Shift shift = scene.Plan(site, startDate, "08:00", "16:00");

        // Assert
        Assert.Equal(Clock.Day(startDate), shift.Request.StartDate);
    }

    [Theory]
    [InlineData("2027-01-03")]   // the Sunday before the period
    [InlineData("2027-02-01")]   // the Monday after it
    public void A_shift_starting_outside_the_planning_period_is_refused(string startDate) {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");

        // Act
        Action plan = () => scene.Plan(site, startDate, "08:00", "16:00");

        // Assert
        Assert.Equal(RefusalReason.OutsidePlanningPeriod, Refusal.Of(plan));
    }

    [Fact]
    public void An_overnight_shift_starting_on_the_last_day_may_end_after_the_period() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");

        // Act
        Shift shift = scene.Plan(site, "2027-01-31", "22:00", "06:00");

        // Assert
        Assert.Equal(Clock.Day("2027-02-01"), shift.Request.EndDate);
    }

    [Fact]
    public void An_overnight_shift_ending_on_the_first_day_but_starting_before_the_period_is_refused() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");

        // Act
        Action plan = () => scene.Plan(site, "2027-01-03", "22:00", "06:00");

        // Assert
        Assert.Equal(RefusalReason.OutsidePlanningPeriod, Refusal.Of(plan));
    }

    [Fact]
    public void The_period_is_checked_before_the_local_times_are_resolved() {
        // Arrange
        Scene scene = new Scene();
        Site paris = scene.OpenSite("Europe/Paris");

        // Act
        Action planInTheGapOutsideThePeriod = () => scene.Plan(paris, "2027-03-28", "02:30", "08:30");

        // Assert
        Assert.Equal(RefusalReason.OutsidePlanningPeriod, Refusal.Of(planInTheGapOutsideThePeriod));
    }

    [Fact]
    public void The_site_is_checked_before_the_period() {
        // Arrange
        Scene scene = new Scene();
        ShiftRequest outsideThePeriodAtAnUnknownSite = new ShiftRequest(Any.SiteId().Generate(), Clock.Day("2027-06-01"), Clock.At("08:00"), Clock.At("16:00"));

        // Act
        Action plan = () => scene.Roster.PlanShift(outsideThePeriodAtAnUnknownSite);

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, Refusal.Of(plan));
    }

    // ---- Local times that do not resolve -----------------------------------------------------------------------

    private static Scene SceneAroundSpringForward(out Site paris) {
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 12), 1));   // 2027-03-22 to 2027-03-28
        paris = scene.OpenSite("Europe/Paris");

        return scene;
    }

    private static Scene SceneAroundFallBack(out Site paris) {
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 43), 1));   // 2027-10-25 to 2027-10-31
        paris = scene.OpenSite("Europe/Paris");

        return scene;
    }

    [Theory]
    [InlineData(OffsetChoice.Unspecified)]
    [InlineData(OffsetChoice.Earlier)]
    [InlineData(OffsetChoice.Later)]
    public void A_start_in_the_spring_forward_gap_is_refused_whatever_the_choice(OffsetChoice choice) {
        // Arrange
        Scene scene = SceneAroundSpringForward(out Site paris);

        // Act
        Action plan = () => scene.Plan(paris, "2027-03-28", "02:30", "08:30", startChoice: choice);

        // Assert
        Assert.Equal(RefusalReason.StartTimeDoesNotExist, Refusal.Of(plan));
    }

    [Theory]
    [InlineData(OffsetChoice.Unspecified)]
    [InlineData(OffsetChoice.Earlier)]
    [InlineData(OffsetChoice.Later)]
    public void An_end_in_the_spring_forward_gap_is_refused_whatever_the_choice(OffsetChoice choice) {
        // Arrange
        Scene scene = SceneAroundSpringForward(out Site paris);

        // Act
        Action plan = () => scene.Plan(paris, "2027-03-27", "22:30", "02:30", endChoice: choice);

        // Assert
        Assert.Equal(RefusalReason.EndTimeDoesNotExist, Refusal.Of(plan));
    }

    [Fact]
    public void A_start_in_the_fall_back_overlap_needs_a_choice() {
        // Arrange
        Scene scene = SceneAroundFallBack(out Site paris);

        // Act
        Action plan = () => scene.Plan(paris, "2027-10-31", "02:30", "08:30");

        // Assert
        Assert.Equal(RefusalReason.StartTimeAmbiguous, Refusal.Of(plan));
    }

    [Fact]
    public void An_end_in_the_fall_back_overlap_needs_a_choice() {
        // Arrange
        Scene scene = SceneAroundFallBack(out Site paris);

        // Act
        Action plan = () => scene.Plan(paris, "2027-10-30", "22:30", "02:30");

        // Assert
        Assert.Equal(RefusalReason.EndTimeAmbiguous, Refusal.Of(plan));
    }

    [Theory]
    [InlineData(OffsetChoice.Earlier, 7)]
    [InlineData(OffsetChoice.Later, 6)]
    public void An_ambiguous_start_with_a_choice_is_planned_and_the_choice_sets_the_elapsed_length(OffsetChoice choice, int expectedHours) {
        // Arrange
        Scene scene = SceneAroundFallBack(out Site paris);

        // Act
        Shift shift = scene.Plan(paris, "2027-10-31", "02:30", "08:30", startChoice: choice);

        // Assert
        Assert.Equal(TimeSpan.FromHours(expectedHours), shift.ElapsedDuration);
    }

    [Theory]
    [InlineData(OffsetChoice.Earlier, 3)]
    [InlineData(OffsetChoice.Later, 4)]
    public void An_ambiguous_end_with_a_choice_is_planned_and_the_choice_sets_the_elapsed_length(OffsetChoice choice, int expectedHours) {
        // Arrange
        Scene scene = SceneAroundFallBack(out Site paris);

        // Act
        Shift shift = scene.Plan(paris, "2027-10-30", "23:30", "02:30", endChoice: choice);   // 23:30 CEST = 21:30Z

        // Assert
        Assert.Equal(TimeSpan.FromHours(expectedHours), shift.ElapsedDuration);
    }

    [Fact]
    public void A_choice_given_for_a_time_that_occurs_once_changes_nothing() {
        // Arrange
        Scene scene = SceneAroundFallBack(out Site paris);

        // Act
        Shift unspecified = scene.Plan(paris, "2027-10-26", "08:00", "16:00");
        Shift withChoices = scene.Plan(paris, "2027-10-26", "08:00", "16:00", OffsetChoice.Later, OffsetChoice.Earlier);

        // Assert
        Assert.Equal(unspecified.StartUtc, withChoices.StartUtc);
        Assert.Equal(unspecified.EndUtc, withChoices.EndUtc);
    }

    [Fact]
    public void Choices_that_would_end_the_shift_before_it_starts_are_refused() {
        // Arrange
        // Troll station falls back two hours at 2027-10-31 01:00 UTC, so 01:00 to 02:59 on the wall clock happens twice.
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 43), 1));
        Site troll = scene.OpenSite("Antarctica/Troll");

        // Act
        Action plan = () => scene.Plan(troll, "2027-10-31", "01:00", "02:00", OffsetChoice.Later, OffsetChoice.Earlier);

        // Assert
        Assert.Equal(RefusalReason.EndNotAfterStart, Refusal.Of(plan));
    }

    [Fact]
    public void The_same_wall_clock_shift_with_consistent_choices_is_planned() {
        // Arrange
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 43), 1));
        Site troll = scene.OpenSite("Antarctica/Troll");

        // Act
        Shift shift = scene.Plan(troll, "2027-10-31", "01:00", "02:00", OffsetChoice.Earlier, OffsetChoice.Later);

        // Assert
        Assert.Equal(TimeSpan.FromHours(3), shift.ElapsedDuration);   // 01:00 at +02:00 is 23:00Z, 02:00 at +00:00 is 02:00Z
    }

    // ---- Cancelling --------------------------------------------------------------------------------------------

    [Fact]
    public void A_cancelled_shift_disappears_from_the_roster() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Shift kept = scene.PlanAnyShiftAt(site);
        Shift cancelled = scene.PlanAnyShiftAt(site);

        // Act
        scene.Roster.CancelShift(cancelled.Number);

        // Assert
        Assert.Same(kept, Assert.Single(scene.Roster.Shifts));
        Assert.Null(scene.Roster.FindShift(cancelled.Number));
    }

    [Fact]
    public void A_cancelled_staffed_shift_leaves_the_employees_shifts() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        Shift staffed = scene.PlanAnyShiftAt(site);
        scene.Roster.Assign(staffed.Number, employee.Id);

        // Act
        scene.Roster.CancelShift(staffed.Number);

        // Assert
        Assert.Empty(scene.Roster.ShiftsOf(employee.Id));
    }

    [Fact]
    public void A_shift_that_does_not_exist_cannot_be_cancelled() {
        // Arrange
        Scene scene = new Scene();

        // Act
        Action cancel = () => scene.Roster.CancelShift(1);

        // Assert
        Assert.Equal(RefusalReason.UnknownShift, Refusal.Of(cancel));
    }

    // ---- A published roster is frozen --------------------------------------------------------------------------

    private static Scene PublishedScene(out Site site, out Employee employee) {
        Scene scene = new Scene();
        site = scene.OpenSite();
        employee = scene.Hire(site);
        scene.PublishOneStaffedShift(employee, site);

        return scene;
    }

    [Fact]
    public void Nothing_more_can_be_planned_once_the_roster_is_published() {
        // Arrange
        Scene scene = PublishedScene(out Site site, out _);
        ShiftRequest request = QuietPeriod.AnyRequestAt(site.Id);

        // Act
        Action plan = () => scene.Roster.PlanShift(request);

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, Refusal.Of(plan));
    }

    [Fact]
    public void Publication_is_reported_before_an_unknown_site() {
        // Arrange
        Scene scene = PublishedScene(out _, out _);
        ShiftRequest requestAtAnUnknownSite = QuietPeriod.AnyRequestAt(Any.SiteId().Generate());

        // Act
        Action plan = () => scene.Roster.PlanShift(requestAtAnUnknownSite);

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, Refusal.Of(plan));
    }

    [Fact]
    public void No_shift_can_be_cancelled_once_the_roster_is_published() {
        // Arrange
        Scene scene = PublishedScene(out _, out _);
        int existingShift = scene.Roster.Shifts.Single().Number;

        // Act
        Action cancel = () => scene.Roster.CancelShift(existingShift);

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, Refusal.Of(cancel));
    }

    [Fact]
    public void Publication_is_reported_before_an_unknown_shift_when_cancelling() {
        // Arrange
        Scene scene = PublishedScene(out _, out _);
        int unknownShift = scene.Roster.Shifts.Single().Number + 1;

        // Act
        Action cancel = () => scene.Roster.CancelShift(unknownShift);

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, Refusal.Of(cancel));
    }

}
