// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Route --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using Aviation.Domain;
using JustDummies;

namespace Aviation.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Route" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoute : IAny<Route> {

    private IAny<IataAirportCode> _origin;
    private IAny<IataAirportCode> _destination;
    private IAny<int>             _distanceKm;

    private AnyIataAirportCode _originRecipe;
    private AnyIataAirportCode _destinationRecipe;
    private AnyInt32           _distanceKmRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoute()
        : this(origin:      AnyValidOrigin(),
               destination: AnyValidDestination(),
               distanceKm:  AnyValidDistanceKm()) { }

    private static AnyIataAirportCode AnyValidOrigin() {
        // Any airport; Generate() sees to it that the destination is another.
        return new AnyIataAirportCode();
    }

    private static AnyIataAirportCode AnyValidDestination() {
        // Any airport, as long as it is not the origin: Generate() sees to that.
        return new AnyIataAirportCode();
    }

    private static AnyInt32 AnyValidDistanceKm() {
        return Any.Int32().Between(50, 20000);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRoute(AnyIataAirportCode origin,
                     AnyIataAirportCode destination,
                     AnyInt32           distanceKm) {
        _origin            = origin;
        _originRecipe      = origin;
        _destination       = destination;
        _destinationRecipe = destination;
        _distanceKm        = distanceKm;
        _distanceKmRecipe  = distanceKm;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRoute(AnyRoute from) {
        _origin            = from._origin;
        _originRecipe      = from._originRecipe;
        _destination       = from._destination;
        _destinationRecipe = from._destinationRecipe;
        _distanceKm        = from._distanceKm;
        _distanceKmRecipe  = from._distanceKmRecipe;
    }

    /// <summary>Pins <c>origin</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithOrigin(IataAirportCode value) {
        AnyRoute copy = new AnyRoute(this);

        copy._origin = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>origin</c> is drawn from — <c>AnyValidOrigin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyIataAirportCode, AnyIataAirportCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoute WithOrigin(Func<AnyIataAirportCode, AnyIataAirportCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoute copy = new AnyRoute(this);

        copy._originRecipe = configure(_originRecipe);
        copy._origin       = copy._originRecipe;

        return copy;
    }

    /// <summary>Pins <c>destination</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithDestination(IataAirportCode value) {
        AnyRoute copy = new AnyRoute(this);

        copy._destination = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>destination</c> is drawn from — <c>AnyValidDestination()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyIataAirportCode, AnyIataAirportCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoute WithDestination(Func<AnyIataAirportCode, AnyIataAirportCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoute copy = new AnyRoute(this);

        copy._destinationRecipe = configure(_destinationRecipe);
        copy._destination       = copy._destinationRecipe;

        return copy;
    }

    /// <summary>Pins <c>distanceKm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithDistanceKm(int value) {
        AnyRoute copy = new AnyRoute(this);

        copy._distanceKm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>distanceKm</c> is drawn from — <c>AnyValidDistanceKm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoute WithDistanceKm(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoute copy = new AnyRoute(this);

        copy._distanceKmRecipe = configure(_distanceKmRecipe);
        copy._distanceKm       = copy._distanceKmRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Route" />.</summary>
    public Route Generate() {
        IataAirportCode origin      = _origin.Generate();
        IataAirportCode destination = _destination.Generate();

        // The tie between the two airports: redraw both on a collision, so whichever of them is not pinned moves.
        // Two pinned airports that are the same stay the same, and the constructor refuses them, as a test about that wants.
        for (int attempt = 0; attempt < 8 && origin.Equals(destination); attempt++) {
            origin      = _origin.Generate();
            destination = _destination.Generate();
        }

        return new Route(origin,
                         destination,
                         _distanceKm.Generate());
    }

}
