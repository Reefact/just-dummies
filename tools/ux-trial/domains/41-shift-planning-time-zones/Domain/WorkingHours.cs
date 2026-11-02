namespace ShiftPlanning.Domain;

/// <summary>
/// A site's office hours in local wall-clock time: the same daytime window on each working day.
/// The window does not cross midnight and lasts at least one hour.
/// </summary>
public sealed class WorkingHours : IEquatable<WorkingHours>
{
    private readonly HashSet<DayOfWeek> _days;

    public WorkingHours(TimeOnly opens, TimeOnly closes, IEnumerable<DayOfWeek> days)
    {
        ArgumentNullException.ThrowIfNull(days);
        if (closes <= opens)
        {
            throw new ArgumentException("Working hours close after they open, on the same day.", nameof(closes));
        }

        if (closes - opens < TimeSpan.FromHours(1))
        {
            throw new ArgumentException("Working hours last at least one hour.", nameof(closes));
        }

        List<DayOfWeek> list = days.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("At least one working day is required.", nameof(days));
        }

        foreach (DayOfWeek day in list)
        {
            if (!Enum.IsDefined(day))
            {
                throw new ArgumentOutOfRangeException(nameof(days), day, "Unknown day of week.");
            }
        }

        _days = new HashSet<DayOfWeek>(list);
        if (_days.Count != list.Count)
        {
            throw new ArgumentException("A working day is listed once.", nameof(days));
        }

        Opens = opens;
        Closes = closes;
    }

    public TimeOnly Opens { get; }

    public TimeOnly Closes { get; }

    public IReadOnlyCollection<DayOfWeek> Days => _days;

    /// <summary>True when both local ends fall on the same working day, inside [opens, closes].</summary>
    public bool Covers(DateTime localStart, DateTime localEnd)
    {
        if (localEnd < localStart)
        {
            throw new ArgumentException("The end cannot precede the start.", nameof(localEnd));
        }

        return localStart.Date == localEnd.Date
            && _days.Contains(localStart.DayOfWeek)
            && TimeOnly.FromDateTime(localStart) >= Opens
            && TimeOnly.FromDateTime(localEnd) <= Closes;
    }

    public bool Equals(WorkingHours? other) =>
        other is not null && other.Opens == Opens && other.Closes == Closes && other._days.SetEquals(_days);

    public override bool Equals(object? obj) => Equals(obj as WorkingHours);

    public override int GetHashCode()
    {
        int days = 0;
        foreach (DayOfWeek day in _days)
        {
            days |= 1 << (int)day;
        }

        return HashCode.Combine(Opens, Closes, days);
    }
}
