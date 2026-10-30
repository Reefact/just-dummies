namespace Aviation.Domain;

/// <summary>An airport slot: the right to use the runway during a window starting on a five-minute boundary.</summary>
public sealed class Slot {

    public Slot(IcaoAirportCode airport, DateTime start, TimeSpan duration) {
        ArgumentNullException.ThrowIfNull(airport);
        if (start.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("A slot is coordinated in UTC.", nameof(start));
        }

        if (start.Second != 0 || start.Millisecond != 0 || start.Minute % 5 != 0) {
            throw new ArgumentException("A slot starts on a five-minute boundary.", nameof(start));
        }

        if (duration < TimeSpan.FromMinutes(5) || duration > TimeSpan.FromHours(1)) {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "A slot lasts from five minutes to one hour.");
        }

        Airport  = airport;
        Start    = start;
        Duration = duration;
    }

    public IcaoAirportCode Airport { get; }

    public DateTime Start { get; }

    public TimeSpan Duration { get; }

    public DateTime End => Start + Duration;

    public bool Overlaps(Slot other) {
        ArgumentNullException.ThrowIfNull(other);
        return Airport.Value == other.Airport.Value && Start < other.End && other.Start < End;
    }

    /// <summary>Whether a departure at <paramref name="actualDeparture"/> respects the slot, with the usual five-minute tolerance either side.</summary>
    public bool IsRespectedBy(DateTime actualDeparture) {
        TimeSpan tolerance = TimeSpan.FromMinutes(5);
        return actualDeparture >= Start - tolerance && actualDeparture <= End + tolerance;
    }

}
