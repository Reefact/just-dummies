namespace SmartGrid.Domain;

/// <summary>An off-peak window on half-hour boundaries; it may wrap past midnight (22:00 to 06:00).</summary>
public sealed class OffPeakWindow {

    public OffPeakWindow(TimeOnly start, TimeOnly end) {
        if (start == end) {
            throw new ArgumentException("An off-peak window cannot be empty.", nameof(end));
        }
        if (start.Ticks % TimeSpan.FromMinutes(30).Ticks != 0) {
            throw new ArgumentException("An off-peak window starts on a half-hour boundary.", nameof(start));
        }
        if (end.Ticks % TimeSpan.FromMinutes(30).Ticks != 0) {
            throw new ArgumentException("An off-peak window ends on a half-hour boundary.", nameof(end));
        }

        Start = start;
        End   = end;
    }

    public TimeOnly Start { get; }
    public TimeOnly End   { get; }

    public bool WrapsMidnight => End < Start;

    /// <summary>Whether a slot starting at <paramref name="slotStart" /> lies in the window: start inclusive, end exclusive.</summary>
    public bool Contains(TimeOnly slotStart) {
        return WrapsMidnight ? slotStart >= Start || slotStart < End : slotStart >= Start && slotStart < End;
    }

}
