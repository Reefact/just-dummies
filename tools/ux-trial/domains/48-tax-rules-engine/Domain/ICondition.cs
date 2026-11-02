namespace TaxRules.Domain;

/// <summary>
/// A condition under which a rule applies. Conditions form an expression tree: leaves test the
/// taxpayer, and <see cref="AndCondition"/>, <see cref="OrCondition"/> and <see cref="NotCondition"/>
/// combine them, at most <see cref="MaxDepth"/> levels deep.
/// </summary>
public interface ICondition
{
    const int MaxDepth = 8;

    /// <summary>1 for a leaf; one more than the deepest operand for a combination.</summary>
    int Depth { get; }

    /// <summary>The direct operands; empty for a leaf.</summary>
    IReadOnlyList<ICondition> Operands { get; }

    bool IsSatisfiedBy(Taxpayer taxpayer, int taxYear);
}
