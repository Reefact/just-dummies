using JustDummies;

using ShiftPlanning.Domain;

namespace ShiftPlanning.Domain.Tests.Regular.Support;

/// <summary>Zones and builders shared by the tests. A zone is pinned wherever the test is about the clock.</summary>
internal static class Kit {

    public static readonly SiteZone Paris    = new SiteZone("Europe/Paris");
    public static readonly SiteZone NewYork  = new SiteZone("America/New_York");
    public static readonly SiteZone Tokyo    = new SiteZone("Asia/Tokyo");
    public static readonly SiteZone Sydney   = new SiteZone("Australia/Sydney");
    public static readonly SiteZone Utc      = new SiteZone("UTC");

    public static readonly WorkingHours OfficeHours = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0), new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday });

    public static Site SiteIn(SiteZone zone) {
        return new AnySite().WithZone(zone).Generate();
    }

    public static Site OfficeIn(SiteZone zone) {
        return new AnySite().WithZone(zone).WithWorkingHours(OfficeHours).Generate();
    }

    /// <summary>Several sites in the given zones, with distinct ids.</summary>
    public static List<Site> OfficesIn(params SiteZone[] zones) {
        List<SiteId> ids = Any.ListOf(new AnySiteId()).WithCount(zones.Length).Distinct().Generate();

        return zones.Select((zone, i) => new AnySite().WithId(ids[i]).WithZone(zone).WithWorkingHours(OfficeHours).Generate()).ToList();
    }

    /// <summary>Several employees of the same home site, with distinct ids.</summary>
    public static List<Employee> EmployeesAt(Site home, int count) {
        List<EmployeeId> ids = Any.ListOf(new AnyEmployeeId()).WithCount(count).Distinct().Generate();

        return ids.Select(id => new AnyEmployee().WithId(id).WithHomeSite(home).Generate()).ToList();
    }

    public static Roster RosterWith(PlanningPeriod period, params Site[] sites) {
        Roster roster = new Roster(period);
        foreach (Site site in sites) { roster.RegisterSite(site); }

        return roster;
    }

    public static ShiftRequest Request(Site site, DateOnly date, string start, string end, OffsetChoice startChoice = OffsetChoice.Unspecified, OffsetChoice endChoice = OffsetChoice.Unspecified) {
        return new ShiftRequest(site.Id, date, TimeOnly.Parse(start), TimeOnly.Parse(end), startChoice, endChoice);
    }

    public static Shift Plan(Roster roster, Site site, DateOnly date, string start, string end, OffsetChoice startChoice = OffsetChoice.Unspecified, OffsetChoice endChoice = OffsetChoice.Unspecified) {
        return roster.PlanShift(Request(site, date, start, end, startChoice, endChoice));
    }

    public static DateOnly D(int year, int month, int day) {
        return new DateOnly(year, month, day);
    }

    public static DateTimeOffset Utc_(int year, int month, int day, int hour, int minute) {
        return new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
    }

}
