namespace NetworkAcl.Domain;

/// <summary>
/// A finding of the policy analysis: a rule, how it is useless, and the rule responsible. The related rule is
/// absent exactly when the anomaly is <see cref="AnomalyKind.RedundantToDefault"/>, and is never the rule itself.
/// </summary>
public sealed class RuleAnomaly : IEquatable<RuleAnomaly>
{
    public RuleAnomaly(RuleId rule, AnomalyKind kind, RuleId? relatedRule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown anomaly kind.");
        }

        if (kind == AnomalyKind.RedundantToDefault && relatedRule is not null)
        {
            throw new ArgumentException("A rule redundant with the default action has no related rule.", nameof(relatedRule));
        }

        if (kind != AnomalyKind.RedundantToDefault && relatedRule is null)
        {
            throw new ArgumentNullException(nameof(relatedRule), $"A {kind} anomaly names the rule that causes it.");
        }

        if (rule.Equals(relatedRule))
        {
            throw new ArgumentException("A rule cannot be made useless by itself.", nameof(relatedRule));
        }

        Rule = rule;
        Kind = kind;
        RelatedRule = relatedRule;
    }

    public RuleId Rule { get; }

    public AnomalyKind Kind { get; }

    public RuleId? RelatedRule { get; }

    /// <summary>Shadowed and redundant-to-earlier rules can never match a packet.</summary>
    public bool IsUnreachable => Kind is AnomalyKind.Shadowed or AnomalyKind.RedundantToEarlier;

    public bool Equals(RuleAnomaly? other) =>
        other is not null && other.Rule.Equals(Rule) && other.Kind == Kind && Equals(other.RelatedRule, RelatedRule);

    public override bool Equals(object? obj) => Equals(obj as RuleAnomaly);

    public override int GetHashCode() => HashCode.Combine(Rule, Kind, RelatedRule);

    public override string ToString() => RelatedRule is null ? $"{Rule}: {Kind}" : $"{Rule}: {Kind} by {RelatedRule}";
}
