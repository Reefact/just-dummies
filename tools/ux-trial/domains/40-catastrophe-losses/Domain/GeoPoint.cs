namespace CatastropheLosses.Domain;

/// <summary>A location on the Earth, in decimal degrees (WGS84).</summary>
public sealed class GeoPoint : IEquatable<GeoPoint>
{
    private const double EarthRadiusKm = 6371.0;

    public GeoPoint(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude < -90.0 || latitude > 90.0)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must lie within [-90, 90] degrees.");
        }

        if (double.IsNaN(longitude) || longitude < -180.0 || longitude > 180.0)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must lie within [-180, 180] degrees.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    /// <summary>Great-circle distance in kilometres (haversine formula).</summary>
    public double DistanceKmTo(GeoPoint other)
    {
        ArgumentNullException.ThrowIfNull(other);
        double phi1 = ToRadians(Latitude);
        double phi2 = ToRadians(other.Latitude);
        double deltaPhi = ToRadians(other.Latitude - Latitude);
        double deltaLambda = ToRadians(other.Longitude - Longitude);
        double a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2)
                   + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    public bool Equals(GeoPoint? other) =>
        other is not null && Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);

    public override bool Equals(object? obj) => Equals(obj as GeoPoint);

    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);

    public override string ToString() => $"({Latitude:0.####}, {Longitude:0.####})";

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
