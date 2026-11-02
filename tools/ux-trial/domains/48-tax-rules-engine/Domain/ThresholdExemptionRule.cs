namespace TaxRules.Domain;

/// <summary>
/// Exempts the base up to a threshold and taxes the excess at one rate. When a withdrawal
/// ceiling is set, a base at or above it loses the exemption and is taxed in full.
/// </summary>
public sealed class ThresholdExemptionRule : TaxRule
{
    public ThresholdExemptionRule(
        RuleCode code,
        string label,
        IncomeKind taxBase,
        Money threshold,
        Rate rate,
        Money? withdrawalCeiling = null,
        ICondition? condition = null)
        : base(code, label, condition)
    {
        TaxBase = Guard.IncomeKinds(taxBase, nameof(taxBase));
        ArgumentNullException.ThrowIfNull(threshold);
        ArgumentNullException.ThrowIfNull(rate);
        if (threshold.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "An exemption threshold is positive.");
        }

        if (rate.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), rate, "The excess over the threshold is taxed at a positive rate.");
        }

        if (withdrawalCeiling is not null && withdrawalCeiling.CompareTo(threshold) <= 0)
        {
            throw new ArgumentException(
                $"The withdrawal ceiling ({withdrawalCeiling}) lies above the threshold ({threshold}).",
                nameof(withdrawalCeiling));
        }

        Threshold = threshold;
        Rate = rate;
        WithdrawalCeiling = withdrawalCeiling;
    }

    public IncomeKind TaxBase { get; }

    public Money Threshold { get; }

    public Rate Rate { get; }

    public Money? WithdrawalCeiling { get; }

    public bool IsExemptionWithdrawnAt(Money income)
    {
        ArgumentNullException.ThrowIfNull(income);
        return WithdrawalCeiling is not null && income.CompareTo(WithdrawalCeiling) >= 0;
    }

    protected override BreakdownLine Apply(Taxpayer taxpayer, int taxYear)
    {
        Money income = taxpayer.TaxableIncome(TaxBase);
        Money taxed = IsExemptionWithdrawnAt(income) ? income : income.MinusOrZero(Threshold);
        return new BreakdownLine(Code, Label, Effect, LineOutcome.Applied, income, taxed.Times(Rate));
    }
}
