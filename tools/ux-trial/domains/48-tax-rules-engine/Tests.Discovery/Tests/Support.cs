using JustDummies;
using JustDummies.Xunit;

using TaxRules.Domain;

[assembly: Reproducible]

namespace TaxRules.Tests.Discovery;

internal static class Fixtures {

    public static RuleCode Code(string value) => new RuleCode(value);

    public static string AnySource() => Any.String().NotBlank().WithMaxLength(IncomeComponent.MaxSourceLength).Generate();

    public static EmploymentIncome Salary(decimal taxable) => new EmploymentIncome(AnySource(), new Money(taxable), Money.Zero);

    public static SelfEmploymentIncome Profit(decimal profit) => new SelfEmploymentIncome(AnySource(), new Money(profit), Money.Zero);

    public static InvestmentIncome Interest(decimal gross) => new InvestmentIncome(AnySource(), new Money(gross), InvestmentKind.Interest);

    public static InvestmentIncome Dividend(decimal gross) => new InvestmentIncome(AnySource(), new Money(gross), InvestmentKind.Dividend);

    /// <summary>A taxpayer whose only relevant facts are the incomes; everything else is arbitrary.</summary>
    public static Taxpayer EarningTaxpayer(params IncomeComponent[] incomes) {
        return Any.Taxpayer().WithIncomes(incomes).Generate();
    }

    public static Taxpayer TaxpayerEarning(decimal salary) => EarningTaxpayer(Salary(salary));

    public static TaxBracket Band(decimal lower, decimal? upper, decimal rate) {
        return new TaxBracket(new Money(lower), upper is null ? null : new Money((decimal)upper), new Rate(rate));
    }

    public static FlatRateRule FlatOn(IncomeKind taxBase, decimal rate, string code = "FLAT_TAX") {
        return Any.FlatRateRule().WithCode(Code(code)).WithTaxBase(taxBase).WithRate(new Rate(rate)).Generate();
    }

    public static TaxCreditRule Credit(string code, decimal baseAmount, decimal perMember = 0m) {
        return Any.TaxCreditRule().WithCode(Code(code)).WithBaseAmount(new Money(baseAmount)).WithPerAdditionalMember(new Money(perMember)).Generate();
    }

    public static ICondition ChainOfDepth(int depth) {
        ICondition condition = Any.LeafCondition().Generate();
        for (int level = 1; level < depth; level++) {
            condition = new AndCondition([condition, Any.LeafCondition().Generate()]);
        }

        return condition;
    }

}
