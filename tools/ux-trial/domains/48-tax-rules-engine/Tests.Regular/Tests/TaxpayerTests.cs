using JustDummies;

using Xunit;

namespace TaxRules.Domain.Tests;

public sealed class TaxpayerTests {

    private static Taxpayer TaxpayerWith(params IncomeComponent[] incomes) {
        return new AnyTaxpayer().WithIncomes(incomes).Generate();
    }

    [Fact]
    public void A_taxpayer_keeps_what_it_was_given() {
        // Arrange
        TaxpayerId       anyId        = new AnyTaxpayerId().Generate();
        JurisdictionCode anyResidence = new AnyJurisdictionCode().Generate();
        DateOnly         anyBirth     = Any.DateOnly().Between(Taxpayer.EarliestDateOfBirth, new DateOnly(2020, 12, 31)).Generate();
        int              anyHousehold = Any.Int32().Between(1, Taxpayer.MaxHouseholdSize).Generate();
        IncomeComponent  anyIncome    = Dummies.Incomes().Generate();

        // Act
        Taxpayer taxpayer = new Taxpayer(anyId, anyBirth, anyResidence, anyHousehold, [anyIncome]);

        // Assert
        Assert.Same(anyId, taxpayer.Id);
        Assert.Equal(anyBirth, taxpayer.DateOfBirth);
        Assert.Same(anyResidence, taxpayer.Residence);
        Assert.Equal(anyHousehold, taxpayer.HouseholdSize);
        Assert.Same(anyIncome, Assert.Single(taxpayer.Incomes));
    }

    [Fact]
    public void A_taxpayer_may_declare_no_income() {
        Taxpayer anyTaxpayer = TaxpayerWith();

        Assert.Empty(anyTaxpayer.Incomes);
        Assert.True(anyTaxpayer.TaxableIncome(IncomeKind.All).IsZero);
    }

    [Theory]
    [InlineData(1900, 1, 1)]
    [InlineData(1900, 1, 2)]
    [InlineData(2024, 12, 31)]
    public void A_date_of_birth_on_or_after_1900_is_accepted(int year, int month, int day) {
        Taxpayer taxpayer = new AnyTaxpayer().WithDateOfBirth(new DateOnly(year, month, day)).Generate();

        Assert.Equal(new DateOnly(year, month, day), taxpayer.DateOfBirth);
    }

