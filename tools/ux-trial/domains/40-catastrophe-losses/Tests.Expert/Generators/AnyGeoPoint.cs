// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GeoPoint --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using CatastropheLosses.Domain;
using JustDummies;

namespace CatastropheLosses.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="GeoPoint" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGeoPoint : IAny<GeoPoint> {

    private IAny<double> _latitude;
    private IAny<double> _longitude;

    private AnyDouble _latitudeRecipe;
    private AnyDouble _longitudeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeoPoint()
        : this(latitude:  AnyValidLatitude(),
               longitude: AnyValidLongitude()) { }

    private static AnyDouble AnyValidLatitude() {
        return Any.Double().Between(-90d, 90d);
    }

    private static AnyDouble AnyValidLongitude() {
        return Any.Double().Between(-180d, 180d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyGeoPoint(AnyDouble latitude,
                        AnyDouble longitude) {
        _latitude        = latitude;
        _latitudeRecipe  = latitude;
        _longitude       = longitude;
        _longitudeRecipe = longitude;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGeoPoint(AnyGeoPoint from) {
        _latitude        = from._latitude;
        _latitudeRecipe  = from._latitudeRecipe;
        _longitude       = from._longitude;
        _longitudeRecipe = from._longitudeRecipe;
    }

    /// <summary>Pins <c>latitude</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGeoPoint WithLatitude(double value) {
        AnyGeoPoint copy = new AnyGeoPoint(this);

        copy._latitude = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>latitude</c> is drawn from — <c>AnyValidLatitude()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeoPoint WithLatitude(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeoPoint copy = new AnyGeoPoint(this);

        copy._latitudeRecipe = configure(_latitudeRecipe);
        copy._latitude       = copy._latitudeRecipe;

        return copy;
    }

    /// <summary>Pins <c>longitude</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGeoPoint WithLongitude(double value) {
        AnyGeoPoint copy = new AnyGeoPoint(this);

        copy._longitude = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>longitude</c> is drawn from — <c>AnyValidLongitude()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeoPoint WithLongitude(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeoPoint copy = new AnyGeoPoint(this);

        copy._longitudeRecipe = configure(_longitudeRecipe);
        copy._longitude       = copy._longitudeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="GeoPoint" />.</summary>
    public GeoPoint Generate() {
        return new GeoPoint(_latitude.Generate(),
                            _longitude.Generate());
    }

}
