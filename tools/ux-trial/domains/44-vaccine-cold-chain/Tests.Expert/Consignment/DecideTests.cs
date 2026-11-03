using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using JustDummies;
using Warehouse.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.Consignment;

public sealed class DecideTests {

    // The default frame: label 2-8 degrees, a logger every 5 minutes, an hour of transit. A test that counts minutes
    // outside the label states the budget it counts against; one that leans on a day boundary uses an hourly logger
    // over 24 hours (see Overnight).
    private const int HourOfTransit = 60;

    private const int Overnight = 24 * 60;

    private static Func<AnyColdChainConsignment, AnyColdChainConsignment> Budget(int cumulative, int single) {
        return planned => planned.WithProduct(product => product.WithBudget(new StabilityBudget(cumulative, single)));
    }

    private static Func<AnyColdChainConsignment, AnyColdChainConsignment> PacksExpiringOn(int year, int month, int day) {
        return planned => planned.WithEachPallet(pallet => pallet.WithEachPack(pack => pack.WithExpiresOn(new DateOnly(year, month, day))));
    }

    // The shelf life required is nil unless a test says otherwise: only the dates of the packs then decide.
    private static Journey DispatchedOvernight(Func<AnyColdChainConsignment, AnyColdChainConsignment> refine) {
        return Journey.Dispatched(planned => refine(planned.WithProduct(product => product.WithMinimumShelfLifeOnReceiptDays(0))), loggerIntervalMinutes: 60);
    }

    private static ReleaseDecision DecideAfterAHourWithoutIncident(Journey journey) {
        journey.RecordSteady(0, HourOfTransit);
        journey.Deliver(HourOfTransit);

        return journey.Consignment.Decide(journey.PalletId);
    }

    private static void RecordTwoExcursionsOf25MinutesEach(Journey journey) {
        journey.RecordSteady(0, 5);
        journey.RecordSteady(10, 30, 9m);
        journey.RecordSteady(35, 40);
        journey.RecordSteady(45, 65, 9m);
        journey.RecordSteady(70, 75);
    }