    [Fact]
    public void A_date_of_birth_before_1900_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyTaxpayer().WithDateOfBirth(new DateOnly(1899, 12, 31)).Generate());
    }

    [Fact]
    public void Any_earlier_date_of_birth_is_refused() {
        DateOnly anyEarlyDate = Any.DateOnly().Before(Taxpayer.EarliestDateOfBirth).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyTaxpayer().WithDateOfBirth(anyEarlyDate).Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void A_household_of_one_to_twenty_is_accepted(int members) {
        Assert.Equal(members, new AnyTaxpayer().WithHouseholdSize(members).Generate().HouseholdSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    public void A_household_outside_one_to_twenty_is_refused(int members) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyTaxpayer().WithHouseholdSize(members).Generate());
    }

    [Fact]
    public void Thirty_income_components_are_accepted_and_thirty_one_refused() {
        // Arrange
        List<IncomeComponent> thirty = Any.ListOf(Dummies.Incomes()).WithCount(Taxpayer.MaxIncomeComponents).Generate();
        List<IncomeComponent> thirtyOne = Any.ListOf(Dummies.Incomes()).WithCount(Taxpayer.MaxIncomeComponents + 1).Generate();

        // Act / Assert
        Assert.Equal(30, new AnyTaxpayer().WithIncomes(thirty).Generate().Incomes.Count);
        Assert.Throws<ArgumentException>(() => new AnyTaxpayer().WithIncomes(thirtyOne).Generate());
    }

    [Fact]
    public void A_null_income_is_refused() {
        List<IncomeComponent?> withNull = [Dummies.Incomes().Generate(), null];

        Assert.Throws<ArgumentException>(() => new AnyTaxpayer().WithIncomes(withNull!).Generate());
    }

    [Fact]
    public void Null_collaborators_are_refused() {
        TaxpayerId       anyId        = new AnyTaxpayerId().Generate();
        JurisdictionCode anyResidence = new AnyJurisdictionCode().Generate();
        DateOnly         anyBirth     = new DateOnly(1980, 1, 1);

        Assert.Throws<ArgumentNullException>(() => new Taxpayer(null!, anyBirth, anyResidence, 1, []));
        Assert.Throws<ArgumentNullException>(() => new Taxpayer(anyId, anyBirth, null!, 1, []));
        Assert.Throws<ArgumentNullException>(() => new Taxpayer(anyId, anyBirth, anyResidence, 1, null!));
    }

    [Fact]
    public void The_taxpayer_does_not_follow_later_changes_to_the_list_it_was_given() {
        // Arrange
        List<IncomeComponent> declared = [Dummies.Incomes().Generate()];
        Taxpayer              taxpayer = new AnyTaxpayer().WithIncomes(declared).Generate();

        // Act
        declared.Add(Dummies.Incomes().Generate());

        // Assert
        Assert.Single(taxpayer.Incomes);
    }

    [Theory]
    [InlineData(1990, 6, 15, 2020, 30)]
    [InlineData(2000, 12, 31, 2030, 30)]
    [InlineData(2000, 1, 1, 2030, 30)]
    [InlineData(2000, 7, 7, 2000, 0)]
    [InlineData(1900, 1, 1, 2099, 199)]
    public void Age_is_counted_in_years_on_31_December_of_the_tax_year(int year, int month, int day, int taxYear, int expected) {
        Taxpayer taxpayer = new AnyTaxpayer().WithDateOfBirth(new DateOnly(year, month, day)).Generate();

        Assert.Equal(expected, taxpayer.AgeAtEndOf(taxYear));
    }

    [Fact]
    public void Age_is_refused_for_a_year_before_the_birth_year() {
        // Arrange
        Taxpayer anyTaxpayer = new AnyTaxpayer().WithDateOfBirth(new DateOnly(1990, 6, 15)).Generate();
        int      anyEarlierYear = Any.Int32().Between(1800, 1989).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyTaxpayer.AgeAtEndOf(anyEarlierYear));
    }

    [Fact]
    public void Taxable_income_sums_the_components_of_the_requested_kinds() {
        // Arrange
        Taxpayer taxpayer = TaxpayerWith(
            new EmploymentIncome("ACME", Lit.M("50000"), Lit.M("5000")),
            new EmploymentIncome("Globex", Lit.M("10000"), Lit.M("0")),
            new SelfEmploymentIncome("consulting", Lit.M("20000"), Lit.M("8000")),
            new SelfEmploymentIncome("failed venture", Lit.M("1000"), Lit.M("9000")),
            new InvestmentIncome("bank", Lit.M("300"), InvestmentKind.Interest),
            new InvestmentIncome("fund", Lit.M("1000"), InvestmentKind.Dividend));

        // Act / Assert
        Assert.Equal(Lit.M("55000"), taxpayer.TaxableIncome(IncomeKind.Employment));
        Assert.Equal(Lit.M("12000"), taxpayer.TaxableIncome(IncomeKind.SelfEmployment));
        Assert.Equal(Lit.M("900"), taxpayer.TaxableIncome(IncomeKind.Investment));
        Assert.Equal(Lit.M("67000"), taxpayer.TaxableIncome(IncomeKind.Employment | IncomeKind.SelfEmployment));
        Assert.Equal(Lit.M("55900"), taxpayer.TaxableIncome(IncomeKind.Employment | IncomeKind.Investment));
        Assert.Equal(Lit.M("67900"), taxpayer.TaxableIncome(IncomeKind.All));
    }

    [Fact]
    public void A_kind_the_taxpayer_has_no_income_of_is_zero() {
        Taxpayer taxpayer = TaxpayerWith(new EmploymentIncome("ACME", Lit.M("50000"), Lit.M("0")));

        Assert.True(taxpayer.TaxableIncome(IncomeKind.Investment).IsZero);
    }

    [Fact]
    public void Taxable_income_of_all_kinds_equals_the_sum_of_the_taxable_amounts() {
        // Arrange
        List<IncomeComponent> anyIncomes = Any.ListOf(Dummies.Incomes()).WithCountBetween(0, 10).Generate();
        Taxpayer              taxpayer   = new AnyTaxpayer().WithIncomes(anyIncomes).Generate();

        // Act
        Money total = taxpayer.TaxableIncome(IncomeKind.All);

        // Assert
        Assert.Equal(anyIncomes.Sum(i => i.TaxableAmount.Amount), total.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(15)]
    [InlineData(-1)]
    public void A_tax_base_naming_no_kind_or_an_unknown_one_is_refused(int kinds) {
        Taxpayer anyTaxpayer = new AnyTaxpayer().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyTaxpayer.TaxableIncome((IncomeKind)kinds));
    }

    [Fact]
    public void ToString_names_the_taxpayer_and_the_residence() {
        Taxpayer anyTaxpayer = new AnyTaxpayer().Generate();

        Assert.Equal($"Taxpayer {anyTaxpayer.Id} ({anyTaxpayer.Residence})", anyTaxpayer.ToString());
    }

}
