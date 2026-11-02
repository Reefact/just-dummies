namespace TaxRules.Domain;

/// <summary>
/// A non-refundable credit: a base amount plus an amount for each household member beyond the
/// taxpayer. It is set against the tax of the schedule, never nested in a cap or a composite.
/// </summary>
public sealed class TaxCreditRule : TaxRule
{
    public TaxCreditRule(RuleCode code, string label, Money baseAmount, Money perAdditionalMember, ICondition? condition = null)
        : base(code, label, condition)
    {
        ArgumentNullException.ThrowIfNull(baseAmount);
        ArgumentNullException.ThrowIfNull(perAdditionalMember);
        if (baseAmount.IsZero && perAdditionalMember.IsZero)
        {
            throw new ArgumentException("A credit grants something: a base amount, an amount per additional member, or both.", nameof(perAdditionalMember));
        }

        BaseAmount = baseAmount;
        PerAdditionalMember = perAdditionalMember;
    }

    public Money BaseAmount { get; }

    public Money PerAdditionalMember { get; }

    public override RuleEffect Effect => RuleEffect.Credit;

    protected override BreakdownLine Apply(Taxpayer taxpayer, int taxYear)
    {
        Money amount = BaseAmount.Plus(PerAdditionalMember.Times(taxpayer.HouseholdSize - 1));
        return new BreakdownLine(Code, Label, Effect, LineOutcome.Applied, null, amount);
    }
}
