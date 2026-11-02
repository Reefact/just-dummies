namespace TaxRules.Domain;

/// <summary>
/// A rule of a tax schedule. Rules are identified by their code, may be guarded by a
/// condition, and nest: <see cref="CappedRule"/> wraps one rule and <see cref="CompositeRule"/>
/// sums several. Each rule rounds its own amount to the cent, half away from zero.
/// </summary>
public abstract class TaxRule
{
    protected TaxRule(RuleCode code, string label, ICondition? condition)
    {
        ArgumentNullException.ThrowIfNull(code);
        Code = code;
        Label = Guard.Label(label, nameof(label));
        Condition = condition;
    }

    public RuleCode Code { get; }

    public string Label { get; }

    /// <summary>The condition under which the rule applies; <c>null</c> when it always applies.</summary>
    public ICondition? Condition { get; }

    public virtual RuleEffect Effect => RuleEffect.Tax;

    /// <summary>The rules nested directly in this one; empty for a leaf rule.</summary>
    public virtual IReadOnlyList<TaxRule> Children => [];

    /// <summary>This rule, then every nested rule, depth first.</summary>
    public IEnumerable<TaxRule> SelfAndDescendants()
    {
        yield return this;
        foreach (TaxRule child in Children)
        {
            foreach (TaxRule descendant in child.SelfAndDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>Evaluates the rule for one taxpayer and year, explaining the amount it produces.</summary>
    public BreakdownLine Evaluate(Taxpayer taxpayer, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(taxpayer);
        if (Condition is not null && !Condition.IsSatisfiedBy(taxpayer, taxYear))
        {
            return new BreakdownLine(Code, Label, Effect, LineOutcome.NotApplicable, null, Money.Zero);
        }

        return Apply(taxpayer, taxYear);
    }

    /// <summary>Computes the line once the condition holds.</summary>
    protected abstract BreakdownLine Apply(Taxpayer taxpayer, int taxYear);

    public override string ToString() => $"{Code} ({Label})";
}
