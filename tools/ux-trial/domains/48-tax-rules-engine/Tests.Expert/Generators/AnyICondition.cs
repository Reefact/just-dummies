using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     Written by hand (the scaffolder cannot choose among the concrete types of an interface): an arbitrary
///     condition, a leaf or a combination of at most <see cref="MaxCombinationDepth" /> levels. The depth limit is a
///     bound on what the generator attempts, never on what a condition may be: the domain refuses beyond 8.
/// </summary>
public sealed class AnyICondition : IAny<ICondition> {

    private readonly int _depth;

    public AnyICondition()
        : this(depth: 2) { }

    private AnyICondition(int depth) {
        _depth = depth;
    }

    public int MaxCombinationDepth => _depth;

    /// <summary>Only leaves: no combination at all.</summary>
    public AnyICondition Leaves() {
        return new AnyICondition(0);
    }

    /// <summary>One of the four leaf conditions.</summary>
    public static IAny<ICondition> Leaf() {
        return Any.Int32().Between(0, 3).Then(choice => choice switch {
            0 => new AnyIncomeAboveCondition().As(c => (ICondition)c),
            1 => new AnyResidentOfCondition().As(c => (ICondition)c),
            2 => new AnyAgeAtLeastCondition().As(c => (ICondition)c),
            _ => new AnyHouseholdSizeAtLeastCondition().As(c => (ICondition)c)
        });
    }

    public ICondition Generate() {
        return Build(_depth).Generate();
    }

    internal static IAny<ICondition> Build(int depth) {
        if (depth <= 0) { return Leaf(); }

        IAny<ICondition> below = Build(depth - 1);

        return Any.Int32().Between(0, 4).Then(choice => choice switch {
            0 => Leaf(),
            1 => Leaf(),
            2 => Any.ListOf(below).WithCountBetween(2, 3).As(operands => (ICondition)new AndCondition(operands)),
            3 => Any.ListOf(below).WithCountBetween(2, 3).As(operands => (ICondition)new OrCondition(operands)),
            // A negation of a negation is refused: the operand of a Not is a leaf here.
            _ => Leaf().As(operand => (ICondition)new NotCondition(operand))
        });
    }

}
