using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Leg" /> values, hand-completed from the scaffold: a leg is drawn as a departure plus a
///     duration (so arrival is after departure and within <see cref="Leg.MaxTransit" /> by construction), and the destination
///     differs from the origin.
/// </summary>
public sealed partial class AnyLeg : IAny<Leg> {

    private IAny<UnLocode>      _origin;
    private IAny<UnLocode>      _destination;
    private IAny<DateTimeOffset> _departure;
    private IAny<TimeSpan>       _duration;

    private AnyUnLocode       _originRecipe;
    private AnyUnLocode       _destinationRecipe;
    private AnyDateTimeOffset _departureRecipe;
    private AnyTimeSpan       _durationRecipe;

    public AnyLeg()
        : this(origin:      new AnyUnLocode(),
               destination: new AnyUnLocode(),
               departure:   Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                          new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero)),
               duration:    Any.TimeSpan().Between(TimeSpan.FromMinutes(1), Leg.MaxTransit)) { }

    private AnyLeg(AnyUnLocode       origin,
                   AnyUnLocode       destination,
                   AnyDateTimeOffset departure,
                   AnyTimeSpan       duration) {
        _origin            = origin;
        _originRecipe      = origin;
        _destination       = destination;
        _destinationRecipe = destination;
        _departure         = departure;
        _departureRecipe   = departure;
        _duration          = duration;
        _durationRecipe    = duration;
    }

    private AnyLeg(AnyLeg from) {
        _origin            = from._origin;
        _originRecipe      = from._originRecipe;
        _destination       = from._destination;
        _destinationRecipe = from._destinationRecipe;
        _departure         = from._departure;
        _departureRecipe   = from._departureRecipe;
        _duration          = from._duration;
        _durationRecipe    = from._durationRecipe;
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

        copy._departure = Any.Fixed(value);

        return copy;
    }

    public AnyLeg WithDeparture(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._departureRecipe = configure(_departureRecipe);
        copy._departure       = copy._departureRecipe;

        return copy;
    }

    public AnyLeg WithDuration(TimeSpan value) {
        AnyLeg copy = new AnyLeg(this);

        copy._duration = Any.Fixed(value);

        return copy;
    }

    public AnyLeg WithDuration(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._durationRecipe = configure(_durationRecipe);
        copy._duration       = copy._durationRecipe;

        return copy;
    }

    public Leg Generate() {
        UnLocode       origin      = _origin.Generate();
        UnLocode       destination = _destination.DifferentFrom(origin).Generate();
        DateTimeOffset departure   = _departure.Generate();
        TimeSpan       duration    = _duration.Generate();

        return new Leg(origin, destination, departure, departure + duration);
    }

}
