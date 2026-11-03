using JustDummies;

namespace ShiftPlanning.Domain.Tests.Regular.Support;

/// <summary>
///     A draft roster over a drawn period, with one registered site in a zone without daylight saving (Tokyo)
///     and its employees. The calendar is arbitrary, so no clock transition can ever fall inside it.
/// </summary>
internal sealed class Scene {

    public Scene(int employees = 2, int minimumWeeks = 1) {
        Period    = new AnyPlanningPeriod().WithWeekCount(c => c.GreaterThanOrEqualTo(minimumWeeks)).Generate();
        Site      = Kit.SiteIn(Kit.Tokyo);
        Roster    = Kit.RosterWith(Period, Site);
        Employees = Kit.EmployeesAt(Site, employees);
        foreach (Employee employee in Employees) { Roster.RegisterEmployee(employee); }
    }

    public PlanningPeriod Period { get; }

    public Site Site { get; }

    public Roster Roster { get; }

    public List<Employee> Employees { get; }

    public Employee First => Employees[0];

    public Employee Second => Employees[1];

    /// <summary>The Monday the period starts on.</summary>
    public DateOnly Monday => Period.FirstDay;

    public DateOnly Day(int offsetFromMonday) {
        return Monday.AddDays(offsetFromMonday);
    }

    public Shift Plan(int dayOffset, string start, string end) {
        return Kit.Plan(Roster, Site, Day(dayOffset), start, end);
    }

    public Shift PlanAndAssign(Employee employee, int dayOffset, string start, string end) {
        Shift shift = Plan(dayOffset, start, end);
        Roster.Assign(shift.Number, employee.Id);

        return shift;
    }

    /// <summary>Publishes the roster through one staffed shift.</summary>
    public void Publish() {
        PlanAndAssign(First, 0, "08:00", "16:00");
        Roster.Publish();
    }

}
