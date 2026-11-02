namespace NetworkAcl.Domain;

/// <summary>
/// A stateless, NAT-free firewall policy: an ordered list of rules where the first match decides, and a default
/// action for packets no rule matches. It counts hits per rule and reports rules that can never matter. In strict
/// mode it refuses any change that would leave a rule unreachable behind an earlier one.
/// </summary>
public sealed class FirewallPolicy
{
    public const int MaxNameLength = 64;
    public const int MaxCapacity = 10_000;

    private readonly List<FirewallRule> _rules = [];
    private readonly Dictionary<RuleId, long> _hits = [];

    public FirewallPolicy(string name, RuleAction defaultAction, int capacity, bool strict)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException($"A policy name has at most {MaxNameLength} characters.", nameof(name));
        }

        if (!Enum.IsDefined(defaultAction))
        {
            throw new ArgumentOutOfRangeException(nameof(defaultAction), defaultAction, "Unknown default action.");
        }

        if (capacity < 1 || capacity > MaxCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"Capacity is between 1 and {MaxCapacity} rules.");
        }

        Name = name;
        DefaultAction = defaultAction;
        Capacity = capacity;
        IsStrict = strict;
    }

    public string Name { get; }

    public RuleAction DefaultAction { get; }

    public int Capacity { get; }

    public bool IsStrict { get; }

    /// <summary>Incremented by every accepted change to the rule list.</summary>
    public long Revision { get; private set; }

    public long DefaultHits { get; private set; }

    public IReadOnlyList<FirewallRule> Rules => _rules;

    public int Count => _rules.Count;

    public int IndexOf(RuleId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _rules.FindIndex(r => r.Id.Equals(id));
    }

    public FirewallRule? Find(RuleId id)
    {
        int index = IndexOf(id);
        return index < 0 ? null : _rules[index];
    }

    public long HitsOf(RuleId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (!_hits.TryGetValue(id, out long hits))
        {
            throw new PolicyRefusedException(PolicyRefusalReason.RuleNotFound, $"Policy {Name} has no rule {id}.");
        }

        return hits;
    }

    public void Append(FirewallRule rule) => Insert(_rules.Count, rule);

    /// <summary>Inserts the rule at <paramref name="position"/> (0 = evaluated first, <see cref="Count"/> = last).</summary>
    public void Insert(int position, FirewallRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (IndexOf(rule.Id) >= 0)
        {
            throw new PolicyRefusedException(PolicyRefusalReason.DuplicateRuleId, $"Policy {Name} already has a rule {rule.Id}.");
        }

        if (position < 0 || position > _rules.Count)
        {
            throw new PolicyRefusedException(PolicyRefusalReason.PositionOutOfRange, $"Position {position} is outside 0..{_rules.Count}.");
        }

        if (_rules.Count == Capacity)
        {
            throw new PolicyRefusedException(PolicyRefusalReason.CapacityReached, $"Policy {Name} is full ({Capacity} rules).");
        }

        if (IsStrict)
        {
            EnsureReachableAt(_rules, position, rule);
        }

        _rules.Insert(position, rule);
        _hits[rule.Id] = 0;
        Revision++;
    }

    /// <summary>Moves a rule so that it ends up at <paramref name="position"/> among the rules (0..<see cref="Count"/> - 1).</summary>
    public void Move(RuleId id, int position)
    {
        int from = IndexOf(id);
        if (from < 0)
        {
            throw new PolicyRefusedException(PolicyRefusalReason.RuleNotFound, $"Policy {Name} has no rule {id}.");
        }

        if (position < 0 || position >= _rules.Count)
        {
            throw new PolicyRefusedException(PolicyRefusalReason.PositionOutOfRange, $"Position {position} is outside 0..{_rules.Count - 1}.");
        }

        if (position == from)
        {
            return;
        }

        FirewallRule rule = _rules[from];
        List<FirewallRule> others = new(_rules);
        others.RemoveAt(from);
        if (IsStrict)
        {
            EnsureReachableAt(others, position, rule);
        }

        others.Insert(position, rule);
        _rules.Clear();
        _rules.AddRange(others);
        Revision++;
    }

    public FirewallRule Remove(RuleId id)
    {
        int index = IndexOf(id);
        if (index < 0)
        {
            throw new PolicyRefusedException(PolicyRefusalReason.RuleNotFound, $"Policy {Name} has no rule {id}.");
        }

        FirewallRule rule = _rules[index];
        _rules.RemoveAt(index);
        _hits.Remove(id);
        Revision++;
        return rule;
    }

    /// <summary>First match wins; the default action applies when no rule matches. Counts the hit.</summary>
    public Verdict Evaluate(Packet packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        foreach (FirewallRule rule in _rules)
        {
            if (rule.Matches(packet))
            {
                _hits[rule.Id]++;
                return new Verdict(rule.Action, rule.Id);
            }
        }

        DefaultHits++;
        return new Verdict(DefaultAction, null);
    }

    public void ResetCounters()
    {
        foreach (RuleId id in _hits.Keys.ToList())
        {
            _hits[id] = 0;
        }

        DefaultHits = 0;
    }

    /// <summary>
    /// Reports, in rule order, at most one anomaly per rule: covered by an earlier rule (shadowed when the actions
    /// differ, redundant when they agree), else covered by a later rule of the same action with no overlapping rule of
    /// the other action in between, else agreeing with the default action with no overlapping later rule of the other
    /// action. Coverage is checked one rule at a time, so a rule hidden only by several rules together is not reported.
    /// </summary>
    public IReadOnlyList<RuleAnomaly> FindAnomalies()
    {
        List<RuleAnomaly> anomalies = [];
        for (int i = 0; i < _rules.Count; i++)
        {
            RuleAnomaly? anomaly = AnalyseRule(i);
            if (anomaly is not null)
            {
                anomalies.Add(anomaly);
            }
        }

        return anomalies;
    }

    /// <summary>True when no earlier rule covers it: some packet can reach it.</summary>
    public bool IsReachable(RuleId id)
    {
        int index = IndexOf(id);
        if (index < 0)
        {
            throw new PolicyRefusedException(PolicyRefusalReason.RuleNotFound, $"Policy {Name} has no rule {id}.");
        }

        return CoveringEarlier(_rules, index, _rules[index]) is null;
    }

    private RuleAnomaly? AnalyseRule(int index)
    {
        FirewallRule rule = _rules[index];
        FirewallRule? earlier = CoveringEarlier(_rules, index, rule);
        if (earlier is not null)
        {
            AnomalyKind kind = earlier.Action == rule.Action ? AnomalyKind.RedundantToEarlier : AnomalyKind.Shadowed;
            return new RuleAnomaly(rule.Id, kind, earlier.Id);
        }

        for (int j = index + 1; j < _rules.Count; j++)
        {
            FirewallRule later = _rules[j];
            if (later.Action != rule.Action)
            {
                if (later.Overlaps(rule))
                {
                    return null;
                }

                continue;
            }

            if (later.Covers(rule))
            {
                return new RuleAnomaly(rule.Id, AnomalyKind.RedundantToLater, later.Id);
            }
        }

        return rule.Action == DefaultAction ? new RuleAnomaly(rule.Id, AnomalyKind.RedundantToDefault, null) : null;
    }

    private static FirewallRule? CoveringEarlier(List<FirewallRule> rules, int index, FirewallRule rule)
    {
        for (int i = 0; i < index; i++)
        {
            if (rules[i].Covers(rule))
            {
                return rules[i];
            }
        }

        return null;
    }

    private void EnsureReachableAt(List<FirewallRule> rules, int position, FirewallRule rule)
    {
        FirewallRule? earlier = CoveringEarlier(rules, position, rule);
        if (earlier is not null)
        {
            throw new PolicyRefusedException(PolicyRefusalReason.RuleWouldBeUnreachable, $"At position {position}, {rule.Id} would be covered by {earlier.Id}.");
        }

        for (int j = position; j < rules.Count; j++)
        {
            if (rule.Covers(rules[j]))
            {
                throw new PolicyRefusedException(PolicyRefusalReason.RuleWouldShadowAnother, $"At position {position}, {rule.Id} would cover {rules[j].Id}.");
            }
        }
    }
}
