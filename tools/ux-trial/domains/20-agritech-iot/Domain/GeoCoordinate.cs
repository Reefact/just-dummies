namespace AgriTech.Domain;

/// <summary>A WGS-84 position. Latitude is closed on both ends; longitude is half-open, [-180, 180).</summary>
public sealed class GeoCoordinate : IEquatable<GeoCoordinate> {

    public GeoCoordinate(double latitude, double longitude) {
        ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90.0);
        if (double.IsNaN(longitude)) {
            throw new ArgumentException("Longitude must be a number.", nameof(longitude));
        }
        if (longitude < -180.0) {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude is at least -180.");
        }
        if (longitude >= 180.0) {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude is below 180; 180 is written -180.");
        }
        Latitude  = latitude;
        Longitude = longitude;
    }

    public double Latitude  { get; }
    public double Longitude { get; }

    public bool Equals(GeoCoordinate? other) => other is not null && Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);

    public override bool Equals(object? obj) => Equals(obj as GeoCoordinate);

    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);

    public override string ToString() => $"({Latitude}, {Longitude})";

}
