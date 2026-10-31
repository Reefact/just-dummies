namespace RailwayInterlocking.Domain;

/// <summary>A track circuit: detects occupancy and can be locked into at most one route.</summary>
public sealed class TrackSection
{
    public const double MinimumLengthMetres = 10;
    public const double MaximumLengthMetres = 5_000;

    public TrackSection(SectionId id, double lengthMetres, double lineSpeedKmh)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (double.IsNaN(lengthMetres) || lengthMetres < MinimumLengthMetres || lengthMetres > MaximumLengthMetres)
        {
            throw new ArgumentOutOfRangeException(nameof(lengthMetres), lengthMetres, $"Length must be between {MinimumLengthMetres} and {MaximumLengthMetres} m.");
        }

        if (double.IsNaN(lineSpeedKmh) || lineSpeedKmh <= 0 || lineSpeedKmh > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(lineSpeedKmh), lineSpeedKmh, "Line speed must be in (0, 300] km/h.");
        }

        Id = id;
        LengthMetres = lengthMetres;
        LineSpeedKmh = lineSpeedKmh;
    }

    public SectionId Id { get; }

    public double LengthMetres { get; }

    public double LineSpeedKmh { get; }

    public bool IsOccupied { get; private set; }

    /// <summary>The route this section is locked into, or null when free.</summary>
    public string? LockedByRoute { get; private set; }

    public bool IsLocked => LockedByRoute is not null;

    /// <summary>Minimum time for a train at line speed to clear the section, in seconds.</summary>
    public double ClearanceTimeSeconds => LengthMetres / (LineSpeedKmh / 3.6);

    internal void MarkOccupied() => IsOccupied = true;

    internal void MarkClear() => IsOccupied = false;

    internal void LockInto(string routeName) => LockedByRoute = routeName;

    internal void Unlock() => LockedByRoute = null;
}
