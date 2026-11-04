using System.Globalization;

using JustDummies;

using Xunit;

namespace TaxRules.Domain.Tests;

internal static class Lit {

    public static Money M(string amount) {
        return new Money(decimal.Parse(amount, CultureInfo.InvariantCulture));
    }

    public static Rate R(string fraction) {
        return new Rate(decimal.Parse(fraction, CultureInfo.InvariantCulture));
    }

    public static RuleCode C(string code) {
        return new RuleCode(code);
    }

    public static decimal D(string value) {
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }

}

public sealed class EmploymentIncomeTests {

    [Fact]
    public void The_taxable_amount_is_the_gross_less_the_deduction() {
        // Arrange
        string anyEmployer = Dummies.Source().Generate();

        // Act
        EmploymentIncome income = new EmploymentIncome(anyEmployer, Lit.M("50000.00"), Lit.M("12000.50"));

        // Assert
        Assert.Equal(Lit.M("37999.50"), income.TaxableAmount);
        Assert.Equal(IncomeKind.Employment, income.Kind);
        Assert.Equal(anyEmployer, income.Source);
    }

    [Fact]
    public void A_deduction_equal_to_the_gross_leaves_nothing_to_tax() {
        Money anyGross = new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(10_000_000m)).Generate();

        EmploymentIncome income = new EmploymentIncome(Dummies.Source().Generate(), anyGross, anyGross);

