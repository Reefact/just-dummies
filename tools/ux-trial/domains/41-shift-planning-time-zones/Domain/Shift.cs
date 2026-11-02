namespace ShiftPlanning.Domain;

/// <summary>
/// A planned shift, stored as UTC instants. Its elapsed duration differs from its wall-clock length when it
/// crosses a daylight-saving transition: an 8-hour night shift lasts 7 h across spring-forward and 9 h across fall-back.
/// </summary>
public sealed class Shift
{
    public Shift(int number, Site site, ShiftRequest request)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(request);
        if (!site.Id.Equals(request.Site))
        {
            throw new ArgumentException($"The request is for site {request.Site}, not {site.Id}.", nameof(site));
        }

        DateTimeOffset start = site.Zone.ToUtc(request.StartDate, request.StartTime, request.StartChoice);
        DateTimeOffset end = site.Zone.ToUtc(request.EndDate, request.EndTime, request.EndChoice);
        if (end <= start)
        {
            throw new ArgumentException("With the chosen offsets the shift would end before it starts.", nameof(request));
        }

        Number = number;
        Site = site;
        Request = request;
        StartUtc = start;
        EndUtc = end;
    }

    public int Number { get; }

    public Site Site { get; }

    public ShiftRequest Request { get; }

    public DateTimeOffset StartUtc { get; }

    public DateTimeOffset EndUtc { get; }

    /// <summary>The start on the site's wall clock, with the offset then in force.</summary>
    public DateTimeOffset LocalStart => Site.Zone.ToLocal(StartUtc);

    public DateTimeOffset LocalEnd => Site.Zone.ToLocal(EndUtc);

    /// <summary>Time actually worked, the measure every rest and weekly rule uses.</summary>
    public TimeSpan ElapsedDuration => EndUtc - StartUtc;

    public TimeSpan WallClockLength => Request.WallClockLength;

    /// <summary>Elapsed minus wall-clock: negative across spring-forward, positive across fall-back, zero otherwise.</summary>
    public TimeSpan DaylightSavingAdjustment => ElapsedDuration - WallClockLength;

    public bool CrossesTransition => DaylightSavingAdjustment != TimeSpan.Zero;

    public EmployeeId? AssignedTo { get; private set; }

    public bool IsStaffed => AssignedTo is not null;

    public bool Overlaps(Shift other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartUtc < other.EndUtc && other.StartUtc < EndUtc;
    }

    /// <summary>Elapsed time between the end of the earlier shift and the start of the later; zero when they overlap.</summary>
    public TimeSpan RestBetween(Shift other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Overlaps(other))
        {
            return TimeSpan.Zero;
        }

        return other.StartUtc >= EndUtc ? other.StartUtc - EndUtc : StartUtc - other.EndUtc;
    }

    internal void AssignTo(EmployeeId employee) => AssignedTo = employee;

    internal void Unassign() => AssignedTo = null;

    public override string ToString() => $"#{Number} {Request} ({ElapsedDuration.TotalHours:0.##} h elapsed)";
}
