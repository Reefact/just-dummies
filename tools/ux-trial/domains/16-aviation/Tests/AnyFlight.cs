// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Flight --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Flight" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFlight : IAny<Flight> {

    private readonly IAny<FlightNumber> _number;
    private readonly IAny<Route>        _route;
    private readonly IAny<Aircraft>     _aircraft;
    private readonly IAny<DateTime>     _scheduledDeparture;
    private readonly IAny<TimeSpan>     _blockTime;

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
        return Any.DateTime();
    }

    private static AnyTimeSpan AnyValidBlockTime() {
        // Positive and at most twenty hours.
        return Any.TimeSpan().Positive().LessThanOrEqualTo(TimeSpan.FromHours(20));
    }

    private AnyFlight(IAny<FlightNumber> number,
                      IAny<Route>        route,
                      IAny<Aircraft>     aircraft,
                      IAny<DateTime>     scheduledDeparture,
                      IAny<TimeSpan>     blockTime) {
        _number             = number;
        _route              = route;
        _aircraft           = aircraft;
        _scheduledDeparture = scheduledDeparture;
        _blockTime          = blockTime;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyFlight WithNumber(FlightNumber value) {
        return new AnyFlight(new FixedValue<FlightNumber>(value), _route, _aircraft, _scheduledDeparture, _blockTime);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFlight WithNumber(Func<AnyFlightNumber, AnyFlightNumber> configure) {
        return new AnyFlight(configure(AnyValidNumber()), _route, _aircraft, _scheduledDeparture, _blockTime);
    }

    /// <summary>Pins <c>route</c> to a fixed value.</summary>
    public AnyFlight WithRoute(Route value) {
        return new AnyFlight(_number, new FixedValue<Route>(value), _aircraft, _scheduledDeparture, _blockTime);
    }

    /// <summary>Draws <c>route</c> from <c>AnyValidRoute()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFlight WithRoute(Func<AnyRoute, AnyRoute> configure) {
        return new AnyFlight(_number, configure(AnyValidRoute()), _aircraft, _scheduledDeparture, _blockTime);
    }

    /// <summary>Pins <c>aircraft</c> to a fixed value.</summary>
    public AnyFlight WithAircraft(Aircraft value) {
        return new AnyFlight(_number, _route, new FixedValue<Aircraft>(value), _scheduledDeparture, _blockTime);
    }

    /// <summary>Draws <c>aircraft</c> from <c>AnyValidAircraft()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFlight WithAircraft(Func<AnyAircraft, AnyAircraft> configure) {
        return new AnyFlight(_number, _route, configure(AnyValidAircraft()), _scheduledDeparture, _blockTime);
    }

    /// <summary>Pins <c>scheduledDeparture</c> to a fixed value.</summary>
    public AnyFlight WithScheduledDeparture(DateTime value) {
        return new AnyFlight(_number, _route, _aircraft, new FixedValue<DateTime>(value), _blockTime);
    }

    /// <summary>Draws <c>scheduledDeparture</c> from <c>AnyValidScheduledDeparture()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFlight WithScheduledDeparture(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyFlight(_number, _route, _aircraft, configure(AnyValidScheduledDeparture()), _blockTime);
    }

    /// <summary>Pins <c>blockTime</c> to a fixed value.</summary>
    public AnyFlight WithBlockTime(TimeSpan value) {
        return new AnyFlight(_number, _route, _aircraft, _scheduledDeparture, new FixedValue<TimeSpan>(value));
    }

    /// <summary>Draws <c>blockTime</c> from <c>AnyValidBlockTime()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFlight WithBlockTime(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyFlight(_number, _route, _aircraft, _scheduledDeparture, configure(AnyValidBlockTime()));
    }

    /// <summary>Produces one arbitrary <see cref="Flight" />.</summary>
    public Flight Generate() {
        return new Flight(_number.Generate(),
                          _route.Generate(),
                          _aircraft.Generate(),
                          _scheduledDeparture.Generate(),
                          _blockTime.Generate());
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
