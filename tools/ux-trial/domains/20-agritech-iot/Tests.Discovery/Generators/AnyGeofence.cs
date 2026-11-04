using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Geofence" /> values. The two corners are tied by an order on each
///     axis, so they are drawn together rather than one parameter at a time.
/// </summary>
public sealed partial class AnyGeofence : IAny<Geofence> {

    private static AnyDouble AnyValidLatitude() {
        return Any.Double().Between(-90d, 90d);
    }

    private static AnyDouble AnyValidLongitude() {
        return Any.Double().GreaterThanOrEqualTo(-180d).LessThan(180d);
    }

    /// <summary>Produces one arbitrary <see cref="Geofence" />.</summary>
    public Geofence Generate() {
        (double southLatitude, double northLatitude) = Any.Pair(AnyValidLatitude(), AnyValidLatitude()).Having.Item1LessThanOrEqualToItem2().Generate();
        (double westLongitude, double eastLongitude) = Any.Pair(AnyValidLongitude(), AnyValidLongitude()).Having.Item1LessThanOrEqualToItem2().Generate();

        return new Geofence(new GeoCoordinate(southLatitude, westLongitude),
                            new GeoCoordinate(northLatitude, eastLongitude));
    }

}
