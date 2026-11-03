using System;

using JustDummies;

using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>
///     Arbitrary <see cref="RuleAnomaly" /> values. Written by hand: the scaffold left all three parameters "to verify" because
///     the related rule is absent exactly for <see cref="AnomalyKind.RedundantToDefault" /> and never equals the rule.
///     Here the related rule is derived from the kind, and drawn different from the rule.
/// </summary>
public sealed class AnyRuleAnomaly : IAny<RuleAnomaly> {

    private RuleId?      _rule;
    private AnomalyKind? _kind;

    public AnyRuleAnomaly WithRule(RuleId value) {
        AnyRuleAnomaly copy = (AnyRuleAnomaly)MemberwiseClone();
        copy._rule = value;

        return copy;
    }

    public AnyRuleAnomaly WithKind(AnomalyKind value) {
        AnyRuleAnomaly copy = (AnyRuleAnomaly)MemberwiseClone();
        copy._kind = value;

        return copy;
    }

    public RuleAnomaly Generate() {
        RuleId      rule = _rule ?? new AnyRuleId().Generate();
        AnomalyKind kind = _kind ?? Any.Enum<AnomalyKind>().Generate();

        if (kind == AnomalyKind.RedundantToDefault) { return new RuleAnomaly(rule, kind, null); }

        RuleId related = new AnyRuleId().DifferentFrom(rule).Generate();

        return new RuleAnomaly(rule, kind, related);
    }

}