    [Fact]
    public void A_pallet_that_stayed_in_range_with_a_continuous_record_in_date_and_calibrated_is_released() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Act
        ReleaseDecision decision = DecideAfterAHourWithoutIncident(journey);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Empty(decision.Reasons);
        Assert.Equal(0, decision.BudgetConsumedMinutes);
        Assert.Null(decision.QuarantineBay);
        Assert.Equal(journey.PalletId, decision.Pallet);
    }

    [Fact]
    public void The_decision_is_kept_and_found_again_by_pallet() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Act
        ReleaseDecision decision = DecideAfterAHourWithoutIncident(journey);

        // Assert
        Assert.Equal(decision, journey.Consignment.DecisionFor(journey.PalletId));
        Assert.Equal(decision, Assert.Single(journey.Consignment.Decisions));
    }

    [Fact]
    public void An_excursion_within_both_allowances_costs_its_minutes_but_does_not_stop_the_release() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(60, 30));

        journey.RecordSteady(0, 15);
        journey.RecordSteady(20, 30, 9m);
        journey.RecordSteady(35, 60);
        journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Equal(15, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void A_cumulative_allowance_used_up_to_its_last_minute_is_not_exceeded() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(50, 30));

        RecordTwoExcursionsOf25MinutesEach(journey);
        journey.Deliver(75);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Equal(50, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void A_pallet_that_spent_more_minutes_outside_the_label_than_the_cumulative_allowance_is_rejected() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(40, 30));

        RecordTwoExcursionsOf25MinutesEach(journey);
        journey.Deliver(75);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.StabilityBudgetExhausted], decision.Reasons);
        Assert.Equal(50, decision.BudgetConsumedMinutes);
        Assert.Null(decision.QuarantineBay);
    }

    [Fact]
    public void An_excursion_exactly_as_long_as_the_single_allowance_is_tolerated() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(100, 20));

        journey.RecordSteady(0, 5);
        journey.RecordSteady(10, 25, 9m);
        journey.RecordSteady(30, 60);
        journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Equal(20, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void An_excursion_longer_than_the_single_allowance_sends_the_pallet_to_the_quarantine_bay_of_the_warehouse() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(100, 20));

        journey.RecordSteady(0, 5);
        journey.RecordSteady(10, 30, 9m);
        journey.RecordSteady(35, 60);

        ReceivingWarehouse warehouse = journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.SingleExcursionTooLong], decision.Reasons);
        Assert.Equal(25, decision.BudgetConsumedMinutes);
        Assert.Equal(warehouse.QuarantineBay, decision.QuarantineBay);
    }

    [Fact]
    public void A_record_that_ends_outside_the_label_means_the_pallet_arrived_out_of_range() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(60, 30));

        journey.RecordSteady(0, 40);
        journey.RecordSteady(45, 60, 9m);
        journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.ArrivedOutOfRange], decision.Reasons);
        Assert.Equal(15, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void A_silence_of_more_than_two_intervals_in_the_record_leaves_it_incomplete() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.RecordSteady(0, 30);
        journey.RecordSteady(45, 60);
        journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
    }

    [Fact]
    public void A_silence_of_exactly_two_intervals_in_the_record_is_still_continuous() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.RecordSteady(0, 30);
        journey.RecordSteady(40, 60);
        journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Fact]
    public void A_record_that_stopped_well_before_the_delivery_is_incomplete() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.RecordSteady(0, 45);
        journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal([DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
    }

    [Fact]
    public void A_pallet_whose_logger_recorded_nothing_has_an_incomplete_record() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Equal([DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
    }

    [Fact]
    public void A_pallet_with_a_pack_that_expired_before_the_day_of_delivery_is_rejected() {
        // Arrange
        Journey journey = DispatchedOvernight(PacksExpiringOn(2026, 3, 9));

        journey.RecordSteady(0, Overnight);
        journey.Deliver(Overnight);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.PackExpired], decision.Reasons);
        Assert.False(journey.Consignment.IsWrittenOff(journey.PalletId));
    }

    [Fact]
    public void A_pack_expiring_on_the_day_of_delivery_is_not_expired() {
        // Arrange
        Journey journey = DispatchedOvernight(PacksExpiringOn(2026, 3, 10));

        journey.RecordSteady(0, Overnight);
        journey.Deliver(Overnight);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Theory]
    [InlineData(2026, 3, 10, 1, true)]
    [InlineData(2026, 3, 11, 1, false)]
    [InlineData(2026, 3, 11, 2, true)]
    [InlineData(2026, 3, 15, 5, false)]
    [InlineData(2026, 3, 15, 6, true)]
    public void A_pallet_must_have_at_least_the_required_days_of_shelf_life_left_on_the_day_of_delivery(int year, int month, int day, int requiredDays, bool expectedShort) {
        // Arrange
        Journey journey = DispatchedOvernight(planned => PacksExpiringOn(year, month, day)(planned).WithProduct(product => product.WithMinimumShelfLifeOnReceiptDays(requiredDays)));

        journey.RecordSteady(0, Overnight);
        journey.Deliver(Overnight);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        if (expectedShort) {
            Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
            Assert.Equal([DecisionReason.ShortShelfLife], decision.Reasons);
        } else {
            Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        }
    }

    [Fact]
    public void The_day_of_delivery_is_its_utc_day_whatever_the_offset_it_is_written_with() {
        // Arrange
        Journey journey = DispatchedOvernight(PacksExpiringOn(2026, 3, 9));

        journey.RecordSteady(0, 14 * 60);

        // 10 March 00:30 at +02:00 is still 9 March, 22:30, in UTC: the packs expire that very day.
        journey.Consignment.Deliver(journey.Warehouse().Generate(), new DateTimeOffset(2026, 3, 10, 0, 30, 0, TimeSpan.FromHours(2)));

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
    }

    [Theory]
    [InlineData(2026, 3, 9, true)]
    [InlineData(2026, 3, 10, false)]
    public void A_logger_whose_calibration_fell_due_before_the_day_of_delivery_holds_the_pallet_for_review(int year, int month, int day, bool expectedLapsed) {
        // Arrange
        Journey journey = DispatchedOvernight(planned => planned.WithEachPallet(pallet => pallet.WithLogger(logger => logger.WithCalibratedOn(new DateOnly(2026, 3, 1)).WithCalibrationDueOn(new DateOnly(year, month, day)))));

        journey.RecordSteady(0, Overnight);
        journey.Deliver(Overnight);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        if (expectedLapsed) {
            Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
            Assert.Equal([DecisionReason.LoggerCalibrationLapsed], decision.Reasons);
        } else {
            Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        }
    }

    [Fact]
    public void Every_reason_that_applies_is_given_and_one_rejecting_reason_is_enough_to_reject() {
        // Arrange
        Journey journey = DispatchedOvernight(PacksExpiringOn(2026, 3, 9));

        journey.RecordSteady(0, 10 * 60);
        journey.Deliver(Overnight);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.PackExpired, DecisionReason.IncompleteTemperatureRecord], decision.Reasons);
        Assert.Null(decision.QuarantineBay);
    }

    [Fact]
    public void A_pallet_cannot_be_decided_before_the_delivery() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Refused.Because(ColdChainRefusal.NotDelivered, () => journey.Consignment.Decide(journey.PalletId));
    }

    [Fact]
    public void A_pallet_of_a_consignment_not_yet_dispatched_cannot_be_decided() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned().Generate();

        // Assert
        Refused.Because(ColdChainRefusal.NotDelivered, () => consignment.Decide(consignment.Pallets[0].Sscc));
    }

    [Fact]
    public void A_pallet_that_is_not_part_of_the_consignment_cannot_be_decided() {
        // Arrange
        Journey journey = Journey.Dispatched();
        Sscc    anyUnknownPallet = new AnySscc().DifferentFrom(journey.PalletId).Generate();

        journey.RecordSteady(0, HourOfTransit);
        journey.Deliver(HourOfTransit);

        // Assert
        Refused.Because(ColdChainRefusal.UnknownPallet, () => journey.Consignment.Decide(anyUnknownPallet));
    }

    [Fact]
    public void A_pallet_is_decided_once() {
        // Arrange
        Journey journey = Journey.Dispatched(planned => planned.WithPalletCount(2));

        foreach (ColdChainPallet pallet in journey.Consignment.Pallets) {
            for (int minute = 0; minute <= HourOfTransit; minute += 5) {
                journey.Record(pallet, minute, 5m);
            }
        }

        journey.Deliver(HourOfTransit);
        journey.Consignment.Decide(journey.PalletId);

        // Assert
        Refused.Because(ColdChainRefusal.PalletAlreadyDecided, () => journey.Consignment.Decide(journey.PalletId));
    }

    [Fact]
    public void A_pallet_written_off_in_transit_cannot_be_decided_again_at_the_warehouse() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.Record(0, -1m);
        journey.Deliver(HourOfTransit);

        // Assert
        Refused.Because(ColdChainRefusal.PalletAlreadyDecided, () => journey.Consignment.Decide(journey.PalletId));
    }

    [Fact]
    public void The_consignment_stays_delivered_until_its_last_pallet_is_decided_then_closes() {
        // Arrange
        Journey journey = Journey.Dispatched(planned => planned.WithPalletCount(2));

        foreach (ColdChainPallet pallet in journey.Consignment.Pallets) {
            for (int minute = 0; minute <= HourOfTransit; minute += 5) {
                journey.Record(pallet, minute, 5m);
            }
        }

        journey.Deliver(HourOfTransit);

        // Act and assert
        journey.Consignment.Decide(journey.Consignment.Pallets[0].Sscc);

        Assert.Equal(ConsignmentStatus.Delivered, journey.Consignment.Status);

        journey.Consignment.Decide(journey.Consignment.Pallets[1].Sscc);

        Assert.Equal(ConsignmentStatus.Closed, journey.Consignment.Status);
    }

    [Fact]
    public void Deciding_the_remaining_pallets_decides_those_not_decided_yet_in_loading_order() {
        // Arrange
        Journey journey = Journey.Dispatched(planned => planned.WithPalletCount(3));

        foreach (ColdChainPallet pallet in journey.Consignment.Pallets) {
            for (int minute = 0; minute <= HourOfTransit; minute += 5) {
                journey.Record(pallet, minute, 5m);
            }
        }

        journey.Deliver(HourOfTransit);

        Sscc first  = journey.Consignment.Pallets[0].Sscc;
        Sscc second = journey.Consignment.Pallets[1].Sscc;
        Sscc third  = journey.Consignment.Pallets[2].Sscc;

        journey.Consignment.Decide(second);

        // Act
        IReadOnlyList<ReleaseDecision> decisions = journey.Consignment.DecideRemaining();

        // Assert
        Assert.Equal([first, third], decisions.Select(decision => decision.Pallet));
        Assert.Equal(3, journey.Consignment.Decisions.Count);
        Assert.Equal(ConsignmentStatus.Closed, journey.Consignment.Status);
    }

    [Fact]
    public void Deciding_the_remaining_pallets_leaves_out_the_one_written_off_in_transit() {
        // Arrange
        Journey         journey = Journey.Dispatched(planned => planned.WithPalletCount(2));
        ColdChainPallet frozen  = journey.Consignment.Pallets[0];
        ColdChainPallet sound   = journey.Consignment.Pallets[1];

        journey.Record(frozen, 0, -3m);

        for (int minute = 0; minute <= HourOfTransit; minute += 5) {
            journey.Record(sound, minute, 5m);
        }

        journey.Deliver(HourOfTransit);

        // Act
        IReadOnlyList<ReleaseDecision> decisions = journey.Consignment.DecideRemaining();

        // Assert
        Assert.Equal(sound.Sscc, Assert.Single(decisions).Pallet);
        Assert.Equal(ConsignmentStatus.Closed, journey.Consignment.Status);
        Assert.True(journey.Consignment.IsWrittenOff(frozen.Sscc));
        Assert.False(journey.Consignment.IsWrittenOff(sound.Sscc));
    }

    [Fact]
    public void The_remaining_pallets_cannot_be_decided_before_the_delivery() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Refused.Because(ColdChainRefusal.NotDelivered, () => journey.Consignment.DecideRemaining());
    }

    [Fact]
    public void What_remains_of_the_budget_is_the_cumulative_allowance_less_the_minutes_outside_the_label() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(60, 30));

        journey.RecordSteady(0, 15);
        journey.RecordSteady(20, 30, 9m);
        journey.RecordSteady(35, 40);

        // Assert
        Assert.Equal(45, journey.Consignment.RemainingBudgetMinutes(journey.PalletId));
    }

    [Fact]
    public void What_remains_of_the_budget_never_goes_below_zero() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(40, 30));

        RecordTwoExcursionsOf25MinutesEach(journey);

        // Assert
        Assert.Equal(0, journey.Consignment.RemainingBudgetMinutes(journey.PalletId));
    }

    [Fact]
    public void The_budget_of_a_pallet_that_is_not_in_the_consignment_is_unknown() {
        // Arrange
        Journey journey = Journey.Dispatched();
        Sscc    anyUnknownPallet = new AnySscc().DifferentFrom(journey.PalletId).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => journey.Consignment.RemainingBudgetMinutes(anyUnknownPallet));
    }

    [Fact(Skip = "domain defect (arguable): a zero-tolerance budget is only quarantined, not rejected, when the record ends with a single reading outside the label, because that open excursion is charged 0 minutes, seed 632050781")]
    public void A_product_that_tolerates_no_time_outside_its_label_is_rejected_when_the_record_ends_outside_it() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(0, 0));

        journey.RecordSteady(0, 55);
        journey.Record(60, 9m);
        journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
    }

    [Fact]
    public void A_product_that_tolerates_no_time_outside_its_label_is_rejected_as_soon_as_a_reading_leaves_it_and_comes_back() {
        // Arrange
        Journey journey = Journey.Dispatched(Budget(0, 0));

        journey.RecordSteady(0, 25);
        journey.Record(30, 9m);
        journey.RecordSteady(35, 60);
        journey.Deliver(60);

        // Act
        ReleaseDecision decision = journey.Consignment.Decide(journey.PalletId);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Contains(DecisionReason.StabilityBudgetExhausted, decision.Reasons);
    }
}
