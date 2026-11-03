using System;
using System.Collections.Generic;

using Freight.Domain;
using JustDummies;

namespace Freight.Tests.Expert.Generators;

/// <summary>
///     Hand-written (the tool cannot scaffold a contiguity rule): a contiguous itinerary, each leg leaving the
///     port the previous one reached, no earlier than the previous one arrived.
/// </summary>
public sealed class AnyItinerary : IAny<IReadOnlyList<Leg>> {

    private static readonly AnyTimeSpan Layover = Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(10));

    private readonly AnyInt32           _legCount;
    private readonly DateTimeOffset?    _firstDeparturePin;

    public AnyItinerary()
        : this(Any.Int32().Between(1, 4), null) { }

    private AnyItinerary(AnyInt32 legCount, DateTimeOffset? firstDeparturePin) {
        _legCount             = legCount;
        _firstDeparturePin    = firstDeparturePin;
    }

    public AnyItinerary WithLegCount(int count) {
        return new AnyItinerary(_legCount.OneOf(count), _firstDeparturePin);
    }

    public AnyItinerary WithLegCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyItinerary(configure(_legCount), _firstDeparturePin);
    }

    public AnyItinerary DepartingAt(DateTimeOffset firstDeparture) {
        return new AnyItinerary(_legCount, firstDeparture);
    }

    public IReadOnlyList<Leg> Generate() {
        int count = _legCount.Generate();

        AnyLeg firstLeg = new AnyLeg();
        if (_firstDeparturePin is { } pinned) { firstLeg = firstLeg.WithDeparture(pinned); }

        List<Leg> legs = new List<Leg>();
        Leg       leg  = firstLeg.Generate();
        legs.Add(leg);
        for (int index = 1; index < count; index++) {
            leg = new AnyLeg().WithOrigin(leg.Destination)
                              .WithDeparture(leg.Arrival + Layover.Generate())
                              .Generate();
            legs.Add(leg);
        }

        return legs;
    }

}
