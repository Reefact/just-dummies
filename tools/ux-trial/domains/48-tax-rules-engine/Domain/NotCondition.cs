namespace TaxRules.Domain;

/// <summary>Satisfied when its operand is not. A double negation is refused: write the operand itself.</summary>
public sealed class NotCondition : ICondition, IEquatable<NotCondition>
{
    public NotCondition(ICondition operand)
    {
        ArgumentNullException.ThrowIfNull(operand);
        if (operand is NotCondition)
        {
            throw new ArgumentException("A negation of a negation is the condition itself; use the inner operand.", nameof(operand));
        }

        ConditionOperands.CheckDepth(1 + operand.Depth, nameof(operand));
        Operand = operand;
    }

    public ICondition Operand { get; }

    public IReadOnlyList<ICondition> Operands => [Operand];

    public int Depth => 1 + Operand.Depth;

    public bool IsSatisfiedBy(Taxpayer taxpayer, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(taxpayer);
        return !Operand.IsSatisfiedBy(taxpayer, taxYear);
    }

    public bool Equals(NotCondition? other) => other is not null && other.Operand.Equals(Operand);

    public override bool Equals(object? obj) => Equals(obj as NotCondition);

    public override int GetHashCode() => HashCode.Combine(nameof(NotCondition), Operand);

    public override string ToString() => $"not {Operand}";
}
