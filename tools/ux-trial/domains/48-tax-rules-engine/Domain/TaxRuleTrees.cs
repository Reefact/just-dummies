namespace TaxRules.Domain;

internal static class TaxRuleTrees
{
    public static IEnumerable<TaxRule> SelfAndDescendantsOfAll(this IEnumerable<TaxRule> rules) =>
        rules.SelectMany(r => r.SelfAndDescendants());

    public static IEnumerable<ICondition> SelfAndDescendants(this ICondition condition)
    {
        yield return condition;
        foreach (ICondition operand in condition.Operands)
        {
            foreach (ICondition descendant in operand.SelfAndDescendants())
            {
                yield return descendant;
            }
        }
    }
}
