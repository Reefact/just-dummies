namespace TaxRules.Domain;

internal static class ConditionOperands
{
    public static IReadOnlyList<ICondition> Validate(IEnumerable<ICondition> operands, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(operands, parameterName);
        ICondition[] list = operands.ToArray();
        if (list.Length < 2)
        {
            throw new ArgumentException($"A combination joins at least two conditions; got {list.Length}.", parameterName);
        }

        if (list.Any(c => c is null))
        {
            throw new ArgumentException("A combination cannot contain a null condition.", parameterName);
        }

        CheckDepth(1 + list.Max(c => c.Depth), parameterName);
        return list;
    }

    public static void CheckDepth(int depth, string parameterName)
    {
        if (depth > ICondition.MaxDepth)
        {
            throw new ArgumentException($"A condition is at most {ICondition.MaxDepth} levels deep; this one would be {depth}.", parameterName);
        }
    }

    public static int Hash(IReadOnlyList<ICondition> operands)
    {
        HashCode hash = default;
        foreach (ICondition operand in operands)
        {
            hash.Add(operand);
        }

        return hash.ToHashCode();
    }
}
