namespace TaxRules.Domain;

/// <summary>
/// Profit from an activity: turnover less expenses. A loss is allowed; it makes the taxable
/// amount zero and is not set against other income.
/// </summary>
public sealed class SelfEmploymentIncome : IncomeComponent, IEquatable<SelfEmploymentIncome>
{
    public SelfEmploymentIncome(string activity, Money turnover, Money expenses)
        : base(activity)
    {
        ArgumentNullException.ThrowIfNull(turnover);
        ArgumentNullException.ThrowIfNull(expenses);
        Turnover = turnover;
        Expenses = expenses;
    }

    public Money Turnover { get; }

    public Money Expenses { get; }

    public Money Loss => Expenses.MinusOrZero(Turnover);

    public override IncomeKind Kind => IncomeKind.SelfEmployment;

    public override Money TaxableAmount => Turnover.MinusOrZero(Expenses);

    public bool Equals(SelfEmploymentIncome? other) =>
        other is not null && other.Source == Source && other.Turnover.Equals(Turnover) && other.Expenses.Equals(Expenses);

    public override bool Equals(object? obj) => Equals(obj as SelfEmploymentIncome);

    public override int GetHashCode() => HashCode.Combine(Source, Turnover, Expenses);

    public override string ToString() => $"Self-employment ({Source}): {Turnover} less {Expenses}";
}
