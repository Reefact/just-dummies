// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Footprint --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Footprint" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFootprint : IAny<Footprint> {

    private readonly IAny<GeoPoint> _center;
    private readonly IAny<double>   _radiusKm;
    private readonly IAny<double>   _peakIntensity;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFootprint()
        : this(center:        AnyValidCenter(),
               radiusKm:      AnyValidRadiusKm(),
               peakIntensity: AnyValidPeakIntensity()) { }

    private static AnyGeoPoint AnyValidCenter() {
        return new AnyGeoPoint();
    }

    private static AnyDouble AnyValidRadiusKm() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Footprint.MaxRadiusKm);
    }

    private static AnyDouble AnyValidPeakIntensity() {
        return Any.Double().Positive();
    }

    private AnyFootprint(IAny<GeoPoint> center,
                         IAny<double>   radiusKm,
                         IAny<double>   peakIntensity) {
        _center        = center;
        _radiusKm      = radiusKm;
        _peakIntensity = peakIntensity;
    }

    /// <summary>Pins <c>center</c> to a fixed value.</summary>
    public AnyFootprint WithCenter(GeoPoint value) {
        return new AnyFootprint(new FixedValue<GeoPoint>(value), _radiusKm, _peakIntensity);
    }

    /// <summary>Draws <c>center</c> from <c>AnyValidCenter()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFootprint WithCenter(Func<AnyGeoPoint, AnyGeoPoint> configure) {
        return new AnyFootprint(configure(AnyValidCenter()), _radiusKm, _peakIntensity);
    }

    /// <summary>Pins <c>radiusKm</c> to a fixed value.</summary>
    public AnyFootprint WithRadiusKm(double value) {
        return new AnyFootprint(_center, new FixedValue<double>(value), _peakIntensity);
    }

    /// <summary>Draws <c>radiusKm</c> from <c>AnyValidRadiusKm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFootprint WithRadiusKm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyFootprint(_center, configure(AnyValidRadiusKm()), _peakIntensity);
    }

    /// <summary>Pins <c>peakIntensity</c> to a fixed value.</summary>
    public AnyFootprint WithPeakIntensity(double value) {
        return new AnyFootprint(_center, _radiusKm, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>peakIntensity</c> from <c>AnyValidPeakIntensity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFootprint WithPeakIntensity(Func<AnyDouble, AnyDouble> configure) {
        return new AnyFootprint(_center, _radiusKm, configure(AnyValidPeakIntensity()));
    }

    /// <summary>Produces one arbitrary <see cref="Footprint" />.</summary>
    public Footprint Generate() {
        return new Footprint(_center.Generate(),
                             _radiusKm.Generate(),
                             _peakIntensity.Generate());
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
