using System;
using System.Collections.Generic;

using JustDummies;

namespace Freight.Domain;

/// <summary>Hand-written: a contiguous chain of legs (each starts where the previous one ended, never before it arrived).</summary>
public sealed class AnyItinerary : IAny<IReadOnlyList<Leg>> {

    private readonly AnyLeg _firstLeg;
    private readonly int    _minLegs;
    private readonly int    _maxLegs;

    public AnyItinerary()
        : this(new AnyLeg(), 1, 4) { }

    private AnyItinerary(AnyLeg firstLeg, int minLegs, int maxLegs) {
        _firstLeg = firstLeg;
        _minLegs  = minLegs;
        _maxLegs  = maxLegs;
    }

    /// <summary>Refines the first leg of the itinerary.</summary>
    public AnyItinerary WithFirstLeg(Func<AnyLeg, AnyLeg> configure) {
        return new AnyItinerary(configure(_firstLeg), _minLegs, _maxLegs);
    }

    public AnyItinerary WithLegCount(int count) {
        return new AnyItinerary(_firstLeg, count, count);
    }

    public IReadOnlyList<Leg> Generate() {
        int       count = Any.Int32().Between(_minLegs, _maxLegs).Generate();
        List<Leg> legs  = new List<Leg>();
        Leg       leg   = _firstLeg.Generate();
        legs.Add(leg);

        for (int index = 1; index < count; index++) {
            TimeSpan wait = Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(10)).Generate();
            leg = new AnyLeg().WithOrigin(leg.Destination).WithDeparture(leg.Arrival + wait).Generate();
            legs.Add(leg);
        }

        return legs;
    }

}
