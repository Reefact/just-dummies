using ColdChain.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class ReleaseDecisionTests {

    private static readonly DecisionReason[] Rejecting = [DecisionReason.HardLimitBreached, DecisionReason.StabilityBudgetExhausted, DecisionReason.PackExpired];
    private static readonly DecisionReason[] Holding   = [DecisionReason.ShortShelfLife, DecisionReason.SingleExcursionTooLong, DecisionReason.ArrivedOutOfRange, DecisionReason.IncompleteTemperatureRecord, DecisionReason.LoggerCalibrationLapsed];

    private static BinLocation Bay() {
        return new BinLocation('Q', 3, 2);
    }

    [Fact]
    public void A_released_pallet_has_no_reason_and_no_bay() {
        // Arrange
        Sscc anyPallet = new AnySscc().Generate();

        // Act
        ReleaseDecision decision = new ReleaseDecision(anyPallet, ReleaseOutcome.Released, [], 15, null);

        // Assert
        Assert.Same(anyPallet, decision.Pallet);
        Assert.Equal(ReleaseOutcome.Released, decision.Outcome);
        Assert.Empty(decision.Reasons);
        Assert.Equal(15, decision.BudgetConsumedMinutes);
        Assert.Null(decision.QuarantineBay);
    }

    [Fact]
    public void A_released_pallet_with_a_reason_is_refused() {
        // Arrange
        DecisionReason anyReason = Any.Enum<DecisionReason>().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Released, [anyReason], 0, null));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("reasons", refusal.ParamName);
    }

    [Fact]
    public void A_rejected_pallet_needs_a_rejecting_reason() {
        // Arrange
        Sscc anyPallet = new AnySscc().Generate();

        // Act
        ReleaseDecision decision = new ReleaseDecision(anyPallet, ReleaseOutcome.Rejected, [DecisionReason.PackExpired], 0, null);

        // Assert
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.PackExpired], decision.Reasons);
    }

    [Fact]
    public void A_rejection_for_holding_reasons_only_is_refused() {
        // Arrange
        Sscc anyPallet = new AnySscc().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ReleaseDecision(anyPallet, ReleaseOutcome.Rejected, Holding, 0, null));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("reasons", refusal.ParamName);
    }

    [Fact]
    public void A_rejection_without_any_reason_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Rejected, [], 0, null));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void A_rejection_may_carry_holding_reasons_beside_the_rejecting_one() {
        // Act
        ReleaseDecision decision = new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Rejected, [DecisionReason.ArrivedOutOfRange, DecisionReason.StabilityBudgetExhausted], 400, null);

        // Assert
        Assert.Equal(2, decision.Reasons.Count);
    }

    [Fact]
    public void A_quarantined_pallet_has_holding_reasons_and_sits_in_the_bay() {
        // Arrange
        BinLocation bay = Bay();

        // Act
        ReleaseDecision decision = new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife], 20, bay);

        // Assert
        Assert.Equal(ReleaseOutcome.Quarantined, decision.Outcome);
        Assert.Same(bay, decision.QuarantineBay);
    }

    [Fact]
    public void A_quarantine_without_a_reason_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Quarantined, [], 0, Bay()));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("reasons", refusal.ParamName);
    }

    [Theory]
    [InlineData(DecisionReason.HardLimitBreached)]
    [InlineData(DecisionReason.StabilityBudgetExhausted)]
    [InlineData(DecisionReason.PackExpired)]
    public void A_quarantine_for_a_rejecting_reason_is_refused(DecisionReason rejectingReason) {
        // Arrange
        DecisionReason anyHoldingReason = Any.ElementOf(Holding).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Quarantined, [anyHoldingReason, rejectingReason], 0, Bay()));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("reasons", refusal.ParamName);
    }

    [Fact]
    public void Only_a_quarantined_pallet_is_given_a_bay() {
        // Assert
        ArgumentException released = Assert.Throws<ArgumentException>(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Released, [], 0, Bay()));
        Assert.Equal("quarantineBay", released.ParamName);
        ArgumentException rejected = Assert.Throws<ArgumentException>(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Rejected, [DecisionReason.PackExpired], 0, Bay()));
        Assert.Equal("quarantineBay", rejected.ParamName);
    }

    [Fact]
    public void A_quarantined_pallet_without_a_bay_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife], 0, null));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("quarantineBay", refusal.ParamName);
    }

    [Fact]
    public void A_reason_stated_twice_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife, DecisionReason.ShortShelfLife], 0, Bay()));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("reasons", refusal.ParamName);
    }

    [Fact]
    public void Reasons_are_kept_sorted() {
        // Act
        ReleaseDecision decision = new ReleaseDecision(
            new AnySscc().Generate(),
            ReleaseOutcome.Rejected,
            [DecisionReason.LoggerCalibrationLapsed, DecisionReason.PackExpired, DecisionReason.ArrivedOutOfRange, DecisionReason.HardLimitBreached],
            0,
            null);

        // Assert
        Assert.Equal([DecisionReason.HardLimitBreached, DecisionReason.PackExpired, DecisionReason.ArrivedOutOfRange, DecisionReason.LoggerCalibrationLapsed], decision.Reasons);
    }

    [Fact]
    public void An_unknown_outcome_or_reason_is_refused() {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReleaseDecision(new AnySscc().Generate(), (ReleaseOutcome)99, [], 0, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Quarantined, [(DecisionReason)99], 0, Bay()));
    }

    [Fact]
    public void A_negative_consumption_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Released, [], -1, null));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void The_consumed_budget_is_carried_forward_whatever_the_outcome() {
        // Arrange
        int anyConsumption = Any.Int32().Between(0, StabilityBudget.MaxCumulativeMinutes).Generate();

        // Act
        ReleaseDecision decision = new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Released, [], anyConsumption, null);

        // Assert
        Assert.Equal(anyConsumption, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void A_pallet_and_its_reasons_are_required() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new ReleaseDecision(null!, ReleaseOutcome.Released, [], 0, null));
        Assert.Throws<ArgumentNullException>(() => new ReleaseDecision(new AnySscc().Generate(), ReleaseOutcome.Released, null!, 0, null));
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
    public void Three_reasons_reject_and_the_others_hold_the_pallet(DecisionReason reason, bool rejects) {
        // Act
        bool actual = ReleaseDecision.IsRejecting(reason);

        // Assert
        Assert.Equal(rejects, actual);
    }

    [Fact]
    public void The_outcome_follows_the_reasons() {
        // Assert
        Assert.Equal(ReleaseOutcome.Released, ReleaseDecision.OutcomeFor([]));
        Assert.Equal(ReleaseOutcome.Quarantined, ReleaseDecision.OutcomeFor([DecisionReason.ArrivedOutOfRange]));
        Assert.Equal(ReleaseOutcome.Quarantined, ReleaseDecision.OutcomeFor(Holding));
        Assert.Equal(ReleaseOutcome.Rejected, ReleaseDecision.OutcomeFor([DecisionReason.PackExpired]));
        Assert.Equal(ReleaseOutcome.Rejected, ReleaseDecision.OutcomeFor([DecisionReason.ShortShelfLife, DecisionReason.HardLimitBreached]));
        Assert.Equal(ReleaseOutcome.Rejected, ReleaseDecision.OutcomeFor(Rejecting));
    }

    [Fact]
    public void A_missing_list_of_reasons_has_no_outcome() {
        // Act
        Exception? thrown = Record.Exception(() => ReleaseDecision.OutcomeFor(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void Decisions_are_equal_when_all_their_parts_are() {
        // Arrange
        Sscc            anyPallet = new AnySscc().Generate();
        ReleaseDecision decision  = new ReleaseDecision(anyPallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife, DecisionReason.ArrivedOutOfRange], 30, Bay());
        ReleaseDecision same      = new ReleaseDecision(anyPallet, ReleaseOutcome.Quarantined, [DecisionReason.ArrivedOutOfRange, DecisionReason.ShortShelfLife], 30, Bay());

        // Assert
        Assert.Equal(decision, same);
        Assert.Equal(decision.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(decision, new ReleaseDecision(anyPallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife], 30, Bay()));
        Assert.NotEqual(decision, new ReleaseDecision(anyPallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife, DecisionReason.ArrivedOutOfRange], 31, Bay()));
        Assert.NotEqual(decision, new ReleaseDecision(anyPallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife, DecisionReason.ArrivedOutOfRange], 30, new BinLocation('Q', 3, 3)));
    }

    [Fact]
    public void A_decision_reads_back_the_pallet_the_outcome_and_the_reasons() {
        // Arrange
        Sscc anyPallet = new AnySscc().Generate();

        // Act
        string released    = new ReleaseDecision(anyPallet, ReleaseOutcome.Released, [], 0, null).ToString();
        string quarantined = new ReleaseDecision(anyPallet, ReleaseOutcome.Quarantined, [DecisionReason.ArrivedOutOfRange, DecisionReason.ShortShelfLife], 0, Bay()).ToString();

        // Assert
        Assert.Equal($"{anyPallet}: Released", released);
        Assert.Equal($"{anyPallet}: Quarantined (ShortShelfLife, ArrivedOutOfRange)", quarantined);
    }

    [Fact]
    public void The_generator_only_draws_decisions_the_constructor_accepts() {
        // Act
        ReleaseDecision anyDecision = new AnyReleaseDecision().Generate();

        // Assert
        Assert.Equal(ReleaseDecision.OutcomeFor(anyDecision.Reasons), anyDecision.Outcome);
        Assert.Equal(anyDecision.Outcome == ReleaseOutcome.Quarantined, anyDecision.QuarantineBay is not null);
    }

}
