namespace ShiftPlanning.Domain.Tests.Expert.Support;

internal static class Hours {

    public static readonly DayOfWeek[] Weekdays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    /// <summary>Office hours from nine to five on weekdays — the hours the meeting tests reason on.</summary>
    public static WorkingHours NineToFive() {
        return new WorkingHours(Clock.At("09:00"), Clock.At("17:00"), Weekdays);
    }

}
