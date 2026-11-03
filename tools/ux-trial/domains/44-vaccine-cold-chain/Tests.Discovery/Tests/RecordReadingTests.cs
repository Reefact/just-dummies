using AgriTech.Domain;

using ColdChain.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class RecordReadingTests {

    private static ColdChainRefusal RefusalOf(Action action) {
        return Assert.Throws<ColdChainRefusedException>(action).Refusal;
    }

    [Fact]
    public void A_reading_is_added_to_the_history_of_its_pallet() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 2 }).Dispatched();
        Sscc    first   = journey.Consignment.Pallets[0].Sscc;
        Sscc    second  = journey.Consignment.Pallets[1].Sscc;
        decimal anyInRange = Any.Decimal().Between(2m, 8m).WithScale(1).Generate();

        // Act
        LoggerReading reading = journey.ReadingOf(first, 10, anyInRange);
        journey.Consignment.RecordReading(first, reading);

        // Assert
        Assert.Same(reading, Assert.Single(journey.Consignment.HistoryOf(first).Readings));
        Assert.Empty(journey.Consignment.HistoryOf(second).Readings);
    }

    [Fact]
    public void A_reading_is_refused_before_the_dispatch() {
        // Arrange
        Journey journey = Journey.Planned();
        Sscc    pallet  = journey.FirstPallet;

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.RecordReading(pallet, journey.ReadingOf(pallet, 0, 5m)));

        // Assert
        Assert.Equal(ColdChainRefusal.NotDispatched, refusal);
    }

    [Fact]
    public void A_reading_of_a_pallet_outside_the_consignment_is_refused() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    stranger = new AnySscc().DifferentFrom(journey.FirstPallet).Generate();
        LoggerReading reading = journey.ReadingOf(journey.FirstPallet, 10, 5m);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.RecordReading(stranger, reading));

        // Assert
        Assert.Equal(ColdChainRefusal.UnknownPallet, refusal);
    }

    [Fact]
    public void A_reading_from_the_logger_of_another_pallet_is_refused() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 2 }).Dispatched();
        Sscc    first   = journey.Consignment.Pallets[0].Sscc;
        Sscc    second  = journey.Consignment.Pallets[1].Sscc;
        LoggerReading fromSecondLogger = journey.ReadingOf(second, 10, 5m);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.RecordReading(first, fromSecondLogger));

        // Assert
        Assert.Equal(ColdChainRefusal.LoggerMismatch, refusal);
        Assert.Empty(journey.Consignment.HistoryOf(first).Readings);
        Assert.Empty(journey.Consignment.HistoryOf(second).Readings);
    }

    [Fact]
    public void A_reading_is_accepted_at_the_very_moment_of_dispatch() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();

        // Act
        journey.Record(journey.FirstPallet, 0, 5m);

        // Assert
        Assert.Single(journey.Consignment.HistoryOf(journey.FirstPallet).Readings);
    }

    [Fact]
    public void A_reading_taken_before_the_dispatch_is_refused() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        LoggerReading early = new AnyLoggerReading().WithLogger(journey.LoggerOf(pallet)).WithTakenAt(Journey.DispatchedAt.AddTicks(-1)).WithCelsius(5m).Generate();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.RecordReading(pallet, early));

        // Assert
        Assert.Equal(ColdChainRefusal.ReadingOutsideTransit, refusal);
    }

    [Fact]
    public void A_reading_not_after_the_previous_one_is_refused() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        journey.Record(pallet, 20, 5m);

        // Assert
        Assert.Equal(ColdChainRefusal.ReadingOutOfOrder, RefusalOf(() => journey.Record(pallet, 20, 5m)));
        Assert.Equal(ColdChainRefusal.ReadingOutOfOrder, RefusalOf(() => journey.Record(pallet, 10, 5m)));
        Assert.Single(journey.Consignment.HistoryOf(pallet).Readings);
    }

    [Fact]
    public void Each_pallet_has_its_own_order_of_readings() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 2 }).Dispatched();
        Sscc    first   = journey.Consignment.Pallets[0].Sscc;
        Sscc    second  = journey.Consignment.Pallets[1].Sscc;
        journey.Record(first, 30, 5m);

        // Act
        journey.Record(second, 10, 5m);

        // Assert
        Assert.Single(journey.Consignment.HistoryOf(second).Readings);
    }

    [Fact]
    public void A_reading_taken_while_the_consignment_is_still_unloaded_up_to_the_moment_of_delivery_is_accepted_after_delivery() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt);

        // Act: the logger is read out at the warehouse, and its last readings come in late.
        journey.Record(pallet, journey.Options.DurationMinutes - 5, 5m);
        journey.Record(pallet, journey.Options.DurationMinutes, 5m);

        // Assert
        Assert.Equal(2, journey.Consignment.HistoryOf(pallet).Readings.Count);
    }

    [Fact]
    public void A_reading_taken_after_the_delivery_is_refused() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Record(pallet, journey.Options.DurationMinutes + 1, 5m));

        // Assert
        Assert.Equal(ColdChainRefusal.ReadingOutsideTransit, refusal);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-40)]
    [InlineData(25.1)]
    [InlineData(85)]
    public void A_reading_beyond_a_hard_limit_writes_the_pallet_off_at_once(double celsius) {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        journey.Record(pallet, 0, 5m);

        // Act
        journey.Record(pallet, 10, (decimal)celsius);

        // Assert
        ReleaseDecision decision = journey.Consignment.DecisionFor(pallet)!;
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.HardLimitBreached], decision.Reasons);
        Assert.Null(decision.QuarantineBay);
        Assert.Same(pallet, decision.Pallet);
        Assert.True(journey.Consignment.IsWrittenOff(pallet));
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(0.1)]
    [InlineData(24.9)]
    public void A_reading_exactly_at_a_hard_limit_does_not_write_the_pallet_off(double celsius) {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;

        // Act
        journey.Record(pallet, 10, (decimal)celsius);

        // Assert
        Assert.Null(journey.Consignment.DecisionFor(pallet));
        Assert.False(journey.Consignment.IsWrittenOff(pallet));
    }

    [Fact]
    public void A_written_off_pallet_carries_the_minutes_outside_the_label_up_to_the_breach() {
        // Arrange: 9 degrees from minute 10 to minute 30, then 30 degrees at minute 40.
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        journey.Record(pallet, 0, 5m);
        journey.Record(pallet, 10, 9m);
        journey.Record(pallet, 20, 9m);

        // Act
        journey.Record(pallet, 30, 30m);

        // Assert: one open warm excursion from minute 10 to minute 30, then the reading itself extends the same warm run.
        ReleaseDecision decision = journey.Consignment.DecisionFor(pallet)!;
        Assert.Equal(journey.Consignment.HistoryOf(pallet).MinutesOutsideRange, decision.BudgetConsumedMinutes);
        Assert.Equal(20, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void A_written_off_pallet_takes_no_further_reading() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        journey.Record(pallet, 10, -5m);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Record(pallet, 20, 5m));

        // Assert
        Assert.Equal(ColdChainRefusal.PalletAlreadyDecided, refusal);
    }

    [Fact]
    public void Writing_a_pallet_off_leaves_the_others_recording() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 2 }).Dispatched();
        Sscc    first   = journey.Consignment.Pallets[0].Sscc;
        Sscc    second  = journey.Consignment.Pallets[1].Sscc;

        // Act
        journey.Record(first, 10, 30m);
        journey.Record(second, 10, 5m);

        // Assert
        Assert.True(journey.Consignment.IsWrittenOff(first));
        Assert.False(journey.Consignment.IsWrittenOff(second));
        Assert.Single(journey.Consignment.Decisions);
    }

    [Fact]
    public void A_pallet_written_off_in_transit_is_not_decided_again_at_delivery() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { Pallets = 2 }).Dispatched();
        Sscc    first   = journey.Consignment.Pallets[0].Sscc;
        journey.Record(first, 10, 30m);

        // Act
        journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt);
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Decide(first));

        // Assert
        Assert.Equal(ColdChainRefusal.PalletAlreadyDecided, refusal);
        Assert.Equal(ConsignmentStatus.Delivered, journey.Consignment.Status);
    }

    [Fact]
    public void A_consignment_whose_only_pallet_was_written_off_is_closed_on_delivery() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        journey.Record(journey.FirstPallet, 10, 30m);

        // Act
        journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt);

        // Assert
        Assert.Equal(ConsignmentStatus.Closed, journey.Consignment.Status);
    }

    [Fact]
    public void A_pallet_written_off_by_a_late_reading_after_delivery_closes_the_consignment_when_it_was_the_last() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();
        Sscc    pallet  = journey.FirstPallet;
        journey.Consignment.Deliver(journey.Warehouse, journey.DeliveredAt);

        // Act
        journey.Record(pallet, journey.Options.DurationMinutes - 10, -3m);

        // Assert
        Assert.Equal(ConsignmentStatus.Closed, journey.Consignment.Status);
    }

    [Fact]
    public void A_pallet_and_a_reading_are_required() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();

        // Assert
        Assert.Throws<ArgumentNullException>(() => journey.Consignment.RecordReading(null!, journey.ReadingOf(journey.FirstPallet, 10, 5m)));
        Assert.Throws<ArgumentNullException>(() => journey.Consignment.RecordReading(journey.FirstPallet, null!));
    }

}
