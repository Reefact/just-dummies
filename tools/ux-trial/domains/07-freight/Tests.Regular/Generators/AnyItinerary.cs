using System;
using System.Collections.Generic;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A contiguous run of legs: each leg starts where the previous one ended, and never before it arrived.
///     Written by hand: no constructor of the domain builds it, <c>Shipment</c> only checks it.
/// </summary>
public sealed class AnyItinerary : IAny<IReadOnlyList<Leg>> {

    private readonly AnyInt32   _legCount;
    private readonly AnyTimeSpan _layover;

    public AnyItinerary()
        : this(Any.Int32().Between(1, 4), Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(10))) { }

    private AnyItinerary(AnyInt32 legCount, AnyTimeSpan layover) {
        _legCount = legCount;
        _layover  = layover;
    }

    public AnyItinerary WithLegCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyItinerary(configure(_legCount), _layover);
    }

    public AnyItinerary WithLegCount(int count) {
        return new AnyItinerary(Any.Int32().Between(count, count), _layover);
    }

    public AnyItinerary WithLayover(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyItinerary(_legCount, configure(_layover));
    }

    public IReadOnlyList<Leg> Generate() {
        int            count  = _legCount.Generate();
        List<UnLocode> ports  = Any.ListOf(new AnyUnLocode()).WithCount(count + 1).Distinct().Generate();
        DateTimeOffset cursor = new AnyLeg().Generate().Departure;
        List<Leg>      legs   = new List<Leg>();

        for (int index = 0; index < count; index++) {
            Leg leg = new AnyLeg().WithOrigin(ports[index]).WithDestination(ports[index + 1]).WithDeparture(cursor).Generate();
            legs.Add(leg);
            cursor = leg.Arrival + _layover.Generate();
        }

        return legs;
    }

}
