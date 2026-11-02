namespace TaxRules.Domain;

/// <summary>Satisfied when every operand is (at least two operands).</summary>
public sealed class AndCondition : ICondition, IEquatable<AndCondition>
{
    public AndCondition(IEnumerable<ICondition> operands)
    {
        Operands = ConditionOperands.Validate(operands, nameof(operands));
        Depth = 1 + Operands.Max(o => o.Depth);
    }

    public IReadOnlyList<ICondition> Operands { get; }

    public int Depth { get; }

    public bool IsSatisfiedBy(Taxpayer taxpayer, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(taxpayer);
        return Operands.All(o => o.IsSatisfiedBy(taxpayer, taxYear));
    }

    public bool Equals(AndCondition? other) => other is not null && other.Operands.SequenceEqual(Operands);

    public override bool Equals(object? obj) => Equals(obj as AndCondition);

    public override int GetHashCode() => ConditionOperands.Hash(Operands);

    public override string ToString() => "(" + string.Join(" and ", Operands) + ")";
}
