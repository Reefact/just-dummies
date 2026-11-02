namespace TaxRules.Domain;

/// <summary>Satisfied when the taxable income of the given kinds is strictly above the threshold.</summary>
public sealed class IncomeAboveCondition : ICondition, IEquatable<IncomeAboveCondition>
{
    public IncomeAboveCondition(IncomeKind kinds, Money threshold)
    {
        Kinds = Guard.IncomeKinds(kinds, nameof(kinds));
        Threshold = threshold ?? throw new ArgumentNullException(nameof(threshold));
    }

    public IncomeKind Kinds { get; }

    public Money Threshold { get; }

    public int Depth => 1;

    public IReadOnlyList<ICondition> Operands => [];

    public bool IsSatisfiedBy(Taxpayer taxpayer, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(taxpayer);
        return taxpayer.TaxableIncome(Kinds).CompareTo(Threshold) > 0;
    }

    public bool Equals(IncomeAboveCondition? other) => other is not null && other.Kinds == Kinds && other.Threshold.Equals(Threshold);

    public override bool Equals(object? obj) => Equals(obj as IncomeAboveCondition);

    public override int GetHashCode() => HashCode.Combine(Kinds, Threshold);

    public override string ToString() => $"{Kinds} income above {Threshold}";
}
