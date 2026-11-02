// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Geofence --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using AgriTech.Domain;
using JustDummies;

namespace AgriTech.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Geofence" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGeofence : IAny<Geofence> {

    private IAny<GeoCoordinate> _southWest;
    private IAny<GeoCoordinate> _northEast;

    private AnyGeoCoordinate _southWestRecipe;
    private AnyGeoCoordinate _northEastRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeofence()
        : this(southWest: AnyValidSouthWest(),
               northEast: AnyValidNorthEast()) { }

    private static AnyGeoCoordinate AnyValidSouthWest() {
        return new AnyGeoCoordinate();
    }

    private static AnyGeoCoordinate AnyValidNorthEast() {
        return new AnyGeoCoordinate();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyGeofence(AnyGeoCoordinate southWest,
                        AnyGeoCoordinate northEast) {
        _southWest       = southWest;
        _southWestRecipe = southWest;
        _northEast       = northEast;
        _northEastRecipe = northEast;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGeofence(AnyGeofence from) {
        _southWest       = from._southWest;
        _southWestRecipe = from._southWestRecipe;
        _northEast       = from._northEast;
        _northEastRecipe = from._northEastRecipe;
    }

    /// <summary>Pins <c>southWest</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGeofence WithSouthWest(GeoCoordinate value) {
        AnyGeofence copy = new AnyGeofence(this);

        copy._southWest = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>southWest</c> is drawn from — <c>AnyValidSouthWest()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGeoCoordinate, AnyGeoCoordinate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeofence WithSouthWest(Func<AnyGeoCoordinate, AnyGeoCoordinate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeofence copy = new AnyGeofence(this);

        copy._southWestRecipe = configure(_southWestRecipe);
        copy._southWest       = copy._southWestRecipe;

        return copy;
    }

    /// <summary>Refines the recipe <c>northEast</c> is drawn from — <c>AnyValidNorthEast()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGeoCoordinate, AnyGeoCoordinate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeofence WithNorthEast(Func<AnyGeoCoordinate, AnyGeoCoordinate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeofence copy = new AnyGeofence(this);

        copy._northEastRecipe = configure(_northEastRecipe);
        copy._northEast       = copy._northEastRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Geofence" />.</summary>
    public Geofence Generate() {
        // The tie (SouthWest never north or east of NorthEast) is written here: NorthEast is drawn from its
        // recipe, kept at or above the south-west corner on both axes. For that reason there is no pin
        // for NorthEast: a test that fixes both corners constructs the Geofence itself.
        GeoCoordinate southWest = _southWest.Generate();
        GeoCoordinate northEast = _northEastRecipe
                                 .WithLatitude(latitude => latitude.GreaterThanOrEqualTo(southWest.Latitude))
                                 .WithLongitude(longitude => longitude.GreaterThanOrEqualTo(southWest.Longitude))
                                 .Generate();

        return new Geofence(southWest, northEast);
    }

}
