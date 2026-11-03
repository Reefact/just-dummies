// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Leg --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Leg" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLeg : IAny<Leg> {

    private IAny<UnLocode>                 _origin;
    private IAny<UnLocode>                 _destination;

    private AnyUnLocode       _originRecipe;
    private AnyUnLocode       _destinationRecipe;
    private AnyDateTimeOffset _departureRecipe;
    private AnyDateTimeOffset _arrivalRecipe;

    private DateTimeOffset? _departurePin;
    private DateTimeOffset? _arrivalPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLeg()
        : this(origin:      AnyValidOrigin(),
               destination: AnyValidDestination(),
               departure:   AnyValidDeparture(),
               arrival:     AnyValidArrival()) { }

    private static AnyUnLocode AnyValidOrigin() {
        return new AnyUnLocode();
    }

    private static AnyUnLocode AnyValidDestination() {
        return new AnyUnLocode();
    }

    private static AnyDateTimeOffset AnyValidDeparture() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 12, 31, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyDateTimeOffset AnyValidArrival() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 12, 31, 0, 0, 0, TimeSpan.Zero));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLeg(AnyUnLocode       origin,
                   AnyUnLocode       destination,
                   AnyDateTimeOffset departure,
                   AnyDateTimeOffset arrival) {
        _origin              = origin;
        _originRecipe        = origin;
        _destination         = destination;
        _destinationRecipe   = destination;
        _departureRecipe     = departure;
        _arrivalRecipe       = arrival;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLeg(AnyLeg from) {
        _origin              = from._origin;
        _originRecipe        = from._originRecipe;
        _destination         = from._destination;
        _destinationRecipe   = from._destinationRecipe;
        _departureRecipe     = from._departureRecipe;
        _departurePin        = from._departurePin;
        _arrivalRecipe       = from._arrivalRecipe;
        _arrivalPin          = from._arrivalPin;
    }

    /// <summary>Pins <c>origin</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLeg WithOrigin(UnLocode value) {
        AnyLeg copy = new AnyLeg(this);

        copy._origin = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>origin</c> is drawn from — <c>AnyValidOrigin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUnLocode, AnyUnLocode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLeg WithOrigin(Func<AnyUnLocode, AnyUnLocode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._originRecipe = configure(_originRecipe);
        copy._origin       = copy._originRecipe;

        return copy;
    }

    /// <summary>Pins <c>destination</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLeg WithDestination(UnLocode value) {
        AnyLeg copy = new AnyLeg(this);

        copy._destination = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>destination</c> is drawn from — <c>AnyValidDestination()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUnLocode, AnyUnLocode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLeg WithDestination(Func<AnyUnLocode, AnyUnLocode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._destinationRecipe = configure(_destinationRecipe);
        copy._destination       = copy._destinationRecipe;

        return copy;
    }

    /// <summary>Pins <c>departure</c> to a fixed value, which bounds <c>arrival</c>; its recipe is kept for a later refinement.</summary>
    public AnyLeg WithDeparture(DateTimeOffset value) {
        AnyLeg copy = new AnyLeg(this);

        copy._departurePin        = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>departure</c> is drawn from — <c>AnyValidDeparture()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>arrival</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLeg WithDeparture(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._departureRecipe     = configure(_departureRecipe);
        copy._departurePin        = null;

        return copy;
    }

    /// <summary>Pins <c>arrival</c> to a fixed value, which bounds <c>departure</c>; its recipe is kept for a later refinement.</summary>
    public AnyLeg WithArrival(DateTimeOffset value) {
        AnyLeg copy = new AnyLeg(this);

        copy._arrivalPin          = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>arrival</c> is drawn from — <c>AnyValidArrival()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>departure</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLeg WithArrival(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._arrivalRecipe       = configure(_arrivalRecipe);
        copy._arrivalPin          = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Leg" />.</summary>
    public Leg Generate() {
        // Two ties live in the constructor: the destination is not the origin, and the arrival follows the
        // departure by at most 90 days. Whichever side is not pinned is derived from the other.
        UnLocode origin      = _origin.Generate();
        UnLocode destination = _destination.DifferentFrom(origin).Generate();
        TimeSpan transit     = Any.TimeSpan().Between(TimeSpan.FromHours(1), Leg.MaxTransit).WithGranularity(TimeSpan.FromMinutes(1)).Generate();

        DateTimeOffset departure;
        DateTimeOffset arrival;
        if (_arrivalPin is { } pinnedArrival && _departurePin is null) {
            arrival   = pinnedArrival;
            departure = pinnedArrival - transit;
        } else {
            departure = _departurePin ?? _departureRecipe.Generate();
            arrival   = _arrivalPin ?? departure + transit;
        }

        return new Leg(origin, destination, departure, arrival);
    }

}
