using System.Collections.Generic;

using JustDummies;

namespace Freight.Domain;

// Written by hand, beside the scaffolded file: `any generate Freight.Domain.Shipment --force` leaves it alone.
public sealed partial class AnyShipment {

    /// <summary>A shipment whose itinerary has exactly <paramref name="count" /> contiguous legs, drawn afresh each time.</summary>
    public AnyShipment WithLegCount(int count) {
        AnyShipment copy = new AnyShipment(this);

        copy._legs = Any.Fixed(count).As(DrawItinerary);

        return copy;
    }

}
