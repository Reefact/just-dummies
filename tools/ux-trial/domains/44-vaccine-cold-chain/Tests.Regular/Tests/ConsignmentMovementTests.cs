using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests.Regular.Tests;

public sealed class HandOverTests {

    private static (ColdChainConsignment Consignment, DateTimeOffset DispatchedAt, CustodianId FirstCarrier) InTransit(int legCount) {
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt, legCount: legCount);
        CustodianId          carrier     = Journeys.AnyCustodian();
        consignment.Dispatch(carrier, dispatchAt);

        return (consignment, dispatchAt, carrier);
    }

    [Fact]
    public void A_hand_over_ends_the_custody_of_the_leg_and_opens_the_next_one() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt, CustodianId firstCarrier) = InTransit(legCount: 3);
        CustodianId    forwarder = Journeys.AnyCustodian();
        DateTimeOffset at        = dispatchedAt.AddHours(5);

        // Act
        consignment.HandOverToNextLeg(forwarder, at);

        // Assert
        Assert.Equal(2, consignment.Custody.Count);
        Assert.Equal(at, consignment.Custody[0].ReleasedAt);
        Assert.False(consignment.Custody[0].IsOpen);
        Assert.Equal(firstCarrier, consignment.Custody[0].Custodian);
        Assert.Equal(1, consignment.Custody[1].LegIndex);
        Assert.Equal(forwarder, consignment.Custody[1].Custodian);
        Assert.Equal(at, consignment.Custody[1].AcceptedAt);
        Assert.Same(consignment.Shipment.Legs[1], consignment.Custody[1].Leg);
        Assert.Same(consignment.Shipment.Legs[1], consignment.CurrentLeg);
        Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
    }

    [Fact]
    public void The_same_carrier_may_continue_on_the_next_leg() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt, CustodianId firstCarrier) = InTransit(legCount: 2);

        // Act
        consignment.HandOverToNextLeg(firstCarrier, dispatchedAt.AddHours(2));

        // Assert
        Assert.Equal(firstCarrier, consignment.Custody[1].Custodian);
    }

    [Fact]
    public void The_consignment_goes_through_every_leg_in_turn() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt, CustodianId _) = InTransit(legCount: 4);

        // Act
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(1));
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(2));
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(3));

        // Assert
        Assert.Equal([0, 1, 2, 3], consignment.Custody.Select(c => c.LegIndex).ToList());
        Assert.Equal(3, consignment.Custody.Count(c => !c.IsOpen));
        Assert.Same(consignment.Shipment.Legs[3], consignment.CurrentLeg);
    }

    [Fact]
    public void The_last_leg_has_no_next_one() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt, CustodianId _) = InTransit(legCount: 1);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(1)));

        // Assert
        Assert.Equal(ColdChainRefusal.NoFurtherLeg, refusal.Refusal);
        Assert.Single(consignment.Custody);
    }

    [Fact]
    public void A_hand_over_cannot_precede_the_acceptance_of_the_current_custody() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt, CustodianId _) = InTransit(legCount: 2);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddTicks(-1)));
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt);

        // Assert
        Assert.Equal(ColdChainRefusal.BeforeCurrentCustody, refusal.Refusal);
        Assert.Equal(2, consignment.Custody.Count);
    }

    [Fact]
    public void A_second_hand_over_cannot_precede_the_first() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt, CustodianId _) = InTransit(legCount: 3);
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(4));

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(3)));

        // Assert
        Assert.Equal(ColdChainRefusal.BeforeCurrentCustody, refusal.Refusal);
    }

    [Fact]
    public void A_consignment_not_yet_dispatched_cannot_change_hands() {
        // Arrange
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt, legCount: 2);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchAt));

        // Assert
        Assert.Equal(ColdChainRefusal.NotInTransit, refusal.Refusal);
    }

    [Fact]
    public void A_delivered_consignment_cannot_change_hands() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), Journeys.Steady(4));

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.HandOverToNextLeg(Journeys.AnyCustodian(), consignment.DeliveredAt!.Value));

        // Assert
        Assert.Equal(ColdChainRefusal.NotInTransit, refusal.Refusal);
    }

    [Fact]
    public void A_hand_over_needs_a_custodian() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt, CustodianId _) = InTransit(legCount: 2);

        // Assert
        Assert.Throws<ArgumentNullException>(() => consignment.HandOverToNextLeg(null!, dispatchedAt));
    }

}

public sealed class DeliverTests {

    private static (ColdChainConsignment Consignment, DateTimeOffset DispatchedAt) InTransit(VaccineProduct product, int legCount = 1, int palletCount = 1) {
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(product, dispatchAt, palletCount, legCount);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt);

