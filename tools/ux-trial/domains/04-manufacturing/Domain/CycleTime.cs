namespace Manufacturing.Domain;

/// <summary>The time one station takes per part: whole seconds, from one second to one hour.</summary>
public sealed class CycleTime : IEquatable<CycleTime> {

    private static readonly TimeSpan Shortest = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Longest  = TimeSpan.FromHours(1);

    public CycleTime(TimeSpan duration) {
        if (duration < Shortest) {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "A cycle lasts at least one second.");
        }
        if (duration > Longest) {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "A cycle lasts at most one hour.");
        }
        if (duration.Ticks % TimeSpan.TicksPerSecond != 0) {
            throw new ArgumentException("A cycle time is recorded in whole seconds.", nameof(duration));
        }
        Duration = duration;
    }

    public TimeSpan Duration { get; }

    /// <summary>The cadence a single station reaches at this cycle time, truncated.</summary>
    public int PartsPerHour => (int)(TimeSpan.FromHours(1).Ticks / Duration.Ticks);

    public bool Equals(CycleTime? other) => other is not null && Duration == other.Duration;
    public override bool Equals(object? obj) => Equals(obj as CycleTime);
    public override int GetHashCode() => Duration.GetHashCode();

}
