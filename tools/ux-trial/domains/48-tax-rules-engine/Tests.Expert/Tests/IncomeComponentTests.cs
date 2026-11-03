using JustDummies;

using TaxRules.Domain;

using Xunit;

using static TaxRules.Tests.Build;

namespace TaxRules.Tests;

public sealed class EmploymentIncomeTests {

    [Fact]
    public void The_taxable_amount_is_the_gross_less_the_expense_deduction() {
        EmploymentIncome salary = Any.EmploymentIncome().WithGross(M(50_000m)).WithExpenseDeduction(M(1_500.50m)).Generate();

        Assert.Equal(M(48_499.50m), salary.TaxableAmount);
    }

    [Fact]
    public void A_deduction_equal_to_the_gross_leaves_nothing_to_tax() {
        EmploymentIncome salary = Any.EmploymentIncome().WithGross(M(1_000m)).WithExpenseDeduction(M(1_000m)).Generate();

        Assert.True(salary.TaxableAmount.IsZero);
    }

    [Fact]
    public void A_deduction_above_the_gross_is_refused() {
        string anyEmployer = Plausible.Label().WithMaxLength(IncomeComponent.MaxSourceLength).Generate();

        Assert.Throws<ArgumentException>(() => new EmploymentIncome(anyEmployer, M(1_000m), M(1_000.01m)));
    }

    [Fact]
    public void An_employment_is_of_the_employment_kind() {
        EmploymentIncome anySalary = Any.EmploymentIncome().Generate();

        Assert.Equal(IncomeKind.Employment, anySalary.Kind);
    }

    [Fact]
    public void An_employment_keeps_what_it_was_built_from() {
        EmploymentIncome salary = Any.EmploymentIncome().WithEmployer("ACME Ltd").WithGross(M(100m)).WithExpenseDeduction(M(10m)).Generate();

        Assert.Equal("ACME Ltd", salary.Source);
        Assert.Equal(M(100m), salary.Gross);
        Assert.Equal(M(10m), salary.ExpenseDeduction);
    }

    [Fact]
    public void An_employment_needs_its_amounts() {
        Assert.Throws<ArgumentNullException>(() => new EmploymentIncome("ACME", null!, Money.Zero));
        Assert.Throws<ArgumentNullException>(() => new EmploymentIncome("ACME", Money.Zero, null!));
    }

    [Fact]
    public void Two_employments_with_the_same_employer_and_amounts_are_equal() {
        EmploymentIncome salary = Any.EmploymentIncome().WithEmployer("ACME").WithGross(M(100m)).WithExpenseDeduction(M(10m)).Generate();
        EmploymentIncome sameSalary = new EmploymentIncome("ACME", M(100.00m), M(10.0m));

        Assert.Equal(salary, sameSalary);
        Assert.Equal(salary.GetHashCode(), sameSalary.GetHashCode());
    }

    [Fact]
    public void Employments_differing_by_employer_or_by_an_amount_are_not_equal() {
        EmploymentIncome salary = new EmploymentIncome("ACME", M(100m), M(10m));

        Assert.NotEqual(salary, new EmploymentIncome("ACMF", M(100m), M(10m)));
        Assert.NotEqual(salary, new EmploymentIncome("ACME", M(101m), M(10m)));
        Assert.NotEqual(salary, new EmploymentIncome("ACME", M(100m), M(11m)));
    }

    [Fact]
    public void An_employment_does_not_equal_another_kind_of_income_with_the_same_source_and_amounts() {
        IncomeComponent salary = new EmploymentIncome("ACME", M(100m), M(10m));
        IncomeComponent business = new SelfEmploymentIncome("ACME", M(100m), M(10m));

        Assert.False(salary.Equals(business));
        Assert.False(business.Equals(salary));
    }

    [Fact]
    public void An_employment_is_described_by_its_employer_and_amounts() {
        Assert.Equal("Employment at ACME: 100.00 less 10.00", new EmploymentIncome("ACME", M(100m), M(10m)).ToString());
    }

}

