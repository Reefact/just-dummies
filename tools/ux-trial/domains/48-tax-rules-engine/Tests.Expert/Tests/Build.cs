using JustDummies;

using TaxRules.Domain;

namespace TaxRules.Tests;

/// <summary>
///     Short spellings for the literals the tests are about. Everything else about an object is left to the
///     generators, so a reader sees exactly the numbers that matter.
/// </summary>
internal static class Build {

    public static Money M(decimal amount) => new(amount);

    public static Rate R(decimal fraction) => new(fraction);

    public static RuleCode Code(string value) => new(value);

    public static JurisdictionCode Jurisdiction(string value) => new(value);

    public static IncomeComponent Salary(decimal gross, decimal expenseDeduction = 0m) {
        return Any.EmploymentIncome().WithGross(M(gross)).WithExpenseDeduction(M(expenseDeduction)).Generate();
    }

    public static IncomeComponent Business(decimal turnover, decimal expenses = 0m) {
        return Any.SelfEmploymentIncome().WithTurnover(M(turnover)).WithExpenses(M(expenses)).Generate();
    }

    public static IncomeComponent Interest(decimal gross) {
        return Any.InvestmentIncome().WithGross(M(gross)).WithInvestmentKind(InvestmentKind.Interest).Generate();
    }

    public static IncomeComponent Dividend(decimal gross) {
        return Any.InvestmentIncome().WithGross(M(gross)).WithInvestmentKind(InvestmentKind.Dividend).Generate();
    }

    /// <summary>A taxpayer whose incomes are the ones given; who they are, where they live and when they were born are not the point.</summary>
    public static Taxpayer TaxpayerEarning(params IncomeComponent[] incomes) {
        return Any.Taxpayer().WithIncomes(incomes).Generate();
    }

    public static FlatRateRule Flat(IncomeKind taxBase, decimal rate, string? code = null) {
        AnyFlatRateRule generator = Any.FlatRateRule().WithTaxBase(taxBase).WithRate(R(rate)).WithCondition(null);

        return (code is null ? generator : generator.WithCode(Code(code))).Generate();
    }

    public static CompositeRule CompositeOf(params TaxRule[] components) {
        return Any.CompositeRule().WithComponents(components).WithCondition(null).Generate();
    }

    /// <summary>A taxpayer-independent line, for tests of the breakdown arithmetic only.</summary>
    public static BreakdownLine TaxLine(decimal amount) {
        return new BreakdownLine(Any.RuleCode().Generate(), Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.Applied, null, M(amount));
    }

    public static BreakdownLine CreditLine(decimal amount) {
        return new BreakdownLine(Any.RuleCode().Generate(), Plausible.Label().Generate(), RuleEffect.Credit, LineOutcome.Applied, null, M(amount));
    }

    /// <summary>A condition of exactly <paramref name="depth" /> levels: a leaf wrapped in alternating And and Or.</summary>
    public static ICondition ConditionOfDepth(int depth) {
        ICondition condition = Any.Condition().Leaves().Generate();
        for (int level = 2; level <= depth; level++) {
            ICondition other = Any.Condition().Leaves().Generate();
            condition = level % 2 == 0 ? new AndCondition([condition, other]) : new OrCondition([condition, other]);
        }

        return condition;
    }

    /// <summary>A condition that no taxpayer without income satisfies, and that every one earning more than a cent does.</summary>
    public static ICondition IncomeAboveNothing() => new IncomeAboveCondition(IncomeKind.All, Money.Zero);

}