        Assert.True(income.TaxableAmount.IsZero);
    }

    [Fact]
    public void A_deduction_above_the_gross_is_refused() {
        // Arrange
        (decimal anySmaller, decimal anyLarger) = Any.Pair(Dummies.IncomeAmount(), Dummies.IncomeAmount()).Having.Item1LessThanItem2().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new EmploymentIncome(Dummies.Source().Generate(), new Money(anySmaller), new Money(anyLarger)));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void A_deduction_one_cent_above_the_gross_is_refused() {
        Assert.Throws<ArgumentException>(() => new EmploymentIncome("ACME", Lit.M("100.00"), Lit.M("100.01")));
    }

    [Fact]
    public void A_generated_income_taxes_what_remains_after_the_deduction() {
        EmploymentIncome anyIncome = new AnyEmploymentIncome().Generate();

        Assert.Equal(anyIncome.Gross.Amount - anyIncome.ExpenseDeduction.Amount, anyIncome.TaxableAmount.Amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_employer_is_refused(string? employer) {
        Assert.ThrowsAny<ArgumentException>(() => new EmploymentIncome(employer!, Lit.M("1"), Lit.M("0")));
    }

    [Fact]
    public void An_employer_of_one_hundred_characters_is_accepted_and_one_hundred_and_one_refused() {
        // Arrange
        string atLimit = Any.String().Alpha().WithLength(IncomeComponent.MaxSourceLength).Generate();
        string tooLong = Any.String().Alpha().WithLength(IncomeComponent.MaxSourceLength + 1).Generate();

        // Act / Assert
        Assert.Equal(atLimit, new EmploymentIncome(atLimit, Lit.M("1"), Lit.M("0")).Source);
        Assert.Throws<ArgumentException>(() => new EmploymentIncome(tooLong, Lit.M("1"), Lit.M("0")));
    }

    [Fact]
    public void Null_amounts_are_refused() {
        Assert.Throws<ArgumentNullException>(() => new EmploymentIncome("ACME", null!, Lit.M("0")));
        Assert.Throws<ArgumentNullException>(() => new EmploymentIncome("ACME", Lit.M("1"), null!));
    }

    [Fact]
    public void Equality_compares_source_gross_and_deduction() {
        EmploymentIncome anyIncome = new AnyEmploymentIncome().Generate();
        EmploymentIncome same      = new EmploymentIncome(anyIncome.Source, anyIncome.Gross, anyIncome.ExpenseDeduction);

        Assert.Equal(anyIncome, same);
        Assert.Equal(anyIncome.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void A_different_source_makes_a_different_income() {
        string anyEmployer      = Dummies.Source().Generate();
        string anyOtherEmployer = Dummies.Source().DifferentFrom(anyEmployer).Generate();

        EmploymentIncome first  = new EmploymentIncome(anyEmployer, Lit.M("100"), Lit.M("10"));
        EmploymentIncome second = new EmploymentIncome(anyOtherEmployer, Lit.M("100"), Lit.M("10"));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_different_gross_or_deduction_makes_a_different_income() {
        EmploymentIncome baseline = new EmploymentIncome("ACME", Lit.M("100"), Lit.M("10"));

        Assert.NotEqual(baseline, new EmploymentIncome("ACME", Lit.M("101"), Lit.M("10")));
        Assert.NotEqual(baseline, new EmploymentIncome("ACME", Lit.M("100"), Lit.M("11")));
        Assert.False(baseline.Equals(null));
    }

}

public sealed class SelfEmploymentIncomeTests {

    [Fact]
    public void A_profit_is_taxed_in_full_and_has_no_loss() {
        SelfEmploymentIncome income = new SelfEmploymentIncome(Dummies.Source().Generate(), Lit.M("80000"), Lit.M("30000.25"));

        Assert.Equal(Lit.M("49999.75"), income.TaxableAmount);
        Assert.True(income.Loss.IsZero);
        Assert.Equal(IncomeKind.SelfEmployment, income.Kind);
    }

    [Fact]
    public void A_loss_makes_the_taxable_amount_zero_and_is_reported() {
        SelfEmploymentIncome income = new SelfEmploymentIncome(Dummies.Source().Generate(), Lit.M("30000"), Lit.M("80000.25"));

        Assert.True(income.TaxableAmount.IsZero);
        Assert.Equal(Lit.M("50000.25"), income.Loss);
    }

    [Fact]
    public void Turnover_equal_to_expenses_is_neither_profit_nor_loss() {
        Money anyAmount = new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(10_000_000m)).Generate();

        SelfEmploymentIncome income = new SelfEmploymentIncome(Dummies.Source().Generate(), anyAmount, anyAmount);

        Assert.True(income.TaxableAmount.IsZero);
        Assert.True(income.Loss.IsZero);
    }

    [Fact]
    public void A_generated_income_never_has_both_a_taxable_amount_and_a_loss() {
        SelfEmploymentIncome anyIncome = new AnySelfEmploymentIncome().Generate();

        Assert.True(anyIncome.TaxableAmount.IsZero || anyIncome.Loss.IsZero);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_activity_is_refused(string? activity) {
        Assert.ThrowsAny<ArgumentException>(() => new SelfEmploymentIncome(activity!, Lit.M("1"), Lit.M("0")));
    }

    [Fact]
    public void An_activity_over_one_hundred_characters_is_refused() {
        string anyTooLong = Any.String().Alpha().WithLength(IncomeComponent.MaxSourceLength + 1).Generate();

        Assert.Throws<ArgumentException>(() => new SelfEmploymentIncome(anyTooLong, Lit.M("1"), Lit.M("0")));
    }

    [Fact]
    public void Null_amounts_are_refused() {
        Assert.Throws<ArgumentNullException>(() => new SelfEmploymentIncome("shop", null!, Lit.M("0")));
        Assert.Throws<ArgumentNullException>(() => new SelfEmploymentIncome("shop", Lit.M("0"), null!));
    }

    [Fact]
    public void Equality_compares_source_turnover_and_expenses() {
        SelfEmploymentIncome anyIncome = new AnySelfEmploymentIncome().Generate();
        SelfEmploymentIncome same      = new SelfEmploymentIncome(anyIncome.Source, anyIncome.Turnover, anyIncome.Expenses);

        Assert.Equal(anyIncome, same);
        Assert.Equal(anyIncome.GetHashCode(), same.GetHashCode());
        string anyOtherSource = Dummies.Source().DifferentFrom(anyIncome.Source).Generate();
        Assert.NotEqual(anyIncome, new SelfEmploymentIncome(anyOtherSource, anyIncome.Turnover, anyIncome.Expenses));
        Assert.NotEqual(new SelfEmploymentIncome("shop", Lit.M("10"), Lit.M("1")), new SelfEmploymentIncome("shop", Lit.M("11"), Lit.M("1")));
        Assert.NotEqual(new SelfEmploymentIncome("shop", Lit.M("10"), Lit.M("1")), new SelfEmploymentIncome("shop", Lit.M("10"), Lit.M("2")));
    }

}

public sealed class InvestmentIncomeTests {

    [Fact]
    public void Interest_is_taxable_in_full() {
        Money anyGross = new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(10_000_000m)).Generate();

        InvestmentIncome income = new InvestmentIncome(Dummies.Source().Generate(), anyGross, InvestmentKind.Interest);

        Assert.Equal(anyGross, income.TaxableAmount);
        Assert.Equal(IncomeKind.Investment, income.Kind);
    }

    [Theory]
    [InlineData("100.00", "60.00")]
    [InlineData("1000.50", "600.30")]
    [InlineData("0.00", "0.00")]
    [InlineData("0.01", "0.01")]
    [InlineData("0.02", "0.01")]
    [InlineData("0.03", "0.02")]
    [InlineData("0.04", "0.02")]
    [InlineData("0.05", "0.03")]
    [InlineData("10.01", "6.01")]
    public void Dividends_are_included_at_sixty_percent_rounded_to_the_cent(string gross, string expected) {
        InvestmentIncome income = new InvestmentIncome(Dummies.Source().Generate(), Lit.M(gross), InvestmentKind.Dividend);

        Assert.Equal(Lit.M(expected), income.TaxableAmount);
    }

    [Fact]
    public void The_dividend_inclusion_rate_is_sixty_percent() {
        Assert.Equal(Lit.R("0.6"), InvestmentIncome.DividendInclusion);
    }

    [Fact]
    public void An_undefined_investment_kind_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvestmentIncome("bank", Lit.M("1"), (InvestmentKind)42));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_payer_is_refused(string? payer) {
        Assert.ThrowsAny<ArgumentException>(() => new InvestmentIncome(payer!, Lit.M("1"), InvestmentKind.Interest));
    }

    [Fact]
    public void A_null_gross_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new InvestmentIncome("bank", null!, InvestmentKind.Interest));
    }

    [Fact]
    public void Equality_compares_payer_gross_and_kind() {
        InvestmentIncome anyIncome = new AnyInvestmentIncome().Generate();
        InvestmentIncome same      = new InvestmentIncome(anyIncome.Source, anyIncome.Gross, anyIncome.InvestmentKind);

        Assert.Equal(anyIncome, same);
        Assert.Equal(anyIncome.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void The_same_gross_as_interest_and_as_dividend_are_different_incomes() {
        Assert.NotEqual(new InvestmentIncome("bank", Lit.M("10"), InvestmentKind.Interest),
                        new InvestmentIncome("bank", Lit.M("10"), InvestmentKind.Dividend));
    }

}
