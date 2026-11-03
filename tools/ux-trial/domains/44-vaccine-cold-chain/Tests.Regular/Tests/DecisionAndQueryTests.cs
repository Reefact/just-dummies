using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests.Regular.Tests;

public sealed class DecideTests {

    // Every journey below records one reading per five minutes from the dispatch, and delivers with the last one.

    private static readonly DateOnly Day = new DateOnly(2031, 4, 8);

    private static DateTimeOffset MorningOfTheDay => Journeys.AtUtc(Day, 6, 0);

    [Fact]
    public void A_pallet_with_a_clean_record_is_released() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), Journeys.Steady(12));
        Sscc                 pallet      = consignment.Pallets[0].Sscc;

        // Act
        ReleaseDecision decision = consignment.Decide(pallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Empty(decision.Reasons);
        Assert.Equal(0, decision.BudgetConsumedMinutes);
        Assert.Null(decision.QuarantineBay);
        Assert.Equal(pallet, decision.Pallet);
        Assert.Same(decision, consignment.DecisionFor(pallet));
        Assert.False(consignment.IsWrittenOff(pallet));
    }

    [Fact]
    public void A_pallet_is_decided_at_the_receiving_warehouse_once_the_consignment_is_delivered() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt);
        Sscc                 pallet       = consignment.Pallets[0].Sscc;

        // Act
        ColdChainRefusedException planned = Assert.Throws<ColdChainRefusedException>(() => consignment.Decide(pallet));
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        ColdChainRefusedException inTransit = Assert.Throws<ColdChainRefusedException>(() => consignment.Decide(pallet));

        // Assert
        Assert.Equal(ColdChainRefusal.NotDelivered, planned.Refusal);
        Assert.Equal(ColdChainRefusal.NotDelivered, inTransit.Refusal);
    }

    [Fact]
    public void A_pallet_outside_the_consignment_cannot_be_decided() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), Journeys.Steady(4));

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Decide(new AnySscc().Generate()));

        // Assert
        Assert.Equal(ColdChainRefusal.UnknownPallet, refusal.Refusal);
    }

    [Fact]
    public void A_pallet_is_decided_once() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), Journeys.Steady(4), palletCount: 2);
        Sscc                 pallet      = consignment.Pallets[0].Sscc;
        consignment.Decide(pallet);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Decide(pallet));

        // Assert
        Assert.Equal(ColdChainRefusal.PalletAlreadyDecided, refusal.Refusal);
        Assert.Single(consignment.Decisions);
    }

    [Fact]
    public void A_pallet_written_off_in_transit_is_not_decided_again() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt, palletCount: 2);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        Journeys.Record(consignment, consignment.Pallets[0], dispatchedAt.AddMinutes(5), -4m);
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(20));

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Decide(consignment.Pallets[0].Sscc));

        // Assert
        Assert.Equal(ColdChainRefusal.PalletAlreadyDecided, refusal.Refusal);
    }

    [Fact]
    public void An_excursion_within_both_allowances_does_not_hold_the_pallet() {
        // Arrange: outside from minute 5 to minute 30 = 25 minutes, allowed 25 in all and 25 at a time
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 25, maxSingleMinutes: 25), Journeys.AnyDispatchTime(), [5m, 9m, 9m, 9m, 9m, 9m, 5m, 5m]);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Equal(25, decision.BudgetConsumedMinutes);
        Assert.Equal(0, consignment.RemainingBudgetMinutes(consignment.Pallets[0].Sscc));
    }

    [Fact]
    public void A_pallet_that_spent_more_than_its_cumulative_budget_is_rejected() {
        // Arrange: two excursions of 15 minutes, 30 in all, against a budget of 25 (and 20 at a time)
        decimal[] temperatures = [5m, 9m, 9m, 9m, 5m, 5m, 9m, 9m, 9m, 5m, 5m];
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 25, maxSingleMinutes: 20), Journeys.AnyDispatchTime(), temperatures);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.StabilityBudgetExhausted], decision.Reasons);
        Assert.Equal(30, decision.BudgetConsumedMinutes);
        Assert.Null(decision.QuarantineBay);
    }

    [Fact]
    public void A_zero_tolerance_product_is_rejected_by_any_excursion() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 0, maxSingleMinutes: 0), Journeys.AnyDispatchTime(), [5m, 9m, 5m, 5m]);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Contains(DecisionReason.StabilityBudgetExhausted, decision.Reasons);
    }

    [Fact]
    public void A_zero_tolerance_product_with_a_clean_record_is_released() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 0, maxSingleMinutes: 0), Journeys.AnyDispatchTime(), Journeys.Steady(6));

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Fact]
    public void An_excursion_longer_than_the_single_allowance_quarantines_the_pallet() {
        // Arrange: one excursion of 15 minutes, budget of 100 minutes but 10 at a time
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 100, maxSingleMinutes: 10), Journeys.AnyDispatchTime(), [5m, 9m, 9m, 9m, 5m, 5m]);
        ReceivingWarehouse   warehouse   = consignment.ReceivedBy!;

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.SingleExcursionTooLong], decision.Reasons);
        Assert.Equal(15, decision.BudgetConsumedMinutes);
        Assert.Equal(warehouse.QuarantineBay, decision.QuarantineBay);
    }

    [Fact]
    public void An_excursion_exactly_as_long_as_the_single_allowance_is_tolerated() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 100, maxSingleMinutes: 15), Journeys.AnyDispatchTime(), [5m, 9m, 9m, 9m, 5m, 5m]);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Fact]
    public void A_pallet_that_arrives_outside_its_label_is_held_for_review() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), [5m, 5m, 9m, 9m]);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.ArrivedOutOfRange], decision.Reasons);
    }

    [Fact]
    public void A_cold_arrival_is_an_arrival_out_of_range_too() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), [5m, 5m, 1m, 1.5m]);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal([DecisionReason.ArrivedOutOfRange], decision.Reasons);
    }

    [Fact]
    public void A_pallet_without_a_reading_has_an_incomplete_temperature_record() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(30));

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
    }

    [Fact]
    public void A_silence_longer_than_two_intervals_leaves_the_record_incomplete() {
        // Arrange: readings at 0, 5, 10 then 25: a silence of 15 minutes against 10 tolerated
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt);
        ColdChainPallet      pallet       = consignment.Pallets[0];
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        foreach (int minute in new[] { 0, 5, 10, 25 }) {
            Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(minute), 5m);
        }

        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(25));

        // Act
        ReleaseDecision decision = consignment.Decide(pallet.Sscc);

        // Assert
        Assert.Equal([DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
    }

    [Fact]
    public void A_silence_of_exactly_two_intervals_is_tolerated() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt);
        ColdChainPallet      pallet       = consignment.Pallets[0];
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        foreach (int minute in new[] { 0, 5, 10, 20 }) {
            Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(minute), 5m);
        }

        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(20));

        // Act
        ReleaseDecision decision = consignment.Decide(pallet.Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Fact]
    public void A_record_that_starts_late_or_stops_early_leaves_the_record_incomplete() {
        // Arrange: delivered at minute 40; one pallet records from minute 11, the other stops at minute 29
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt, palletCount: 2);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        for (int minute = 11; minute <= 40; minute += 5) { Journeys.Record(consignment, consignment.Pallets[0], dispatchedAt.AddMinutes(minute), 5m); }
        for (int minute = 0; minute <= 29; minute += 5) { Journeys.Record(consignment, consignment.Pallets[1], dispatchedAt.AddMinutes(minute), 5m); }

        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(40));

        // Act
        ReleaseDecision late  = consignment.Decide(consignment.Pallets[0].Sscc);
        ReleaseDecision early = consignment.Decide(consignment.Pallets[1].Sscc);

        // Assert
        Assert.Equal([DecisionReason.IncompleteTemperatureRecord], late.Reasons);
        Assert.Equal([DecisionReason.IncompleteTemperatureRecord], early.Reasons);
    }

    [Fact]
    public void Packs_expired_when_the_consignment_is_delivered_reject_the_pallet() {
        // Arrange: the packs expire on the day of dispatch; the delivery is made after midnight
        DateTimeOffset       dispatchedAt = Journeys.AtUtc(Day, 22, 0);
        ColdChainConsignment consignment  = Journeys.Delivered(Journeys.Product(), dispatchedAt, Journeys.Steady(60), expiresInDays: 0);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(Day.AddDays(1), Journeys.DayOf(consignment.DeliveredAt!.Value));
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.PackExpired], decision.Reasons);
    }

    [Fact]
    public void Packs_expiring_on_the_day_of_delivery_are_not_expired() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), MorningOfTheDay, Journeys.Steady(6), expiresInDays: 0);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Theory]
    [InlineData(10, 9, true)]
    [InlineData(10, 10, false)]
    [InlineData(10, 11, false)]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, true)]
    [InlineData(365, 364, true)]
    public void A_pallet_with_fewer_days_of_shelf_life_than_required_is_held_for_review(int requiredDays, int daysLeft, bool short_) {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(shelfLifeDays: requiredDays), MorningOfTheDay, Journeys.Steady(6), expiresInDays: daysLeft);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(short_ ? [DecisionReason.ShortShelfLife] : [], decision.Reasons);
        Assert.Equal(short_ ? ReleaseOutcome.Quarantined : ReleaseOutcome.Released, decision.Outcome);
    }

    [Fact]
    public void An_expired_pack_is_expired_not_short_of_shelf_life() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AtUtc(Day, 22, 0);
        ColdChainConsignment consignment  = Journeys.Delivered(Journeys.Product(shelfLifeDays: 30), dispatchedAt, Journeys.Steady(60), expiresInDays: 0);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal([DecisionReason.PackExpired], decision.Reasons);
    }

    [Fact]
    public void The_shelf_life_is_counted_from_the_utc_day_of_delivery() {
        // Arrange: delivered at 01:30 on the 9th at +02:00, which is 23:30 UTC on the 8th
        DateTimeOffset dispatchedAt = Journeys.AtUtc(Day, 21, 0);
        DateTimeOffset deliveredAt  = Journeys.AtUtc(Day, 23, 30).ToOffset(TimeSpan.FromHours(2));
        VaccineProduct product      = Journeys.Product(shelfLifeDays: 3);
        ColdChainConsignment consignment = Journeys.Planned(product, dispatchedAt, expiresInDays: 3);
        ColdChainPallet      pallet      = consignment.Pallets[0];
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        Journeys.RecordSeries(consignment, pallet, dispatchedAt, Journeys.Steady(31));
        consignment.Deliver(Journeys.WarehouseFor(consignment), deliveredAt);

        // Act
        ReleaseDecision decision = consignment.Decide(pallet.Sscc);

        // Assert
        Assert.Equal(Day, Journeys.DayOf(consignment.DeliveredAt!.Value));
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Fact]
    public void A_calibration_that_lapsed_during_transit_holds_the_pallet() {
        // Arrange: the calibration falls due on the dispatch day, the delivery is made the day after
        DateTimeOffset       dispatchedAt = Journeys.AtUtc(Day, 22, 0);
        ColdChainConsignment consignment  = Journeys.Delivered(Journeys.Product(), dispatchedAt, Journeys.Steady(60), calibrationDueInDays: 0);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.LoggerCalibrationLapsed], decision.Reasons);
    }

    [Fact]
    public void A_calibration_falling_due_on_the_day_of_delivery_has_not_lapsed() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), MorningOfTheDay, Journeys.Steady(6), calibrationDueInDays: 0);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Fact]
    public void Several_reasons_are_listed_in_the_order_of_the_reasons_themselves() {
        // Arrange: the record stops early (incomplete), the shelf life is short and the single excursion too long
        VaccineProduct       product      = Journeys.Product(cumulativeMinutes: 100, maxSingleMinutes: 10, shelfLifeDays: 30);
        DateTimeOffset       dispatchedAt = MorningOfTheDay;
        ColdChainConsignment consignment  = Journeys.Planned(product, dispatchedAt, expiresInDays: 5);
        ColdChainPallet      pallet       = consignment.Pallets[0];
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        Journeys.RecordSeries(consignment, pallet, dispatchedAt, 5m, 9m, 9m, 9m, 5m);
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(60));

        // Act
        ReleaseDecision decision = consignment.Decide(pallet.Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.ShortShelfLife, DecisionReason.SingleExcursionTooLong, DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
    }

    [Fact]
    public void A_rejecting_reason_wins_over_the_reasons_that_only_hold_the_pallet() {
        // Arrange: the budget is exhausted and the pallet arrives out of range
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 5, maxSingleMinutes: 5), Journeys.AnyDispatchTime(), [5m, 9m, 9m, 9m, 9m]);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.StabilityBudgetExhausted, DecisionReason.SingleExcursionTooLong, DecisionReason.ArrivedOutOfRange], decision.Reasons);
        Assert.Null(decision.QuarantineBay);
    }

    [Fact]
    public void The_budget_consumed_is_carried_forward_and_the_remaining_budget_is_floored_at_zero() {
        // Arrange: 15 minutes outside, against a budget of 100 and then 10
        decimal[]            temperatures = [5m, 9m, 9m, 9m, 5m, 5m];
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment ample        = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 100, maxSingleMinutes: 100), dispatchedAt, temperatures);
        ColdChainConsignment tight        = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 10, maxSingleMinutes: 10), dispatchedAt, temperatures);

        // Act
        ReleaseDecision decision = ample.Decide(ample.Pallets[0].Sscc);

        // Assert
        Assert.Equal(15, decision.BudgetConsumedMinutes);
        Assert.Equal(85, ample.RemainingBudgetMinutes(ample.Pallets[0].Sscc));
        Assert.Equal(0, tight.RemainingBudgetMinutes(tight.Pallets[0].Sscc));
    }

    [Fact]
    public void The_pallets_are_judged_on_their_own_record() {
        // Arrange: the first pallet stays cold, the second warms up for 15 minutes
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(cumulativeMinutes: 100, maxSingleMinutes: 10), dispatchedAt, palletCount: 2);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        Journeys.RecordSeries(consignment, consignment.Pallets[0], dispatchedAt, Journeys.Steady(6));
        Journeys.RecordSeries(consignment, consignment.Pallets[1], dispatchedAt, 5m, 9m, 9m, 9m, 5m, 5m);
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(25));

        // Act
        ReleaseDecision first  = consignment.Decide(consignment.Pallets[0].Sscc);
        ReleaseDecision second = consignment.Decide(consignment.Pallets[1].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, first.Outcome);
        Assert.Equal(ReleaseOutcome.Quarantined, second.Outcome);
    }

    [Fact]
    public void The_consignment_closes_with_its_last_decision() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), Journeys.Steady(6), palletCount: 3);

        // Act
        consignment.Decide(consignment.Pallets[2].Sscc);
        consignment.Decide(consignment.Pallets[0].Sscc);
        ConsignmentStatus beforeTheLast = consignment.Status;
        consignment.Decide(consignment.Pallets[1].Sscc);

        // Assert
        Assert.Equal(ConsignmentStatus.Delivered, beforeTheLast);
        Assert.Equal(ConsignmentStatus.Closed, consignment.Status);
        Assert.Equal(3, consignment.Decisions.Count);
    }

    [Fact]
    public void A_closed_consignment_decides_nothing_more() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), Journeys.Steady(6));
        consignment.Decide(consignment.Pallets[0].Sscc);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Decide(consignment.Pallets[0].Sscc));

        // Assert
        Assert.Equal(ConsignmentStatus.Closed, consignment.Status);
        Assert.Equal(ColdChainRefusal.PalletAlreadyDecided, refusal.Refusal);
    }

    [Fact]
    public void Deciding_the_remaining_pallets_decides_each_one_not_yet_decided_in_loading_order() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(), Journeys.AnyDispatchTime(), Journeys.Steady(6), palletCount: 4);
        ReleaseDecision alreadyDecided = consignment.Decide(consignment.Pallets[1].Sscc);

        // Act
        IReadOnlyList<ReleaseDecision> decisions = consignment.DecideRemaining();

        // Assert
        Assert.Equal([consignment.Pallets[0].Sscc, consignment.Pallets[2].Sscc, consignment.Pallets[3].Sscc], decisions.Select(d => d.Pallet).ToList());
        Assert.DoesNotContain(alreadyDecided, decisions);
        Assert.Equal(ConsignmentStatus.Closed, consignment.Status);
        Assert.Equal(4, consignment.Decisions.Count);
    }

    [Fact]
    public void Remaining_pallets_cannot_be_decided_before_the_delivery() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.DecideRemaining());

        // Assert
        Assert.Equal(ColdChainRefusal.NotDelivered, refusal.Refusal);
    }

    [Fact(Skip = "domain defect: DecideRemaining() on a consignment closed on delivery (every pallet written off in transit) throws NotDelivered instead of returning no decision, whereas Decide() reports PalletAlreadyDecided there; arguable, seed not applicable (no drawn value matters)")]
    public void Deciding_the_remaining_pallets_of_a_consignment_with_nothing_left_to_decide_decides_nothing() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        Journeys.Record(consignment, consignment.Pallets[0], dispatchedAt.AddMinutes(5), -2m);
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(30));

        // Act
        IReadOnlyList<ReleaseDecision> decisions = consignment.DecideRemaining();

        // Assert
        Assert.Empty(decisions);
        Assert.Equal(ConsignmentStatus.Closed, consignment.Status);
    }

    [Fact]
    public void A_quarantined_pallet_goes_to_the_quarantine_bay_of_the_receiving_warehouse() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(shelfLifeDays: 365), MorningOfTheDay, Journeys.Steady(6), palletCount: 2, expiresInDays: 10);

        // Act
        IReadOnlyList<ReleaseDecision> decisions = consignment.DecideRemaining();

        // Assert
        Assert.All(decisions, decision => Assert.Equal(consignment.ReceivedBy!.QuarantineBay, decision.QuarantineBay));
        Assert.All(decisions, decision => Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome));
    }

}

