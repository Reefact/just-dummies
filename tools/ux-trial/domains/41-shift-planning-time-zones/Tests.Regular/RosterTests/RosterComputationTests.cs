using JustDummies;

using ShiftPlanning.Domain.Tests.Regular.Support;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.RosterTests;

public sealed class RosterComputationTests {

    [Fact]
    public void The_shifts_of_an_employee_come_back_by_start_whatever_the_order_they_were_given_in() {
        // Arrange
        Scene scene  = new Scene();
        Shift third  = scene.PlanAndAssign(scene.First, 2, "08:00", "16:00");
        Shift first  = scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift second = scene.PlanAndAssign(scene.First, 1, "08:00", "16:00");
        scene.PlanAndAssign(scene.Second, 3, "08:00", "16:00");

        // Act
        IReadOnlyList<Shift> shifts = scene.Roster.ShiftsOf(scene.First.Id);

        // Assert
        Assert.Equal(new[] { first, second, third }, shifts);
    }

    [Fact]
    public void An_employee_without_shift_has_none() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.Second, 0, "08:00", "16:00");

        // Act & Assert
        Assert.Empty(scene.Roster.ShiftsOf(scene.First.Id));
        Assert.Throws<ArgumentNullException>(() => scene.Roster.ShiftsOf(null!));
    }

    [Fact]
    public void The_shortest_rest_is_unknown_below_two_shifts() {
        // Arrange
        Scene scene = new Scene();

        // Act & Assert
        Assert.Null(scene.Roster.ShortestRest(scene.First.Id));
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Assert.Null(scene.Roster.ShortestRest(scene.First.Id));
    }

    [Fact]
    public void The_shortest_rest_is_the_smallest_gap_between_consecutive_shifts() {
        // Arrange: gaps of 16 h then 12 h 30 then 20 h
        Scene scene = new Scene(minimumWeeks: 1);
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        scene.PlanAndAssign(scene.First, 1, "08:00", "16:00");
        scene.PlanAndAssign(scene.First, 2, "04:30", "08:30");
        scene.PlanAndAssign(scene.First, 3, "04:30", "08:30");

        // Act
        TimeSpan? shortest = scene.Roster.ShortestRest(scene.First.Id);

        // Assert: 16:00 -> 08:00 = 16 h, 16:00 -> 04:30 = 12 h 30, 08:30 -> 04:30 = 20 h
        Assert.Equal(TimeSpan.FromHours(12.5), shortest);
    }

    [Fact]
    public void The_shortest_rest_ignores_the_shifts_of_colleagues() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        scene.PlanAndAssign(scene.Second, 0, "16:00", "23:00");
        scene.PlanAndAssign(scene.First, 2, "08:00", "16:00");

        // Act & Assert
        Assert.Equal(TimeSpan.FromHours(40), scene.Roster.ShortestRest(scene.First.Id));
    }

    [Fact]
    public void Elapsed_time_adds_up_the_shifts_that_start_in_the_week() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        scene.PlanAndAssign(scene.First, 2, "10:00", "14:30");

        // Act & Assert
        Assert.Equal(TimeSpan.FromHours(12.5), scene.Roster.ElapsedTimeIn(scene.First.Id, scene.Period.FirstWeek));
    }

    [Fact]
    public void Elapsed_time_of_a_week_without_shifts_is_zero() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        IsoWeek anyOtherWeek = new AnyIsoWeek().WithYear(2150).Generate();

        // Act & Assert
        Assert.Equal(TimeSpan.Zero, scene.Roster.ElapsedTimeIn(scene.First.Id, anyOtherWeek));
        Assert.Equal(TimeSpan.Zero, scene.Roster.ElapsedTimeIn(scene.Second.Id, scene.Period.FirstWeek));
    }

    [Fact]
    public void Elapsed_time_of_an_employee_who_is_not_registered_is_refused() {
        // Arrange
        Scene      scene    = new Scene();
        EmployeeId stranger = Any.ListOf(new AnyEmployeeId()).WithCount(3).Distinct().Generate().First(id => scene.Employees.All(e => !e.Id.Equals(id)));

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => scene.Roster.ElapsedTimeIn(stranger, scene.Period.FirstWeek));

        // Assert
        Assert.Equal(RefusalReason.UnknownEmployee, refusal.Reason);
    }

    [Fact]
    public void Elapsed_time_needs_an_employee_and_a_week() {
        // Arrange
        Scene scene = new Scene();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => scene.Roster.ElapsedTimeIn(null!, scene.Period.FirstWeek));
        Assert.Throws<ArgumentNullException>(() => scene.Roster.ElapsedTimeIn(scene.First.Id, null!));
    }

    [Fact]
    public void Elapsed_time_counts_the_hours_really_worked_across_a_transition() {
        // Arrange
        Site     paris    = Kit.SiteIn(Kit.Paris);
        Roster   roster   = Kit.RosterWith(new PlanningPeriod(new IsoWeek(2026, 12), 3), paris);
        Employee employee = Kit.EmployeesAt(paris, 1)[0];
        roster.RegisterEmployee(employee);
        roster.Assign(Kit.Plan(roster, paris, new DateOnly(2026, 3, 28), "22:00", "06:00").Number, employee.Id);

        // Act & Assert: the night starts in week 13 and lasts 7 h
        Assert.Equal(TimeSpan.FromHours(7), roster.ElapsedTimeIn(employee.Id, new IsoWeek(2026, 13)));
        Assert.Equal(TimeSpan.Zero, roster.ElapsedTimeIn(employee.Id, new IsoWeek(2026, 14)));
    }

}
