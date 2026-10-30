namespace Freight.Domain;

/// <summary>One voyage between two locations; departure and arrival carry the local offset of each port.</summary>
public sealed class Leg {

    public static readonly TimeSpan MaxTransit = TimeSpan.FromDays(90);

    public Leg(UnLocode origin, UnLocode destination, DateTimeOffset departure, DateTimeOffset arrival) {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);
        if (origin.Equals(destination)) {
            throw new ArgumentException("A leg must move the container somewhere else.", nameof(destination));
        }
        if (arrival <= departure) {
            throw new ArgumentException("A leg must arrive after it departs.", nameof(arrival));
        }
        if (arrival - departure > MaxTransit) {
            throw new ArgumentException($"A leg cannot last more than {MaxTransit.TotalDays} days.", nameof(arrival));
        }

        Origin      = origin;
        Destination = destination;
        Departure   = departure;
        Arrival     = arrival;
    }

    public UnLocode Origin { get; }

    public UnLocode Destination { get; }

    public DateTimeOffset Departure { get; }

    public DateTimeOffset Arrival { get; }

    public TimeSpan Duration => Arrival - Departure;

}
