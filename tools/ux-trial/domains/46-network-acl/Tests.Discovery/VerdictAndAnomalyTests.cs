using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class VerdictAndAnomalyTests {

    // ---- Verdict

    [Fact]
    public void A_verdict_without_a_matched_rule_is_the_default_one() {
        Verdict verdict = new Verdict(RuleAction.Deny, null);

        Assert.True(verdict.IsDefault);
        Assert.Null(verdict.MatchedRule);
    }

    [Fact]
    public void A_verdict_naming_a_rule_is_not_the_default_one() {
        // Arrange
        RuleId anyRule = Any.RuleId().Generate();

        // Act
        Verdict verdict = new Verdict(RuleAction.Allow, anyRule);

        // Assert
        Assert.False(verdict.IsDefault);
    }

    [Theory]
    [InlineData(RuleAction.Allow, true)]
    [InlineData(RuleAction.Deny, false)]
    public void IsAllowed_follows_the_action_whichever_rule_decided(RuleAction action, bool expected) {
        RuleId anyRule = Any.RuleId().Generate();

        Assert.Equal(expected, new Verdict(action, anyRule).IsAllowed);
        Assert.Equal(expected, new Verdict(action, null).IsAllowed);
    }

    [Fact]
    public void An_undefined_action_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Verdict((RuleAction)7, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Verdict((RuleAction)(-1), new RuleId("x")));
    }

    [Fact]
    public void Verdicts_are_equal_on_action_and_rule() {
        Assert.Equal(new Verdict(RuleAction.Allow, new RuleId("web")), new Verdict(RuleAction.Allow, new RuleId("web")));
        Assert.Equal(new Verdict(RuleAction.Allow, null), new Verdict(RuleAction.Allow, null));
        Assert.Equal(new Verdict(RuleAction.Allow, new RuleId("web")).GetHashCode(), new Verdict(RuleAction.Allow, new RuleId("web")).GetHashCode());
        Assert.NotEqual(new Verdict(RuleAction.Allow, new RuleId("web")), new Verdict(RuleAction.Deny, new RuleId("web")));
        Assert.NotEqual(new Verdict(RuleAction.Allow, new RuleId("web")), new Verdict(RuleAction.Allow, new RuleId("dns")));
        Assert.NotEqual(new Verdict(RuleAction.Allow, new RuleId("web")), new Verdict(RuleAction.Allow, null));
    }

    [Fact]
    public void ToString_names_the_action_and_the_rule_or_the_default() {
        Assert.Equal("Allow (default)", new Verdict(RuleAction.Allow, null).ToString());
        Assert.Equal("Deny by block-ssh", new Verdict(RuleAction.Deny, new RuleId("block-ssh")).ToString());
    }

    // ---- RuleAnomaly

    [Theory]
    [InlineData(AnomalyKind.Shadowed, true)]
    [InlineData(AnomalyKind.RedundantToEarlier, true)]
    [InlineData(AnomalyKind.RedundantToLater, false)]
    public void Shadowed_and_redundant_to_earlier_rules_are_the_unreachable_ones(AnomalyKind kind, bool expected) {
        // Arrange
        RuleId anyRule    = Any.RuleId().Generate();
        RuleId anyRelated = Any.RuleId().DifferentFrom(anyRule).Generate();

        // Act
        RuleAnomaly anomaly = new RuleAnomaly(anyRule, kind, anyRelated);

        // Assert
        Assert.Equal(expected, anomaly.IsUnreachable);
    }

    [Fact]
    public void A_redundancy_with_the_default_is_not_unreachable() {
        RuleId anyRule = Any.RuleId().Generate();

        Assert.False(new RuleAnomaly(anyRule, AnomalyKind.RedundantToDefault, null).IsUnreachable);
    }

    [Fact]
    public void A_redundancy_with_the_default_names_no_related_rule() {
        RuleId anyRule    = Any.RuleId().Generate();
        RuleId anyRelated = Any.RuleId().DifferentFrom(anyRule).Generate();

        Assert.Throws<ArgumentException>(() => new RuleAnomaly(anyRule, AnomalyKind.RedundantToDefault, anyRelated));
    }

    [Theory]
    [InlineData(AnomalyKind.Shadowed)]
    [InlineData(AnomalyKind.RedundantToEarlier)]
    [InlineData(AnomalyKind.RedundantToLater)]
    public void Every_other_kind_names_the_rule_that_causes_it(AnomalyKind kind) {
        RuleId anyRule = Any.RuleId().Generate();

        Assert.Throws<ArgumentNullException>(() => new RuleAnomaly(anyRule, kind, null));
    }

    [Theory]
    [InlineData(AnomalyKind.Shadowed)]
    [InlineData(AnomalyKind.RedundantToEarlier)]
    [InlineData(AnomalyKind.RedundantToLater)]
    public void A_rule_cannot_be_made_useless_by_itself(AnomalyKind kind) {
        RuleId anyRule = Any.RuleId().Generate();

        Assert.Throws<ArgumentException>(() => new RuleAnomaly(anyRule, kind, new RuleId(anyRule.Value)));
    }

    [Fact]
    public void An_undefined_kind_is_refused() {
        RuleId anyRule = Any.RuleId().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new RuleAnomaly(anyRule, (AnomalyKind)9, null));
    }

    [Fact]
    public void A_null_rule_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RuleAnomaly(null!, AnomalyKind.RedundantToDefault, null));
    }

    [Fact]
    public void An_anomaly_is_equal_to_another_with_the_same_rule_kind_and_related_rule() {
        RuleAnomaly first  = new RuleAnomaly(new RuleId("b"), AnomalyKind.Shadowed, new RuleId("a"));
        RuleAnomaly second = new RuleAnomaly(new RuleId("b"), AnomalyKind.Shadowed, new RuleId("a"));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new RuleAnomaly(new RuleId("b"), AnomalyKind.RedundantToEarlier, new RuleId("a")));
        Assert.NotEqual(first, new RuleAnomaly(new RuleId("c"), AnomalyKind.Shadowed, new RuleId("a")));
        Assert.NotEqual(first, new RuleAnomaly(new RuleId("b"), AnomalyKind.Shadowed, new RuleId("z")));
    }

    [Fact]
    public void ToString_names_the_rule_the_kind_and_the_cause() {
        Assert.Equal("b: Shadowed by a", new RuleAnomaly(new RuleId("b"), AnomalyKind.Shadowed, new RuleId("a")).ToString());
        Assert.Equal("b: RedundantToDefault", new RuleAnomaly(new RuleId("b"), AnomalyKind.RedundantToDefault, null).ToString());
    }


}
