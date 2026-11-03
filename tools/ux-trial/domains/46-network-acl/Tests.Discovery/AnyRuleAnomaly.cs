using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>A generator of arbitrary valid <see cref="RuleAnomaly" /> values: the related rule follows the kind.</summary>
public sealed class AnyRuleAnomaly : IAny<RuleAnomaly> {

    private readonly IAny<RuleAnomaly> _recipe;

    public AnyRuleAnomaly() {
        _recipe = Any.Combine(new AnyRuleId(), Any.Enum<AnomalyKind>(), new AnyRuleId(), (rule, kind, other) => {
            RuleId? related = kind == AnomalyKind.RedundantToDefault ? null : (other.Equals(rule) ? new RuleId(rule.Value + "x") : other);

            return new RuleAnomaly(rule, kind, related);
        });
    }

    public RuleAnomaly Generate() {
        return _recipe.Generate();
    }

}
