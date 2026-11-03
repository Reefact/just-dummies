// Started from `any generate Leg`, then rewritten by hand: the scaffolder could not express that a leg
// moves the container somewhere else and arrives after it departs (at most 90 days later), so
// the destination is drawn different from the origin and the arrival is the departure plus a transit time.

using System;

using Freight.Domain;
using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

public sealed partial class AnyLeg : IAny<Leg> {

    private IAny<UnLocode>       _origin        = new AnyUnLocode();
    private IAny<UnLocode>?      _destination;
    private IAny<DateTimeOffset> _departure     = Any.DateTimeOffset().Between(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 12, 31, 0, 0, 0, TimeSpan.Zero));
    private IAny<TimeSpan>       _transit       = Any.TimeSpan().Between(TimeSpan.FromHours(1), TimeSpan.FromDays(10));

    public AnyLeg() { }

    private AnyLeg(AnyLeg from) {
        _origin      = from._origin;
        _destination = from._destination;
        _departure   = from._departure;
        _transit     = from._transit;
    }

    public AnyLeg WithOrigin(UnLocode value) {
        AnyLeg copy = new AnyLeg(this);

        copy._origin = Any.Fixed(value);

        return copy;
    }

    public AnyLeg WithDestination(UnLocode value) {
        AnyLeg copy = new AnyLeg(this);

        copy._destination = Any.Fixed(value);

        return copy;
    }

    public AnyLeg WithDeparture(DateTimeOffset value) {
        AnyLeg copy = new AnyLeg(this);

        copy._departure = Any.Fixed(value);

        return copy;
    }

    public AnyLeg WithTransit(TimeSpan value) {
        AnyLeg copy = new AnyLeg(this);

        copy._transit = Any.Fixed(value);

        return copy;
    }

    public Leg Generate() {
        UnLocode       origin      = _origin.Generate();
        UnLocode       destination = _destination?.Generate() ?? new AnyUnLocode().DifferentFrom(origin).Generate();
        DateTimeOffset departure   = _departure.Generate();

        return new Leg(origin, destination, departure, departure + _transit.Generate());
    }

}
