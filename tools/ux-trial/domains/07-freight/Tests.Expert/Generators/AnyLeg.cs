// Scaffolded by `any` (JustDummies), then rewritten by hand: the scaffold drew departure and arrival as an
// ordered pair, which cannot honour the 90-day ceiling on a leg. Here a leg is a departure plus a duration.

using System;

using Freight.Domain;
using JustDummies;

namespace Freight.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Leg" /> values. Origin and destination always differ; the arrival is
///     the departure plus a duration within the transit ceiling, each instant carrying the offset of its port.
/// </summary>
public sealed partial class AnyLeg : IAny<Leg> {

    private IAny<UnLocode> _origin;
    private IAny<UnLocode> _destination;

    private AnyUnLocode       _originRecipe;
    private AnyUnLocode       _destinationRecipe;
    private AnyDateTimeOffset _departureRecipe;
    private AnyTimeSpan       _durationRecipe;

    private DateTimeOffset? _departurePin;
    private DateTimeOffset? _arrivalPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLeg()
        : this(origin:      AnyValidOrigin(),
               destination: AnyValidDestination(),
               departure:   AnyValidDeparture(),
               duration:    AnyValidDuration()) { }

    private static AnyUnLocode AnyValidOrigin() {
        return new AnyUnLocode();
    }

    private static AnyUnLocode AnyValidDestination() {
        return new AnyUnLocode();
    }

    private static AnyDateTimeOffset AnyValidDeparture() {
        return Any.DateTimeOffset()
                  .Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero))
                  .WithOffsetBetween(TimeSpan.FromHours(-12), TimeSpan.FromHours(14));
    }

    private static AnyTimeSpan AnyValidDuration() {
        return Any.TimeSpan().Between(TimeSpan.FromMinutes(1), Leg.MaxTransit);
    }

    private static IAny<TimeSpan> AnyPortOffset() {
        return Any.Int32().Between(-12 * 60, 14 * 60).As(minutes => TimeSpan.FromMinutes(minutes));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLeg(AnyUnLocode       origin,
                   AnyUnLocode       destination,
                   AnyDateTimeOffset departure,
                   AnyTimeSpan       duration) {
        _origin            = origin;
        _originRecipe      = origin;
        _destination       = destination;
        _destinationRecipe = destination;
        _departureRecipe   = departure;
        _durationRecipe    = duration;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLeg(AnyLeg from) {
        _origin            = from._origin;
        _originRecipe      = from._originRecipe;
        _destination       = from._destination;
        _destinationRecipe = from._destinationRecipe;
        _departureRecipe   = from._departureRecipe;
        _durationRecipe    = from._durationRecipe;
        _departurePin      = from._departurePin;
        _arrivalPin        = from._arrivalPin;
    }

    public AnyLeg WithOrigin(UnLocode value) {
        AnyLeg copy = new AnyLeg(this);

        copy._origin = Any.Fixed(value);

        return copy;
    }

    public AnyLeg WithOrigin(Func<AnyUnLocode, AnyUnLocode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._originRecipe = configure(_originRecipe);
        copy._origin       = copy._originRecipe;

        return copy;
    }

    public AnyLeg WithDestination(UnLocode value) {
        AnyLeg copy = new AnyLeg(this);

        copy._destination = Any.Fixed(value);

        return copy;
    }

    public AnyLeg WithDestination(Func<AnyUnLocode, AnyUnLocode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._destinationRecipe = configure(_destinationRecipe);
        copy._destination       = copy._destinationRecipe;

        return copy;
    }

    public AnyLeg WithDeparture(DateTimeOffset value) {
        AnyLeg copy = new AnyLeg(this);

        copy._departurePin = value;

        return copy;
    }

    public AnyLeg WithDeparture(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._departureRecipe = configure(_departureRecipe);
        copy._departurePin    = null;

        return copy;
    }

    public AnyLeg WithArrival(DateTimeOffset value) {
        AnyLeg copy = new AnyLeg(this);

        copy._arrivalPin = value;

        return copy;
    }

    public AnyLeg WithDuration(TimeSpan value) {
        AnyLeg copy = new AnyLeg(this);

        copy._durationRecipe = Any.TimeSpan().OneOf(value);

        return copy;
    }

    public AnyLeg WithDuration(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._durationRecipe = configure(_durationRecipe);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Leg" />.</summary>
    public Leg Generate() {
        UnLocode origin      = _origin.Generate();
        UnLocode destination = _destination.DifferentFrom(origin).Generate();
        TimeSpan duration    = _durationRecipe.Generate();

        DateTimeOffset departure;
        DateTimeOffset arrival;
        if (_departurePin is { } pinnedDeparture && _arrivalPin is { } pinnedArrival) {
            departure = pinnedDeparture;
            arrival   = pinnedArrival;
        } else if (_arrivalPin is { } onlyArrival) {
            arrival   = onlyArrival;
            departure = (onlyArrival - duration).ToOffset(AnyPortOffset().Generate());
        } else {
            departure = _departurePin ?? _departureRecipe.Generate();
            arrival   = (departure + duration).ToOffset(AnyPortOffset().Generate());
        }

        return new Leg(origin, destination, departure, arrival);
    }

}
