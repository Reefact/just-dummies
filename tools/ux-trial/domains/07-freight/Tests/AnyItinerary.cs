using JustDummies;

namespace Freight.Domain;

/// <summary>
///     Hand-written: an itinerary is a chain, each leg leaving from where the previous one arrived, no
///     earlier than it arrived. A list of independent legs never satisfies that, so it is built leg by leg.
/// </summary>
public sealed class AnyItinerary : IAny<IReadOnlyList<Leg>> {

    private readonly AnyInt32 _legCount;

    public AnyItinerary()
        : this(Any.Int32().Between(1, 4)) { }

    private AnyItinerary(AnyInt32 legCount) {
        _legCount = legCount;
    }

    public AnyItinerary WithLegCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyItinerary(configure(Any.Int32().Between(1, 4)));
    }

    public IReadOnlyList<Leg> Generate() {
        int       count = _legCount.Generate();
        List<Leg> legs  = new(count);
        Leg       first = new AnyLeg().Generate();
        legs.Add(first);
        for (int index = 1; index < count; index++) {
            Leg      previous  = legs[index - 1];
            TimeSpan dwell     = Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(7)).WithGranularity(TimeSpan.FromMinutes(1)).Generate();
            Leg      next      = new AnyLeg().WithOrigin(previous.Destination)
                                             .WithDeparture(previous.Arrival + dwell)
                                             .Generate();
            legs.Add(next);
        }

        return legs;
    }

}
