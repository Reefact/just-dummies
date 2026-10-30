namespace Payments.Domain;

/// <summary>The TARGET2 closing days that matter for a SEPA requested execution date (Easter omitted).</summary>
public static class BusinessCalendar {

    public static bool IsBusinessDay(DateOnly date) {
        return date.DayOfWeek switch {
            DayOfWeek.Saturday or DayOfWeek.Sunday => false,
            _ => !IsFixedHoliday(date)
        };
    }

    public static DateOnly NextBusinessDayOnOrAfter(DateOnly date) {
        DateOnly candidate = date;
        while (!IsBusinessDay(candidate)) {
            candidate = candidate.AddDays(1);
        }

        return candidate;
    }

    private static bool IsFixedHoliday(DateOnly date) {
        return (date.Month, date.Day) is (1, 1) or (5, 1) or (12, 25) or (12, 26);
    }

}
