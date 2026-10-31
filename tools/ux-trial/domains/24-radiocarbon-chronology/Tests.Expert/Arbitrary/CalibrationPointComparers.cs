namespace RadiocarbonChronology.Domain;

/// <summary>A curve holds at most one point per calendar year: that is the equality its points are distinct under.</summary>
public static class CalibrationPointComparers {

    public static readonly IEqualityComparer<CalibrationPoint> ByCalendarYear = new CalendarYearComparer();

    private sealed class CalendarYearComparer : IEqualityComparer<CalibrationPoint> {

        public bool Equals(CalibrationPoint? x, CalibrationPoint? y) {
            return x?.CalendarYear == y?.CalendarYear;
        }

        public int GetHashCode(CalibrationPoint obj) {
            return obj.CalendarYear;
        }

    }

}
