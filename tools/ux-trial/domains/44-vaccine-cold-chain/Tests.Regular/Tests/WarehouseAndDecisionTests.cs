using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests.Regular.Tests;

public sealed class ReceivingWarehouseTests {

    [Theory]
    [InlineData(2.0, 8.0)]
    [InlineData(0.0, 8.0)]
    [InlineData(0.0, 1.0)]
    [InlineData(4.0, 5.0)]
    public void A_chilled_room_that_never_runs_below_zero_receives_vaccine(double min, double max) {
        // Arrange
        TemperatureRange coldRoom = new TemperatureRange((decimal)min, (decimal)max);

        // Act
        ReceivingWarehouse warehouse = new AnyReceivingWarehouse().WithColdRoom(coldRoom).Generate();

        // Assert
        Assert.Same(coldRoom, warehouse.ColdRoom);
    }

    [Theory]
    [InlineData(2.0, 8.1)]
    [InlineData(15.0, 25.0)]
    [InlineData(-25.0, -18.0)]
    [InlineData(-25.0, -20.0)]
    public void A_room_that_is_not_chilled_is_refused(double min, double max) {
        // Arrange
        TemperatureRange coldRoom = new TemperatureRange((decimal)min, (decimal)max);

        // Act
        Action act = () => new AnyReceivingWarehouse().WithColdRoom(coldRoom).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(act);
        Assert.Equal("coldRoom", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.1, 4.0)]
    [InlineData(-5.0, 4.0)]
    [InlineData(-17.0, 8.0)]
    public void A_chilled_room_that_runs_below_zero_is_refused(double min, double max) {
        // Arrange
        TemperatureRange coldRoom = new TemperatureRange((decimal)min, (decimal)max);

        // Act
        Action act = () => new AnyReceivingWarehouse().WithColdRoom(coldRoom).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(act);
        Assert.Equal("coldRoom", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void A_quarantine_bay_at_floor_level_is_accepted(int level) {
        // Arrange
        BinLocation bay = new AnyBinLocation().WithLevel(level).Generate();

        // Act
        ReceivingWarehouse warehouse = new AnyReceivingWarehouse().WithQuarantineBay(bay).Generate();

        // Assert
        Assert.Equal(bay, warehouse.QuarantineBay);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(20)]
    public void A_quarantine_bay_that_needs_a_reach_truck_is_refused(int level) {
        // Arrange
        BinLocation bay = new AnyBinLocation().WithLevel(level).Generate();

        // Act
        Action act = () => new AnyReceivingWarehouse().WithQuarantineBay(bay).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(act);
        Assert.Equal("quarantineBay", refusal.ParamName);
    }

    [Fact]
    public void A_warehouse_knows_where_it_is_and_who_runs_it() {
        // Arrange
        UnLocode    location = new AnyUnLocode().Generate();
        CustodianId operatorId = new AnyCustodianId().Generate();

        // Act
        ReceivingWarehouse warehouse = new AnyReceivingWarehouse().WithLocation(location).WithOperatorId(operatorId).Generate();

        // Assert
        Assert.Equal(location, warehouse.Location);
        Assert.Equal(operatorId, warehouse.OperatorId);
        Assert.Equal($"{operatorId} at {location}", warehouse.ToString());
    }

    [Fact]
    public void Warehouses_that_agree_on_everything_are_equal() {
        // Arrange
        ReceivingWarehouse anyWarehouse = new AnyReceivingWarehouse().Generate();

        // Act
        ReceivingWarehouse same = new ReceivingWarehouse(anyWarehouse.Location, anyWarehouse.OperatorId, new TemperatureRange(anyWarehouse.ColdRoom.MinCelsius, anyWarehouse.ColdRoom.MaxCelsius), anyWarehouse.QuarantineBay);

        // Assert
        Assert.Equal(anyWarehouse, same);
        Assert.Equal(anyWarehouse.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Warehouses_with_another_cold_room_are_not_equal() {
        // Arrange
        ReceivingWarehouse warehouse = new AnyReceivingWarehouse().WithColdRoom(new TemperatureRange(2m, 8m)).Generate();

        // Act
        ReceivingWarehouse other = new ReceivingWarehouse(warehouse.Location, warehouse.OperatorId, new TemperatureRange(3m, 8m), warehouse.QuarantineBay);

        // Assert
        Assert.NotEqual(warehouse, other);
        Assert.False(warehouse.Equals(null));
    }

}

public sealed class ReleaseDecisionTests {

    private static readonly DecisionReason[] RejectingReasons = [DecisionReason.HardLimitBreached, DecisionReason.StabilityBudgetExhausted, DecisionReason.PackExpired];

    private static readonly DecisionReason[] HoldingReasons = [
        DecisionReason.ShortShelfLife, DecisionReason.SingleExcursionTooLong, DecisionReason.ArrivedOutOfRange,
        DecisionReason.IncompleteTemperatureRecord, DecisionReason.LoggerCalibrationLapsed
    ];

    private static BinLocation AnyBay() {
        return new AnyBinLocation().WithLevel(l => l.Between(1, 4)).Generate();
    }

    private static Sscc AnyPallet() {
        return new AnySscc().Generate();
    }

    [Fact]
    public void A_released_pallet_has_no_reason_and_no_bay() {
        // Arrange
        Sscc pallet = AnyPallet();

        // Act
        ReleaseDecision decision = new ReleaseDecision(pallet, ReleaseOutcome.Released, [], 0, null);

        // Assert
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Empty(decision.Reasons);
        Assert.Null(decision.QuarantineBay);
        Assert.Equal(pallet, decision.Pallet);
        Assert.Equal($"{pallet}: Released", decision.ToString());
    }

    [Fact]
    public void A_released_pallet_with_a_reason_against_it_is_refused() {
        // Arrange
        DecisionReason anyReason = Any.Enum<DecisionReason>().Generate();

        // Act
        Action act = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Released, [anyReason], 0, null);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(act);
        Assert.Equal("reasons", refusal.ParamName);
    }

    [Fact]
    public void A_rejection_needs_a_rejecting_reason() {
        // Arrange
        DecisionReason anyHoldingReason = Any.ElementOf(HoldingReasons).Generate();

        // Act
        Action noReason       = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Rejected, [], 0, null);
        Action holdingReason  = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Rejected, [anyHoldingReason], 0, null);

        // Assert
        Assert.Throws<ArgumentException>(noReason);
        Assert.Throws<ArgumentException>(holdingReason);
    }

    [Fact]
    public void A_pallet_is_rejected_by_any_one_rejecting_reason() {
        // Arrange
        DecisionReason anyRejectingReason = Any.ElementOf(RejectingReasons).Generate();

        // Act
        ReleaseDecision decision = new ReleaseDecision(AnyPallet(), ReleaseOutcome.Rejected, [anyRejectingReason], 0, null);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Null(decision.QuarantineBay);
    }

    [Fact]
    public void A_rejected_pallet_may_carry_holding_reasons_too() {
        // Act
        ReleaseDecision decision = new ReleaseDecision(AnyPallet(), ReleaseOutcome.Rejected, [DecisionReason.ShortShelfLife, DecisionReason.PackExpired], 0, null);

        // Assert
        Assert.Equal([DecisionReason.PackExpired, DecisionReason.ShortShelfLife], decision.Reasons);
    }

    [Fact]
    public void A_quarantine_needs_reasons_none_of_them_rejecting() {
        // Arrange
        DecisionReason anyHoldingReason   = Any.ElementOf(HoldingReasons).Generate();
        DecisionReason anyRejectingReason = Any.ElementOf(RejectingReasons).Generate();
        BinLocation    bay                = AnyBay();

        // Act
        Action noReason        = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Quarantined, [], 0, bay);
        Action rejectingReason = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Quarantined, [anyHoldingReason, anyRejectingReason], 0, bay);
        ReleaseDecision decision = new ReleaseDecision(AnyPallet(), ReleaseOutcome.Quarantined, [anyHoldingReason], 0, bay);