        return (consignment, dispatchAt);
    }

    [Fact]
    public void Delivery_hands_the_consignment_over_to_the_warehouse_at_the_end_of_the_last_leg() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(Journeys.Product());
        ReceivingWarehouse warehouse = Journeys.WarehouseFor(consignment);
        DateTimeOffset     at        = dispatchedAt.AddHours(9);

        // Act
        consignment.Deliver(warehouse, at);

        // Assert
        Assert.Equal(ConsignmentStatus.Delivered, consignment.Status);
        Assert.Equal(at, consignment.DeliveredAt);
        Assert.Same(warehouse, consignment.ReceivedBy);
        Assert.Equal(ShipmentStatus.Delivered, consignment.Shipment.Status);
        Assert.Equal(at, consignment.Shipment.DeliveredAt);
        Assert.Equal(at, consignment.Custody[^1].ReleasedAt);
        Assert.Null(consignment.CurrentLeg);
    }

    [Fact]
    public void A_consignment_not_yet_dispatched_cannot_be_delivered() {
        // Arrange
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchAt.AddHours(1)));

        // Assert
        Assert.Equal(ColdChainRefusal.NotInTransit, refusal.Refusal);
    }

    [Fact]
    public void A_consignment_is_delivered_once() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), Journeys.Steady(3));

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Deliver(Journeys.WarehouseFor(consignment), consignment.DeliveredAt!.Value.AddHours(1)));

        // Assert
        Assert.Equal(ColdChainRefusal.NotInTransit, refusal.Refusal);
    }

    [Fact]
    public void Delivery_waits_for_the_last_leg() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(Journeys.Product(), legCount: 3);
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(1));

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddHours(2)));
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(2));
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddHours(3));

        // Assert
        Assert.Equal(ColdChainRefusal.ItineraryIncomplete, refusal.Refusal);
        Assert.Equal(ConsignmentStatus.Delivered, consignment.Status);
    }

    [Fact]
    public void The_warehouse_must_be_where_the_last_leg_ends() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(Journeys.Product(), legCount: 2);
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(1));
        UnLocode           lastDestination = consignment.Shipment.Legs[^1].Destination;
        UnLocode           anywhereElse    = new AnyUnLocode().DifferentFrom(lastDestination).Generate();
        ReceivingWarehouse elsewhere       = Journeys.WarehouseAt(anywhereElse);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Deliver(elsewhere, dispatchedAt.AddHours(2)));

        // Assert
        Assert.Equal(ColdChainRefusal.WrongDestination, refusal.Refusal);
        Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
    }

    [Theory]
    [InlineData(3.0, 7.0, 3.0, 7.0, true)]
    [InlineData(3.0, 7.0, 4.0, 6.0, true)]
    [InlineData(3.0, 7.0, 2.9, 7.0, false)]
    [InlineData(3.0, 7.0, 3.0, 7.1, false)]
    [InlineData(2.0, 8.0, 0.0, 8.0, false)]
    [InlineData(2.0, 8.0, 2.0, 8.0, true)]
    public void The_cold_room_must_keep_the_product_within_its_label(double labelMin, double labelMax, double roomMin, double roomMax, bool accepted) {
        // Arrange
        VaccineProduct product = Journeys.Product(label: new TemperatureRange((decimal)labelMin, (decimal)labelMax));
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(product);
        ReceivingWarehouse warehouse = Journeys.WarehouseFor(consignment, new TemperatureRange((decimal)roomMin, (decimal)roomMax));

        // Act
        Action act = () => consignment.Deliver(warehouse, dispatchedAt.AddHours(1));

        // Assert
        if (accepted) {
            act();
            Assert.Equal(ConsignmentStatus.Delivered, consignment.Status);
        } else {
            Assert.Equal(ColdChainRefusal.ColdRoomOutOfSpecification, Assert.Throws<ColdChainRefusedException>(act).Refusal);
            Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
        }
    }

    [Fact]
    public void Delivery_cannot_precede_the_acceptance_of_the_last_custody() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(Journeys.Product(), legCount: 2);
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), dispatchedAt.AddHours(4));

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddHours(4).AddTicks(-1)));
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddHours(4));

        // Assert
        Assert.Equal(ColdChainRefusal.BeforeCurrentCustody, refusal.Refusal);
        Assert.Equal(ConsignmentStatus.Delivered, consignment.Status);
    }

    [Fact]
    public void A_refused_delivery_leaves_the_shipment_and_the_custody_untouched() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(Journeys.Product(label: new TemperatureRange(3m, 7m)));
        ReceivingWarehouse tooWide = Journeys.WarehouseFor(consignment, new TemperatureRange(2m, 8m));

        // Act
        Assert.Throws<ColdChainRefusedException>(() => consignment.Deliver(tooWide, dispatchedAt.AddHours(1)));

        // Assert
        Assert.Equal(ShipmentStatus.InTransit, consignment.Shipment.Status);
        Assert.True(consignment.Custody[^1].IsOpen);
        Assert.Null(consignment.ReceivedBy);
        Assert.Null(consignment.DeliveredAt);
    }

    [Fact]
    public void Delivery_closes_the_consignment_when_every_pallet_is_already_decided() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(Journeys.Product());
        Journeys.Record(consignment, consignment.Pallets[0], dispatchedAt.AddMinutes(10), -3m);

        // Act
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddHours(1));

        // Assert
        Assert.Equal(ConsignmentStatus.Closed, consignment.Status);
    }

    [Fact]
    public void Delivery_leaves_the_consignment_open_while_a_pallet_awaits_its_decision() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(Journeys.Product(), palletCount: 2);
        Journeys.Record(consignment, consignment.Pallets[0], dispatchedAt.AddMinutes(10), -3m);

        // Act
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddHours(1));

        // Assert
        Assert.Equal(ConsignmentStatus.Delivered, consignment.Status);
    }

    [Fact]
    public void A_delivery_needs_a_warehouse() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(Journeys.Product());

        // Assert
        Assert.Throws<ArgumentNullException>(() => consignment.Deliver(null!, dispatchedAt));
    }

}
