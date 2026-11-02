namespace ShiftPlanning.Domain;

/// <summary>
/// A shift as the planner writes it: a site, a local start date, a start and an end on the site's wall clock.
/// An end at or before the start time means the shift ends the next day. The wall-clock length is 1 to 12 hours;
/// the elapsed length is only known once the site's zone resolves both ends (see <see cref="Shift"/>).
/// Each end carries its own <see cref="OffsetChoice"/> for when it falls in a fall-back overlap.
/// </summary>
public sealed class ShiftRequest : IEquatable<ShiftRequest>
{
    public static readonly TimeSpan MinWallClockLength = TimeSpan.FromHours(1);
    public static readonly TimeSpan MaxWallClockLength = TimeSpan.FromHours(12);

    public ShiftRequest(
        SiteId site,
        DateOnly startDate,
        TimeOnly startTime,
        TimeOnly endTime,
        OffsetChoice startChoice = OffsetChoice.Unspecified,
        OffsetChoice endChoice = OffsetChoice.Unspecified)
    {
        ArgumentNullException.ThrowIfNull(site);
        RequireWholeMinutes(startTime, nameof(startTime));
        RequireWholeMinutes(endTime, nameof(endTime));
        if (!Enum.IsDefined(startChoice))
        {
            throw new ArgumentOutOfRangeException(nameof(startChoice), startChoice, "Unknown offset choice.");
        }

        if (!Enum.IsDefined(endChoice))
        {
            throw new ArgumentOutOfRangeException(nameof(endChoice), endChoice, "Unknown offset choice.");
        }

        if (endTime == startTime)
        {
            throw new ArgumentException("A shift cannot end at the time it starts.", nameof(endTime));
        }

        TimeSpan wallClock = endTime - startTime; // wraps past midnight
        if (wallClock < MinWallClockLength || wallClock > MaxWallClockLength)
        {
            throw new ArgumentException($"A shift lasts between 1 and 12 hours on the wall clock, not {wallClock:h\\:mm}.", nameof(endTime));
        }

        if (endTime < startTime && startDate == DateOnly.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(startDate), startDate, "An overnight shift cannot start on the last representable date.");
        }

        Site = site;
        StartDate = startDate;
        StartTime = startTime;
        EndTime = endTime;
        StartChoice = startChoice;
        EndChoice = endChoice;
    }

    public SiteId Site { get; }

    public DateOnly StartDate { get; }

    public TimeOnly StartTime { get; }

    public TimeOnly EndTime { get; }

    public OffsetChoice StartChoice { get; }

    public OffsetChoice EndChoice { get; }

    public bool IsOvernight => EndTime < StartTime;

    public DateOnly EndDate => IsOvernight ? StartDate.AddDays(1) : StartDate;

    /// <summary>The length the wall clock shows, ignoring any daylight-saving transition.</summary>
    public TimeSpan WallClockLength => EndTime - StartTime;

    private static void RequireWholeMinutes(TimeOnly time, string paramName)
    {
        if (time.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new ArgumentException("Shift times are whole minutes.", paramName);
        }
    }

    public bool Equals(ShiftRequest? other) =>
        other is not null
        && other.Site.Equals(Site)
        && other.StartDate == StartDate
        && other.StartTime == StartTime
        && other.EndTime == EndTime
        && other.StartChoice == StartChoice
        && other.EndChoice == EndChoice;

    public override bool Equals(object? obj) => Equals(obj as ShiftRequest);

    public override int GetHashCode() => HashCode.Combine(Site, StartDate, StartTime, EndTime, StartChoice, EndChoice);

    public override string ToString() => $"{Site} {StartDate:yyyy-MM-dd} {StartTime:HH\\:mm}-{EndTime:HH\\:mm}";
}
