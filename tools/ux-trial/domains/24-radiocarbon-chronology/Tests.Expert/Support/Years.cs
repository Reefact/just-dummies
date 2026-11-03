using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert.Support;

internal static class Years {

    /// <summary>Any year the calendar holds.</summary>
    public static AnyInt32 InTheCalendar() {
        return Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear);
    }

}
