using JustDummies;

using ShiftPlanning.Domain.Tests.Regular.Support;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.RosterTests;

public sealed class RosterPlanningTests {

    // Paris clocks: forward on 2026-03-29 (W13), back on 2026-10-25 (W43)
    private static readonly PlanningPeriod SpringPeriod = new PlanningPeriod(new IsoWeek(2026, 12), 3);
    private static readonly PlanningPeriod AutumnPeriod = new PlanningPeriod(new IsoWeek(2026, 42), 3);

    private static RosterRefusedException PlanRefused(Roster roster, ShiftRequest request) {
        return Assert.Throws<RosterRefusedException>(() => roster.PlanShift(request));
    }

    [Fact]
    public void Planned_shifts_are_numbered_from_one_and_start_unstaffed() {
        // Arrange
        Scene scene = new Scene();

        // Act
        Shift first  = scene.Plan(0, "08:00", "16:00");
        Shift second = scene.Plan(1, "08:00", "16:00");
        Shift third  = scene.Plan(2, "08:00", "16:00");

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, new[] { first.Number, second.Number, third.Number });
        Assert.All(new[] { first, second, third }, s => Assert.False(s.IsStaffed));
        Assert.Equal(3, scene.Roster.Shifts.Count);
        Assert.Same(second, scene.Roster.FindShift(2));
    }

    [Fact]
    public void A_shift_is_resolved_in_the_zone_of_its_site() {
        // Arrange
        Scene scene = new Scene();

        // Act
        Shift shift = scene.Plan(0, "09:00", "17:00");

        // Assert: Tokyo is nine hours ahead of UTC
        Assert.Equal(scene.Day(0).ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc), shift.StartUtc.UtcDateTime);
        Assert.Equal(TimeSpan.FromHours(8), shift.ElapsedDuration);
    }

    [Fact]
    public void An_unknown_shift_is_not_found() {
        // Arrange
        Scene scene = new Scene();

        // Act & Assert
        Assert.Null(scene.Roster.FindShift(1));
        Assert.Null(scene.Roster.FindShift(0));
        Assert.Null(scene.Roster.FindShift(-4));
    }

    [Fact]
    public void A_request_for_a_site_that_is_not_registered_is_refused() {
        // Arrange
        Scene scene    = new Scene();
        Site  stranger = new AnySite().Generate();

        // Act
        RosterRefusedException refusal = PlanRefused(scene.Roster, Kit.Request(stranger, scene.Day(0), "08:00", "16:00"));

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, refusal.Reason);
    }

    [Fact]
    public void A_request_needs_to_be_given() {
        Assert.Throws<ArgumentNullException>(() => new Scene().Roster.PlanShift(null!));
    }

    [Fact]
    public void The_first_and_the_last_day_of_the_period_can_start_a_shift() {
        // Arrange
        Scene scene = new Scene();

        // Act
        Shift onFirstDay = Kit.Plan(scene.Roster, scene.Site, scene.Period.FirstDay, "08:00", "16:00");
        Shift onLastDay  = Kit.Plan(scene.Roster, scene.Site, scene.Period.LastDay, "08:00", "16:00");

        // Assert
        Assert.Equal(1, onFirstDay.Number);
        Assert.Equal(2, onLastDay.Number);
    }

    [Fact]
    public void A_shift_starting_the_day_before_or_the_day_after_the_period_is_refused() {
        // Arrange
        Scene scene = new Scene();

        // Act
        RosterRefusedException before = PlanRefused(scene.Roster, Kit.Request(scene.Site, scene.Period.FirstDay.AddDays(-1), "08:00", "16:00"));
        RosterRefusedException after  = PlanRefused(scene.Roster, Kit.Request(scene.Site, scene.Period.LastDay.AddDays(1), "08:00", "16:00"));

        // Assert
        Assert.Equal(RefusalReason.OutsidePlanningPeriod, before.Reason);
        Assert.Equal(RefusalReason.OutsidePlanningPeriod, after.Reason);
        Assert.Empty(scene.Roster.Shifts);
    }

    [Fact]
    public void A_night_shift_starting_on_the_last_day_may_end_after_the_period() {
        // Arrange
        Scene scene = new Scene();

        // Act
        Shift night = Kit.Plan(scene.Roster, scene.Site, scene.Period.LastDay, "22:00", "06:00");

        // Assert
        Assert.False(scene.Period.Contains(night.Request.EndDate));
    }

    [Fact]
    public void A_night_shift_ending_on_the_first_day_but_starting_before_the_period_is_refused() {
        // Arrange
        Scene scene = new Scene();

        // Act
        RosterRefusedException refusal = PlanRefused(scene.Roster, Kit.Request(scene.Site, scene.Period.FirstDay.AddDays(-1), "22:00", "06:00"));

        // Assert
        Assert.Equal(RefusalReason.OutsidePlanningPeriod, refusal.Reason);
    }

    [Fact]
    public void A_start_time_the_clocks_skip_is_refused_whatever_the_choice() {
        // Arrange
        Site   paris  = Kit.SiteIn(Kit.Paris);
        Roster roster = Kit.RosterWith(SpringPeriod, paris);

        // Act & Assert
        foreach (OffsetChoice choice in new[] { OffsetChoice.Unspecified, OffsetChoice.Earlier, OffsetChoice.Later }) {
            RosterRefusedException refusal = PlanRefused(roster, Kit.Request(paris, new DateOnly(2026, 3, 29), "02:30", "10:30", choice));
            Assert.Equal(RefusalReason.StartTimeDoesNotExist, refusal.Reason);
        }
    }

    [Fact]
    public void An_end_time_the_clocks_skip_is_refused() {
        // Arrange
        Site   paris  = Kit.SiteIn(Kit.Paris);
        Roster roster = Kit.RosterWith(SpringPeriod, paris);

        // Act
        RosterRefusedException refusal = PlanRefused(roster, Kit.Request(paris, new DateOnly(2026, 3, 28), "20:00", "02:30", OffsetChoice.Unspecified, OffsetChoice.Later));

        // Assert
        Assert.Equal(RefusalReason.EndTimeDoesNotExist, refusal.Reason);
    }

    [Fact]
    public void A_start_time_the_clocks_repeat_needs_a_choice() {
        // Arrange
        Site   paris  = Kit.SiteIn(Kit.Paris);
        Roster roster = Kit.RosterWith(AutumnPeriod, paris);

        // Act
        RosterRefusedException refusal = PlanRefused(roster, Kit.Request(paris, new DateOnly(2026, 10, 25), "02:30", "10:30"));

        // Assert
        Assert.Equal(RefusalReason.StartTimeAmbiguous, refusal.Reason);
    }

    [Fact]
    public void An_end_time_the_clocks_repeat_needs_a_choice() {
        // Arrange
        Site   paris  = Kit.SiteIn(Kit.Paris);
        Roster roster = Kit.RosterWith(AutumnPeriod, paris);

        // Act
        RosterRefusedException refusal = PlanRefused(roster, Kit.Request(paris, new DateOnly(2026, 10, 24), "20:00", "02:30", OffsetChoice.Unspecified, OffsetChoice.Unspecified));

        // Assert
        Assert.Equal(RefusalReason.EndTimeAmbiguous, refusal.Reason);
    }

    [Theory]
    [InlineData(OffsetChoice.Earlier, 6.5)]
    [InlineData(OffsetChoice.Later, 7.5)]
    public void An_end_time_the_clocks_repeat_is_planned_once_a_choice_is_made(OffsetChoice endChoice, double expectedHours) {
        // Arrange: 20:00 to 02:30 is 6 h 30 on the wall clock; the repeated hour adds one hour to the later reading
        Site   paris  = Kit.SiteIn(Kit.Paris);
        Roster roster = Kit.RosterWith(AutumnPeriod, paris);

        // Act
        Shift shift = Kit.Plan(roster, paris, new DateOnly(2026, 10, 24), "20:00", "02:30", OffsetChoice.Unspecified, endChoice);

        // Assert
        Assert.Equal(TimeSpan.FromHours(expectedHours), shift.ElapsedDuration);
    }

    [Fact]
    public void The_period_is_checked_before_the_clock() {
        // Arrange: the 29th is not in the first week of the spring period
        Site   paris  = Kit.SiteIn(Kit.Paris);
        Roster roster = Kit.RosterWith(new PlanningPeriod(new IsoWeek(2026, 12), 1), paris);

        // Act
        RosterRefusedException refusal = PlanRefused(roster, Kit.Request(paris, new DateOnly(2026, 3, 29), "02:30", "10:30"));

        // Assert
        Assert.Equal(RefusalReason.OutsidePlanningPeriod, refusal.Reason);
    }

    [Fact]
    public void The_site_is_checked_before_the_period() {
        // Arrange
        Scene scene    = new Scene();
        Site  stranger = new AnySite().Generate();

        // Act
        RosterRefusedException refusal = PlanRefused(scene.Roster, Kit.Request(stranger, scene.Period.LastDay.AddDays(30), "08:00", "16:00"));

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, refusal.Reason);
    }

    [Fact]
    public void Choices_that_put_the_end_before_the_start_are_refused() {
        // Arrange: Troll station sets its clocks back two hours on 2026-10-25, so 01:00 can be read before 02:00 and after it
        Site   troll  = Kit.SiteIn(new SiteZone("Antarctica/Troll"));
        Roster roster = Kit.RosterWith(AutumnPeriod, troll);

        // Act
        RosterRefusedException refusal = PlanRefused(roster, Kit.Request(troll, new DateOnly(2026, 10, 25), "01:00", "02:00", OffsetChoice.Later, OffsetChoice.Earlier));

        // Assert
        Assert.Equal(RefusalReason.EndNotAfterStart, refusal.Reason);
        Assert.Empty(roster.Shifts);
    }

    [Fact]
    public void A_refused_request_does_not_use_up_a_number() {
        // Arrange
        Scene scene = new Scene();
        PlanRefused(scene.Roster, Kit.Request(scene.Site, scene.Period.LastDay.AddDays(1), "08:00", "16:00"));

        // Act
        Shift shift = scene.Plan(0, "08:00", "16:00");

        // Assert
        Assert.Equal(1, shift.Number);
    }

    [Fact]
    public void A_cancelled_shift_disappears_and_its_number_is_not_reused() {
        // Arrange
        Scene scene = new Scene();
        scene.Plan(0, "08:00", "16:00");
        scene.Plan(1, "08:00", "16:00");

        // Act
        scene.Roster.CancelShift(2);
        Shift next = scene.Plan(2, "08:00", "16:00");

        // Assert
        Assert.Null(scene.Roster.FindShift(2));
        Assert.Equal(3, next.Number);
        Assert.Equal(new[] { 1, 3 }, scene.Roster.Shifts.Select(s => s.Number));
    }

    [Fact]
    public void Cancelling_a_staffed_shift_frees_the_employee() {
        // Arrange
        Scene scene = new Scene();
        Shift shift = scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");

        // Act
        scene.Roster.CancelShift(shift.Number);

        // Assert
        Assert.Empty(scene.Roster.ShiftsOf(scene.First.Id));
    }

    [Fact]
    public void Cancelling_a_shift_that_does_not_exist_is_refused() {
        // Arrange
        Scene scene = new Scene();
        scene.Plan(0, "08:00", "16:00");

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => scene.Roster.CancelShift(2));

        // Assert
        Assert.Equal(RefusalReason.UnknownShift, refusal.Reason);
    }

    [Fact]
    public void Nothing_can_be_planned_or_cancelled_once_the_roster_is_published() {
        // Arrange
        Scene scene = new Scene();
        scene.Publish();

        // Act
        RosterRefusedException planning     = PlanRefused(scene.Roster, Kit.Request(scene.Site, scene.Day(1), "08:00", "16:00"));
        RosterRefusedException cancellation = Assert.Throws<RosterRefusedException>(() => scene.Roster.CancelShift(1));

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, planning.Reason);
        Assert.Equal(RefusalReason.RosterPublished, cancellation.Reason);
    }

    [Fact]
    public void Publication_is_checked_before_everything_else_when_planning() {
        // Arrange
        Scene scene = new Scene();
        scene.Publish();
        Site stranger = new AnySite().Generate();

        // Act
        RosterRefusedException refusal = PlanRefused(scene.Roster, Kit.Request(stranger, scene.Period.LastDay.AddDays(30), "08:00", "16:00"));

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, refusal.Reason);
    }

    [Fact]
    public void Publication_is_checked_before_the_shift_number_when_cancelling() {
        // Arrange
        Scene scene = new Scene();
        scene.Publish();

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => scene.Roster.CancelShift(999));

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, refusal.Reason);
    }

}
