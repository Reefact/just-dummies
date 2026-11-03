using ColdChain.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class DecisionTests {

    private static ColdChainRefusal RefusalOf(Action action) {
        return Assert.Throws<ColdChainRefusedException>(action).Refusal;
    }

    /// <summary>A delivered journey whose single pallet recorded the given readings (minute to temperature), the rest inside the label.</summary>
    private static Journey DeliveredWith(JourneyOptions options, Dictionary<int, decimal> overrides) {
        Journey journey = Journey.Planned(options).Dispatched();
        journey.RecordWholeTrip(journey.FirstPallet, overrides);
        journey.DeliverAtTheEnd();

        return journey;
    }

    private static Dictionary<int, decimal> WarmFor(int fromMinute, int minutes) {
        Dictionary<int, decimal> warm = new Dictionary<int, decimal>();
        for (int minute = fromMinute; minute < fromMinute + minutes; minute += 10) {
            warm[minute] = Any.Decimal().Between(8.1m, 24.9m).WithScale(1).Generate();
        }

        return warm;
    }

    [Fact]
    public void A_pallet_that_stayed_inside_the_label_with_a_full_record_is_released() {
        // Arrange
        Journey journey = DeliveredWith(new JourneyOptions(), []);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Empty(decision.Reasons);
        Assert.Equal(0, decision.BudgetConsumedMinutes);
        Assert.Null(decision.QuarantineBay);
        Assert.Same(journey.FirstPallet, decision.Pallet);
    }

    [Fact]
    public void The_decision_is_kept_with_the_consignment() {
        // Arrange
        Journey journey = DeliveredWith(new JourneyOptions(), []);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Same(decision, journey.Consignment.DecisionFor(journey.FirstPallet));
        Assert.Same(decision, Assert.Single(journey.Consignment.Decisions));
    }

    [Fact]
    public void An_excursion_within_both_allowances_costs_budget_and_still_releases() {
        // Arrange: 30 minutes outside, against 120 cumulative and 60 per excursion.
        Journey journey = DeliveredWith(new JourneyOptions(), WarmFor(100, 30));

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Equal(30, decision.BudgetConsumedMinutes);
        Assert.Equal(90, journey.Consignment.RemainingBudgetMinutes(journey.FirstPallet));
    }

    [Fact]
    public void An_excursion_exactly_as_long_as_the_single_allowance_is_tolerated() {
        // Arrange
        Journey journey = DeliveredWith(new JourneyOptions(), WarmFor(100, 60));

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Equal(60, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void An_excursion_longer_than_the_single_allowance_holds_the_pallet_in_quarantine() {
        // Arrange
        Journey journey = DeliveredWith(new JourneyOptions(), WarmFor(100, 70));

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.SingleExcursionTooLong], decision.Reasons);
        Assert.Equal(70, decision.BudgetConsumedMinutes);
        Assert.Same(journey.Warehouse.QuarantineBay, decision.QuarantineBay);
    }

    [Fact]
    public void Cold_excursions_are_charged_like_warm_ones() {
        // Arrange
        Dictionary<int, decimal> cold = new Dictionary<int, decimal> { [100] = 1.5m, [110] = 0.5m, [120] = 1.9m, [130] = 1m, [140] = 1m, [150] = 1m, [160] = 1m };
        Journey journey = DeliveredWith(new JourneyOptions(), cold);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert: the excursion runs from minute 100 to minute 170.
        Assert.Equal(70, decision.BudgetConsumedMinutes);
        Assert.Contains(DecisionReason.SingleExcursionTooLong, decision.Reasons);
    }

    [Fact]
    public void Excursions_adding_up_to_exactly_the_cumulative_allowance_are_tolerated() {
        // Arrange: two excursions of 60 minutes each, against 120 cumulative.
        Dictionary<int, decimal> overrides = WarmFor(50, 60);
        foreach ((int minute, decimal celsius) in WarmFor(200, 60)) {
            overrides[minute] = celsius;
        }
        Journey journey = DeliveredWith(new JourneyOptions(), overrides);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Equal(120, decision.BudgetConsumedMinutes);
        Assert.Equal(0, journey.Consignment.RemainingBudgetMinutes(journey.FirstPallet));
    }

    [Fact]
    public void Excursions_adding_up_beyond_the_cumulative_allowance_reject_the_pallet() {
        // Arrange: 50 + 50 + 30 minutes, none above the single allowance.
        Dictionary<int, decimal> overrides = WarmFor(50, 50);
        foreach ((int minute, decimal celsius) in WarmFor(150, 50)) { overrides[minute] = celsius; }
        foreach ((int minute, decimal celsius) in WarmFor(250, 30)) { overrides[minute] = celsius; }
        Journey journey = DeliveredWith(new JourneyOptions(), overrides);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.StabilityBudgetExhausted], decision.Reasons);
        Assert.Equal(130, decision.BudgetConsumedMinutes);
        Assert.Null(decision.QuarantineBay);
        Assert.Equal(0, journey.Consignment.RemainingBudgetMinutes(journey.FirstPallet));
    }

    [Fact]
    public void A_product_with_no_allowance_at_all_rejects_any_excursion() {
        // Arrange
        Journey journey = DeliveredWith(new JourneyOptions { CumulativeMinutes = 0, SingleMinutes = 0 }, WarmFor(100, 10));

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Contains(DecisionReason.StabilityBudgetExhausted, decision.Reasons);
        Assert.Contains(DecisionReason.SingleExcursionTooLong, decision.Reasons);
    }

    [Fact]
    public void A_record_ending_outside_the_label_is_held_for_review() {
        // Arrange: the last three readings are warm, an open excursion of 20 minutes.
        Journey journey = DeliveredWith(new JourneyOptions(), WarmFor(340, 30));

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.ArrivedOutOfRange], decision.Reasons);
        Assert.Equal(20, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void A_gap_longer_than_two_intervals_in_the_record_holds_the_pallet() {
        // Arrange: no reading between minute 100 and minute 140, a silence of 40 minutes for a 20-minute tolerance.
        Journey journey = Journey.Planned().Dispatched();
        for (int minute = 0; minute <= 100; minute += 10) { journey.Record(journey.FirstPallet, minute, 5m); }
        for (int minute = 140; minute <= 360; minute += 10) { journey.Record(journey.FirstPallet, minute, 5m); }
        journey.DeliverAtTheEnd();

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
    }

    [Fact]
    public void A_pallet_that_recorded_nothing_has_an_incomplete_record() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        journey.DeliverAtTheEnd();

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
        Assert.Equal(0, decision.BudgetConsumedMinutes);
    }

    [Theory]
    [InlineData(20, true)]
    [InlineData(30, false)]
    public void The_record_must_start_within_two_intervals_of_the_dispatch(int firstReadingMinute, bool complete) {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        for (int minute = firstReadingMinute; minute <= 360; minute += 10) { journey.Record(journey.FirstPallet, minute, 5m); }
        journey.DeliverAtTheEnd();

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(complete, !decision.Reasons.Contains(DecisionReason.IncompleteTemperatureRecord));
    }

    [Theory]
    [InlineData(340, true)]
    [InlineData(330, false)]
    public void The_record_must_end_within_two_intervals_of_the_delivery(int lastReadingMinute, bool complete) {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        for (int minute = 0; minute <= lastReadingMinute; minute += 10) { journey.Record(journey.FirstPallet, minute, 5m); }
        journey.DeliverAtTheEnd();

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(complete, !decision.Reasons.Contains(DecisionReason.IncompleteTemperatureRecord));
    }

    [Fact]
    public void A_pack_expired_by_the_day_of_delivery_rejects_the_pallet() {
        // Arrange: dispatched on 2 March, delivered on 4 March, packs expiring on 3 March.
        JourneyOptions options = new JourneyOptions { IntervalMinutes = 60, DurationMinutes = 2880, PackExpiresOn = new DateOnly(2026, 3, 3) };
        Journey        journey = DeliveredWith(options, []);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.PackExpired], decision.Reasons);
    }

    [Fact]
    public void A_pack_expiring_on_the_day_of_delivery_is_not_expired() {
        // Arrange
        JourneyOptions options = new JourneyOptions { IntervalMinutes = 60, DurationMinutes = 2880, PackExpiresOn = new DateOnly(2026, 3, 4) };
        Journey        journey = DeliveredWith(options, []);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Theory]
    [InlineData("2026-04-03", true)]
    [InlineData("2026-04-04", true)]
    [InlineData("2026-04-02", false)]
    public void The_shelf_life_left_on_the_day_of_delivery_must_reach_the_requirement(string expiryDay, bool enough) {
        // Arrange: 30 days required on receipt, on 4 March.
        JourneyOptions options = new JourneyOptions { IntervalMinutes = 60, DurationMinutes = 2880, ShelfLifeDays = 30, PackExpiresOn = DateOnly.Parse(expiryDay) };
        Journey        journey = DeliveredWith(options, []);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        if (enough) {
            Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        } else {
            Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
            Assert.Equal([DecisionReason.ShortShelfLife], decision.Reasons);
        }
    }

    [Fact]
    public void An_expired_pack_is_not_also_reported_as_short_of_shelf_life() {
        // Arrange
        JourneyOptions options = new JourneyOptions { IntervalMinutes = 60, DurationMinutes = 2880, ShelfLifeDays = 30, PackExpiresOn = new DateOnly(2026, 3, 3) };
        Journey        journey = DeliveredWith(options, []);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal([DecisionReason.PackExpired], decision.Reasons);
    }

    [Theory]
    [InlineData("2026-03-04", false)]
    [InlineData("2026-03-03", true)]
    public void A_logger_must_still_be_calibrated_on_the_day_of_delivery_the_due_day_included(string dueDay, bool lapsed) {
        // Arrange: dispatched on 2 March, delivered on 4 March.
        JourneyOptions options = new JourneyOptions { IntervalMinutes = 60, DurationMinutes = 2880, CalibrationDueOn = DateOnly.Parse(dueDay) };
        Journey        journey = DeliveredWith(options, []);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        if (lapsed) {
            Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
            Assert.Equal([DecisionReason.LoggerCalibrationLapsed], decision.Reasons);
        } else {
            Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        }
    }

    [Fact]
    public void Reasons_pile_up_in_the_order_of_the_enumeration_and_the_worst_decides_the_outcome() {
        // Arrange: a 70-minute excursion (too long for one) that ends open, a short shelf life, and a calibration that lapsed.
        JourneyOptions options = new JourneyOptions {
            IntervalMinutes   = 60,
            DurationMinutes   = 2880,
            ShelfLifeDays     = 30,
            PackExpiresOn     = new DateOnly(2026, 3, 20),
            CalibrationDueOn  = new DateOnly(2026, 3, 3)
        };
        Dictionary<int, decimal> overrides = new Dictionary<int, decimal> { [2820] = 12m, [2880] = 12m };
        Journey journey = DeliveredWith(options, overrides);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.ShortShelfLife, DecisionReason.ArrivedOutOfRange, DecisionReason.LoggerCalibrationLapsed], decision.Reasons);
        Assert.Same(journey.Warehouse.QuarantineBay, decision.QuarantineBay);
    }

    [Fact]
    public void A_rejecting_reason_beside_holding_ones_still_rejects() {
        // Arrange: an exhausted budget and an incomplete record.
        Dictionary<int, decimal> overrides = WarmFor(50, 50);
        foreach ((int minute, decimal celsius) in WarmFor(150, 50)) { overrides[minute] = celsius; }
        foreach ((int minute, decimal celsius) in WarmFor(250, 30)) { overrides[minute] = celsius; }
        Journey journey = Journey.Planned().Dispatched();
        for (int minute = 0; minute <= 360; minute += 10) {
            if (minute is >= 300 and <= 330) { continue; }
            journey.Record(journey.FirstPallet, minute, overrides.TryGetValue(minute, out decimal celsius) ? celsius : 5m);
        }
        journey.DeliverAtTheEnd();

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.FirstPallet);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.StabilityBudgetExhausted, DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
        Assert.Null(decision.QuarantineBay);
    }

    [Fact]
    public void A_pallet_cannot_be_decided_while_the_consignment_is_in_transit() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        journey.RecordWholeTrip(journey.FirstPallet);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Decide(journey.FirstPallet));

        // Assert
        Assert.Equal(ColdChainRefusal.NotDelivered, refusal);
        Assert.Null(journey.Consignment.DecisionFor(journey.FirstPallet));
    }

    [Fact]
    public void A_pallet_cannot_be_decided_before_the_dispatch() {
        // Arrange
        Journey journey = Journey.Planned();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Decide(journey.FirstPallet));

        // Assert
        Assert.Equal(ColdChainRefusal.NotDelivered, refusal);
    }

    [Fact]
    public void A_pallet_outside_the_consignment_cannot_be_decided() {
        // Arrange
        Journey journey = DeliveredWith(new JourneyOptions(), []);
        Sscc    stranger = new AnySscc().DifferentFrom(journey.FirstPallet).Generate();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Decide(stranger));

        // Assert
        Assert.Equal(ColdChainRefusal.UnknownPallet, refusal);
    }

    [Fact]
    public void A_pallet_is_decided_once() {
        // Arrange
        Journey journey = DeliveredWith(new JourneyOptions { Pallets = 2 }, []);
        journey.Consignment.Decide(journey.FirstPallet);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Decide(journey.FirstPallet));

        // Assert
        Assert.Equal(ColdChainRefusal.PalletAlreadyDecided, refusal);
    }

    [Fact]
    public void A_pallet_is_required_to_decide() {
        // Arrange
        Journey journey = DeliveredWith(new JourneyOptions(), []);

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.Decide(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void The_consignment_stays_delivered_until_every_pallet_is_decided_and_then_closes() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 3 }).Dispatched();
        journey.RecordWholeTripOfEveryPallet();
        journey.DeliverAtTheEnd();

        // Act and assert
        journey.Consignment.Decide(journey.Consignment.Pallets[0].Sscc);
        Assert.Equal(ConsignmentStatus.Delivered, journey.Consignment.Status);
        journey.Consignment.Decide(journey.Consignment.Pallets[1].Sscc);
        Assert.Equal(ConsignmentStatus.Delivered, journey.Consignment.Status);
        journey.Consignment.Decide(journey.Consignment.Pallets[2].Sscc);
        Assert.Equal(ConsignmentStatus.Closed, journey.Consignment.Status);
        Assert.Equal(3, journey.Consignment.Decisions.Count);
    }

    [Fact]
    public void Deciding_the_remaining_pallets_decides_those_not_decided_in_loading_order() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 4 }).Dispatched();
        journey.RecordWholeTripOfEveryPallet();
        journey.DeliverAtTheEnd();
        Sscc alreadyDecided = journey.Consignment.Pallets[1].Sscc;
        ReleaseDecision first = journey.Consignment.Decide(alreadyDecided);

        // Act
        IReadOnlyList<ReleaseDecision> decisions = journey.Consignment.DecideRemaining();

        // Assert
        Assert.Equal(
            [journey.Consignment.Pallets[0].Sscc, journey.Consignment.Pallets[2].Sscc, journey.Consignment.Pallets[3].Sscc],
            decisions.Select(d => d.Pallet));
        Assert.DoesNotContain(first, decisions);
        Assert.Equal(ConsignmentStatus.Closed, journey.Consignment.Status);
        Assert.Equal(4, journey.Consignment.Decisions.Count);
    }

    [Fact]
    public void Deciding_the_remaining_pallets_when_none_is_left_decides_nothing() {
        // Arrange
        Journey journey = DeliveredWith(new JourneyOptions(), []);
        journey.Consignment.Decide(journey.FirstPallet);

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.DecideRemaining());

        // Assert: a closed consignment is not Delivered any more.
        ColdChainRefusedException refusal = Assert.IsType<ColdChainRefusedException>(thrown);
        Assert.Equal(ColdChainRefusal.NotDelivered, refusal.Refusal);
    }

    [Fact]
    public void Deciding_the_remaining_pallets_before_delivery_is_refused() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.DecideRemaining());

        // Assert
        Assert.Equal(ColdChainRefusal.NotDelivered, refusal);
    }

    [Fact]
    public void Each_pallet_is_decided_on_its_own_record() {
        // Arrange: the first pallet stays cold-chain clean, the second one has a 70-minute excursion.
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 2 }).Dispatched();
        Sscc    clean   = journey.Consignment.Pallets[0].Sscc;
        Sscc    warm    = journey.Consignment.Pallets[1].Sscc;
        journey.RecordWholeTrip(clean);
        journey.RecordWholeTrip(warm, WarmFor(100, 70));
        journey.DeliverAtTheEnd();

        // Act
        IReadOnlyList<ReleaseDecision> decisions = journey.Consignment.DecideRemaining();

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decisions[0].Outcome);
        Assert.Equal(ReleaseOutcome.Quarantined, decisions[1].Outcome);
    }

}
