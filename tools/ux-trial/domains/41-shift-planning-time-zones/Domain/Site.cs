namespace ShiftPlanning.Domain;

/// <summary>An operating site: where shifts are worked, in which zone, and when its offices are open.</summary>
public sealed class Site
{
    public const int MaxNameLength = 80;

    public Site(SiteId id, string name, SiteZone zone, WorkingHours workingHours)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(workingHours);
        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException($"A site name has at most {MaxNameLength} characters.", nameof(name));
        }

        Id = id;
        Name = name;
        Zone = zone;
        WorkingHours = workingHours;
    }

    public SiteId Id { get; }

    public string Name { get; }

    public SiteZone Zone { get; }

    public WorkingHours WorkingHours { get; }

    /// <summary>Whether the interval falls inside this site's working hours, read on its own wall clock.</summary>
    public bool IsWithinWorkingHours(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
        {
            throw new ArgumentException("The end cannot precede the start.", nameof(end));
        }

        return WorkingHours.Covers(Zone.ToLocal(start).DateTime, Zone.ToLocal(end).DateTime);
    }
}
