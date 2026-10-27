namespace AgriTech.Domain;

/// <summary>A rectangular field boundary that does not cross the antimeridian.</summary>
public sealed class Geofence {

    public Geofence(GeoCoordinate southWest, GeoCoordinate northEast) {
        ArgumentNullException.ThrowIfNull(southWest);
        ArgumentNullException.ThrowIfNull(northEast);
        if (southWest.Latitude > northEast.Latitude) {
            throw new ArgumentException("The south-west corner must lie south of the north-east corner.", nameof(southWest));
        }
        if (southWest.Longitude > northEast.Longitude) {
            throw new ArgumentException("A geofence crossing the antimeridian is not supported.", nameof(southWest));
        }
        SouthWest = southWest;
        NorthEast = northEast;
    }

    public GeoCoordinate SouthWest { get; }
    public GeoCoordinate NorthEast { get; }

    public bool Contains(GeoCoordinate point) =>
        point.Latitude  >= SouthWest.Latitude  && point.Latitude  <= NorthEast.Latitude &&
        point.Longitude >= SouthWest.Longitude && point.Longitude <= NorthEast.Longitude;

}
