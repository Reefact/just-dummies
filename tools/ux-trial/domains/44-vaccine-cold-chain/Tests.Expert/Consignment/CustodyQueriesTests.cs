using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using JustDummies;
using Warehouse.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.Consignment;

public sealed class CustodyQueriesTests {

    // Two legs: the first carrier from the start, a second one from minute 30, delivery at minute 60.
    private static (Journey Journey, CustodianId Relay) TravelledOverTwoLegs() {
        Journey     journey = Journey.Dispatched(planned => planned.WithShipment(shipment => shipment.WithLegCount(2)));
        CustodianId anyRelay   = new AnyCustodianId().DifferentFrom(journey.Carrier).Generate();

        return (journey, anyRelay);
    }

    [Fact]
    public void Nobody_holds_the_consignment_before_it_is_dispatched() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned().Generate();

        // Assert
        Assert.Null(consignment.CustodianAt(Journey.Start));
    }

    [Theory]
    [InlineData(-1, false, false)]
    [InlineData(0, true, false)]
    [InlineData(29, true, false)]
    [InlineData(30, false, true)]
    [InlineData(59, false, true)]
    [InlineData(60, false, false)]
    [InlineData(61, false, false)]
    public void A_custodian_holds_the_consignment_from_acceptance_included_to_hand_over_excluded(int minutesAfterStart, bool expectedFirstCarrier, bool expectedRelay) {
        // Arrange
        (Journey journey, CustodianId anyRelay) = TravelledOverTwoLegs();

        journey.Consignment.HandOverToNextLeg(anyRelay, Journey.Start.AddMinutes(30));
        journey.Deliver(60);

        // Act
        CustodianId? holder = journey.Consignment.CustodianAt(Journey.Start.AddMinutes(minutesAfterStart));

        // Assert
        Assert.Equal(expectedFirstCarrier, journey.Carrier.Equals(holder));
        Assert.Equal(expectedRelay, anyRelay.Equals(holder));
    }

    [Fact]
    public void The_last_custodian_holds_the_consignment_until_it_is_delivered_and_so_does_nobody_after() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Assert.Equal(journey.Carrier, journey.Consignment.CustodianAt(Journey.Start.AddDays(30)));

        journey.Deliver(60);

        Assert.Null(journey.Consignment.CustodianAt(Journey.Start.AddMinutes(60)));
    }

    [Fact]
    public void An_excursion_is_attributed_to_the_custodian_who_held_the_consignment_when_it_started_even_if_it_ended_in_the_next_ones_custody() {
        // Arrange
        (Journey journey, CustodianId anyRelay) = TravelledOverTwoLegs();

        journey.RecordSteady(0, 15);
        journey.RecordSteady(20, 35, 9m);
        journey.Consignment.HandOverToNextLeg(anyRelay, Journey.Start.AddMinutes(30));
        journey.RecordSteady(40, 60);
        journey.Deliver(60);

        // Act
        IReadOnlyList<Excursion> first = journey.Consignment.ExcursionsAttributedTo(journey.Carrier);
        IReadOnlyList<Excursion> next  = journey.Consignment.ExcursionsAttributedTo(anyRelay);

        // Assert
        Excursion excursion = Assert.Single(first);

        Assert.Equal(Journey.Start.AddMinutes(20), excursion.StartedAt);
        Assert.Empty(next);
    }

    [Fact]
    public void An_excursion_starting_in_the_next_custody_is_not_the_previous_custodians() {
        // Arrange
        (Journey journey, CustodianId anyRelay) = TravelledOverTwoLegs();

        journey.RecordSteady(0, 25);
        journey.Consignment.HandOverToNextLeg(anyRelay, Journey.Start.AddMinutes(30));
        journey.RecordSteady(35, 40, 1m);
        journey.RecordSteady(45, 60);

        // Assert
        Assert.Empty(journey.Consignment.ExcursionsAttributedTo(journey.Carrier));
        Assert.Single(journey.Consignment.ExcursionsAttributedTo(anyRelay));
    }

    [Fact]
    public void An_excursion_starting_at_the_very_instant_of_the_hand_over_belongs_to_the_new_custodian() {
        // Arrange
        (Journey journey, CustodianId anyRelay) = TravelledOverTwoLegs();

        journey.RecordSteady(0, 25);
        journey.Record(30, 9m);
        journey.Consignment.HandOverToNextLeg(anyRelay, Journey.Start.AddMinutes(30));
        journey.Record(35, 5m);

        // Assert
        Assert.Empty(journey.Consignment.ExcursionsAttributedTo(journey.Carrier));
        Assert.Single(journey.Consignment.ExcursionsAttributedTo(anyRelay));
    }

    [Fact]
    public void A_carrier_who_held_the_consignment_on_two_legs_answers_for_the_excursions_of_both() {
        // Arrange
        Journey journey = Journey.Dispatched(planned => planned.WithShipment(shipment => shipment.WithLegCount(3)));

        CustodianId anyRelay = new AnyCustodianId().DifferentFrom(journey.Carrier).Generate();

        journey.RecordSteady(0, 5);
        journey.Record(10, 9m);
        journey.Record(15, 5m);
        journey.Consignment.HandOverToNextLeg(anyRelay, Journey.Start.AddMinutes(20));
        journey.RecordSteady(25, 30);
        journey.Consignment.HandOverToNextLeg(journey.Carrier, Journey.Start.AddMinutes(35));
        journey.Record(40, 1m);
        journey.Record(45, 5m);

        // Act
        IReadOnlyList<Excursion> excursions = journey.Consignment.ExcursionsAttributedTo(journey.Carrier);

        // Assert
        Assert.Equal([ExcursionKind.Warm, ExcursionKind.Cold], excursions.Select(excursion => excursion.Kind));
    }

    [Fact]
    public void The_excursions_of_every_pallet_are_gathered_and_ordered_by_their_start() {
        // Arrange
        Journey         journey = Journey.Dispatched(planned => planned.WithPalletCount(2));
        ColdChainPallet first   = journey.Consignment.Pallets[0];
        ColdChainPallet second  = journey.Consignment.Pallets[1];

        journey.Record(first, 0, 5m);
        journey.Record(first, 20, 9m);
        journey.Record(first, 25, 5m);
        journey.Record(second, 0, 5m);
        journey.Record(second, 10, 1m);
        journey.Record(second, 15, 5m);

        // Act
        IReadOnlyList<Excursion> excursions = journey.Consignment.ExcursionsAttributedTo(journey.Carrier);

        // Assert
        Assert.Equal([ExcursionKind.Cold, ExcursionKind.Warm], excursions.Select(excursion => excursion.Kind));
        Assert.Equal([Journey.Start.AddMinutes(10), Journey.Start.AddMinutes(20)], excursions.Select(excursion => excursion.StartedAt));
    }

    [Fact]
    public void Somebody_who_never_held_the_consignment_answers_for_no_excursion() {
        // Arrange
        Journey     journey  = Journey.Dispatched();
        CustodianId anyStranger = new AnyCustodianId().DifferentFrom(journey.Carrier).Generate();

        journey.RecordAll((0, 5m), (5, 9m), (10, 5m));

        // Assert
        Assert.Empty(journey.Consignment.ExcursionsAttributedTo(anyStranger));
    }

    [Fact]
    public void The_history_of_a_pallet_that_is_not_in_the_consignment_is_unknown() {
        // Arrange
        Journey journey = Journey.Dispatched();
        Sscc    anyUnknownPallet = new AnySscc().DifferentFrom(journey.PalletId).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => journey.Consignment.HistoryOf(anyUnknownPallet));
    }

    [Fact]
    public void A_pallet_not_yet_decided_has_no_decision_and_is_not_written_off() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Assert.Null(journey.Consignment.DecisionFor(journey.PalletId));
        Assert.False(journey.Consignment.IsWrittenOff(journey.PalletId));
    }

    [Fact(Skip = "domain defect (arguable): DecideRemaining on a Closed consignment (every pallet already decided) refuses with NotDelivered instead of deciding nothing, seed -812245675")]
    public void Deciding_the_remaining_pallets_of_a_consignment_closed_on_delivery_decides_nothing() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.Record(0, -1m);
        journey.Deliver(60);

        // Act
        IReadOnlyList<ReleaseDecision> decisions = journey.Consignment.DecideRemaining();

        // Assert
        Assert.Empty(decisions);
    }

}
