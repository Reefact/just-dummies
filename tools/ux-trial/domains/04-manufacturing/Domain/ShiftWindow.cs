namespace Manufacturing.Domain;

/// <summary>The time slot a work order is scheduled into: start strictly before end, twelve hours at most.</summary>
public sealed class ShiftWindow {

    public static readonly TimeSpan LongestShift = TimeSpan.FromHours(12);

    public ShiftWindow(DateTime start, DateTime end) {
        if (start >= end) {
            throw new ArgumentException("A shift starts before it ends.", nameof(end));
        }
        if (end - start > LongestShift) {
            throw new ArgumentException("A shift lasts twelve hours at most.", nameof(end));
        }
        Start = start;
        End   = end;
    }

    public DateTime Start { get; }
    public DateTime End   { get; }

    public TimeSpan Length => End - Start;

}
