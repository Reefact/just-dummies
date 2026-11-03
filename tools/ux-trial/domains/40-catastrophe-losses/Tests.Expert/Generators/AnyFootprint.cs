// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Footprint --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using CatastropheLosses.Domain;
using JustDummies;

namespace CatastropheLosses.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Footprint" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFootprint : IAny<Footprint> {

    private IAny<GeoPoint> _center;
    private IAny<double>   _radiusKm;
    private IAny<double>   _peakIntensity;

    private AnyGeoPoint _centerRecipe;
    private AnyDouble   _radiusKmRecipe;
    private AnyDouble   _peakIntensityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFootprint()
        : this(center:        AnyValidCenter(),
               radiusKm:      AnyValidRadiusKm(),
               peakIntensity: AnyValidPeakIntensity()) { }

    private static AnyGeoPoint AnyValidCenter() {
        return new AnyGeoPoint();
    }

    private static AnyDouble AnyValidRadiusKm() {
        return Any.Double().Positive().LessThanOrEqualTo(Footprint.MaxRadiusKm);
    }

    private static AnyDouble AnyValidPeakIntensity() {
        return Any.Double().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyFootprint(AnyGeoPoint center,
                         AnyDouble   radiusKm,
                         AnyDouble   peakIntensity) {
        _center              = center;
        _centerRecipe        = center;
        _radiusKm            = radiusKm;
        _radiusKmRecipe      = radiusKm;
        _peakIntensity       = peakIntensity;
        _peakIntensityRecipe = peakIntensity;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyFootprint(AnyFootprint from) {
        _center              = from._center;
        _centerRecipe        = from._centerRecipe;
        _radiusKm            = from._radiusKm;
        _radiusKmRecipe      = from._radiusKmRecipe;
        _peakIntensity       = from._peakIntensity;
        _peakIntensityRecipe = from._peakIntensityRecipe;
    }

    /// <summary>Pins <c>center</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFootprint WithCenter(GeoPoint value) {
        AnyFootprint copy = new AnyFootprint(this);

        copy._center = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>center</c> is drawn from — <c>AnyValidCenter()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGeoPoint, AnyGeoPoint&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFootprint WithCenter(Func<AnyGeoPoint, AnyGeoPoint> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFootprint copy = new AnyFootprint(this);

        copy._centerRecipe = configure(_centerRecipe);
        copy._center       = copy._centerRecipe;

        return copy;
    }

    /// <summary>Pins <c>radiusKm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFootprint WithRadiusKm(double value) {
        AnyFootprint copy = new AnyFootprint(this);

        copy._radiusKm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>radiusKm</c> is drawn from — <c>AnyValidRadiusKm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFootprint WithRadiusKm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFootprint copy = new AnyFootprint(this);

        copy._radiusKmRecipe = configure(_radiusKmRecipe);
        copy._radiusKm       = copy._radiusKmRecipe;

        return copy;
    }

    /// <summary>Pins <c>peakIntensity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFootprint WithPeakIntensity(double value) {
        AnyFootprint copy = new AnyFootprint(this);

        copy._peakIntensity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>peakIntensity</c> is drawn from — <c>AnyValidPeakIntensity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFootprint WithPeakIntensity(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFootprint copy = new AnyFootprint(this);

        copy._peakIntensityRecipe = configure(_peakIntensityRecipe);
        copy._peakIntensity       = copy._peakIntensityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Footprint" />.</summary>
    public Footprint Generate() {
        return new Footprint(_center.Generate(),
                             _radiusKm.Generate(),
                             _peakIntensity.Generate());
    }

}
