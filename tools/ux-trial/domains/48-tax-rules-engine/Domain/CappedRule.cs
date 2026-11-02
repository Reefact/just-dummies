namespace TaxRules.Domain;

/// <summary>
/// Bounds the amount of another rule: never above <see cref="Maximum"/> and, when the wrapped
/// rule produces anything at all, never below <see cref="Minimum"/>. A cap does not wrap
/// another cap, nor a credit.
/// </summary>
public sealed class CappedRule : TaxRule
{
    public CappedRule(RuleCode code, string label, TaxRule inner, Money maximum, Money? minimum = null, ICondition? condition = null)
        : base(code, label, condition)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(maximum);
        if (inner is CappedRule)
        {
            throw new ArgumentException($"A cap cannot wrap another cap ({inner.Code}); set a single cap instead.", nameof(inner));
        }

        if (inner.Effect == RuleEffect.Credit)
        {
            throw new ArgumentException($"A credit ({inner.Code}) cannot be capped as a tax.", nameof(inner));
        }

        if (inner.SelfAndDescendants().Any(r => r.Code.Equals(code)))
        {
            throw new ArgumentException($"Rule code {code} already appears in the wrapped rule.", nameof(code));
        }

        if (maximum.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "A cap is positive; a zero cap is an exemption.");
        }

        Money floor = minimum ?? Money.Zero;
        if (floor.CompareTo(maximum) > 0)
        {
            throw new ArgumentException($"The minimum ({floor}) cannot exceed the maximum ({maximum}).", nameof(minimum));
        }

        Inner = inner;
        Maximum = maximum;
        Minimum = floor;
    }

    public TaxRule Inner { get; }

    public Money Maximum { get; }

    public Money Minimum { get; }

    public override IReadOnlyList<TaxRule> Children => [Inner];

    protected override BreakdownLine Apply(Taxpayer taxpayer, int taxYear)
    {
        BreakdownLine inner = Inner.Evaluate(taxpayer, taxYear);
        Money amount = inner.Amount;
        LineOutcome outcome = LineOutcome.Applied;
        if (amount.CompareTo(Maximum) > 0)
        {
            amount = Maximum;
            outcome = LineOutcome.CappedAtMaximum;
        }
        else if (!amount.IsZero && amount.CompareTo(Minimum) < 0)
        {
            amount = Minimum;
            outcome = LineOutcome.RaisedToMinimum;
        }

        return new BreakdownLine(Code, Label, Effect, outcome, null, amount, [inner]);
    }
}
