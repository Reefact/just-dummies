namespace Aviation.Domain;

/// <summary>A city pair flown between two airports, with its great-circle distance.</summary>
public sealed class Route {

    public Route(IataAirportCode origin, IataAirportCode destination, int distanceKm) {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);
        if (origin.Equals(destination)) {
            throw new ArgumentException("A route joins two different airports.", nameof(destination));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(distanceKm, 50);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(distanceKm, 20_000);

        Origin      = origin;
        Destination = destination;
        DistanceKm  = distanceKm;
    }

    public IataAirportCode Origin { get; }

    public IataAirportCode Destination { get; }

    public int DistanceKm { get; }

    public Route Reversed() => new Route(Destination, Origin, DistanceKm);

}
