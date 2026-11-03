namespace Freight.Domain.Tests.Discovery;

/// <summary>Literal times and itineraries for tests whose assertion is about them.</summary>
internal static class Calendar {

    public static DateTimeOffset At(int day, int hour = 0) {
        return new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero).AddDays(day - 1).AddHours(hour);
    }

    public static Leg Leg(string origin, string destination, DateTimeOffset departure, DateTimeOffset arrival) {
        return new Leg(new UnLocode(origin), new UnLocode(destination), departure, arrival);
    }

    /// <summary>A single leg Le Havre to Shanghai, leaving on day 1 and arriving on day 31.</summary>
    public static Leg LeHavreToShanghai() {
        return Leg("FRLEH", "CNSHA", At(1), At(31));
    }

}