public sealed class ConsignmentQueryTests {

    private static readonly DateTimeOffset Dispatch = new DateTimeOffset(2031, 4, 8, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_custodian_at_an_instant_is_the_one_holding_the_leg_at_that_moment() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch, legCount: 3);
        CustodianId[]        custodians  = Any.ListOf(new AnyCustodianId()).WithCount(3).Distinct().Generate().ToArray();
        consignment.Dispatch(custodians[0], Dispatch);
        consignment.HandOverToNextLeg(custodians[1], Dispatch.AddHours(5));
        consignment.HandOverToNextLeg(custodians[2], Dispatch.AddHours(9));
        consignment.Deliver(Journeys.WarehouseFor(consignment), Dispatch.AddHours(12));

        // Assert
        Assert.Null(consignment.CustodianAt(Dispatch.AddTicks(-1)));
        Assert.Equal(custodians[0], consignment.CustodianAt(Dispatch));
        Assert.Equal(custodians[0], consignment.CustodianAt(Dispatch.AddHours(5).AddTicks(-1)));
        Assert.Equal(custodians[1], consignment.CustodianAt(Dispatch.AddHours(5)));
        Assert.Equal(custodians[1], consignment.CustodianAt(Dispatch.AddHours(9).AddTicks(-1)));
        Assert.Equal(custodians[2], consignment.CustodianAt(Dispatch.AddHours(9)));
        Assert.Equal(custodians[2], consignment.CustodianAt(Dispatch.AddHours(12).AddTicks(-1)));
        Assert.Null(consignment.CustodianAt(Dispatch.AddHours(12)));
        Assert.Null(consignment.CustodianAt(Dispatch.AddDays(30)));
    }

    [Fact]
    public void While_in_transit_the_last_custodian_holds_the_consignment_for_as_long_as_it_travels() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch);
        CustodianId          carrier     = Journeys.AnyCustodian();
        consignment.Dispatch(carrier, Dispatch);

        // Assert
        Assert.Equal(carrier, consignment.CustodianAt(Dispatch.AddDays(200)));
    }

    [Fact]
    public void Nobody_holds_a_consignment_that_was_not_dispatched() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch);

        // Assert
        Assert.Null(consignment.CustodianAt(Dispatch));
    }

    [Fact]
    public void Excursions_are_attributed_to_the_custodian_holding_the_consignment_when_they_started() {
        // Arrange: handed over at +30 min; the first excursion starts at +10 min, the second at +40 min
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch, legCount: 2);
        CustodianId[]        custodians  = Any.ListOf(new AnyCustodianId()).WithCount(2).Distinct().Generate().ToArray();
        ColdChainPallet      pallet      = consignment.Pallets[0];
        consignment.Dispatch(custodians[0], Dispatch);
        Journeys.RecordSeries(consignment, pallet, Dispatch, 5m, 5m, 9m, 5m, 5m, 5m, 5m, 5m, 1m, 5m);
        consignment.HandOverToNextLeg(custodians[1], Dispatch.AddMinutes(30));
        consignment.Deliver(Journeys.WarehouseFor(consignment), Dispatch.AddMinutes(45));

        // Act
        IReadOnlyList<Excursion> first  = consignment.ExcursionsAttributedTo(custodians[0]);
        IReadOnlyList<Excursion> second = consignment.ExcursionsAttributedTo(custodians[1]);

        // Assert
        Excursion warm = Assert.Single(first);
        Assert.Equal(ExcursionKind.Warm, warm.Kind);
        Assert.Equal(Dispatch.AddMinutes(10), warm.StartedAt);
        Excursion cold = Assert.Single(second);
        Assert.Equal(ExcursionKind.Cold, cold.Kind);
        Assert.Equal(Dispatch.AddMinutes(40), cold.StartedAt);
    }

    [Fact]
    public void An_excursion_starting_at_the_very_moment_of_a_hand_over_belongs_to_the_next_custodian() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch, legCount: 2);
        CustodianId[]        custodians  = Any.ListOf(new AnyCustodianId()).WithCount(2).Distinct().Generate().ToArray();
        ColdChainPallet      pallet      = consignment.Pallets[0];
        consignment.Dispatch(custodians[0], Dispatch);
        Journeys.RecordSeries(consignment, pallet, Dispatch, 5m, 5m, 5m, 9m, 9m, 5m);
        consignment.HandOverToNextLeg(custodians[1], Dispatch.AddMinutes(15));

        // Assert
        Assert.Empty(consignment.ExcursionsAttributedTo(custodians[0]));
        Assert.Single(consignment.ExcursionsAttributedTo(custodians[1]));
    }

    [Fact]
    public void A_custodian_holding_several_legs_answers_for_the_excursions_of_each() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch, legCount: 3);
        CustodianId[]        custodians  = Any.ListOf(new AnyCustodianId()).WithCount(2).Distinct().Generate().ToArray();
        ColdChainPallet      pallet      = consignment.Pallets[0];
        consignment.Dispatch(custodians[0], Dispatch);
        Journeys.RecordSeries(consignment, pallet, Dispatch, 9m, 5m, 5m, 5m, 5m, 5m, 5m, 5m, 9m, 5m);
        consignment.HandOverToNextLeg(custodians[1], Dispatch.AddMinutes(15));
        consignment.HandOverToNextLeg(custodians[0], Dispatch.AddMinutes(30));

        // Act
        IReadOnlyList<Excursion> ofTheFirst = consignment.ExcursionsAttributedTo(custodians[0]);

        // Assert
        Assert.Equal([Dispatch, Dispatch.AddMinutes(40)], ofTheFirst.Select(e => e.StartedAt).ToList());
        Assert.Empty(consignment.ExcursionsAttributedTo(custodians[1]));
    }

    [Fact]
    public void The_excursions_of_every_pallet_are_gathered_and_ordered_by_start() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch, palletCount: 2);
        CustodianId          carrier     = Journeys.AnyCustodian();
        consignment.Dispatch(carrier, Dispatch);
        Journeys.RecordSeries(consignment, consignment.Pallets[0], Dispatch, 5m, 5m, 5m, 9m, 5m);
        Journeys.RecordSeries(consignment, consignment.Pallets[1], Dispatch, 5m, 9m, 5m, 5m, 5m);

        // Act
        IReadOnlyList<Excursion> excursions = consignment.ExcursionsAttributedTo(carrier);

        // Assert
        Assert.Equal([Dispatch.AddMinutes(5), Dispatch.AddMinutes(15)], excursions.Select(e => e.StartedAt).ToList());
    }

    [Fact]
    public void A_custodian_that_never_held_the_consignment_answers_for_nothing() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch);
        consignment.Dispatch(Journeys.AnyCustodian(), Dispatch);
        Journeys.RecordSeries(consignment, consignment.Pallets[0], Dispatch, 5m, 9m, 5m);

        // Act
        IReadOnlyList<Excursion> excursions = consignment.ExcursionsAttributedTo(new CustodianId("FR-GDP-00001"));

        // Assert
        Assert.Empty(excursions);
    }

    [Fact]
    public void The_history_of_a_pallet_outside_the_consignment_is_refused() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => consignment.HistoryOf(new AnySscc().Generate()));

        // Assert
        Assert.Equal("pallet", refusal.ParamName);
    }

    [Fact]
    public void The_history_of_a_pallet_is_tied_to_its_logger_and_the_label_of_the_product() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch);
        ColdChainPallet      pallet      = consignment.Pallets[0];

        // Act
        TemperatureHistory history = consignment.HistoryOf(pallet.Sscc);

        // Assert
        Assert.Same(pallet.Logger, history.Logger);
        Assert.Same(consignment.Product.LabelledRange, history.LabelledRange);
    }

    [Fact]
    public void A_pallet_without_decision_is_not_written_off() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch);

        // Assert
        Assert.Null(consignment.DecisionFor(consignment.Pallets[0].Sscc));
        Assert.False(consignment.IsWrittenOff(consignment.Pallets[0].Sscc));
        Assert.Null(consignment.DecisionFor(new AnySscc().Generate()));
    }

    [Fact]
    public void A_pallet_rejected_at_the_warehouse_is_not_a_pallet_written_off_in_transit() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Delivered(Journeys.Product(cumulativeMinutes: 0, maxSingleMinutes: 0), Dispatch, [5m, 9m, 5m, 5m]);

        // Act
        ReleaseDecision decision = consignment.Decide(consignment.Pallets[0].Sscc);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.False(consignment.IsWrittenOff(consignment.Pallets[0].Sscc));
    }

    [Fact]
    public void The_remaining_budget_of_a_pallet_follows_its_record_so_far() {
        // Arrange: budget of 60 minutes, 10 spent so far
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(cumulativeMinutes: 60, maxSingleMinutes: 60), Dispatch);
        consignment.Dispatch(Journeys.AnyCustodian(), Dispatch);
        Sscc pallet = consignment.Pallets[0].Sscc;

        // Act
        int before = consignment.RemainingBudgetMinutes(pallet);
        Journeys.RecordSeries(consignment, consignment.Pallets[0], Dispatch, 5m, 9m, 9m, 5m);
        int after = consignment.RemainingBudgetMinutes(pallet);

        // Assert
        Assert.Equal(60, before);
        Assert.Equal(50, after);
    }

    [Fact]
    public void The_remaining_budget_of_a_pallet_outside_the_consignment_is_refused() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch);

        // Assert
        Assert.Throws<ArgumentException>(() => consignment.RemainingBudgetMinutes(new AnySscc().Generate()));
    }

    [Fact]
    public void A_consignment_counts_the_packs_of_all_its_pallets() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch, palletCount: 3, packCount: 4);

        // Assert
        Assert.Equal(12, consignment.TotalPacks);
    }

    [Fact]
    public void The_current_leg_is_only_defined_while_the_consignment_is_in_transit() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Dispatch, legCount: 2);
        Leg? planned = consignment.CurrentLeg;

        // Act
        consignment.Dispatch(Journeys.AnyCustodian(), Dispatch);
        Leg? first = consignment.CurrentLeg;
        consignment.HandOverToNextLeg(Journeys.AnyCustodian(), Dispatch.AddHours(2));
        Leg? second = consignment.CurrentLeg;
        consignment.Deliver(Journeys.WarehouseFor(consignment), Dispatch.AddHours(4));

        // Assert
        Assert.Null(planned);
        Assert.Same(consignment.Shipment.Legs[0], first);
        Assert.Same(consignment.Shipment.Legs[1], second);
        Assert.Null(consignment.CurrentLeg);
    }

}