public sealed class SelfEmploymentIncomeTests {

    [Fact]
    public void The_taxable_amount_is_the_turnover_less_the_expenses() {
        SelfEmploymentIncome profit = Any.SelfEmploymentIncome().WithTurnover(M(80_000m)).WithExpenses(M(30_250m)).Generate();

        Assert.Equal(M(49_750m), profit.TaxableAmount);
        Assert.True(profit.Loss.IsZero);
    }

    [Fact]
    public void Expenses_above_the_turnover_leave_no_taxable_amount_and_a_loss() {
        SelfEmploymentIncome loss = Any.SelfEmploymentIncome().WithTurnover(M(10_000m)).WithExpenses(M(12_500.25m)).Generate();

        Assert.True(loss.TaxableAmount.IsZero);
        Assert.Equal(M(2_500.25m), loss.Loss);
    }

    [Fact]
    public void Expenses_equal_to_the_turnover_leave_neither_profit_nor_loss() {
        SelfEmploymentIncome even = Any.SelfEmploymentIncome().WithTurnover(M(500m)).WithExpenses(M(500m)).Generate();

        Assert.True(even.TaxableAmount.IsZero);
        Assert.True(even.Loss.IsZero);
    }

    [Fact]
    public void A_self_employment_is_of_the_self_employment_kind() {
        SelfEmploymentIncome anyActivity = Any.SelfEmploymentIncome().Generate();

        Assert.Equal(IncomeKind.SelfEmployment, anyActivity.Kind);
    }

    [Fact]
    public void A_self_employment_needs_its_amounts() {
        Assert.Throws<ArgumentNullException>(() => new SelfEmploymentIncome("Plumbing", null!, Money.Zero));
        Assert.Throws<ArgumentNullException>(() => new SelfEmploymentIncome("Plumbing", Money.Zero, null!));
    }

    [Fact]
    public void Two_self_employments_with_the_same_activity_and_amounts_are_equal() {
        SelfEmploymentIncome activity = new SelfEmploymentIncome("Plumbing", M(100m), M(10m));

        Assert.Equal(activity, new SelfEmploymentIncome("Plumbing", M(100.0m), M(10.00m)));
        Assert.Equal(activity.GetHashCode(), new SelfEmploymentIncome("Plumbing", M(100m), M(10m)).GetHashCode());
        Assert.NotEqual(activity, new SelfEmploymentIncome("Plumbing", M(100m), M(11m)));
        Assert.NotEqual(activity, new SelfEmploymentIncome("Roofing", M(100m), M(10m)));
        Assert.NotEqual(activity, new SelfEmploymentIncome("Plumbing", M(101m), M(10m)));
    }

    [Fact]
    public void A_self_employment_is_described_by_its_activity_and_amounts() {
        Assert.Equal("Self-employment (Plumbing): 100.00 less 10.00", new SelfEmploymentIncome("Plumbing", M(100m), M(10m)).ToString());
    }

}

public sealed class InvestmentIncomeTests {

    [Fact]
    public void Interest_is_taxable_in_full() {
        InvestmentIncome interest = Any.InvestmentIncome().WithInvestmentKind(InvestmentKind.Interest).WithGross(M(1_234.56m)).Generate();

        Assert.Equal(M(1_234.56m), interest.TaxableAmount);
    }

