using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RuleAnomaly" /> values. Written by hand: the related rule is absent
///     exactly for <see cref="AnomalyKind.RedundantToDefault" /> and is never the rule itself.
/// </summary>
public sealed class AnyRuleAnomaly : IAny<RuleAnomaly> {

    private readonly AnomalyKind? _kind;

    public AnyRuleAnomaly()
        : this(null) { }

    private AnyRuleAnomaly(AnomalyKind? kind) {
        _kind = kind;
    }

    public AnyRuleAnomaly WithKind(AnomalyKind kind) {
        return new AnyRuleAnomaly(kind);
    }

    public RuleAnomaly Generate() {
        IAny<AnomalyKind> kinds = _kind is { } kind ? Any.Fixed(kind) : Any.Enum<AnomalyKind>();

        return Any.Combine(kinds,
                           Any.ListOf(new AnyRuleId()).WithCount(2).Distinct(),
                           (kind, ids) => new RuleAnomaly(ids[0],
                                                          kind,
                                                          kind == AnomalyKind.RedundantToDefault ? null : ids[1])).Generate();
    }

}
