namespace TaxRules.Domain;

/// <summary>
/// Sums two or more component rules under one code and one condition. Components are taxes,
/// never credits, and no rule code appears twice in the composite's tree, so no rule is
/// counted twice and no rule contains itself.
/// </summary>
public sealed class CompositeRule : TaxRule
{
    public const int MaxComponents = 16;

    private readonly List<TaxRule> components;

    public CompositeRule(RuleCode code, string label, IEnumerable<TaxRule> components, ICondition? condition = null)
        : base(code, label, condition)
    {
        ArgumentNullException.ThrowIfNull(components);
        List<TaxRule> list = components.ToList();
        if (list.Count < 2 || list.Count > MaxComponents)
        {
            throw new ArgumentException($"A composite sums 2 to {MaxComponents} rules; got {list.Count}.", nameof(components));
        }

        if (list.Contains(null!))
        {
            throw new ArgumentException("A composite cannot contain a null rule.", nameof(components));
        }

        TaxRule? credit = list.FirstOrDefault(r => r.Effect == RuleEffect.Credit);
        if (credit is not null)
        {
            throw new ArgumentException($"A composite sums taxes; {credit.Code} is a credit.", nameof(components));
        }

        HashSet<RuleCode> seen = [code];
        foreach (TaxRule rule in list.SelfAndDescendantsOfAll())
        {
            if (!seen.Add(rule.Code))
            {
                throw new ArgumentException($"Rule code {rule.Code} appears more than once in composite {code}.", nameof(components));
            }
        }

        this.components = list;
    }

    public override IReadOnlyList<TaxRule> Children => components;

    protected override BreakdownLine Apply(Taxpayer taxpayer, int taxYear)
    {
        List<BreakdownLine> lines = components.Select(c => c.Evaluate(taxpayer, taxYear)).ToList();
        Money total = Money.Zero;
        foreach (BreakdownLine line in lines)
        {
            total = total.Plus(line.Amount);
        }

        return new BreakdownLine(Code, Label, Effect, LineOutcome.Applied, null, total, lines);
    }
}
