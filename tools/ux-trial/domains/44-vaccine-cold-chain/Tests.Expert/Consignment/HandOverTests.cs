using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using JustDummies;
using Xunit;

namespace ColdChain.Tests.Expert.Consignment;

public sealed class HandOverTests {

    private static Journey DispatchedOverThreeLegs() {
        return Journey.Dispatched(planned => planned.WithShipment(shipment => shipment.WithLegCount(3)));
    }

    [Fact]
    public void Handing_over_releases_the_current_custody_and_opens_that_of_the_next_leg() {
        // Arrange
        Journey     journey = DispatchedOverThreeLegs();
        CustodianId anyRelay   = Any.CustodianId().Generate();
        DateTimeOffset at   = Journey.Start.AddMinutes(30);

        // Act
        journey.Consignment.HandOverToNextLeg(anyRelay, at);

        // Assert
        Assert.Equal(2, journey.Consignment.Custody.Count);

        LegCustody released = journey.Consignment.Custody[0];
        LegCustody current  = journey.Consignment.Custody[1];

        Assert.Equal(at, released.ReleasedAt);
        Assert.False(released.IsOpen);
        Assert.Equal(1, current.LegIndex);
        Assert.Equal(anyRelay, current.Custodian);
        Assert.Equal(at, current.AcceptedAt);
        Assert.True(current.IsOpen);
        Assert.Same(journey.Consignment.Shipment.Legs[1], journey.Consignment.CurrentLeg);
    }

    [Fact]
    public void The_same_carrier_may_carry_on_to_the_next_leg() {
        // Arrange
        Journey journey = DispatchedOverThreeLegs();

        // Act
        journey.Consignment.HandOverToNextLeg(journey.Carrier, Journey.Start.AddMinutes(30));

        // Assert
        Assert.Equal(journey.Carrier, journey.Consignment.Custody[1].Custodian);
    }

    [Fact]
    public void A_hand_over_may_happen_at_the_very_instant_the_custody_began() {
        // Arrange
        Journey journey = DispatchedOverThreeLegs();

        // Act
        journey.Consignment.HandOverToNextLeg(Any.CustodianId().Generate(), Journey.Start);

        // Assert
        Assert.Equal(Journey.Start, journey.Consignment.Custody[0].ReleasedAt);
    }

    [Fact]
    public void A_hand_over_cannot_precede_the_acceptance_of_the_current_custody() {
        // Arrange
        Journey journey = DispatchedOverThreeLegs();

        // Assert
        Refused.Because(ColdChainRefusal.BeforeCurrentCustody, () => journey.Consignment.HandOverToNextLeg(Any.CustodianId().Generate(), Journey.Start.AddSeconds(-1)));
    }

    [Fact]
    public void The_custody_of_every_leg_but_the_last_can_be_handed_on_and_the_last_leg_has_no_next() {
        // Arrange
        Journey journey = Journey.Dispatched(planned => planned.WithShipment(shipment => shipment.WithLegCount(2)));

        journey.Consignment.HandOverToNextLeg(Any.CustodianId().Generate(), Journey.Start.AddMinutes(10));

        // Assert
        Refused.Because(ColdChainRefusal.NoFurtherLeg, () => journey.Consignment.HandOverToNextLeg(Any.CustodianId().Generate(), Journey.Start.AddMinutes(20)));
    }

    [Fact]
    public void A_single_leg_itinerary_has_nobody_to_hand_over_to() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Refused.Because(ColdChainRefusal.NoFurtherLeg, () => journey.Consignment.HandOverToNextLeg(Any.CustodianId().Generate(), Journey.Start.AddMinutes(10)));
    }

    [Fact]
    public void A_consignment_not_yet_dispatched_cannot_be_handed_over() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned().WithShipment(shipment => shipment.WithLegCount(2)).Generate();

        // Assert
        Refused.Because(ColdChainRefusal.NotInTransit, () => consignment.HandOverToNextLeg(Any.CustodianId().Generate(), Journey.Start));
    }

    [Fact]
    public void A_delivered_consignment_cannot_be_handed_over() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.RecordSteady(0, 25);
        journey.Deliver(30);

        // Assert
        Refused.Because(ColdChainRefusal.NotInTransit, () => journey.Consignment.HandOverToNextLeg(Any.CustodianId().Generate(), Journey.Start.AddMinutes(40)));
    }

    [Fact]
    public void A_refused_hand_over_leaves_the_custody_as_it_was() {
        // Arrange
        Journey journey = DispatchedOverThreeLegs();

        // Act
        Assert.ThrowsAny<ColdChainRefusedException>(() => journey.Consignment.HandOverToNextLeg(Any.CustodianId().Generate(), Journey.Start.AddSeconds(-1)));

        // Assert
        LegCustody only = Assert.Single(journey.Consignment.Custody);

        Assert.True(only.IsOpen);
    }

    [Fact]
    public void A_missing_custodian_is_refused() {
        // Arrange
        Journey journey = DispatchedOverThreeLegs();

        // Assert
        Assert.Throws<ArgumentNullException>(() => journey.Consignment.HandOverToNextLeg(null!, Journey.Start.AddMinutes(30)));
    }

}
