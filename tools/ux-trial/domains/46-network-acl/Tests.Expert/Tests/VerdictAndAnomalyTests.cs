using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

using static NetworkAcl.Tests.Expert.Shorthand;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class VerdictTests {

    [Fact]
    public void A_verdict_without_a_rule_is_the_default_one() {
        // Act
        Verdict verdict = new Verdict(RuleAction.Deny, null);

        // Assert
        Assert.True(verdict.IsDefault);
        Assert.Null(verdict.MatchedRule);
        Assert.Equal(RuleAction.Deny, verdict.Action);
    }

    [Fact]
    public void A_verdict_with_a_rule_is_not_the_default_one() {
        // Arrange
        RuleId anyRule = Any.RuleId().Generate();

        // Act
        Verdict verdict = new Verdict(RuleAction.Allow, anyRule);

        // Assert
        Assert.False(verdict.IsDefault);
        Assert.Equal(anyRule, verdict.MatchedRule);
    }

    [Theory]
    [InlineData(RuleAction.Allow, true)]
    [InlineData(RuleAction.Deny, false)]
    public void IsAllowed_follows_the_action(RuleAction action, bool expected) {
        // Arrange
        RuleId? anyRule = Any.RuleId().OrNull().Generate();

        // Act & Assert
        Assert.Equal(expected, new Verdict(action, anyRule).IsAllowed);
    }

    [Fact]
    public void An_undefined_action_is_refused() {
        // Arrange
        RuleId? anyRule = Any.RuleId().OrNull().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Verdict((RuleAction)7, anyRule));
    }

    [Fact]
    public void Verdicts_with_the_same_action_and_rule_are_equal_and_hash_alike() {
        // Arrange
        Verdict first  = new Verdict(RuleAction.Allow, Id("allow-web"));
        Verdict second = new Verdict(RuleAction.Allow, Id("allow-web"));

        // Act & Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Verdicts_differing_by_action_or_rule_are_not_equal() {
        Assert.NotEqual(new Verdict(RuleAction.Allow, Id("a")), new Verdict(RuleAction.Deny, Id("a")));
        Assert.NotEqual(new Verdict(RuleAction.Allow, Id("a")), new Verdict(RuleAction.Allow, Id("b")));
        Assert.NotEqual(new Verdict(RuleAction.Allow, Id("a")), new Verdict(RuleAction.Allow, null));
        Assert.False(new Verdict(RuleAction.Allow, null).Equals(null));
    }

    [Theory]
    [InlineData(RuleAction.Allow, null, "Allow (default)")]
    [InlineData(RuleAction.Deny, null, "Deny (default)")]
    [InlineData(RuleAction.Allow, "allow-web", "Allow by allow-web")]
    [InlineData(RuleAction.Deny, "block-ssh", "Deny by block-ssh")]
    public void ToString_names_the_action_and_the_deciding_rule(RuleAction action, string? ruleText, string expectedText) {
        // Arrange
        RuleId? rule = ruleText is null ? null : Id(ruleText);

        // Act & Assert
        Assert.Equal(expectedText, new Verdict(action, rule).ToString());
    }

}

public sealed class RuleAnomalyTests {

    [Theory]
    [InlineData(AnomalyKind.Shadowed)]
    [InlineData(AnomalyKind.RedundantToEarlier)]
    [InlineData(AnomalyKind.RedundantToLater)]
    public void An_anomaly_between_two_rules_names_the_related_one(AnomalyKind kind) {
        // Arrange
        RuleId anyRule    = Any.RuleId().Generate();
        RuleId anyRelated = Any.RuleId().DifferentFrom(anyRule).Generate();

        // Act
        RuleAnomaly anomaly = new RuleAnomaly(anyRule, kind, anyRelated);

        // Assert
        Assert.Equal(kind, anomaly.Kind);
        Assert.Equal(anyRelated, anomaly.RelatedRule);
    }

