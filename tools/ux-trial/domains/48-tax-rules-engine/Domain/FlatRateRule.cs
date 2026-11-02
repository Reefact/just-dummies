namespace TaxRules.Domain;

/// <summary>One rate on the whole of a base.</summary>
public sealed class FlatRateRule : TaxRule
{
    public FlatRateRule(RuleCode code, string label, IncomeKind taxBase, Rate rate, ICondition? condition = null)
        : base(code, label, condition)
    {
        TaxBase = Guard.IncomeKinds(taxBase, nameof(taxBase));
        ArgumentNullException.ThrowIfNull(rate);
        if (rate.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), rate, "A flat-rate rule taxes at a positive rate.");
        }

        Rate = rate;
    }

    public IncomeKind TaxBase { get; }

    public Rate Rate { get; }

    protected override BreakdownLine Apply(Taxpayer taxpayer, int taxYear)
    {
        Money income = taxpayer.TaxableIncome(TaxBase);
        return new BreakdownLine(Code, Label, Effect, LineOutcome.Applied, income, income.Times(Rate));
    }
}
