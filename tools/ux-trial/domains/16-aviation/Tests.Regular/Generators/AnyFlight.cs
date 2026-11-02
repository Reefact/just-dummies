// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Flight --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Flight" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFlight : IAny<Flight> {

    private IAny<FlightNumber> _number;
    private IAny<Route>        _route;
    private IAny<Aircraft>     _aircraft;
    private IAny<DateTime>     _scheduledDeparture;
    private IAny<TimeSpan>     _blockTime;

    private AnyFlightNumber _numberRecipe;
    private AnyRoute        _routeRecipe;
    private AnyAircraft     _aircraftRecipe;
    private AnyDateTime     _scheduledDepartureRecipe;
    private AnyTimeSpan     _blockTimeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFlight()
        : this(number:             AnyValidNumber(),
               route:              AnyValidRoute(),
               aircraft:           AnyValidAircraft(),
               scheduledDeparture: AnyValidScheduledDeparture(),
               blockTime:          AnyValidBlockTime()) { }

    private static AnyFlightNumber AnyValidNumber() {
        return new AnyFlightNumber();
    }

    private static AnyRoute AnyValidRoute() {
        return new AnyRoute();
    }

    private static AnyAircraft AnyValidAircraft() {
        return new AnyAircraft();
    }

    private static AnyDateTime AnyValidScheduledDeparture() {
        return Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2100, 1, 1));
    }

    private static AnyTimeSpan AnyValidBlockTime() {
        return Any.TimeSpan().Positive().LessThanOrEqualTo(TimeSpan.FromHours(20));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyFlight(AnyFlightNumber number,
                      AnyRoute        route,
                      AnyAircraft     aircraft,
                      AnyDateTime     scheduledDeparture,
                      AnyTimeSpan     blockTime) {
        _number                   = number;
        _numberRecipe             = number;
        _route                    = route;
        _routeRecipe              = route;
        _aircraft                 = aircraft;
        _aircraftRecipe           = aircraft;
        _scheduledDeparture       = scheduledDeparture;
        _scheduledDepartureRecipe = scheduledDeparture;
        _blockTime                = blockTime;
        _blockTimeRecipe          = blockTime;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyFlight(AnyFlight from) {
        _number                   = from._number;
        _numberRecipe             = from._numberRecipe;
        _route                    = from._route;
        _routeRecipe              = from._routeRecipe;
        _aircraft                 = from._aircraft;
        _aircraftRecipe           = from._aircraftRecipe;
        _scheduledDeparture       = from._scheduledDeparture;
        _scheduledDepartureRecipe = from._scheduledDepartureRecipe;
        _blockTime                = from._blockTime;
        _blockTimeRecipe          = from._blockTimeRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlight WithNumber(FlightNumber value) {
        AnyFlight copy = new AnyFlight(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyFlightNumber, AnyFlightNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlight WithNumber(Func<AnyFlightNumber, AnyFlightNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlight copy = new AnyFlight(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>route</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlight WithRoute(Route value) {
        AnyFlight copy = new AnyFlight(this);

        copy._route = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>route</c> is drawn from — <c>AnyValidRoute()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRoute, AnyRoute&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlight WithRoute(Func<AnyRoute, AnyRoute> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlight copy = new AnyFlight(this);

        copy._routeRecipe = configure(_routeRecipe);
        copy._route       = copy._routeRecipe;

        return copy;
    }

    /// <summary>Pins <c>aircraft</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlight WithAircraft(Aircraft value) {
        AnyFlight copy = new AnyFlight(this);

        copy._aircraft = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>aircraft</c> is drawn from — <c>AnyValidAircraft()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAircraft, AnyAircraft&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlight WithAircraft(Func<AnyAircraft, AnyAircraft> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlight copy = new AnyFlight(this);

        copy._aircraftRecipe = configure(_aircraftRecipe);
        copy._aircraft       = copy._aircraftRecipe;

        return copy;
    }

    /// <summary>Pins <c>scheduledDeparture</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlight WithScheduledDeparture(DateTime value) {
        AnyFlight copy = new AnyFlight(this);

        copy._scheduledDeparture = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>scheduledDeparture</c> is drawn from — <c>AnyValidScheduledDeparture()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTime, AnyDateTime&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlight WithScheduledDeparture(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlight copy = new AnyFlight(this);

        copy._scheduledDepartureRecipe = configure(_scheduledDepartureRecipe);
        copy._scheduledDeparture       = copy._scheduledDepartureRecipe;

        return copy;
    }

    /// <summary>Pins <c>blockTime</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlight WithBlockTime(TimeSpan value) {
        AnyFlight copy = new AnyFlight(this);

        copy._blockTime = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>blockTime</c> is drawn from — <c>AnyValidBlockTime()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlight WithBlockTime(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlight copy = new AnyFlight(this);

        copy._blockTimeRecipe = configure(_blockTimeRecipe);
        copy._blockTime       = copy._blockTimeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Flight" />.</summary>
    public Flight Generate() {
        return new Flight(_number.Generate(),
                          _route.Generate(),
                          _aircraft.Generate(),
                          _scheduledDeparture.Generate(),
                          _blockTime.Generate());
    }

}