    [Fact]
    public void An_anomaly_redundant_to_the_default_has_no_related_rule() {
        // Arrange
        RuleId anyRule = Any.RuleId().Generate();

        // Act
        RuleAnomaly anomaly = new RuleAnomaly(anyRule, AnomalyKind.RedundantToDefault, null);

        // Assert
        Assert.Null(anomaly.RelatedRule);
    }

    [Fact]
    public void An_anomaly_redundant_to_the_default_refuses_a_related_rule() {
        // Arrange
        RuleId anyRule    = Any.RuleId().Generate();
        RuleId anyRelated = Any.RuleId().DifferentFrom(anyRule).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RuleAnomaly(anyRule, AnomalyKind.RedundantToDefault, anyRelated));

        // Assert
        Assert.Equal("relatedRule", refusal.ParamName);
    }

    [Theory]
    [InlineData(AnomalyKind.Shadowed)]
    [InlineData(AnomalyKind.RedundantToEarlier)]
    [InlineData(AnomalyKind.RedundantToLater)]
    public void Any_other_anomaly_refuses_a_missing_related_rule(AnomalyKind kind) {
        // Arrange
        RuleId anyRule = Any.RuleId().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new RuleAnomaly(anyRule, kind, null));

        // Assert
        Assert.Equal("relatedRule", refusal.ParamName);
    }

    [Theory]
    [InlineData(AnomalyKind.Shadowed)]
    [InlineData(AnomalyKind.RedundantToEarlier)]
    [InlineData(AnomalyKind.RedundantToLater)]
    public void A_rule_cannot_be_made_useless_by_itself(AnomalyKind kind) {
        // Arrange
        RuleId anyRule = Any.RuleId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RuleAnomaly(anyRule, kind, anyRule));
    }

    [Fact]
    public void An_undefined_kind_is_refused() {
        // Arrange
        RuleId anyRule    = Any.RuleId().Generate();
        RuleId anyRelated = Any.RuleId().DifferentFrom(anyRule).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new RuleAnomaly(anyRule, (AnomalyKind)9, anyRelated));
    }

    [Fact]
    public void A_missing_rule_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RuleAnomaly(null!, AnomalyKind.RedundantToDefault, null));
    }

    [Theory]
    [InlineData(AnomalyKind.Shadowed, true)]
    [InlineData(AnomalyKind.RedundantToEarlier, true)]
    [InlineData(AnomalyKind.RedundantToLater, false)]
    [InlineData(AnomalyKind.RedundantToDefault, false)]
    public void Only_the_shadowed_and_the_redundant_to_earlier_rules_are_unreachable(AnomalyKind kind, bool expected) {
        // Arrange: rule and related rule are dummies, the kind alone decides.
        RuleAnomaly anyAnomaly = Any.RuleAnomaly().WithKind(kind).Generate();

        // Act & Assert
        Assert.Equal(expected, anyAnomaly.IsUnreachable);
    }

    [Fact]
    public void Anomalies_with_the_same_rule_kind_and_related_rule_are_equal_and_hash_alike() {
        // Arrange
        RuleAnomaly first  = new RuleAnomaly(Id("narrow"), AnomalyKind.Shadowed, Id("broad"));
        RuleAnomaly second = new RuleAnomaly(Id("narrow"), AnomalyKind.Shadowed, Id("broad"));

        // Act & Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new RuleAnomaly(Id("narrow"), AnomalyKind.RedundantToEarlier, Id("broad")));
        Assert.NotEqual(first, new RuleAnomaly(Id("narrow"), AnomalyKind.Shadowed, Id("other")));
        Assert.NotEqual(first, new RuleAnomaly(Id("other"), AnomalyKind.Shadowed, Id("broad")));
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void ToString_names_the_rule_the_kind_and_the_related_rule() {
        Assert.Equal("narrow: Shadowed by broad", new RuleAnomaly(Id("narrow"), AnomalyKind.Shadowed, Id("broad")).ToString());
        Assert.Equal("last: RedundantToDefault", new RuleAnomaly(Id("last"), AnomalyKind.RedundantToDefault, null).ToString());
    }

}
