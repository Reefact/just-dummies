// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Geofence --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Geofence" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGeofence : IAny<Geofence> {

    private readonly IAny<GeoCoordinate> _southWest;
    private readonly IAny<GeoCoordinate> _northEast;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeofence()
        : this(southWest: AnyValidSouthWest(),
               northEast: AnyValidNorthEast()) { }

    private static AnyGeoCoordinate AnyValidSouthWest() {
        // Cross-parameter invariant (south-west lies south and west of north-east): two disjoint quadrants.
        return new AnyGeoCoordinate().WithLatitude(latitude => latitude.LessThanOrEqualTo(0d))
                                     .WithLongitude(longitude => longitude.LessThan(0d));
    }

    private static AnyGeoCoordinate AnyValidNorthEast() {
        return new AnyGeoCoordinate().WithLatitude(latitude => latitude.GreaterThanOrEqualTo(0d))
                                     .WithLongitude(longitude => longitude.GreaterThanOrEqualTo(0d));
    }

    private AnyGeofence(IAny<GeoCoordinate> southWest,
                        IAny<GeoCoordinate> northEast) {
        _southWest = southWest;
        _northEast = northEast;
    }

    /// <summary>Pins <c>southWest</c> to a fixed value.</summary>
    public AnyGeofence WithSouthWest(GeoCoordinate value) {
        return new AnyGeofence(new FixedValue<GeoCoordinate>(value), _northEast);
    }

    /// <summary>Draws <c>southWest</c> from <c>AnyValidSouthWest()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeofence WithSouthWest(Func<AnyGeoCoordinate, AnyGeoCoordinate> configure) {
        return new AnyGeofence(configure(AnyValidSouthWest()), _northEast);
    }

    /// <summary>Pins <c>northEast</c> to a fixed value.</summary>
    public AnyGeofence WithNorthEast(GeoCoordinate value) {
        return new AnyGeofence(_southWest, new FixedValue<GeoCoordinate>(value));
    }

    /// <summary>Draws <c>northEast</c> from <c>AnyValidNorthEast()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeofence WithNorthEast(Func<AnyGeoCoordinate, AnyGeoCoordinate> configure) {
        return new AnyGeofence(_southWest, configure(AnyValidNorthEast()));
    }

    /// <summary>Produces one arbitrary <see cref="Geofence" />.</summary>
    public Geofence Generate() {
        return new Geofence(_southWest.Generate(),
                            _northEast.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
