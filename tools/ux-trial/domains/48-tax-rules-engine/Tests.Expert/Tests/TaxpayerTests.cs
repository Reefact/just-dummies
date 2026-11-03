using JustDummies;

using TaxRules.Domain;

using Xunit;

using static TaxRules.Tests.Build;

namespace TaxRules.Tests;

public sealed class TaxpayerTests {

    private static AnyTaxpayer AnyTaxpayer() => Any.Taxpayer();

    [Fact]
    public void A_taxpayer_keeps_what_it_was_built_from() {
        TaxpayerId id = Any.TaxpayerId().Generate();
        JurisdictionCode residence = Any.JurisdictionCode().Generate();
        IncomeComponent salary = Salary(1_000m);

        Taxpayer taxpayer = new Taxpayer(id, new DateOnly(1985, 7, 14), residence, 3, [salary]);

        Assert.Same(id, taxpayer.Id);
        Assert.Equal(new DateOnly(1985, 7, 14), taxpayer.DateOfBirth);
        Assert.Same(residence, taxpayer.Residence);
        Assert.Equal(3, taxpayer.HouseholdSize);
        Assert.Same(salary, Assert.Single(taxpayer.Incomes));
    }

    [Fact]
    public void The_first_of_january_nineteen_hundred_is_the_earliest_date_of_birth() {
        Assert.Equal(new DateOnly(1900, 1, 1), Taxpayer.EarliestDateOfBirth);
        Assert.Equal(new DateOnly(1900, 1, 1), AnyTaxpayer().WithDateOfBirth(new DateOnly(1900, 1, 1)).Generate().DateOfBirth);
    }

    [Fact]
    public void A_date_of_birth_before_nineteen_hundred_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyTaxpayer().WithDateOfBirth(new DateOnly(1899, 12, 31)).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyTaxpayer().WithDateOfBirth(DateOnly.MinValue).Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void A_household_of_one_to_twenty_members_is_accepted(int members) {
        Assert.Equal(members, AnyTaxpayer().WithHouseholdSize(members).Generate().HouseholdSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void Any_other_household_size_is_refused(int members) {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyTaxpayer().WithHouseholdSize(members).Generate());
    }

    [Fact]
    public void Thirty_incomes_are_accepted_and_thirty_one_refused() {
        Assert.Equal(30, Taxpayer.MaxIncomeComponents);
        Assert.Equal(30, AnyTaxpayer().WithIncomes(Enumerable.Range(0, 30).Select(_ => Salary(1m))).Generate().Incomes.Count);
        Assert.Throws<ArgumentException>(() => AnyTaxpayer().WithIncomes(Enumerable.Range(0, 31).Select(_ => Salary(1m))).Generate());
    }

    [Fact]
    public void A_taxpayer_may_have_no_income() {
        Taxpayer jobless = TaxpayerEarning();

        Assert.Empty(jobless.Incomes);
        Assert.True(jobless.TaxableIncome(IncomeKind.All).IsZero);
    }

    [Fact]
    public void A_null_income_is_refused() {
        Assert.Throws<ArgumentException>(() => AnyTaxpayer().WithIncomes([Salary(1m), null!]).Generate());
    }

    [Fact]
    public void A_taxpayer_needs_an_identity_a_residence_and_a_list_of_incomes() {
        Assert.Throws<ArgumentNullException>(() => AnyTaxpayer().WithId((TaxpayerId)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => AnyTaxpayer().WithResidence((JurisdictionCode)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => AnyTaxpayer().WithIncomes(null!).Generate());
    }

    [Fact]
    public void The_age_is_counted_on_the_last_day_of_the_tax_year_as_the_difference_of_years() {
        Taxpayer taxpayer = AnyTaxpayer().WithDateOfBirth(new DateOnly(1990, 12, 31)).Generate();

        Assert.Equal(0, taxpayer.AgeAtEndOf(1990));
        Assert.Equal(1, taxpayer.AgeAtEndOf(1991));
        Assert.Equal(30, taxpayer.AgeAtEndOf(2020));
    }

    [Fact]
    public void The_age_in_a_year_before_the_birth_is_refused() {
        Taxpayer taxpayer = AnyTaxpayer().WithDateOfBirth(new DateOnly(1990, 1, 1)).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => taxpayer.AgeAtEndOf(1989));
    }

    [Fact]
    public void The_taxable_income_of_a_kind_is_the_sum_of_the_taxable_amounts_of_the_incomes_of_that_kind() {
        Taxpayer taxpayer = TaxpayerEarning(Salary(20_000m, 2_000m),
                                            Salary(5_000m),
                                            Business(10_000m, 4_000m),
                                            Business(1_000m, 3_000m),
                                            Interest(100m),
                                            Dividend(100m));

        Assert.Equal(M(23_000m), taxpayer.TaxableIncome(IncomeKind.Employment));
        Assert.Equal(M(6_000m), taxpayer.TaxableIncome(IncomeKind.SelfEmployment));
        Assert.Equal(M(160m), taxpayer.TaxableIncome(IncomeKind.Investment));
    }

    [Fact]
    public void The_taxable_income_of_several_kinds_adds_them_and_a_loss_is_not_set_against_other_income() {
        Taxpayer taxpayer = TaxpayerEarning(Salary(20_000m), Business(1_000m, 5_000m), Interest(100m));

        Assert.Equal(M(20_000m), taxpayer.TaxableIncome(IncomeKind.Employment | IncomeKind.SelfEmployment));
        Assert.Equal(M(20_100m), taxpayer.TaxableIncome(IncomeKind.All));
    }

    [Fact]
    public void The_taxable_income_of_a_kind_the_taxpayer_does_not_earn_is_zero() {
        Taxpayer taxpayer = TaxpayerEarning(Salary(20_000m));

        Assert.True(taxpayer.TaxableIncome(IncomeKind.Investment).IsZero);
    }

    [Fact]
    public void The_taxable_income_of_no_kind_or_an_unknown_kind_is_refused() {
        Taxpayer taxpayer = TaxpayerEarning(Salary(1m));

        Assert.Throws<ArgumentOutOfRangeException>(() => taxpayer.TaxableIncome(IncomeKind.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => taxpayer.TaxableIncome((IncomeKind)16));
    }

    [Fact]
    public void A_taxpayer_is_described_by_its_number_and_residence() {
        Taxpayer taxpayer = AnyTaxpayer().WithId(new TaxpayerId("123456782")).WithResidence(Jurisdiction("FR")).Generate();

        Assert.Equal("Taxpayer 123456782 (FR)", taxpayer.ToString());
    }

}
