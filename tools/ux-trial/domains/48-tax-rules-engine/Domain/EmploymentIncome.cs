namespace TaxRules.Domain;

/// <summary>Salary from an employer, less the professional expenses claimed against it.</summary>
public sealed class EmploymentIncome : IncomeComponent, IEquatable<EmploymentIncome>
{
    public EmploymentIncome(string employer, Money gross, Money expenseDeduction)
        : base(employer)
    {
        ArgumentNullException.ThrowIfNull(gross);
        ArgumentNullException.ThrowIfNull(expenseDeduction);
        if (expenseDeduction.CompareTo(gross) > 0)
        {
            throw new ArgumentException(
                $"The expense deduction ({expenseDeduction}) cannot exceed the gross salary ({gross}).",
                nameof(expenseDeduction));
        }

        Gross = gross;
        ExpenseDeduction = expenseDeduction;
    }

    public Money Gross { get; }

    public Money ExpenseDeduction { get; }

    public override IncomeKind Kind => IncomeKind.Employment;

    public override Money TaxableAmount => Gross.Minus(ExpenseDeduction);

    public bool Equals(EmploymentIncome? other) =>
        other is not null && other.Source == Source && other.Gross.Equals(Gross) && other.ExpenseDeduction.Equals(ExpenseDeduction);

    public override bool Equals(object? obj) => Equals(obj as EmploymentIncome);

    public override int GetHashCode() => HashCode.Combine(Source, Gross, ExpenseDeduction);

    public override string ToString() => $"Employment at {Source}: {Gross} less {ExpenseDeduction}";
}
