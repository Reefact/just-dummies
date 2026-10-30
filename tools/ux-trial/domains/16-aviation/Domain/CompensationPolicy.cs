namespace Aviation.Domain;

/// <summary>An EU261-style compensation scale: owed for an arrival at least three hours late, banded by distance.</summary>
public static class CompensationPolicy {

    public static decimal CompensationFor(Flight flight) {
        ArgumentNullException.ThrowIfNull(flight);
        if (flight.Status == FlightStatus.Cancelled || flight.TotalDelay < TimeSpan.FromHours(3)) {
            return flight.Status == FlightStatus.Cancelled ? BandFor(flight.Route.DistanceKm) : 0m;
        }

        return BandFor(flight.Route.DistanceKm);
    }

    private static decimal BandFor(int distanceKm) => distanceKm switch {
        <= 1_500 => 250m,
        <= 3_500 => 400m,
        _        => 600m
    };

}
