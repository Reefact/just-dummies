namespace ShiftPlanning.Domain.Tests.Expert.Support;

/// <summary>
///     A roster with the sites, the employees and the shifts a test needs. Identities and names are dummies; what a test
///     cares about (a zone, a date, a clock time) is given by the test, as a literal.
/// </summary>
internal sealed class Scene {

    // Identifiers are drawn up front, distinct, so two sites or two employees of one scene never collide.
    private readonly Queue<SiteId>     _siteIds     = new Queue<SiteId>(Any.ListOf(Any.SiteId()).WithCount(8).Distinct().Generate());
    private readonly Queue<EmployeeId> _employeeIds = new Queue<EmployeeId>(Any.ListOf(Any.EmployeeId()).WithCount(12).Distinct().Generate());

    public Scene() : this(QuietPeriod.Period) { }

    public Scene(PlanningPeriod period) {
        Roster = new Roster(period);
    }

    public Roster Roster { get; }

    /// <summary>Registers a site in the given zone; its working hours, when the test does not name them, are arbitrary.</summary>
    public Site OpenSite(string zoneId) {
        Site site = Any.Site().WithId(_siteIds.Dequeue()).WithZone(zone => zone.WithIanaId(zoneId)).Generate();
        Roster.RegisterSite(site);

        return site;
    }

    /// <summary>Registers a site in an arbitrary zone, for the tests to which the zone makes no difference.</summary>
    public Site OpenSite() {
        Site site = Any.Site().WithId(_siteIds.Dequeue()).Generate();
        Roster.RegisterSite(site);

        return site;
    }

    public Site OpenSite(string zoneId, WorkingHours workingHours) {
        Site site = Any.Site().WithId(_siteIds.Dequeue()).WithZone(zone => zone.WithIanaId(zoneId)).WithWorkingHours(workingHours).Generate();
        Roster.RegisterSite(site);

        return site;
    }

    public Employee Hire(Site homeSite) {
        Employee employee = Any.Employee().WithId(_employeeIds.Dequeue()).WithHomeSite(homeSite).Generate();
        Roster.RegisterEmployee(employee);

        return employee;
    }

    /// <summary>Plans a shift given as the planner writes it: local date, local start and end.</summary>
    public Shift Plan(Site site, string date, string start, string end, OffsetChoice startChoice = OffsetChoice.Unspecified, OffsetChoice endChoice = OffsetChoice.Unspecified) {
        return Roster.PlanShift(new ShiftRequest(site.Id, Clock.Day(date), Clock.At(start), Clock.At(end), startChoice, endChoice));
    }

    /// <summary>Plans a shift and gives it to the employee, who must be allowed to take it.</summary>
    public Shift Staff(Employee employee, Site site, string date, string start, string end) {
        Shift shift = Plan(site, date, start, end);
        Roster.Assign(shift.Number, employee.Id);

        return shift;
    }

    /// <summary>A shift whose details no test cares about, planned at the site on an arbitrary day of the quiet period.</summary>
    public Shift PlanAnyShiftAt(Site site) {
        return Roster.PlanShift(QuietPeriod.AnyRequestAt(site.Id));
    }

    /// <summary>The roster once published: one arbitrary shift, staffed, then published.</summary>
    public void PublishOneStaffedShift(Employee employee, Site site) {
        Shift shift = PlanAnyShiftAt(site);
        Roster.Assign(shift.Number, employee.Id);
        Roster.Publish();
    }

}
