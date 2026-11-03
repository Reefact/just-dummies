// Written by hand: `any generate Shipment` draws `legs` as a list of independent legs, while a shipment
// demands a contiguous itinerary (each leg starts where the previous one ends, not before it arrives).

using System;
using System.Collections.Generic;

using Freight.Domain;
using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

public sealed class AnyItinerary : IAny<IReadOnlyList<Leg>> {

    private IAny<int> _legCount = Any.Int32().Between(1, 4);

    public AnyItinerary() { }

    private AnyItinerary(AnyItinerary from) {
        _legCount = from._legCount;
    }

    public AnyItinerary WithLegCount(int count) {
        AnyItinerary copy = new AnyItinerary(this);

        copy._legCount = Any.Fixed(count);

        return copy;
    }

    public IReadOnlyList<Leg> Generate() {
        int       count = _legCount.Generate();
        List<Leg> legs  = new List<Leg>();
        Leg       first = new AnyLeg().Generate();

        legs.Add(first);
        for (int index = 1; index < count; index++) {
            Leg      previous = legs[index - 1];
            TimeSpan wait     = Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(3)).Generate();

            legs.Add(new AnyLeg().WithOrigin(previous.Destination).WithDeparture(previous.Arrival + wait).Generate());
        }

        return legs;
    }

}
