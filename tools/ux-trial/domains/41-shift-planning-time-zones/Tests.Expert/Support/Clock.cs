namespace ShiftPlanning.Domain.Tests.Expert.Support;

/// <summary>Short literals for the dates, times and instants the tests spell out: they are the point of those tests.</summary>
internal static class Clock {

    public static DateOnly Day(string isoDate) {
        return DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static TimeOnly At(string hoursMinutes) {
        return TimeOnly.ParseExact(hoursMinutes, "HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>An instant given in UTC, e.g. <c>"2027-01-12 08:00"</c>.</summary>
    public static DateTimeOffset Utc(string dateAndTime) {
        return new DateTimeOffset(DateTime.ParseExact(dateAndTime, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), TimeSpan.Zero);
    }

}
