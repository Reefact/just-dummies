// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Leg --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Leg" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLeg : IAny<Leg> {

    private readonly IAny<UnLocode>       _origin;
    private readonly IAny<UnLocode>       _destination;
    private readonly IAny<DateTimeOffset> _departure;
    private readonly IAny<DateTimeOffset>? _arrival;
    private readonly IAny<TimeSpan>        _transit;
    private readonly IAny<TimeSpan>        _arrivalOffset;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLeg()
        : this(origin:      AnyValidOrigin(),
               destination: AnyValidDestination(),
               departure:   AnyValidDeparture(),
               arrival:     null) { }

    private static AnyUnLocode AnyValidOrigin() {
        return new AnyUnLocode();
    }

    private static AnyUnLocode AnyValidDestination() {
        return new AnyUnLocode();
    }

    private static AnyDateTimeOffset AnyValidDeparture() {
        // A sailing departs at a port's local time: whole minutes, any real UTC offset.
        return Any.DateTimeOffset()
                  .Between(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2034, 1, 1, 0, 0, 0, TimeSpan.Zero))
                  .WithOffsetBetween(TimeSpan.FromHours(-12), TimeSpan.FromHours(14))
                  .WithGranularity(TimeSpan.FromMinutes(1));
    }

    // Hand-written: the arrival is not independent of the departure (departure < arrival <= departure + 90 days),
    // so by default it is drawn as a transit time added to the departure, then shown at the arrival port's offset.
    private static AnyTimeSpan AnyValidTransit() {
        return Any.TimeSpan().Between(TimeSpan.FromHours(1), Leg.MaxTransit).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private static AnyTimeSpan AnyValidArrivalOffset() {
        return Any.TimeSpan().Between(TimeSpan.FromHours(-12), TimeSpan.FromHours(14)).WithGranularity(TimeSpan.FromMinutes(15));
    }

    private AnyLeg(IAny<UnLocode>        origin,
                   IAny<UnLocode>        destination,
                   IAny<DateTimeOffset>  departure,
                   IAny<DateTimeOffset>? arrival,
                   IAny<TimeSpan>?       transit       = null,
                   IAny<TimeSpan>?       arrivalOffset = null) {
        _origin        = origin;
        _destination   = destination;
        _departure     = departure;
        _arrival       = arrival;
        _transit       = transit ?? AnyValidTransit();
        _arrivalOffset = arrivalOffset ?? AnyValidArrivalOffset();
    }

    /// <summary>Pins <c>origin</c> to a fixed value.</summary>
    public AnyLeg WithOrigin(UnLocode value) {
        return new AnyLeg(new FixedValue<UnLocode>(value), _destination, _departure, _arrival, _transit, _arrivalOffset);
    }

    /// <summary>Draws <c>origin</c> from <c>AnyValidOrigin()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLeg WithOrigin(Func<AnyUnLocode, AnyUnLocode> configure) {
        return new AnyLeg(configure(AnyValidOrigin()), _destination, _departure, _arrival, _transit, _arrivalOffset);
    }

    /// <summary>Pins <c>destination</c> to a fixed value.</summary>
    public AnyLeg WithDestination(UnLocode value) {
        return new AnyLeg(_origin, new FixedValue<UnLocode>(value), _departure, _arrival, _transit, _arrivalOffset);
    }

    /// <summary>Draws <c>destination</c> from <c>AnyValidDestination()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLeg WithDestination(Func<AnyUnLocode, AnyUnLocode> configure) {
        return new AnyLeg(_origin, configure(AnyValidDestination()), _departure, _arrival, _transit, _arrivalOffset);
    }

    /// <summary>Pins <c>departure</c> to a fixed value.</summary>
    public AnyLeg WithDeparture(DateTimeOffset value) {
        return new AnyLeg(_origin, _destination, new FixedValue<DateTimeOffset>(value), _arrival, _transit, _arrivalOffset);
    }

    /// <summary>Draws <c>departure</c> from <c>AnyValidDeparture()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLeg WithDeparture(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyLeg(_origin, _destination, configure(AnyValidDeparture()), _arrival, _transit, _arrivalOffset);
    }

    /// <summary>Pins <c>arrival</c> to a fixed value.</summary>
    public AnyLeg WithArrival(DateTimeOffset value) {
        return new AnyLeg(_origin, _destination, _departure, new FixedValue<DateTimeOffset>(value), _transit, _arrivalOffset);
    }

    /// <summary>Draws the transit time from <c>AnyValidTransit()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLeg WithTransit(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyLeg(_origin, _destination, _departure, null, configure(AnyValidTransit()), _arrivalOffset);
    }

    /// <summary>Pins the UTC offset the arrival is expressed in.</summary>
    public AnyLeg WithArrivalOffset(TimeSpan offset) {
        return new AnyLeg(_origin, _destination, _departure, _arrival, _transit, new FixedValue<TimeSpan>(offset));
    }

    /// <summary>Produces one arbitrary <see cref="Leg" />.</summary>
    public Leg Generate() {
        UnLocode origin      = _origin.Generate();
        UnLocode destination = _destination.Generate();
        for (int attempt = 0; attempt < 10 && destination.Equals(origin); attempt++) {
            destination = _destination.Generate();
        }
        DateTimeOffset departure = _departure.Generate();
        DateTimeOffset arrival   = _arrival?.Generate() ?? (departure + _transit.Generate()).ToOffset(_arrivalOffset.Generate());

        return new Leg(origin, destination, departure, arrival);
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