    [Theory]
    [InlineData("1000.00", "600.00")]
    [InlineData("100.05", "60.03")]
    [InlineData("0.25", "0.15")]
    [InlineData("0.01", "0.01")]
    [InlineData("0.00", "0.00")]
    [InlineData("0.03", "0.02")]
    public void Dividends_are_taxable_at_sixty_percent_rounded_to_the_cent(string gross, string expected) {
        InvestmentIncome dividend = Any.InvestmentIncome()
                                       .WithInvestmentKind(InvestmentKind.Dividend)
                                       .WithGross(M(decimal.Parse(gross, System.Globalization.CultureInfo.InvariantCulture)))
                                       .Generate();

        Assert.Equal(M(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)), dividend.TaxableAmount);
    }

    [Fact]
    public void The_dividend_inclusion_is_sixty_percent() {
        Assert.Equal(0.60m, InvestmentIncome.DividendInclusion.Fraction);
    }

    [Fact]
    public void An_investment_is_of_the_investment_kind() {
        InvestmentIncome anyInvestment = Any.InvestmentIncome().Generate();

        Assert.Equal(IncomeKind.Investment, anyInvestment.Kind);
    }

    [Fact]
    public void An_undefined_investment_kind_is_refused() {
        InvestmentIncome anyInvestment = Any.InvestmentIncome().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new InvestmentIncome(anyInvestment.Source, anyInvestment.Gross, (InvestmentKind)2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvestmentIncome(anyInvestment.Source, anyInvestment.Gross, (InvestmentKind)(-1)));
    }

    [Fact]
    public void An_investment_needs_its_gross_amount() {
        Assert.Throws<ArgumentNullException>(() => new InvestmentIncome("Bank", null!, InvestmentKind.Interest));
    }

    [Fact]
    public void Two_investments_of_the_same_payer_amount_and_kind_are_equal() {
        InvestmentIncome interest = new InvestmentIncome("Bank", M(100m), InvestmentKind.Interest);

        Assert.Equal(interest, new InvestmentIncome("Bank", M(100.00m), InvestmentKind.Interest));
        Assert.Equal(interest.GetHashCode(), new InvestmentIncome("Bank", M(100m), InvestmentKind.Interest).GetHashCode());
        Assert.NotEqual(interest, new InvestmentIncome("Bank", M(100m), InvestmentKind.Dividend));
        Assert.NotEqual(interest, new InvestmentIncome("Bank", M(101m), InvestmentKind.Interest));
        Assert.NotEqual(interest, new InvestmentIncome("Fund", M(100m), InvestmentKind.Interest));
    }

    [Fact]
    public void An_investment_is_described_by_its_kind_payer_and_gross() {
        Assert.Equal("Dividend from Bank: 100.00", new InvestmentIncome("Bank", M(100m), InvestmentKind.Dividend).ToString());
    }

}

public sealed class IncomeSourceTests {

    [Fact]
    public void A_source_of_one_hundred_characters_is_accepted() {
        string hundred = new string('x', IncomeComponent.MaxSourceLength);

        EmploymentIncome salary = Any.EmploymentIncome().WithEmployer(hundred).Generate();

        Assert.Equal(hundred, salary.Source);
    }

    [Fact]
    public void A_source_of_more_than_one_hundred_characters_is_refused_whatever_the_kind() {
        string tooLong = new string('x', IncomeComponent.MaxSourceLength + 1);

        Assert.Throws<ArgumentException>(() => Any.EmploymentIncome().WithEmployer(tooLong).Generate());
        Assert.Throws<ArgumentException>(() => Any.SelfEmploymentIncome().WithActivity(tooLong).Generate());
        Assert.Throws<ArgumentException>(() => Any.InvestmentIncome().WithPayer(tooLong).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \t\r\n ")]
    public void A_blank_source_is_refused_whatever_the_kind(string blank) {
        Assert.Throws<ArgumentException>(() => Any.EmploymentIncome().WithEmployer(blank).Generate());
        Assert.Throws<ArgumentException>(() => Any.SelfEmploymentIncome().WithActivity(blank).Generate());
        Assert.Throws<ArgumentException>(() => Any.InvestmentIncome().WithPayer(blank).Generate());
    }

    [Fact]
    public void A_missing_source_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.EmploymentIncome().WithEmployer((string)null!).Generate());
    }

    [Fact]
    public void A_source_with_interior_blanks_is_accepted() {
        string spaced = "  Café  du  Coin  ";

        Assert.Equal(spaced, Any.SelfEmploymentIncome().WithActivity(spaced).Generate().Source);
    }

}
