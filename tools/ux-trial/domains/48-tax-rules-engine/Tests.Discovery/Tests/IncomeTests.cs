using JustDummies;

using TaxRules.Domain;

namespace TaxRules.Tests.Discovery;

public sealed class EmploymentIncomeTests {

    [Fact]
    public void The_taxable_amount_is_the_salary_less_the_expense_deduction() {
        // Arrange
        EmploymentIncome income = new EmploymentIncome(Fixtures.AnySource(), new Money(50_000m), new Money(1_500.50m));

        // Act and assert
        Assert.Equal(new Money(48_499.50m), income.TaxableAmount);
        Assert.Equal(IncomeKind.Employment, income.Kind);
    }

    [Fact]
    public void A_deduction_equal_to_the_salary_leaves_nothing_to_tax() {
        EmploymentIncome income = new EmploymentIncome(Fixtures.AnySource(), new Money(1_000m), new Money(1_000m));

        Assert.True(income.TaxableAmount.IsZero);
    }

    [Fact]
    public void A_deduction_above_the_salary_is_refused() {
        Assert.Throws<ArgumentException>(() => new EmploymentIncome(Fixtures.AnySource(), new Money(1_000m), new Money(1_000.01m)));
    }

    [Fact]
    public void A_missing_amount_is_refused() {
        string anySource = Fixtures.AnySource();

        Assert.Throws<ArgumentNullException>(() => new EmploymentIncome(anySource, null!, Money.Zero));
        Assert.Throws<ArgumentNullException>(() => new EmploymentIncome(anySource, Money.Zero, null!));
    }

    [Fact]
    public void The_source_keeps_the_employer_it_was_given() {
        EmploymentIncome income = new EmploymentIncome("Acme", new Money(10m), Money.Zero);

        Assert.Equal("Acme", income.Source);
    }

    [Fact]
    public void Two_incomes_with_the_same_source_and_amounts_are_equal() {
        string anySource = Fixtures.AnySource();

        EmploymentIncome first  = new EmploymentIncome(anySource, new Money(100m), new Money(10m));
        EmploymentIncome second = new EmploymentIncome(anySource, new Money(100m), new Money(10m));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_different_source_or_amount_makes_a_different_income() {
        EmploymentIncome reference = new EmploymentIncome("Acme", new Money(100m), new Money(10m));

        Assert.NotEqual(reference, new EmploymentIncome("Other", new Money(100m), new Money(10m)));
        Assert.NotEqual(reference, new EmploymentIncome("Acme", new Money(101m), new Money(10m)));
        Assert.NotEqual(reference, new EmploymentIncome("Acme", new Money(100m), new Money(11m)));
    }

    [Fact]
    public void A_generated_income_is_always_constructible_and_its_deduction_never_exceeds_its_salary() {
        for (int i = 0; i < 100; i++) {
            EmploymentIncome anyIncome = Any.EmploymentIncome().Generate();

            Assert.True(anyIncome.ExpenseDeduction.CompareTo(anyIncome.Gross) <= 0);
        }
    }

}

public sealed class SelfEmploymentIncomeTests {

    [Fact]
    public void The_taxable_amount_is_the_profit() {
        SelfEmploymentIncome income = new SelfEmploymentIncome(Fixtures.AnySource(), new Money(20_000m), new Money(7_250.25m));

        Assert.Equal(new Money(12_749.75m), income.TaxableAmount);
        Assert.True(income.Loss.IsZero);
        Assert.Equal(IncomeKind.SelfEmployment, income.Kind);
    }

    [Fact]
    public void Expenses_above_turnover_are_a_loss_and_tax_nothing() {
        SelfEmploymentIncome income = new SelfEmploymentIncome(Fixtures.AnySource(), new Money(1_000m), new Money(1_800m));

        Assert.True(income.TaxableAmount.IsZero);
        Assert.Equal(new Money(800m), income.Loss);
    }

    [Fact]
    public void Expenses_equal_to_turnover_are_neither_a_profit_nor_a_loss() {
        SelfEmploymentIncome income = new SelfEmploymentIncome(Fixtures.AnySource(), new Money(1_000m), new Money(1_000m));

        Assert.True(income.TaxableAmount.IsZero);
        Assert.True(income.Loss.IsZero);
    }

    [Fact]
    public void A_missing_amount_is_refused() {
        string anySource = Fixtures.AnySource();

        Assert.Throws<ArgumentNullException>(() => new SelfEmploymentIncome(anySource, null!, Money.Zero));
        Assert.Throws<ArgumentNullException>(() => new SelfEmploymentIncome(anySource, Money.Zero, null!));
    }

    [Fact]
    public void Incomes_equal_by_source_turnover_and_expenses() {
        Assert.Equal(new SelfEmploymentIncome("Bakery", new Money(5m), new Money(2m)), new SelfEmploymentIncome("Bakery", new Money(5m), new Money(2m)));
        Assert.NotEqual(new SelfEmploymentIncome("Bakery", new Money(5m), new Money(2m)), new SelfEmploymentIncome("Bakery", new Money(5m), new Money(3m)));
        Assert.NotEqual(new SelfEmploymentIncome("Bakery", new Money(5m), new Money(2m)), new SelfEmploymentIncome("Bakery", new Money(6m), new Money(2m)));
        Assert.NotEqual(new SelfEmploymentIncome("Bakery", new Money(5m), new Money(2m)), new SelfEmploymentIncome("Garage", new Money(5m), new Money(2m)));
    }

}

public sealed class InvestmentIncomeTests {

