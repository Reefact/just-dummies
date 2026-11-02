namespace TaxRules.Domain;

/// <summary>
/// The amount one rule produced for a taxpayer, with the base it was computed on (for rules that
/// have one) and the lines of its nested rules.
/// </summary>
public sealed class BreakdownLine
{
    private readonly List<BreakdownLine> children;

    public BreakdownLine(
        RuleCode code,
        string label,
        RuleEffect effect,
        LineOutcome outcome,
        Money? taxableBase,
        Money amount,
        IEnumerable<BreakdownLine>? children = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(amount);
        if (!Enum.IsDefined(effect))
        {
            throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unknown rule effect.");
        }

        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown line outcome.");
        }

        List<BreakdownLine> list = children?.ToList() ?? [];
        if (list.Contains(null!))
        {
            throw new ArgumentException("A breakdown line cannot have a null child.", nameof(children));
        }

        if (outcome == LineOutcome.NotApplicable && (!amount.IsZero || list.Count > 0 || taxableBase is not null))
        {
            throw new ArgumentException("A rule that does not apply produces nothing: no base, no amount, no nested lines.", nameof(outcome));
        }

        Code = code;
        Label = Guard.Label(label, nameof(label));
        Effect = effect;
        Outcome = outcome;
        TaxableBase = taxableBase;
        Amount = amount;
        this.children = list;
    }

    public RuleCode Code { get; }

    public string Label { get; }

    public RuleEffect Effect { get; }

    public LineOutcome Outcome { get; }

    /// <summary>The income the rule was computed on; <c>null</c> for caps, composites and credits.</summary>
    public Money? TaxableBase { get; }

    public Money Amount { get; }

    public IReadOnlyList<BreakdownLine> Children => children;

    /// <summary>This line or a nested one with the given code.</summary>
    public BreakdownLine? Find(RuleCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (Code.Equals(code))
        {
            return this;
        }

        return children.Select(c => c.Find(code)).FirstOrDefault(l => l is not null);
    }

    public override string ToString() => $"{Code} {Outcome}: {Amount}";
}
