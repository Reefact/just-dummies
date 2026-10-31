namespace RailwayInterlocking.Domain;

/// <summary>
/// A route from an entry signal to an exit signal: the ordered sections a train traverses and the
/// point positions it requires. Defined once in the interlocking's control tables.
/// </summary>
public sealed class Route
{
    public Route(string name, string entrySignal, string exitSignal, IReadOnlyList<SectionId> sections, IReadOnlyList<PointSetting> pointSettings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(entrySignal);
        ArgumentException.ThrowIfNullOrWhiteSpace(exitSignal);
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(pointSettings);
        if (string.Equals(entrySignal, exitSignal, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Route '{name}' cannot start and end at the same signal '{entrySignal}'.", nameof(exitSignal));
        }

        if (sections.Count == 0)
        {
            throw new ArgumentException($"Route '{name}' must traverse at least one section.", nameof(sections));
        }

        if (sections.Any(s => s is null))
        {
            throw new ArgumentException($"Route '{name}' contains a null section.", nameof(sections));
        }

        if (sections.Distinct().Count() != sections.Count)
        {
            throw new ArgumentException($"Route '{name}' traverses a section twice.", nameof(sections));
        }

        if (pointSettings.Any(p => p is null))
        {
            throw new ArgumentException($"Route '{name}' contains a null point setting.", nameof(pointSettings));
        }

        if (pointSettings.Select(p => p.PointName).Distinct(StringComparer.Ordinal).Count() != pointSettings.Count)
        {
            throw new ArgumentException($"Route '{name}' sets the same point twice.", nameof(pointSettings));
        }

        Name = name;
        EntrySignal = entrySignal;
        ExitSignal = exitSignal;
        Sections = sections.ToArray();
        PointSettings = pointSettings.ToArray();
    }

    public string Name { get; }

    public string EntrySignal { get; }

    public string ExitSignal { get; }

    public IReadOnlyList<SectionId> Sections { get; }

    public IReadOnlyList<PointSetting> PointSettings { get; }

    /// <summary>
    /// Two routes conflict when they share a section or require the same point in different positions.
    /// </summary>
    public bool ConflictsWith(Route other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (ReferenceEquals(this, other) || Name == other.Name)
        {
            return false;
        }

        if (Sections.Intersect(other.Sections).Any())
        {
            return true;
        }

        return PointSettings.Any(mine => other.PointSettings.Any(theirs => theirs.PointName == mine.PointName && theirs.Required != mine.Required));
    }
}
