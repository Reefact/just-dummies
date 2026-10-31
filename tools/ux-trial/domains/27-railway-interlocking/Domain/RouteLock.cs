namespace RailwayInterlocking.Domain;

/// <summary>
/// A route set and locked for one train. Sections release one by one behind the train
/// (sequential release); the lock ends when its last section is released.
/// </summary>
public sealed class RouteLock
{
    private readonly List<SectionId> _remaining;
    private readonly HashSet<SectionId> _visited = new();

    public RouteLock(Route route, TrainId train, DateTimeOffset lockedAt)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(train);
        if (lockedAt == default)
        {
            throw new ArgumentException("Lock time must be set.", nameof(lockedAt));
        }

        Route = route;
        Train = train;
        LockedAt = lockedAt;
        _remaining = route.Sections.ToList();
    }

    public Route Route { get; }

    public TrainId Train { get; }

    public DateTimeOffset LockedAt { get; }

    /// <summary>True once the train has entered the first section: the route can no longer be cancelled.</summary>
    public bool TrainHasEntered { get; private set; }

    /// <summary>Sections still locked, in running order.</summary>
    public IReadOnlyList<SectionId> RemainingSections => _remaining;

    public bool IsFullyReleased => _remaining.Count == 0;

    /// <summary>True when the train has been detected in this section since the route was set.</summary>
    public bool HasVisited(SectionId section) => _visited.Contains(section);

    internal void MarkVisited(SectionId section)
    {
        _visited.Add(section);
        TrainHasEntered = true;
    }

    /// <summary>Releases the rear-most locked section; only that one may be released.</summary>
    internal void ReleaseRear(SectionId section)
    {
        if (_remaining.Count == 0 || !_remaining[0].Equals(section))
        {
            throw new InterlockingRefusedException(RefusalReason.OutOfSequenceRelease, $"Section {section} is not the rear of route {Route.Name}.");
        }

        _remaining.RemoveAt(0);
    }
}