    [Fact]
    public void Interest_is_taxable_in_full() {
        InvestmentIncome income = Fixtures.Interest(1_234.56m);

        Assert.Equal(new Money(1_234.56m), income.TaxableAmount);
        Assert.Equal(IncomeKind.Investment, income.Kind);
    }

    [Fact]
    public void Dividends_are_taxable_at_sixty_percent() {
        Assert.Equal(new Money(600m), Fixtures.Dividend(1_000m).TaxableAmount);
        Assert.Equal(new Money(60.03m), Fixtures.Dividend(100.05m).TaxableAmount);
    }

    [Theory]
    [InlineData("0.01", "0.01")]
    [InlineData("0.02", "0.01")]
    [InlineData("0.03", "0.02")]
    [InlineData("0.04", "0.02")]
    [InlineData("0.05", "0.03")]
    public void A_dividend_is_rounded_to_the_cent_after_inclusion(string gross, string taxable) {
        Assert.Equal(
            new Money(decimal.Parse(taxable, System.Globalization.CultureInfo.InvariantCulture)),
            Fixtures.Dividend(decimal.Parse(gross, System.Globalization.CultureInfo.InvariantCulture)).TaxableAmount);
    }

    [Fact]
    public void The_dividend_inclusion_rate_is_sixty_percent() {
        Assert.Equal(new Rate(0.60m), InvestmentIncome.DividendInclusion);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(100)]
    public void An_undefined_investment_kind_is_refused(int kind) {
        string anySource = Fixtures.AnySource();

        Assert.Throws<ArgumentOutOfRangeException>(() => new InvestmentIncome(anySource, Money.Zero, (InvestmentKind)kind));
    }

    [Fact]
    public void A_missing_amount_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new InvestmentIncome(Fixtures.AnySource(), null!, InvestmentKind.Interest));
    }

    [Fact]
    public void Incomes_equal_by_source_gross_and_kind() {
        Assert.Equal(new InvestmentIncome("Bank", new Money(5m), InvestmentKind.Dividend), new InvestmentIncome("Bank", new Money(5m), InvestmentKind.Dividend));
        Assert.NotEqual(new InvestmentIncome("Bank", new Money(5m), InvestmentKind.Dividend), new InvestmentIncome("Bank", new Money(5m), InvestmentKind.Interest));
        Assert.NotEqual(new InvestmentIncome("Bank", new Money(5m), InvestmentKind.Dividend), new InvestmentIncome("Fund", new Money(5m), InvestmentKind.Dividend));
        Assert.NotEqual(new InvestmentIncome("Bank", new Money(5m), InvestmentKind.Dividend), new InvestmentIncome("Bank", new Money(6m), InvestmentKind.Dividend));
    }

}

public sealed class IncomeComponentTests {

    [Fact]
    public void Different_kinds_of_income_are_never_equal_even_with_the_same_source_and_amounts() {
        object salary = new EmploymentIncome("Acme", new Money(100m), Money.Zero);
        object profit = new SelfEmploymentIncome("Acme", new Money(100m), Money.Zero);

        Assert.False(salary.Equals(profit));
        Assert.False(profit.Equals(salary));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_source_is_refused_whatever_the_kind(string source) {
        Assert.Throws<ArgumentException>(() => new EmploymentIncome(source, Money.Zero, Money.Zero));
        Assert.Throws<ArgumentException>(() => new SelfEmploymentIncome(source, Money.Zero, Money.Zero));
        Assert.Throws<ArgumentException>(() => new InvestmentIncome(source, Money.Zero, InvestmentKind.Interest));
    }

    [Fact]
    public void A_missing_source_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new EmploymentIncome(null!, Money.Zero, Money.Zero));
    }

    [Fact]
    public void A_source_of_one_hundred_characters_is_accepted_and_one_more_is_refused() {
        string exactly100 = new string('x', IncomeComponent.MaxSourceLength);
        string tooLong    = new string('x', IncomeComponent.MaxSourceLength + 1);

        Assert.Equal(exactly100, new EmploymentIncome(exactly100, Money.Zero, Money.Zero).Source);
        Assert.Throws<ArgumentException>(() => new EmploymentIncome(tooLong, Money.Zero, Money.Zero));
        Assert.Throws<ArgumentException>(() => new SelfEmploymentIncome(tooLong, Money.Zero, Money.Zero));
        Assert.Throws<ArgumentException>(() => new InvestmentIncome(tooLong, Money.Zero, InvestmentKind.Interest));
    }

    [Fact]
    public void Each_kind_reports_exactly_one_income_flag() {
        IncomeComponent[] components = [Fixtures.Salary(1m), Fixtures.Profit(1m), Fixtures.Interest(1m)];

        Assert.Equal([IncomeKind.Employment, IncomeKind.SelfEmployment, IncomeKind.Investment], components.Select(c => c.Kind));
    }

    [Fact]
    public void A_generated_component_is_always_constructible() {
        for (int i = 0; i < 100; i++) {
            IncomeComponent anyComponent = Any.IncomeComponent().Generate();

            Assert.NotNull(anyComponent.TaxableAmount);
        }
    }

}
