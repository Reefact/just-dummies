using Freight.Domain;
using Freight.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class ShipmentLatenessTests {

    private static readonly DateTimeOffset EstimatedArrival = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);

    private static Shipment ShipmentDueAt(DateTimeOffset arrival) {
        Leg leg = Any.Leg().WithDeparture(arrival.AddDays(-9)).WithArrival(arrival).Generate();

        return Any.Shipment().WithLegs([leg]).Generate();
    }

    [Fact]
    public void A_booked_shipment_is_late_once_the_estimated_arrival_has_passed() {
        Shipment booked = ShipmentDueAt(EstimatedArrival);

        Assert.True(booked.IsLateAt(EstimatedArrival.AddTicks(1)));
        Assert.True(booked.IsLateAt(EstimatedArrival.AddDays(30)));
    }

    [Fact]
    public void A_booked_shipment_is_not_late_before_or_at_the_estimated_arrival() {
        Shipment booked = ShipmentDueAt(EstimatedArrival);

        Assert.False(booked.IsLateAt(EstimatedArrival));
        Assert.False(booked.IsLateAt(EstimatedArrival.AddTicks(-1)));
        Assert.False(booked.IsLateAt(DateTimeOffset.MinValue));
    }

    [Fact]
    public void A_shipment_in_transit_is_late_once_the_estimated_arrival_has_passed() {
        Shipment inTransit = ShipmentDueAt(EstimatedArrival);
        inTransit.Depart(EstimatedArrival.AddDays(-9));

        Assert.True(inTransit.IsLateAt(EstimatedArrival.AddTicks(1)));
        Assert.False(inTransit.IsLateAt(EstimatedArrival));
    }

    [Fact]
    public void Lateness_compares_instants_whatever_the_offset_of_the_clock_asking() {
        Shipment booked = ShipmentDueAt(EstimatedArrival);
        DateTimeOffset sameInstantInTokyo = new DateTimeOffset(2026, 3, 10, 21, 0, 0, TimeSpan.FromHours(9));

        Assert.False(booked.IsLateAt(sameInstantInTokyo));
        Assert.True(booked.IsLateAt(sameInstantInTokyo.AddSeconds(1)));
    }

    [Fact]
    public void A_delivered_shipment_is_never_late() {
        Shipment delivered = ShipmentDueAt(EstimatedArrival);
        delivered.Depart(EstimatedArrival.AddDays(-9));
        delivered.Deliver(EstimatedArrival.AddDays(5));

        Assert.False(delivered.IsLateAt(EstimatedArrival.AddDays(60)));
    }

    [Fact]
    public void A_cancelled_shipment_is_never_late() {
        Shipment cancelled = ShipmentDueAt(EstimatedArrival);
        cancelled.Cancel();

        Assert.False(cancelled.IsLateAt(EstimatedArrival.AddDays(60)));
    }

}
