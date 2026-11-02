namespace NetworkAcl.Domain;

/// <summary>The outcome of evaluating a packet: the action, and the rule that decided it (null when the default action applied).</summary>
public sealed class Verdict : IEquatable<Verdict>
{
    public Verdict(RuleAction action, RuleId? matchedRule)
    {
        if (action != RuleAction.Allow && action != RuleAction.Deny)
        {
            throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown rule action.");
        }

        Action = action;
        MatchedRule = matchedRule;
    }

    public RuleAction Action { get; }

    public RuleId? MatchedRule { get; }

    public bool IsDefault => MatchedRule is null;

    public bool IsAllowed => Action == RuleAction.Allow;

    public bool Equals(Verdict? other) => other is not null && other.Action == Action && Equals(other.MatchedRule, MatchedRule);

    public override bool Equals(object? obj) => Equals(obj as Verdict);

    public override int GetHashCode() => HashCode.Combine(Action, MatchedRule);

    public override string ToString() => IsDefault ? $"{Action} (default)" : $"{Action} by {MatchedRule}";
}
