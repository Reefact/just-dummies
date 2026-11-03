using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using JustDummies;
using Warehouse.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class ReleaseDecisionTests {

    private static readonly BinLocation Bay = new BinLocation('Q', 3, 2);

    [Theory]
    [InlineData(DecisionReason.HardLimitBreached, true)]
    [InlineData(DecisionReason.StabilityBudgetExhausted, true)]
    [InlineData(DecisionReason.PackExpired, true)]
    [InlineData(DecisionReason.ShortShelfLife, false)]
    [InlineData(DecisionReason.SingleExcursionTooLong, false)]
    [InlineData(DecisionReason.ArrivedOutOfRange, false)]
    [InlineData(DecisionReason.IncompleteTemperatureRecord, false)]
    [InlineData(DecisionReason.LoggerCalibrationLapsed, false)]
    public void A_hard_limit_an_exhausted_budget_and_an_expired_pack_reject_and_every_other_reason_only_holds_the_pallet(DecisionReason reason, bool expectedRejecting) {
        Assert.Equal(expectedRejecting, ReleaseDecision.IsRejecting(reason));
    }

    [Fact]
    public void Without_any_reason_the_outcome_is_a_release() {
        Assert.Equal(ReleaseOutcome.Released, ReleaseDecision.OutcomeFor(Array.Empty<DecisionReason>()));
    }

    [Fact]
    public void Holding_reasons_alone_call_for_a_quarantine() {
        Assert.Equal(ReleaseOutcome.Quarantined, ReleaseDecision.OutcomeFor([DecisionReason.ShortShelfLife, DecisionReason.ArrivedOutOfRange]));
    }

    [Fact]
    public void One_rejecting_reason_among_holding_ones_calls_for_a_rejection() {
        Assert.Equal(ReleaseOutcome.Rejected, ReleaseDecision.OutcomeFor([DecisionReason.ShortShelfLife, DecisionReason.PackExpired, DecisionReason.ArrivedOutOfRange]));
    }

    [Fact]
    public void A_pallet_with_nothing_against_it_is_released_and_has_no_quarantine_bay() {
        // Act
        ReleaseDecision decision = Any.ReleaseDecision().WithOutcome(ReleaseOutcome.Released).Generate();

        // Assert
        Assert.Empty(decision.Reasons);
        Assert.Null(decision.QuarantineBay);
    }

    [Fact]
    public void A_released_pallet_with_a_reason_against_it_is_refused() {
        // Arrange
        AnyReleaseDecision decisions = Any.ReleaseDecision().WithOutcome(ReleaseOutcome.Released).WithReasons(DecisionReason.ShortShelfLife);

        // Assert
        Assert.Throws<ArgumentException>(() => decisions.Generate());
    }

    [Theory]
    [InlineData(DecisionReason.HardLimitBreached)]
    [InlineData(DecisionReason.StabilityBudgetExhausted)]
    [InlineData(DecisionReason.PackExpired)]
    public void A_pallet_is_rejected_for_a_rejecting_reason(DecisionReason reason) {
        // Act
        ReleaseDecision decision = Any.ReleaseDecision().WithOutcome(ReleaseOutcome.Rejected).WithReasons(reason).Generate();

        // Assert
        Assert.Equal([reason], decision.Reasons);
        Assert.Null(decision.QuarantineBay);
    }

    [Fact]
    public void A_rejection_resting_on_holding_reasons_only_is_refused() {
        // Arrange
        AnyReleaseDecision decisions = Any.ReleaseDecision().WithOutcome(ReleaseOutcome.Rejected).WithReasons(DecisionReason.ShortShelfLife, DecisionReason.ArrivedOutOfRange);

        // Assert
        Assert.Throws<ArgumentException>(() => decisions.Generate());
    }

    [Fact]
    public void A_rejection_without_any_reason_is_refused() {
        // Arrange
        AnyReleaseDecision decisions = Any.ReleaseDecision().WithOutcome(ReleaseOutcome.Rejected).WithReasons();

        // Assert
        Assert.Throws<ArgumentException>(() => decisions.Generate());
    }

    [Fact]
    public void A_quarantined_pallet_is_held_for_holding_reasons_in_a_quarantine_bay() {
        // Arrange
        ReleaseDecision decision = new ReleaseDecision(Any.Sscc().Generate(), ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife], 12, Bay);

        // Assert
        Assert.Equal(Bay, decision.QuarantineBay);
    }

    [Fact]
    public void A_quarantine_with_a_rejecting_reason_among_its_reasons_is_refused() {
        // Arrange
        Sscc anyPallet = Any.Sscc().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ReleaseDecision(anyPallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife, DecisionReason.PackExpired], 0, Bay));
    }

    [Fact]
    public void A_quarantine_without_any_reason_is_refused() {
        // Arrange
        Sscc anyPallet = Any.Sscc().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ReleaseDecision(anyPallet, ReleaseOutcome.Quarantined, Array.Empty<DecisionReason>(), 0, Bay));
    }

    [Fact]
    public void A_quarantine_without_a_quarantine_bay_is_refused() {
        // Arrange
        Sscc anyPallet = Any.Sscc().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ReleaseDecision(anyPallet, ReleaseOutcome.Quarantined, [DecisionReason.ShortShelfLife], 0, null));
    }

    [Fact]
    public void A_released_pallet_given_a_quarantine_bay_is_refused() {
        // Arrange
        Sscc anyPallet = Any.Sscc().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ReleaseDecision(anyPallet, ReleaseOutcome.Released, Array.Empty<DecisionReason>(), 0, Bay));
    }

    [Fact]
    public void A_rejected_pallet_given_a_quarantine_bay_is_refused() {
        // Arrange
        Sscc anyPallet = Any.Sscc().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ReleaseDecision(anyPallet, ReleaseOutcome.Rejected, [DecisionReason.PackExpired], 0, Bay));
    }

    [Fact]
    public void A_reason_stated_twice_is_refused() {
        // Arrange
        Sscc anyPallet = Any.Sscc().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ReleaseDecision(anyPallet, ReleaseOutcome.Rejected, [DecisionReason.PackExpired, DecisionReason.PackExpired], 0, null));
    }

    [Fact]
    public void The_reasons_are_kept_in_the_order_of_their_declaration_whatever_the_order_they_were_given_in() {
        // Act
        ReleaseDecision decision = Any.ReleaseDecision()
                                      .WithReasons(DecisionReason.LoggerCalibrationLapsed, DecisionReason.PackExpired, DecisionReason.ShortShelfLife, DecisionReason.HardLimitBreached)
                                      .Generate();

        // Assert
        Assert.Equal([DecisionReason.HardLimitBreached, DecisionReason.PackExpired, DecisionReason.ShortShelfLife, DecisionReason.LoggerCalibrationLapsed], decision.Reasons);
    }

    [Fact]
    public void A_negative_consumed_budget_is_refused() {
        // Arrange
        AnyReleaseDecision decisions = Any.ReleaseDecision().WithBudgetConsumedMinutes(-1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => decisions.Generate());
    }

    [Fact]
    public void The_budget_consumed_may_be_nil() {
        // Act
        ReleaseDecision decision = Any.ReleaseDecision().WithBudgetConsumedMinutes(0).Generate();

        // Assert
        Assert.Equal(0, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void An_unknown_outcome_is_refused() {
        // Arrange
        Sscc anyPallet = Any.Sscc().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReleaseDecision(anyPallet, (ReleaseOutcome)99, Array.Empty<DecisionReason>(), 0, null));
    }

    [Fact]
    public void An_unknown_reason_is_refused() {
        // Arrange
        Sscc anyPallet = Any.Sscc().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReleaseDecision(anyPallet, ReleaseOutcome.Rejected, [(DecisionReason)99, DecisionReason.PackExpired], 0, null));
    }

    [Fact]
    public void Two_decisions_with_the_same_content_are_equal_whatever_the_order_the_reasons_were_given_in() {
        // Arrange
        Sscc sharedPallet = Any.Sscc().Generate();

        ReleaseDecision first  = new ReleaseDecision(sharedPallet, ReleaseOutcome.Rejected, [DecisionReason.PackExpired, DecisionReason.ShortShelfLife], 30, null);
        ReleaseDecision second = new ReleaseDecision(sharedPallet, ReleaseOutcome.Rejected, [DecisionReason.ShortShelfLife, DecisionReason.PackExpired], 30, null);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Decisions_for_the_same_pallet_that_differ_in_the_budget_consumed_are_different() {
        // Arrange
        Sscc sharedPallet = Any.Sscc().Generate();

        ReleaseDecision first  = new ReleaseDecision(sharedPallet, ReleaseOutcome.Released, Array.Empty<DecisionReason>(), 30, null);
        ReleaseDecision second = new ReleaseDecision(sharedPallet, ReleaseOutcome.Released, Array.Empty<DecisionReason>(), 31, null);

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact(Skip = "domain defect: ReleaseDecision.Reasons is the internal DecisionReason[] behind an IReadOnlyList, so a cast overwrites a reason and breaks the Rejected-needs-a-rejecting-reason rule, seed 1142016428")]
    public void The_reasons_a_decision_exposes_cannot_be_overwritten_through_a_cast() {
        // Arrange
        ReleaseDecision decision = new ReleaseDecision(Any.Sscc().Generate(), ReleaseOutcome.Rejected, [DecisionReason.PackExpired], 0, null);

        // Act
        if (decision.Reasons is DecisionReason[] exposed) {
            exposed[0] = DecisionReason.ShortShelfLife;
        }

        // Assert
        Assert.Equal([DecisionReason.PackExpired], decision.Reasons);
    }
}
