using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class HandOverAndDeliveryTests {

    private static ColdChainRefusal RefusalOf(Action action) {
        return Assert.Throws<ColdChainRefusedException>(action).Refusal;
    }

    [Fact]
    public void A_hand_over_closes_the_custody_of_the_leg_and_opens_the_next_one() {
        // Arrange
        Journey         journey = Journey.Planned(new JourneyOptions { Legs = 3 }).Dispatched();
        CustodianId     forwarder = new AnyCustodianId().DifferentFrom(journey.Carrier).Generate();
        DateTimeOffset  at        = Journey.DispatchedAt.AddHours(8);

        // Act
        journey.Consignment.HandOverToNextLeg(forwarder, at);

        // Assert
        IReadOnlyList<LegCustody> custody = journey.Consignment.Custody;
        Assert.Equal(2, custody.Count);
        Assert.Equal(at, custody[0].ReleasedAt);
        Assert.False(custody[0].IsOpen);
        Assert.Equal(1, custody[1].LegIndex);
        Assert.Same(journey.Consignment.Shipment.Legs[1], custody[1].Leg);
        Assert.Same(forwarder, custody[1].Custodian);
        Assert.Equal(at, custody[1].AcceptedAt);
        Assert.True(custody[1].IsOpen);
        Assert.Same(journey.Consignment.Shipment.Legs[1], journey.Consignment.CurrentLeg);
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
    }

    [Fact]
    public void The_same_carrier_may_continue_on_the_next_leg() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();

        // Act
        journey.Consignment.HandOverToNextLeg(journey.Carrier, Journey.DispatchedAt.AddHours(8));

        // Assert
        Assert.Equal(2, journey.Consignment.Custody.Count);
        Assert.Same(journey.Carrier, journey.Consignment.Custody[1].Custodian);
    }

    [Fact]
    public void A_hand_over_may_happen_at_the_very_moment_the_custody_began() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();

        // Act
        journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt);

        // Assert
        Assert.Equal(2, journey.Consignment.Custody.Count);
    }

    [Fact]
    public void A_hand_over_before_the_custody_began_is_refused() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt.AddTicks(-1)));

        // Assert
        Assert.Equal(ColdChainRefusal.BeforeCurrentCustody, refusal);
        Assert.Single(journey.Consignment.Custody);
        Assert.True(journey.Consignment.Custody[0].IsOpen);
    }

    [Fact]
    public void There_is_no_hand_over_after_the_last_leg() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();
        journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt.AddHours(8));

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt.AddHours(16)));

        // Assert
        Assert.Equal(ColdChainRefusal.NoFurtherLeg, refusal);
    }

    [Fact]
    public void A_single_leg_shipment_has_nothing_to_hand_over_to() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 1 }).Dispatched();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt.AddHours(1)));

        // Assert
        Assert.Equal(ColdChainRefusal.NoFurtherLeg, refusal);
    }

    [Fact]
    public void A_planned_consignment_cannot_be_handed_over() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 2 });

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt));

        // Assert
        Assert.Equal(ColdChainRefusal.NotInTransit, refusal);
    }

    [Fact]
    public void A_delivered_consignment_cannot_be_handed_over() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), journey.DeliveredAt));

        // Assert
        Assert.Equal(ColdChainRefusal.NotInTransit, refusal);
    }

    [Fact]
    public void A_custodian_is_required_to_hand_over() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.HandOverToNextLeg(null!, Journey.DispatchedAt));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void Delivery_closes_the_last_custody_and_delivers_the_freight_shipment() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 2 }).Dispatched();

        // Act
        journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt);

        // Assert
        ColdChainConsignment consignment = journey.Consignment;
        Assert.Equal(ConsignmentStatus.Delivered, consignment.Status);
        Assert.Equal(journey.DeliveredAt, consignment.DeliveredAt);
        Assert.Same(journey.Warehouse, consignment.ReceivedBy);
        Assert.Equal(journey.DeliveredAt, consignment.Custody[^1].ReleasedAt);
        Assert.Equal(ShipmentStatus.Delivered, consignment.Shipment.Status);
        Assert.Equal(journey.DeliveredAt, consignment.Shipment.DeliveredAt);
        Assert.Null(consignment.CurrentLeg);
    }

    [Fact]
    public void A_planned_consignment_cannot_be_delivered() {
        // Arrange
        Journey journey = Journey.Planned();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt));

        // Assert
        Assert.Equal(ColdChainRefusal.NotInTransit, refusal);
    }

    [Fact]
    public void A_consignment_cannot_be_delivered_twice() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt.AddHours(1)));

        // Assert
        Assert.Equal(ColdChainRefusal.NotInTransit, refusal);
    }

    [Fact]
    public void Delivery_waits_for_the_last_leg() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 3 }).Dispatched();
        journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt.AddHours(8));

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt));

        // Assert
        Assert.Equal(ColdChainRefusal.ItineraryIncomplete, refusal);
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
    }

    [Fact]
    public void A_warehouse_elsewhere_than_the_end_of_the_last_leg_is_refused() {
        // Arrange
        Journey            journey = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();
        journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt.AddHours(8));
        UnLocode           destination = journey.Consignment.Shipment.Legs[^1].Destination;
        UnLocode           elsewhere   = new AnyUnLocode().DifferentFrom(destination).Generate();
        ReceivingWarehouse wrongWarehouse = new AnyReceivingWarehouse().WithLocation(elsewhere).Generate();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Deliver(wrongWarehouse, journey.DeliveredAt));

        // Assert
        Assert.Equal(ColdChainRefusal.WrongDestination, refusal);
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
    }

    [Fact]
    public void The_origin_of_the_last_leg_is_not_a_destination() {
        // Arrange
        Journey            journey = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();
        journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt.AddHours(8));
        ReceivingWarehouse atTheOrigin = new AnyReceivingWarehouse().WithLocation(journey.Consignment.Shipment.Legs[^1].Origin).Generate();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Deliver(atTheOrigin, journey.DeliveredAt));

        // Assert
        Assert.Equal(ColdChainRefusal.WrongDestination, refusal);
    }

    [Theory]
    [InlineData(2, 8, 2, 8, true)]
    [InlineData(2, 8, 3, 7, true)]
    [InlineData(2, 8, 2, 7, true)]
    [InlineData(2, 8, 0, 8, false)]
    [InlineData(2, 8, 1, 8, false)]
    [InlineData(4, 6, 2, 8, false)]
    [InlineData(4, 6, 4, 6, true)]
    [InlineData(4, 6, 5, 5.5, true)]
    public void The_cold_room_must_lie_inside_the_label_of_the_product(double labelMin, double labelMax, double roomMin, double roomMax, bool accepted) {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { LabelMin = (decimal)labelMin, LabelMax = (decimal)labelMax }).Dispatched();
        ReceivingWarehouse warehouse = new AnyReceivingWarehouse()
            .WithLocation(journey.Consignment.Shipment.Legs[^1].Destination)
            .WithColdRoom(new TemperatureRange((decimal)roomMin, (decimal)roomMax))
            .Generate();

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.Deliver(warehouse, journey.DeliveredAt));

        // Assert
        if (accepted) {
            Assert.Null(thrown);
            Assert.Equal(ConsignmentStatus.Delivered, journey.Consignment.Status);
        } else {
            ColdChainRefusedException refusal = Assert.IsType<ColdChainRefusedException>(thrown);
            Assert.Equal(ColdChainRefusal.ColdRoomOutOfSpecification, refusal.Refusal);
            Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
        }
    }

    [Fact]
    public void A_delivery_before_the_last_custody_began_is_refused() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();
        journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt.AddHours(8));

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Deliver(journey.Warehouse, Journey.DispatchedAt.AddHours(8).AddTicks(-1)));

        // Assert
        Assert.Equal(ColdChainRefusal.BeforeCurrentCustody, refusal);
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
        Assert.True(journey.Consignment.Custody[^1].IsOpen);
    }

    [Fact]
    public void A_delivery_at_the_very_moment_the_last_custody_began_is_accepted() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();

        // Act
        journey.Consignment.Deliver(journey.Warehouse, Journey.DispatchedAt);

        // Assert
        Assert.Equal(ConsignmentStatus.Delivered, journey.Consignment.Status);
    }

    [Fact]
    public void A_warehouse_is_required_to_deliver() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.Deliver(null!, journey.DeliveredAt));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void A_full_itinerary_leaves_one_closed_custody_per_leg() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 3 }).Dispatched();

        // Act
        journey.DeliverAtTheEnd();

        // Assert
        Assert.Equal(3, journey.Consignment.Custody.Count);
        Assert.All(journey.Consignment.Custody, custody => Assert.False(custody.IsOpen));
        Assert.Equal([0, 1, 2], journey.Consignment.Custody.Select(c => c.LegIndex));
    }

}
