using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>Generators for the abstract types and the vocabulary shared by several scaffolded generators.</summary>
public static class Dummies {

    /// <summary>A label a rule or a line may carry.</summary>
    public static AnyString Label() {
        return Any.String().NotBlank().WithMaxLength(80);
    }

    /// <summary>A source (employer, activity, payer).</summary>
    public static AnyString Source() {
        return Any.String().NotBlank().WithMaxLength(IncomeComponent.MaxSourceLength);
    }

    /// <summary>A tax base: one or several kinds, never None.</summary>
    public static AnyEnum<IncomeKind> TaxBase() {
        return Any.Enum<IncomeKind>().AllowingCombinations().Except(IncomeKind.None);
    }

    /// <summary>Income money: ordinary magnitudes, so that sums of components stay inside Money's range.</summary>
    public static AnyMoney IncomeMoney() {
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(10_000_000m));
    }

    /// <summary>An amount of income: ordinary magnitudes, so that sums of components stay inside Money's range.</summary>
    public static AnyDecimal IncomeAmount() {
        return Any.Decimal().Between(0m, 10_000_000m).WithScale(2);
    }

    /// <summary>Distinct rule codes, so that a tree built from them has no duplicate.</summary>
    public static IReadOnlyList<RuleCode> DistinctCodes(int count) {
        return Any.SetOf(new AnyRuleCode()).WithCount(count).Generate().ToList();
    }

    /// <summary>A leaf condition of any of the four kinds.</summary>
    public static IAny<ICondition> LeafCondition() {
        return Any.Int32().Between(0, 3).Then(choice => choice switch {
            0 => new AnyIncomeAboveCondition().As(c => (ICondition)c),
            1 => new AnyResidentOfCondition().As(c => (ICondition)c),
            2 => new AnyAgeAtLeastCondition().As(c => (ICondition)c),
            _ => new AnyHouseholdSizeAtLeastCondition().As(c => (ICondition)c)
        });
    }

    /// <summary>A progressive scale: 1 to 6 contiguous brackets from zero, rates never decreasing, the last bracket open-ended.</summary>
    public static IAny<IEnumerable<TaxBracket>> Scale() {
        return Any.Int32().Between(1, 6).Then(count => Any.Combine(
            Any.SetOf(Any.Decimal().Between(1m, 5_000_000m).WithScale(2)).WithCount(count - 1),
            Any.ListOf(Any.Decimal().Between(0m, 1m).WithScale(4)).WithCount(count),
            (cuts, fractions) => BuildScale(cuts.OrderBy(c => c).ToList(), fractions.OrderBy(f => f).ToList())));
    }

    private static IEnumerable<TaxBracket> BuildScale(List<decimal> cuts, List<decimal> fractions) {
        List<TaxBracket> brackets = [];
        for (int i = 0; i < fractions.Count; i++) {
            Money  lower = new Money(i == 0 ? 0m : cuts[i - 1]);
            Money? upper = i < cuts.Count ? new Money(cuts[i]) : null;
            brackets.Add(new TaxBracket(lower, upper, new Rate(fractions[i])));
        }

        return brackets;
    }

    /// <summary>A leaf rule (flat, threshold exemption or credit).</summary>
    public static IAny<TaxRule> LeafTaxRule() {
        return Any.Int32().Between(0, 2).Then(choice => choice switch {
            0 => new AnyFlatRateRule().As(r => (TaxRule)r),
            1 => new AnyThresholdExemptionRule().As(r => (TaxRule)r),
            _ => new AnyProgressiveRule().As(r => (TaxRule)r)
        });
    }

    /// <summary>An income component of any of the three kinds.</summary>
    public static IAny<IncomeComponent> Incomes() {
        return Any.Int32().Between(0, 2).Then(choice => choice switch {
            0 => new AnyEmploymentIncome().As(i => (IncomeComponent)i),
            1 => new AnySelfEmploymentIncome().As(i => (IncomeComponent)i),
            _ => new AnyInvestmentIncome().As(i => (IncomeComponent)i)
        });
    }

}
