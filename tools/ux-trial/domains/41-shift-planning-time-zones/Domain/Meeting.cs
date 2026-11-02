namespace ShiftPlanning.Domain;

/// <summary>A meeting between employees of possibly different sites, fixed as an instant and a duration.</summary>
public sealed class Meeting
{
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(4);
    public const int MaxParticipants = 12;

    private readonly List<EmployeeId> _participants;

    public Meeting(string title, DateTimeOffset start, TimeSpan duration, IEnumerable<EmployeeId> participants)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(participants);
        if (title.Length > 120)
        {
            throw new ArgumentException("A meeting title has at most 120 characters.", nameof(title));
        }

        if (duration < MinDuration || duration > MaxDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "A meeting lasts between 15 minutes and 4 hours.");
        }

        if (duration.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new ArgumentException("A meeting lasts a whole number of minutes.", nameof(duration));
        }

        if (start > DateTimeOffset.MaxValue - duration)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "The meeting would end past the last representable instant.");
        }

        List<EmployeeId> list = participants.ToList();
        if (list.Any(p => p is null))
        {
            throw new ArgumentException("A participant cannot be null.", nameof(participants));
        }

        if (list.Count < 2 || list.Count > MaxParticipants)
        {
            throw new ArgumentException($"A meeting has between 2 and {MaxParticipants} participants.", nameof(participants));
        }

        if (list.Distinct().Count() != list.Count)
        {
            throw new ArgumentException("A participant is listed once.", nameof(participants));
        }

        Title = title;
        StartUtc = start.ToUniversalTime();
        Duration = duration;
        _participants = list;
    }

    public string Title { get; }

    public DateTimeOffset StartUtc { get; }

    public TimeSpan Duration { get; }

    public DateTimeOffset EndUtc => StartUtc + Duration;

    public IReadOnlyList<EmployeeId> Participants => _participants;
}
