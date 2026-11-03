using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using Freight.Domain;
using JustDummies;
using Warehouse.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.Consignment;

public sealed class DeliverTests {

    [Fact]
    public void Delivering_closes_the_last_custody_and_hands_the_consignment_to_the_warehouse() {
        // Arrange
        Journey            journey   = Journey.Dispatched();
        ReceivingWarehouse warehouse = journey.Warehouse().Generate();
        DateTimeOffset     at        = Journey.Start.AddMinutes(60);

        // Act
        journey.Consignment.Deliver(warehouse, at);

        // Assert
        Assert.Equal(ConsignmentStatus.Delivered, journey.Consignment.Status);
        Assert.Equal(at, journey.Consignment.DeliveredAt);
        Assert.Same(warehouse, journey.Consignment.ReceivedBy);
        Assert.Equal(at, Assert.Single(journey.Consignment.Custody).ReleasedAt);
        Assert.Null(journey.Consignment.CurrentLeg);
    }

    [Fact]
    public void Delivering_delivers_the_freight_shipment_too() {
        // Arrange
        Journey        journey = Journey.Dispatched();
        DateTimeOffset at      = Journey.Start.AddMinutes(60);

        // Act
        journey.Consignment.Deliver(journey.Warehouse().Generate(), at);

        // Assert
        Assert.Equal(ShipmentStatus.Delivered, journey.Consignment.Shipment.Status);
        Assert.Equal(at, journey.Consignment.Shipment.DeliveredAt);
    }

    [Fact]
    public void A_consignment_not_yet_dispatched_cannot_be_delivered() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned().Generate();
        ReceivingWarehouse   warehouse   = Journey.WarehouseFor(consignment).Generate();

        // Assert
        Refused.Because(ColdChainRefusal.NotInTransit, () => consignment.Deliver(warehouse, Journey.Start));
    }

    [Fact]
    public void A_consignment_cannot_be_delivered_twice() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.Deliver(60);

        // Assert
        Refused.Because(ColdChainRefusal.NotInTransit, () => journey.Consignment.Deliver(journey.Warehouse().Generate(), Journey.Start.AddMinutes(61)));
    }

    [Fact]
    public void A_consignment_still_on_an_early_leg_is_not_ready_to_be_delivered() {
        // Arrange
        Journey journey = Journey.Dispatched(planned => planned.WithShipment(shipment => shipment.WithLegCount(2)));

        // Assert
        Refused.Because(ColdChainRefusal.ItineraryIncomplete, () => journey.Consignment.Deliver(journey.Warehouse().Generate(), Journey.Start.AddMinutes(60)));
    }

    [Fact]
    public void A_consignment_that_travelled_every_leg_is_delivered_at_the_end_of_the_last_one() {
        // Arrange
        Journey journey = Journey.Dispatched(planned => planned.WithShipment(shipment => shipment.WithLegCount(2)));

        journey.Consignment.HandOverToNextLeg(Any.CustodianId().Generate(), Journey.Start.AddMinutes(30));

        // Act
        journey.Consignment.Deliver(journey.Warehouse().Generate(), Journey.Start.AddMinutes(60));

        // Assert
        Assert.Equal(ConsignmentStatus.Delivered, journey.Consignment.Status);
        Assert.All(journey.Consignment.Custody, custody => Assert.False(custody.IsOpen));
    }

    [Fact]
    public void A_warehouse_elsewhere_than_the_end_of_the_last_leg_is_refused() {
        // Arrange
        Journey  journey     = Journey.Dispatched();
        UnLocode destination = journey.Consignment.Shipment.Legs[^1].Destination;
        UnLocode anyOtherLocation   = new AnyUnLocode().DifferentFrom(destination).Generate();

        ReceivingWarehouse warehouse = journey.Warehouse().WithLocation(anyOtherLocation).Generate();

        // Assert
        Refused.Because(ColdChainRefusal.WrongDestination, () => journey.Consignment.Deliver(warehouse, Journey.Start.AddMinutes(60)));
    }

    [Theory]
    [InlineData("2", "8")]
    [InlineData("3", "6")]
    [InlineData("2", "2.5")]
    public void A_cold_room_inside_the_label_is_accepted(string min, string max) {
        // Arrange
        Journey            journey   = Journey.Dispatched();
        ReceivingWarehouse warehouse = journey.Warehouse().WithColdRoom(new TemperatureRange(Celsius.Of(min), Celsius.Of(max))).Generate();

        // Act
        journey.Consignment.Deliver(warehouse, Journey.Start.AddMinutes(60));

        // Assert
        Assert.Equal(ConsignmentStatus.Delivered, journey.Consignment.Status);
    }

    [Theory]
    [InlineData("0", "8")]
    [InlineData("1.9", "5")]
    [InlineData("0", "4")]
    public void A_cold_room_that_can_run_colder_than_the_label_is_refused(string min, string max) {
        // Arrange
        Journey            journey   = Journey.Dispatched();
        ReceivingWarehouse warehouse = journey.Warehouse().WithColdRoom(new TemperatureRange(Celsius.Of(min), Celsius.Of(max))).Generate();

        // Assert
        Refused.Because(ColdChainRefusal.ColdRoomOutOfSpecification, () => journey.Consignment.Deliver(warehouse, Journey.Start.AddMinutes(60)));
    }

    [Fact]
    public void A_cold_room_wider_than_a_narrower_label_is_refused() {
        // Arrange
        Journey            journey   = Journey.Dispatched(planned => planned.WithProduct(product => product.WithLabelledRange(new TemperatureRange(4m, 6m))));
        ReceivingWarehouse warehouse = journey.Warehouse().WithColdRoom(new TemperatureRange(2m, 8m)).Generate();

        // Assert
        Refused.Because(ColdChainRefusal.ColdRoomOutOfSpecification, () => journey.Consignment.Deliver(warehouse, Journey.Start.AddMinutes(60)));
    }

    [Fact]
    public void A_delivery_cannot_precede_the_acceptance_of_the_last_custody() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Refused.Because(ColdChainRefusal.BeforeCurrentCustody, () => journey.Consignment.Deliver(journey.Warehouse().Generate(), Journey.Start.AddSeconds(-1)));
    }

    [Fact]
    public void A_refused_delivery_leaves_the_consignment_in_transit() {
        // Arrange
        Journey            journey   = Journey.Dispatched();
        ReceivingWarehouse warehouse = journey.Warehouse().WithColdRoom(new TemperatureRange(0m, 8m)).Generate();

        // Act
        Assert.ThrowsAny<ColdChainRefusedException>(() => journey.Consignment.Deliver(warehouse, Journey.Start.AddMinutes(60)));

        // Assert
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
        Assert.Null(journey.Consignment.ReceivedBy);
        Assert.True(Assert.Single(journey.Consignment.Custody).IsOpen);
        Assert.Equal(ShipmentStatus.InTransit, journey.Consignment.Shipment.Status);
    }

    [Fact]
    public void A_consignment_whose_pallets_were_all_written_off_in_transit_is_closed_on_delivery() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.Record(0, 30m);

        // Act
        journey.Deliver(60);

        // Assert
        Assert.Equal(ConsignmentStatus.Closed, journey.Consignment.Status);
    }

    [Fact]
    public void A_missing_warehouse_is_refused() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Assert.Throws<ArgumentNullException>(() => journey.Consignment.Deliver(null!, Journey.Start.AddMinutes(60)));
    }

}
