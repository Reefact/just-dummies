using ColdChain.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class ConsignmentQueryTests {

    [Fact]
    public void The_history_of_a_pallet_outside_the_consignment_is_refused() {
        // Arrange
        Journey journey  = Journey.Planned();
        Sscc    stranger = new AnySscc().DifferentFrom(journey.FirstPallet).Generate();

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.HistoryOf(stranger));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("pallet", refusal.ParamName);
    }

    [Fact]
    public void A_pallet_is_required_by_every_query() {
        // Arrange
        Journey journey = Journey.Planned();

        // Assert
        Assert.Throws<ArgumentNullException>(() => journey.Consignment.HistoryOf(null!));
        Assert.Throws<ArgumentNullException>(() => journey.Consignment.DecisionFor(null!));
        Assert.Throws<ArgumentNullException>(() => journey.Consignment.ExcursionsAttributedTo(null!));
    }

    [Fact]
    public void A_pallet_with_no_decision_has_none_and_is_not_written_off() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();

        // Assert
        Assert.Null(journey.Consignment.DecisionFor(journey.FirstPallet));
        Assert.False(journey.Consignment.IsWrittenOff(journey.FirstPallet));
    }

    [Fact]
    public void A_rejection_after_delivery_is_not_a_write_off() {
        // Arrange: 130 minutes outside the label exhaust the budget at delivery, nothing breaches a hard limit.
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        Dictionary<int, decimal> overrides = new Dictionary<int, decimal>();
        for (int minute = 100; minute < 230; minute += 10) { overrides[minute] = 12m; }
        journey.RecordWholeTrip(pallet, overrides);
        journey.DeliverAtTheEnd();

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(pallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.False(journey.Consignment.IsWrittenOff(pallet));
    }

    [Fact]
    public void An_unknown_pallet_is_not_written_off() {
        // Arrange
        Journey journey  = Journey.Planned();
        Sscc    stranger = new AnySscc().DifferentFrom(journey.FirstPallet).Generate();

        // Assert
        Assert.Null(journey.Consignment.DecisionFor(stranger));
        Assert.False(journey.Consignment.IsWrittenOff(stranger));
    }

    [Theory]
    [InlineData(0, 120)]
    [InlineData(30, 90)]
    [InlineData(120, 0)]
    public void The_remaining_budget_is_what_the_excursions_so_far_left_of_the_cumulative_allowance(int minutesOutside, int remaining) {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        journey.Record(pallet, 0, 5m);
        if (minutesOutside > 0) {
            journey.Record(pallet, 10, 12m);
            journey.Record(pallet, 10 + minutesOutside, 5m);
        }

        // Act
        int actual = journey.Consignment.RemainingBudgetMinutes(pallet);

        // Assert
        Assert.Equal(remaining, actual);
    }

    [Fact]
    public void The_remaining_budget_never_goes_below_zero() {
        // Arrange: 200 minutes outside against 120.
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        journey.Record(pallet, 0, 5m);
        journey.Record(pallet, 10, 12m);
        journey.Record(pallet, 210, 5m);

        // Act
        int remaining = journey.Consignment.RemainingBudgetMinutes(pallet);

        // Assert
        Assert.Equal(0, remaining);
    }

    [Fact]
    public void The_remaining_budget_of_a_pallet_outside_the_consignment_is_refused() {
        // Arrange
        Journey journey  = Journey.Planned();
        Sscc    stranger = new AnySscc().DifferentFrom(journey.FirstPallet).Generate();

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.RemainingBudgetMinutes(stranger));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Nobody_holds_the_consignment_before_the_dispatch() {
        // Arrange
        Journey journey = Journey.Planned();

        // Assert
        Assert.Null(journey.Consignment.CustodianAt(Journey.DispatchedAt));
    }

    [Fact]
    public void The_custodian_at_an_instant_is_the_one_whose_custody_covers_it() {
        // Arrange
        Journey     journey   = Journey.Planned(new JourneyOptions { Legs = 3 }).Dispatched();
        CustodianId forwarder = new AnyCustodianId().DifferentFrom(journey.Carrier).Generate();
        CustodianId last      = new AnyCustodianId().DifferentFrom(forwarder).Generate();
        journey.Consignment.HandOverToNextLeg(forwarder, Journey.DispatchedAt.AddMinutes(100));
        journey.Consignment.HandOverToNextLeg(last, Journey.DispatchedAt.AddMinutes(200));
        journey.Consignment.Deliver(journey.Warehouse, Journey.DispatchedAt.AddMinutes(300));

        // Assert
        Assert.Null(journey.Consignment.CustodianAt(Journey.DispatchedAt.AddTicks(-1)));
        Assert.Same(journey.Carrier, journey.Consignment.CustodianAt(Journey.DispatchedAt));
        Assert.Same(journey.Carrier, journey.Consignment.CustodianAt(Journey.DispatchedAt.AddMinutes(99)));
        Assert.Same(forwarder, journey.Consignment.CustodianAt(Journey.DispatchedAt.AddMinutes(100)));
        Assert.Same(forwarder, journey.Consignment.CustodianAt(Journey.DispatchedAt.AddMinutes(199)));
        Assert.Same(last, journey.Consignment.CustodianAt(Journey.DispatchedAt.AddMinutes(200)));
        Assert.Same(last, journey.Consignment.CustodianAt(Journey.DispatchedAt.AddMinutes(299)));
        Assert.Null(journey.Consignment.CustodianAt(Journey.DispatchedAt.AddMinutes(300)));
    }

    [Fact]
    public void The_custodian_at_an_instant_is_found_whatever_the_offset_it_is_written_with() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();

        // Act
        CustodianId? custodian = journey.Consignment.CustodianAt(Journey.DispatchedAt.ToOffset(TimeSpan.FromHours(-5)).AddMinutes(30));

        // Assert
        Assert.Same(journey.Carrier, custodian);
    }

    [Fact]
    public void Excursions_are_attributed_to_the_custodian_holding_the_consignment_when_they_started() {
        // Arrange: the carrier holds the first 100 minutes, the forwarder the rest.
        Journey     journey   = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();
        CustodianId forwarder = new AnyCustodianId().DifferentFrom(journey.Carrier).Generate();
        Sscc        pallet    = journey.FirstPallet;
        journey.Record(pallet, 0, 5m);
        journey.Record(pallet, 50, 12m);   // starts in the carrier's custody, ends in the forwarder's
        journey.Record(pallet, 110, 5m);
        journey.Consignment.HandOverToNextLeg(forwarder, Journey.DispatchedAt.AddMinutes(100));
        journey.Record(pallet, 150, 1m);   // starts in the forwarder's custody
        journey.Record(pallet, 170, 5m);

        // Act
        IReadOnlyList<Excursion> carrierFault   = journey.Consignment.ExcursionsAttributedTo(journey.Carrier);
        IReadOnlyList<Excursion> forwarderFault = journey.Consignment.ExcursionsAttributedTo(forwarder);

        // Assert
        Excursion warm = Assert.Single(carrierFault);
        Assert.Equal(ExcursionKind.Warm, warm.Kind);
        Assert.Equal(Journey.DispatchedAt.AddMinutes(50), warm.StartedAt);
        Excursion cold = Assert.Single(forwarderFault);
        Assert.Equal(ExcursionKind.Cold, cold.Kind);
        Assert.Equal(Journey.DispatchedAt.AddMinutes(150), cold.StartedAt);
    }

    [Fact]
    public void A_custodian_who_held_two_legs_answers_for_both() {
        // Arrange
        Journey     journey   = Journey.Planned(new JourneyOptions { Legs = 3 }).Dispatched();
        CustodianId forwarder = new AnyCustodianId().DifferentFrom(journey.Carrier).Generate();
        Sscc        pallet    = journey.FirstPallet;
        journey.Record(pallet, 0, 5m);
        journey.Record(pallet, 10, 12m);
        journey.Record(pallet, 20, 5m);
        journey.Consignment.HandOverToNextLeg(forwarder, Journey.DispatchedAt.AddMinutes(100));
        journey.Consignment.HandOverToNextLeg(journey.Carrier, Journey.DispatchedAt.AddMinutes(200));
        journey.Record(pallet, 250, 12m);
        journey.Record(pallet, 260, 5m);

        // Act
        IReadOnlyList<Excursion> excursions = journey.Consignment.ExcursionsAttributedTo(journey.Carrier);

        // Assert
        Assert.Equal(2, excursions.Count);
        Assert.True(excursions[0].StartedAt < excursions[1].StartedAt);
        Assert.Empty(journey.Consignment.ExcursionsAttributedTo(forwarder));
    }

    [Fact]
    public void Excursions_of_every_pallet_are_gathered_in_the_order_they_started() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 2 }).Dispatched();
        Sscc    first   = journey.Consignment.Pallets[0].Sscc;
        Sscc    second  = journey.Consignment.Pallets[1].Sscc;
        journey.Record(first, 0, 5m);
        journey.Record(first, 80, 12m);
        journey.Record(first, 90, 5m);
        journey.Record(second, 0, 5m);
        journey.Record(second, 30, 12m);
        journey.Record(second, 40, 5m);

        // Act
        IReadOnlyList<Excursion> excursions = journey.Consignment.ExcursionsAttributedTo(journey.Carrier);

        // Assert
        Assert.Equal([Journey.DispatchedAt.AddMinutes(30), Journey.DispatchedAt.AddMinutes(80)], excursions.Select(e => e.StartedAt));
    }

    [Fact]
    public void A_custodian_who_never_held_the_consignment_answers_for_nothing() {
        // Arrange
        Journey     journey  = Journey.Planned().Dispatched();
        CustodianId stranger = new AnyCustodianId().DifferentFrom(journey.Carrier).Generate();
        journey.Record(journey.FirstPallet, 0, 5m);
        journey.Record(journey.FirstPallet, 10, 12m);

        // Assert
        Assert.Empty(journey.Consignment.ExcursionsAttributedTo(stranger));
    }

    [Fact]
    public void An_excursion_starting_exactly_at_a_hand_over_belongs_to_the_new_custodian() {
        // Arrange
        Journey     journey   = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();
        CustodianId forwarder = new AnyCustodianId().DifferentFrom(journey.Carrier).Generate();
        Sscc        pallet    = journey.FirstPallet;
        journey.Record(pallet, 0, 5m);
        journey.Record(pallet, 90, 5m);
        journey.Consignment.HandOverToNextLeg(forwarder, Journey.DispatchedAt.AddMinutes(100));
        journey.Record(pallet, 100, 12m);
        journey.Record(pallet, 110, 5m);

        // Assert
        Assert.Empty(journey.Consignment.ExcursionsAttributedTo(journey.Carrier));
        Assert.Single(journey.Consignment.ExcursionsAttributedTo(forwarder));
    }

    [Fact]
    public void The_current_leg_is_only_known_while_the_consignment_is_in_transit() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 2 });

        // Assert
        Assert.Null(journey.Consignment.CurrentLeg);
        journey.Consignment.Dispatch(journey.Carrier, Journey.DispatchedAt);
        Assert.Same(journey.Consignment.Shipment.Legs[0], journey.Consignment.CurrentLeg);
        journey.Consignment.HandOverToNextLeg(journey.Carrier, Journey.DispatchedAt.AddHours(2));
        Assert.Same(journey.Consignment.Shipment.Legs[1], journey.Consignment.CurrentLeg);
        journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt);
        Assert.Null(journey.Consignment.CurrentLeg);
    }

    [Fact]
    public void The_total_of_packs_adds_up_every_pallet() {
        // Arrange
        ColdChainConsignment anyConsignment = new AnyColdChainConsignment().WithPalletCount(4).Generate();

        // Assert
        Assert.Equal(anyConsignment.Pallets.Sum(p => p.Packs.Count), anyConsignment.TotalPacks);
    }

}