        // Assert
        Assert.Throws<ArgumentException>(noReason);
        Assert.Throws<ArgumentException>(rejectingReason);
        Assert.Equal(bay, decision.QuarantineBay);
    }

    [Fact]
    public void A_bay_is_given_if_and_only_if_the_pallet_is_quarantined() {
        // Arrange
        BinLocation    bay                = AnyBay();
        DecisionReason anyHoldingReason   = Any.ElementOf(HoldingReasons).Generate();
        DecisionReason anyRejectingReason = Any.ElementOf(RejectingReasons).Generate();

        // Act
        Action quarantinedWithoutBay = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Quarantined, [anyHoldingReason], 0, null);
        Action releasedWithBay       = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Released, [], 0, bay);
        Action rejectedWithBay       = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Rejected, [anyRejectingReason], 0, bay);

        // Assert
        Assert.Equal("quarantineBay", Assert.Throws<ArgumentException>(quarantinedWithoutBay).ParamName);
        Assert.Equal("quarantineBay", Assert.Throws<ArgumentException>(releasedWithBay).ParamName);
        Assert.Equal("quarantineBay", Assert.Throws<ArgumentException>(rejectedWithBay).ParamName);
    }

    [Fact]
    public void A_reason_is_stated_once() {
        // Arrange
        DecisionReason anyRejectingReason = Any.ElementOf(RejectingReasons).Generate();

        // Act
        Action act = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Rejected, [anyRejectingReason, anyRejectingReason], 0, null);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void The_reasons_are_kept_sorted() {
        // Arrange
        DecisionReason[] inReverse = [DecisionReason.LoggerCalibrationLapsed, DecisionReason.ArrivedOutOfRange, DecisionReason.ShortShelfLife];

        // Act
        ReleaseDecision decision = new ReleaseDecision(AnyPallet(), ReleaseOutcome.Quarantined, inReverse, 0, AnyBay());

        // Assert
        Assert.Equal([DecisionReason.ShortShelfLife, DecisionReason.ArrivedOutOfRange, DecisionReason.LoggerCalibrationLapsed], decision.Reasons);
    }

    [Fact]
    public void The_budget_consumed_cannot_be_negative() {
        // Arrange
        int anyNegative = Any.Int32().Negative().Generate();

        // Act
        Action act = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Released, [], anyNegative, null);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void The_budget_consumed_is_carried_forward_with_the_decision() {
        // Arrange
        int anyConsumed = Any.Int32().Between(0, 10_080).Generate();

        // Act
        ReleaseDecision decision = new ReleaseDecision(AnyPallet(), ReleaseOutcome.Released, [], anyConsumed, null);

        // Assert
        Assert.Equal(anyConsumed, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void An_unknown_outcome_or_reason_is_refused() {
        // Act
        Action unknownOutcome = () => new ReleaseDecision(AnyPallet(), (ReleaseOutcome)3, [], 0, null);
        Action unknownReason  = () => new ReleaseDecision(AnyPallet(), ReleaseOutcome.Rejected, [(DecisionReason)99], 0, null);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(unknownOutcome);
        Assert.Throws<ArgumentOutOfRangeException>(unknownReason);
    }

    [Theory]
    [InlineData(DecisionReason.HardLimitBreached, true)]
    [InlineData(DecisionReason.StabilityBudgetExhausted, true)]
    [InlineData(DecisionReason.PackExpired, true)]
    [InlineData(DecisionReason.ShortShelfLife, false)]
    [InlineData(DecisionReason.SingleExcursionTooLong, false)]
    [InlineData(DecisionReason.ArrivedOutOfRange, false)]
    [InlineData(DecisionReason.IncompleteTemperatureRecord, false)]
    [InlineData(DecisionReason.LoggerCalibrationLapsed, false)]
    public void Three_reasons_reject_and_the_others_hold_the_pallet_for_review(DecisionReason reason, bool rejecting) {
        Assert.Equal(rejecting, ReleaseDecision.IsRejecting(reason));
    }

    [Fact]
    public void No_reason_calls_for_a_release() {
        Assert.Equal(ReleaseOutcome.Released, ReleaseDecision.OutcomeFor([]));
    }

    [Fact]
    public void Holding_reasons_alone_call_for_a_quarantine() {
        // Arrange
        List<DecisionReason> anyHoldingReasons = Any.ListOf(Any.ElementOf(HoldingReasons)).WithCountBetween(1, 5).Distinct().Generate();

        // Act
        ReleaseOutcome outcome = ReleaseDecision.OutcomeFor(anyHoldingReasons);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, outcome);
    }

    [Fact]
    public void One_rejecting_reason_among_others_calls_for_a_rejection() {
        // Arrange
        DecisionReason anyRejectingReason = Any.ElementOf(RejectingReasons).Generate();
        List<DecisionReason> anyHoldingReasons = Any.ListOf(Any.ElementOf(HoldingReasons)).WithCountBetween(0, 5).Distinct().Generate();

        // Act
        ReleaseOutcome outcome = ReleaseDecision.OutcomeFor(anyHoldingReasons.Append(anyRejectingReason).ToList());

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, outcome);
    }

    [Fact]
    public void The_outcome_for_missing_reasons_is_refused() {
        Assert.Throws<ArgumentNullException>(() => ReleaseDecision.OutcomeFor(null!));
    }

    [Fact]
    public void Decisions_that_agree_on_everything_are_equal_whatever_the_order_the_reasons_came_in() {
        // Arrange
        Sscc        pallet = AnyPallet();
        BinLocation bay    = AnyBay();

        // Act
        ReleaseDecision first  = new ReleaseDecision(pallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife, DecisionReason.ArrivedOutOfRange], 12, bay);
        ReleaseDecision second = new ReleaseDecision(pallet, ReleaseOutcome.Quarantined, [DecisionReason.ArrivedOutOfRange, DecisionReason.ShortShelfLife], 12, bay);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Contains("ShortShelfLife, ArrivedOutOfRange", first.ToString());
    }

    [Fact]
    public void Decisions_that_differ_by_one_field_are_not_equal() {
        // Arrange
        Sscc        pallet = AnyPallet();
        BinLocation bay    = AnyBay();
        ReleaseDecision decision = new ReleaseDecision(pallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife], 12, bay);

        // Assert
        Assert.NotEqual(decision, new ReleaseDecision(pallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife], 13, bay));
        Assert.NotEqual(decision, new ReleaseDecision(pallet, ReleaseOutcome.Quarantined, [DecisionReason.ArrivedOutOfRange], 12, bay));
        Assert.NotEqual(decision, new ReleaseDecision(pallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife], 12, new BinLocation(bay.Aisle, bay.Rack, bay.Level == 1 ? 2 : 1)));
        Assert.False(decision.Equals(null));
    }

}
