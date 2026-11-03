using System;

namespace ColdChain.Tests.Expert.Generators;

public sealed partial class AnyShipment {

    /// <summary>A shipment whose itinerary has exactly <paramref name="count" /> legs.</summary>
    public AnyShipment WithLegCount(int count) {
        return WithLegs(_ => new AnyItinerary().WithLegCount(count));
    }

}
