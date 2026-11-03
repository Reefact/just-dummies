using Freight.Domain;
using Freight.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class ShipmentLifecycleTests {

    private static readonly DateTimeOffset DepartedAt  = new DateTimeOffset(2026, 3, 2, 9, 30, 0, TimeSpan.FromHours(1));
    private static readonly DateTimeOffset DeliveredAt = new DateTimeOffset(2026, 3, 20, 16, 0, 0, TimeSpan.FromHours(-5));

    private static Shipment BookedShipment() => Any.Shipment().Generate();

    private static Shipment InTransitShipment() {
        Shipment shipment = BookedShipment();
        shipment.Depart(DepartedAt);

        return shipment;
    }

    private static Shipment DeliveredShipment() {
        Shipment shipment = InTransitShipment();
        shipment.Deliver(DeliveredAt);

        return shipment;
    }

    private static Shipment CancelledShipment() {
        Shipment shipment = BookedShipment();
        shipment.Cancel();

        return shipment;
    }

    public static TheoryData<ShipmentStatus> StatusesThatAreNotBooked() => new() { ShipmentStatus.InTransit, ShipmentStatus.Delivered, ShipmentStatus.Cancelled };

    public static TheoryData<ShipmentStatus> StatusesThatAreNotInTransit() => new() { ShipmentStatus.Booked, ShipmentStatus.Delivered, ShipmentStatus.Cancelled };

    private static Shipment InStatus(ShipmentStatus status) {
        return status switch {
            ShipmentStatus.Booked    => BookedShipment(),
            ShipmentStatus.InTransit => InTransitShipment(),
            ShipmentStatus.Delivered => DeliveredShipment(),
            ShipmentStatus.Cancelled => CancelledShipment(),
            _                        => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    [Fact]
    public void A_booked_shipment_departs() {
        Shipment shipment = BookedShipment();

        shipment.Depart(DepartedAt);

        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Equal(DepartedAt, shipment.DepartedAt);
        Assert.Null(shipment.DeliveredAt);
    }

    [Fact]
    public void The_departure_is_not_compared_with_the_first_leg_of_the_itinerary() {
        Leg      leg      = Any.Leg().WithDeparture(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        Shipment shipment = Any.Shipment().WithLegs([leg]).Generate();

        shipment.Depart(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
    }

    [Theory]
    [MemberData(nameof(StatusesThatAreNotBooked))]
    public void Only_a_booked_shipment_departs(ShipmentStatus status) {
        Shipment shipment = InStatus(status);

        Assert.Throws<InvalidOperationException>(() => shipment.Depart(DepartedAt.AddDays(1)));
    }

    [Fact]
    public void A_refused_departure_leaves_the_shipment_as_it_was() {
        Shipment shipment = InTransitShipment();

        Assert.Throws<InvalidOperationException>(() => shipment.Depart(DepartedAt.AddDays(1)));

        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Equal(DepartedAt, shipment.DepartedAt);
    }

    [Fact]
    public void A_shipment_in_transit_is_delivered() {
        Shipment shipment = InTransitShipment();

        shipment.Deliver(DeliveredAt);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(DeliveredAt, shipment.DeliveredAt);
        Assert.Equal(DepartedAt, shipment.DepartedAt);
    }

    [Fact]
    public void A_shipment_can_be_delivered_the_very_instant_it_departed() {
        Shipment shipment = InTransitShipment();

        shipment.Deliver(DepartedAt);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    [Fact]
    public void A_shipment_cannot_be_delivered_before_it_departed() {
        Shipment shipment = InTransitShipment();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => shipment.Deliver(DepartedAt.AddTicks(-1)));

        Assert.Equal("at", refusal.ParamName);
    }

    [Fact]
    public void A_delivery_is_compared_with_the_departure_as_instants() {
        Shipment shipment = InTransitShipment();   // departed 2026-03-02T09:30+01:00, that is 08:30 UTC
        DateTimeOffset sameInstantInAnotherOffset = new DateTimeOffset(2026, 3, 2, 3, 30, 0, TimeSpan.FromHours(-5));

        shipment.Deliver(sameInstantInAnotherOffset);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    [Fact]
    public void A_refused_delivery_leaves_the_shipment_in_transit() {
        Shipment shipment = InTransitShipment();

        Assert.Throws<ArgumentException>(() => shipment.Deliver(DepartedAt.AddHours(-1)));

        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Null(shipment.DeliveredAt);
    }

    [Theory]
    [MemberData(nameof(StatusesThatAreNotInTransit))]
    public void Only_a_shipment_in_transit_is_delivered(ShipmentStatus status) {
        Shipment shipment = InStatus(status);

        Assert.Throws<InvalidOperationException>(() => shipment.Deliver(DeliveredAt.AddDays(1)));
    }

    [Fact]
    public void The_status_is_checked_before_the_time_of_a_delivery() {
        Shipment booked = BookedShipment();

        Assert.Throws<InvalidOperationException>(() => booked.Deliver(DateTimeOffset.MinValue));
    }

    [Fact]
    public void A_booked_shipment_is_cancelled() {
        Shipment shipment = BookedShipment();

        shipment.Cancel();

        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
        Assert.Null(shipment.DepartedAt);
        Assert.Null(shipment.DeliveredAt);
    }

    [Theory]
    [MemberData(nameof(StatusesThatAreNotBooked))]
    public void Only_a_booked_shipment_is_cancelled(ShipmentStatus status) {
        Shipment shipment = InStatus(status);

        Assert.Throws<InvalidOperationException>(() => shipment.Cancel());
        Assert.Equal(status, shipment.Status);
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public void A_delivered_or_cancelled_shipment_has_no_way_out(ShipmentStatus terminal) {
        Shipment shipment = InStatus(terminal);

        Assert.Throws<InvalidOperationException>(() => shipment.Depart(DepartedAt));
        Assert.Throws<InvalidOperationException>(() => shipment.Deliver(DeliveredAt));
        Assert.Throws<InvalidOperationException>(() => shipment.Cancel());
        Assert.Equal(terminal, shipment.Status);
    }

}
