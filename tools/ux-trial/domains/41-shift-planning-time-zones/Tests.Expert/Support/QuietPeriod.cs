namespace ShiftPlanning.Domain.Tests.Expert.Support;

/// <summary>
///     Four weeks, Monday 2027-01-04 to Sunday 2027-01-31, in which none of the zones <c>Any.SiteZone()</c> draws from changes its offset.
///     It is a prerequisite of the tests that draw a zone or a date and do not care about daylight saving; the tests that do care
///     name their own dates and their own zone.
/// </summary>
internal static class QuietPeriod {

    public static readonly PlanningPeriod Period = new PlanningPeriod(new IsoWeek(2027, 1), 4);

    /// <summary>Any date of the period.</summary>
    public static AnyDateOnly AnyDay() {
        return Any.DateOnly().Between(Period.FirstDay, Period.LastDay);
    }

    /// <summary>A shift request at the site, on any day of the period, that has no say in what the test verifies.</summary>
    public static ShiftRequest AnyRequestAt(SiteId site) {
        return Any.ShiftRequest()
                  .WithSite(site)
                  .WithStartDate(date => date.Between(Period.FirstDay, Period.LastDay))
                  .Generate();
    }

    /// <summary>
    ///     A shift at the site that has no say in what the test verifies. The request is drawn within the period rather than over
    ///     all dates, because a drawn date near the ends of the calendar, or in a gap, is a date the site's zone cannot place.
    /// </summary>
    public static Shift AnyShiftAt(Site site) {
        return new Shift(1, site, AnyRequestAt(site.Id));
    }

}
