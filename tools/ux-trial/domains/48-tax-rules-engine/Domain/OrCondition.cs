namespace TaxRules.Domain;

/// <summary>Satisfied when any operand is (at least two operands).</summary>
public sealed class OrCondition : ICondition, IEquatable<OrCondition>
{
    public OrCondition(IEnumerable<ICondition> operands)
    {
        Operands = ConditionOperands.Validate(operands, nameof(operands));
        Depth = 1 + Operands.Max(o => o.Depth);
    }

    public IReadOnlyList<ICondition> Operands { get; }

    public int Depth { get; }

    public bool IsSatisfiedBy(Taxpayer taxpayer, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(taxpayer);
        return Operands.Any(o => o.IsSatisfiedBy(taxpayer, taxYear));
    }

    public bool Equals(OrCondition? other) => other is not null && other.Operands.SequenceEqual(Operands);

    public override bool Equals(object? obj) => Equals(obj as OrCondition);

    public override int GetHashCode() => ConditionOperands.Hash(Operands);

    public override string ToString() => "(" + string.Join(" or ", Operands) + ")";
}
