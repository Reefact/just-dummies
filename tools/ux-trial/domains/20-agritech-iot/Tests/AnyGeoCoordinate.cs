// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GeoCoordinate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GeoCoordinate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGeoCoordinate : IAny<GeoCoordinate> {

    private readonly IAny<double> _latitude;
    private readonly IAny<double> _longitude;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeoCoordinate()
        : this(latitude:  AnyValidLatitude(),
               longitude: AnyValidLongitude()) { }

    private static AnyDouble AnyValidLatitude() {
        return Any.Double().Between(-90d, 90d);
    }

    private static AnyDouble AnyValidLongitude() {
        return Any.Double().GreaterThanOrEqualTo(-180d).LessThan(180d);
    }

    private AnyGeoCoordinate(IAny<double> latitude,
                             IAny<double> longitude) {
        _latitude  = latitude;
        _longitude = longitude;
    }

    /// <summary>Pins <c>latitude</c> to a fixed value.</summary>
    public AnyGeoCoordinate WithLatitude(double value) {
        return new AnyGeoCoordinate(new FixedValue<double>(value), _longitude);
    }

    /// <summary>Draws <c>latitude</c> from <c>AnyValidLatitude()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeoCoordinate WithLatitude(Func<AnyDouble, AnyDouble> configure) {
        return new AnyGeoCoordinate(configure(AnyValidLatitude()), _longitude);
    }

    /// <summary>Pins <c>longitude</c> to a fixed value.</summary>
    public AnyGeoCoordinate WithLongitude(double value) {
        return new AnyGeoCoordinate(_latitude, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>longitude</c> from <c>AnyValidLongitude()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeoCoordinate WithLongitude(Func<AnyDouble, AnyDouble> configure) {
        return new AnyGeoCoordinate(_latitude, configure(AnyValidLongitude()));
    }

    /// <summary>Produces one arbitrary <see cref="GeoCoordinate" />.</summary>
    public GeoCoordinate Generate() {
        return new GeoCoordinate(_latitude.Generate(),
                                 _longitude.Generate());
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
