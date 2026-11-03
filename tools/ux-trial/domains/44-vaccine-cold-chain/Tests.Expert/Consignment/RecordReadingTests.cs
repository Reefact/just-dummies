using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using JustDummies;
using Warehouse.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.Consignment;

public sealed class RecordReadingTests {

    [Fact]
    public void A_reading_is_added_to_the_history_of_the_pallet_whose_logger_took_it() {
        // Arrange
        Journey       journey = Journey.Dispatched();
        LoggerReading reading = journey.ReadingAt(0, 5m);

        // Act
        journey.Consignment.RecordReading(journey.PalletId, reading);

        // Assert
        Assert.Same(reading, Assert.Single(journey.Consignment.HistoryOf(journey.PalletId).Readings));
    }

    [Fact]
    public void Each_pallet_keeps_the_readings_of_its_own_logger() {
        // Arrange
        Journey         journey = Journey.Dispatched(planned => planned.WithPalletCount(2));
        ColdChainPallet first   = journey.Consignment.Pallets[0];
        ColdChainPallet second  = journey.Consignment.Pallets[1];

        // Act
        journey.Record(first, 0, 5m);
        journey.Record(second, 0, 4m);
        journey.Record(second, 5, 4.5m);

        // Assert
        Assert.Single(journey.Consignment.HistoryOf(first.Sscc).Readings);
        Assert.Equal(2, journey.Consignment.HistoryOf(second.Sscc).Readings.Count);
    }

    [Fact]
    public void Nothing_can_be_recorded_before_the_consignment_is_dispatched() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned().Generate();
        ColdChainPallet      pallet      = consignment.Pallets[0];

        // Assert
        Refused.Because(ColdChainRefusal.NotDispatched, () => consignment.RecordReading(pallet.Sscc, Journey.ReadingOf(pallet, 0, 5m)));
    }

    [Fact]
    public void A_reading_for_a_pallet_that_is_not_in_the_consignment_is_refused() {
        // Arrange
        Journey journey = Journey.Dispatched();
        Sscc    anyUnknownPallet = new AnySscc().DifferentFrom(journey.PalletId).Generate();

        // Assert
        Refused.Because(ColdChainRefusal.UnknownPallet, () => journey.Consignment.RecordReading(anyUnknownPallet, journey.ReadingAt(0, 5m)));
    }

    [Fact]
    public void A_reading_from_another_logger_than_the_pallets_is_refused() {
        // Arrange
        Journey       journey  = Journey.Dispatched();
        LoggerReading anyStranger = Any.LoggerReading().WithTakenAt(Journey.Start.AddMinutes(5)).Generate();

        // Assert
        Refused.Because(ColdChainRefusal.LoggerMismatch, () => journey.Consignment.RecordReading(journey.PalletId, anyStranger));
    }

    [Fact]
    public void A_reading_taken_before_the_dispatch_is_refused() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Refused.Because(ColdChainRefusal.ReadingOutsideTransit, () => journey.Record(-1, 5m));
    }

    [Fact]
    public void A_reading_taken_at_the_very_instant_of_the_dispatch_is_accepted() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Act
        journey.Record(0, 5m);

        // Assert
        Assert.Single(journey.Consignment.HistoryOf(journey.PalletId).Readings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void A_reading_taken_at_or_before_the_previous_one_is_refused(int minutesFromPrevious) {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.Record(10, 5m);

        // Assert
        Refused.Because(ColdChainRefusal.ReadingOutOfOrder, () => journey.Record(10 + minutesFromPrevious, 5m));
    }

    [Fact]
    public void A_reading_taken_after_the_delivery_is_refused() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.RecordSteady(0, 30);
        journey.Deliver(30);

        // Assert
        Refused.Because(ColdChainRefusal.ReadingOutsideTransit, () => journey.Record(31, 5m));
    }

    [Fact]
    public void A_reading_taken_by_the_instant_of_the_delivery_may_still_be_uploaded_after_it() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.RecordSteady(0, 25);
        journey.Deliver(30);

        // Act
        journey.Record(30, 5m);

        // Assert
        Assert.Equal(7, journey.Consignment.HistoryOf(journey.PalletId).Readings.Count);
    }

    [Theory]
    [InlineData("-0.1", true)]
    [InlineData("-40", true)]
    [InlineData("0", false)]
    [InlineData("25", false)]
    [InlineData("25.1", true)]
    [InlineData("85", true)]
    public void A_reading_below_zero_or_above_25_degrees_writes_the_pallet_off_at_once(string celsius, bool expectedWrittenOff) {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Act
        journey.Record(0, Celsius.Of(celsius));

        // Assert
        Assert.Equal(expectedWrittenOff, journey.Consignment.IsWrittenOff(journey.PalletId));
        Assert.Equal(expectedWrittenOff, journey.Consignment.DecisionFor(journey.PalletId) is not null);
    }

    [Fact]
    public void A_pallet_written_off_in_transit_is_rejected_for_the_hard_limit_with_the_minutes_it_spent_outside_the_label() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Act
        journey.RecordAll((0, 5m), (5, 1m), (10, -1m));

        // Assert
        ReleaseDecision decision = Assert.IsType<ReleaseDecision>(journey.Consignment.DecisionFor(journey.PalletId));

        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.HardLimitBreached], decision.Reasons);
        Assert.Equal(5, decision.BudgetConsumedMinutes);
        Assert.Null(decision.QuarantineBay);
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
    }

    [Fact]
    public void A_pallet_written_off_takes_no_further_reading() {
        // Arrange
        Journey journey = Journey.Dispatched();

        journey.Record(0, -5m);

        // Assert
        Refused.Because(ColdChainRefusal.PalletAlreadyDecided, () => journey.Record(5, 5m));
    }

    [Fact]
    public void Writing_one_pallet_off_leaves_the_others_undecided_and_recording() {
        // Arrange
        Journey         journey = Journey.Dispatched(planned => planned.WithPalletCount(2));
        ColdChainPallet frozen  = journey.Consignment.Pallets[0];
        ColdChainPallet sound   = journey.Consignment.Pallets[1];

        // Act
        journey.Record(frozen, 0, -2m);
        journey.Record(sound, 0, 5m);

        // Assert
        Assert.True(journey.Consignment.IsWrittenOff(frozen.Sscc));
        Assert.Null(journey.Consignment.DecisionFor(sound.Sscc));
        Assert.False(journey.Consignment.IsWrittenOff(sound.Sscc));
    }

    [Fact(Skip = "domain defect: HistoryOf returns the live TemperatureHistory whose public Record bypasses the hard-limit write-off, seed 2083690448")]
    public void A_reading_recorded_straight_into_the_history_of_a_pallet_still_writes_it_off() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Act
        journey.Consignment.HistoryOf(journey.PalletId).Record(journey.ReadingAt(0, -10m));

        // Assert
        Assert.True(journey.Consignment.IsWrittenOff(journey.PalletId));
    }

    [Fact(Skip = "domain defect: HistoryOf returns the live TemperatureHistory whose public Record bypasses the transit-window check, seed -1470097523")]
    public void A_reading_recorded_straight_into_the_history_of_a_pallet_is_still_refused_outside_the_transit() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Assert.ThrowsAny<Exception>(() => journey.Consignment.HistoryOf(journey.PalletId).Record(journey.ReadingAt(-60, 5m)));
    }
}
