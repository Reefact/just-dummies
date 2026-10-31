// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GeoPoint --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GeoPoint" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGeoPoint : IAny<GeoPoint> {

    private readonly IAny<double> _latitude;
    private readonly IAny<double> _longitude;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeoPoint()
        : this(latitude:  AnyValidLatitude(),
               longitude: AnyValidLongitude()) { }

    private static AnyDouble AnyValidLatitude() {
        return Any.Double().Between(-90.0, 90.0);
    }

    private static AnyDouble AnyValidLongitude() {
        return Any.Double().Between(-180.0, 180.0);
    }

    private AnyGeoPoint(IAny<double> latitude,
                        IAny<double> longitude) {
        _latitude  = latitude;
        _longitude = longitude;
    }

    /// <summary>Pins <c>latitude</c> to a fixed value.</summary>
    public AnyGeoPoint WithLatitude(double value) {
        return new AnyGeoPoint(new FixedValue<double>(value), _longitude);
    }

    /// <summary>Draws <c>latitude</c> from <c>AnyValidLatitude()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeoPoint WithLatitude(Func<AnyDouble, AnyDouble> configure) {
        return new AnyGeoPoint(configure(AnyValidLatitude()), _longitude);
    }

    /// <summary>Pins <c>longitude</c> to a fixed value.</summary>
    public AnyGeoPoint WithLongitude(double value) {
        return new AnyGeoPoint(_latitude, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>longitude</c> from <c>AnyValidLongitude()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeoPoint WithLongitude(Func<AnyDouble, AnyDouble> configure) {
        return new AnyGeoPoint(_latitude, configure(AnyValidLongitude()));
    }

    /// <summary>Produces one arbitrary <see cref="GeoPoint" />.</summary>
    public GeoPoint Generate() {
        return new GeoPoint(_latitude.Generate(),
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
