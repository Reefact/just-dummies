namespace RailwayInterlocking.Domain;

/// <summary>A set of points (a switch), lying over the track section it sits in.</summary>
public sealed class Point
{
    public Point(string name, SectionId section, PointPosition initialPosition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(section);
        if (!Enum.IsDefined(initialPosition))
        {
            throw new ArgumentOutOfRangeException(nameof(initialPosition), initialPosition, "Unknown point position.");
        }

        Name = name;
        Section = section;
        Position = initialPosition;
    }

    public string Name { get; }

    public SectionId Section { get; }

    public PointPosition Position { get; private set; }

    /// <summary>The route holding this point locked, or null when free to move.</summary>
    public string? LockedByRoute { get; private set; }

    public bool IsLocked => LockedByRoute is not null;

    internal void Throw(PointPosition target) => Position = target;

    internal void LockInto(string routeName) => LockedByRoute = routeName;

    internal void Unlock() => LockedByRoute = null;
}
