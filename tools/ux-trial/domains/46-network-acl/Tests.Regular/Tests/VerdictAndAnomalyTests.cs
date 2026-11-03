using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Regular;

public sealed class VerdictAndAnomalyTests {

    [Fact]
    public void A_verdict_without_a_rule_is_the_default_one() {
        // Arrange
        RuleAction anyAction = Any.Enum<RuleAction>().Generate();

        // Act
        Verdict verdict = new Verdict(anyAction, null);

        // Assert
        Assert.True(verdict.IsDefault);
        Assert.Equal(anyAction, verdict.Action);
        Assert.Null(verdict.MatchedRule);
    }

    [Fact]
    public void A_verdict_with_a_rule_is_not_the_default_one() {
        // Arrange
        RuleId anyRule = Any.RuleId().Generate();

        // Act
        Verdict verdict = new Verdict(RuleAction.Deny, anyRule);

        // Assert
        Assert.False(verdict.IsDefault);
        Assert.Equal(anyRule, verdict.MatchedRule);
    }

    [Theory]
    [InlineData(RuleAction.Allow, true)]
    [InlineData(RuleAction.Deny, false)]
    public void A_verdict_is_allowed_when_its_action_is_allow(RuleAction action, bool expected) {
        // Arrange
        RuleId anyRule = Any.RuleId().Generate();

        // Assert
        Assert.Equal(expected, new Verdict(action, anyRule).IsAllowed);
        Assert.Equal(expected, new Verdict(action, null).IsAllowed);
    }

    [Fact]
    public void An_undefined_action_is_refused() {
        // Arrange
        RuleAction undefined = (RuleAction)Any.Int32().Between(2, 100).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Verdict(undefined, null));
    }

    [Fact]
    public void Verdicts_are_equal_by_action_and_rule() {
        // Arrange
        Verdict anyVerdict = Any.Verdict().Generate();
        Verdict copy       = new Verdict(anyVerdict.Action, anyVerdict.MatchedRule);
        Verdict otherAction = new Verdict(anyVerdict.Action == RuleAction.Allow ? RuleAction.Deny : RuleAction.Allow, anyVerdict.MatchedRule);

        // Assert
        Assert.Equal(anyVerdict, copy);
        Assert.Equal(anyVerdict.GetHashCode(), copy.GetHashCode());
        Assert.NotEqual(anyVerdict, otherAction);
        Assert.False(anyVerdict.Equals(null));
    }

    [Fact]
    public void A_verdict_reads_as_its_action_and_the_rule_that_decided_it() {
        // Assert
        Assert.Equal("Allow (default)", new Verdict(RuleAction.Allow, null).ToString());
        Assert.Equal("Deny by block-ssh", new Verdict(RuleAction.Deny, new RuleId("block-ssh")).ToString());
    }

    [Theory]
    [InlineData(AnomalyKind.Shadowed)]
    [InlineData(AnomalyKind.RedundantToEarlier)]
    [InlineData(AnomalyKind.RedundantToLater)]
    public void An_anomaly_names_the_rule_that_causes_it(AnomalyKind kind) {
        // Arrange
        List<RuleId> anyTwoRules = Any.ListOf(Any.RuleId()).WithCount(2).Distinct().Generate();
        RuleId       anyRule     = anyTwoRules[0];
        RuleId       otherRule   = anyTwoRules[1];

        // Act
        RuleAnomaly anomaly = new RuleAnomaly(anyRule, kind, otherRule);

        // Assert
        Assert.Equal(anyRule, anomaly.Rule);
        Assert.Equal(kind, anomaly.Kind);
        Assert.Equal(otherRule, anomaly.RelatedRule);
    }

    [Theory]
    [InlineData(AnomalyKind.Shadowed)]
    [InlineData(AnomalyKind.RedundantToEarlier)]
    [InlineData(AnomalyKind.RedundantToLater)]
    public void An_anomaly_without_the_rule_that_causes_it_is_refused(AnomalyKind kind) {
        // Arrange
        RuleId anyRule = Any.RuleId().Generate();

        // Act
        ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(() => new RuleAnomaly(anyRule, kind, null));

        // Assert
        Assert.Equal("relatedRule", thrown.ParamName);
    }

    [Fact]
    public void Redundancy_with_the_default_has_no_related_rule() {
        // Arrange
        RuleAnomaly anyAnomaly = Any.RuleAnomaly().WithKind(AnomalyKind.RedundantToDefault).Generate();
        RuleId      anyRule    = Any.RuleId().Generate();
        RuleId      otherRule  = new RuleId("x" + anyRule.Value);

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new RuleAnomaly(anyRule, AnomalyKind.RedundantToDefault, otherRule));

        // Assert
        Assert.Null(anyAnomaly.RelatedRule);
        Assert.Equal("relatedRule", thrown.ParamName);
    }

    [Theory]
    [InlineData(AnomalyKind.Shadowed)]
    [InlineData(AnomalyKind.RedundantToEarlier)]
    [InlineData(AnomalyKind.RedundantToLater)]
    public void A_rule_cannot_be_made_useless_by_itself(AnomalyKind kind) {
        // Arrange
        RuleId anyRule = Any.RuleId().Generate();
        RuleId sameRule = new RuleId(anyRule.Value);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RuleAnomaly(anyRule, kind, sameRule));
    }

    [Fact]
    public void An_undefined_kind_is_refused_before_anything_else() {
        // Arrange
        AnomalyKind undefined = (AnomalyKind)Any.Int32().Between(4, 100).Generate();
        RuleId      anyRule   = Any.RuleId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new RuleAnomaly(anyRule, undefined, null));
    }

    [Fact]
    public void An_anomaly_needs_a_rule() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RuleAnomaly(null!, AnomalyKind.RedundantToDefault, null));
    }

    [Theory]
    [InlineData(AnomalyKind.Shadowed, true)]
    [InlineData(AnomalyKind.RedundantToEarlier, true)]
    [InlineData(AnomalyKind.RedundantToLater, false)]
    [InlineData(AnomalyKind.RedundantToDefault, false)]
    public void Only_shadowed_and_redundant_to_earlier_rules_are_unreachable(AnomalyKind kind, bool expected) {
        // Arrange
        RuleAnomaly anyAnomaly = Any.RuleAnomaly().WithKind(kind).Generate();

        // Assert
        Assert.Equal(expected, anyAnomaly.IsUnreachable);
    }

    [Fact]
    public void Anomalies_are_equal_by_rule_kind_and_related_rule() {
        // Arrange
        RuleAnomaly anyAnomaly = Any.RuleAnomaly().Generate();
        RuleAnomaly copy       = new RuleAnomaly(anyAnomaly.Rule, anyAnomaly.Kind, anyAnomaly.RelatedRule);

        // Assert
        Assert.Equal(anyAnomaly, copy);
        Assert.Equal(anyAnomaly.GetHashCode(), copy.GetHashCode());
        Assert.False(anyAnomaly.Equals(null));
    }

    [Fact]
    public void An_anomaly_reads_as_the_rule_the_kind_and_the_cause() {
        // Assert
        Assert.Equal("ssh: Shadowed by allow-all", new RuleAnomaly(new RuleId("ssh"), AnomalyKind.Shadowed, new RuleId("allow-all")).ToString());
        Assert.Equal("ssh: RedundantToDefault", new RuleAnomaly(new RuleId("ssh"), AnomalyKind.RedundantToDefault, null).ToString());
    }

}
