using JustDummies;

using TaxRules.Domain;

namespace TaxRules.Tests.Discovery;

public sealed class TaxpayerTests {

    [Fact]
    public void A_taxpayer_keeps_what_it_was_built_with() {
        // Arrange
        TaxpayerId       id        = new TaxpayerId("123456782");
        JurisdictionCode residence = new JurisdictionCode("FR-IDF");
        DateOnly         birth     = new DateOnly(1985, 7, 14);
        IncomeComponent  salary    = Fixtures.Salary(1_000m);

        // Act
        Taxpayer taxpayer = new Taxpayer(id, birth, residence, 3, [salary]);

        // Assert
        Assert.Same(id, taxpayer.Id);
        Assert.Equal(birth, taxpayer.DateOfBirth);
        Assert.Same(residence, taxpayer.Residence);
        Assert.Equal(3, taxpayer.HouseholdSize);
        Assert.Equal([salary], taxpayer.Incomes);
        Assert.Equal("Taxpayer 123456782 (FR-IDF)", taxpayer.ToString());
    }

    [Fact]
    public void The_earliest_date_of_birth_is_the_first_of_january_1900() {
        DateOnly anyDate = new DateOnly(1900, 1, 1);

        Assert.Equal(anyDate, Taxpayer.EarliestDateOfBirth);
        Assert.Equal(anyDate, Any.Taxpayer().WithDateOfBirth(anyDate).Generate().DateOfBirth);
    }

    [Fact]
    public void A_date_of_birth_before_1900_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Taxpayer().WithDateOfBirth(new DateOnly(1899, 12, 31)).Generate());
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(20, true)]
    [InlineData(0, false)]
    [InlineData(-3, false)]
    [InlineData(21, false)]
    public void A_household_counts_one_to_twenty_members(int members, bool accepted) {
        if (accepted) {
            Assert.Equal(members, Any.Taxpayer().WithHouseholdSize(members).Generate().HouseholdSize);
        } else {
            Assert.Throws<ArgumentOutOfRangeException>(() => Any.Taxpayer().WithHouseholdSize(members).Generate());
        }
    }

    [Fact]
    public void Thirty_income_components_are_the_most_a_taxpayer_may_declare() {
        IncomeComponent[] thirty = Enumerable.Range(0, 30).Select(_ => (IncomeComponent)Fixtures.Salary(1m)).ToArray();
        IncomeComponent[] thirtyOne = Enumerable.Range(0, 31).Select(_ => (IncomeComponent)Fixtures.Salary(1m)).ToArray();

        Assert.Equal(30, Any.Taxpayer().WithIncomes(thirty).Generate().Incomes.Count);
        Assert.Throws<ArgumentException>(() => Any.Taxpayer().WithIncomes(thirtyOne).Generate());
    }

    [Fact]
    public void A_null_income_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.Taxpayer().WithIncomes(new IncomeComponent[] { Fixtures.Salary(1m), null! }).Generate());
    }

    [Fact]
    public void A_missing_identity_residence_or_income_list_is_refused() {
        TaxpayerId       anyId        = Any.TaxpayerId().Generate();
        JurisdictionCode anyResidence = Any.JurisdictionCode().Generate();

        Assert.Throws<ArgumentNullException>(() => new Taxpayer(null!, new DateOnly(1990, 1, 1), anyResidence, 1, []));
        Assert.Throws<ArgumentNullException>(() => new Taxpayer(anyId, new DateOnly(1990, 1, 1), null!, 1, []));
        Assert.Throws<ArgumentNullException>(() => new Taxpayer(anyId, new DateOnly(1990, 1, 1), anyResidence, 1, null!));
    }

    [Fact]
    public void A_taxpayer_may_have_no_income() {
        Taxpayer taxpayer = Fixtures.EarningTaxpayer();

        Assert.Empty(taxpayer.Incomes);
        Assert.True(taxpayer.TaxableIncome(IncomeKind.All).IsZero);
    }

    [Fact]
    public void The_incomes_are_a_copy_of_the_list_that_was_given() {
        List<IncomeComponent> incomes = [Fixtures.Salary(1m)];
        Taxpayer              taxpayer = Any.Taxpayer().WithIncomes(incomes).Generate();

        incomes.Add(Fixtures.Salary(2m));

        Assert.Single(taxpayer.Incomes);
    }

    [Theory]
    [InlineData(1985, 2024, 39)]
    [InlineData(1985, 1985, 0)]
    [InlineData(1900, 2099, 199)]
    public void Age_is_the_difference_of_the_years(int birthYear, int taxYear, int expected) {
        Taxpayer taxpayer = Any.Taxpayer().WithDateOfBirth(new DateOnly(birthYear, 12, 31)).Generate();

        Assert.Equal(expected, taxpayer.AgeAtEndOf(taxYear));
    }

    [Fact]
    public void Age_in_a_year_before_the_birth_year_is_refused() {
        Taxpayer taxpayer = Any.Taxpayer().WithDateOfBirth(new DateOnly(1985, 1, 1)).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => taxpayer.AgeAtEndOf(1984));
    }

    [Fact]
    public void Taxable_income_sums_the_taxable_amounts_of_the_kinds_asked_for() {
        // Arrange
        Taxpayer taxpayer = Fixtures.EarningTaxpayer(
            new EmploymentIncome(Fixtures.AnySource(), new Money(50_000m), new Money(2_000m)),
            new EmploymentIncome(Fixtures.AnySource(), new Money(10_000m), Money.Zero),
            new SelfEmploymentIncome(Fixtures.AnySource(), new Money(8_000m), new Money(3_000m)),
            new SelfEmploymentIncome(Fixtures.AnySource(), new Money(1_000m), new Money(4_000m)),
            Fixtures.Interest(500m),
            Fixtures.Dividend(1_000m));

        // Act and assert
        Assert.Equal(new Money(58_000m), taxpayer.TaxableIncome(IncomeKind.Employment));
        Assert.Equal(new Money(5_000m), taxpayer.TaxableIncome(IncomeKind.SelfEmployment));
        Assert.Equal(new Money(1_100m), taxpayer.TaxableIncome(IncomeKind.Investment));
        Assert.Equal(new Money(63_000m), taxpayer.TaxableIncome(IncomeKind.Employment | IncomeKind.SelfEmployment));
        Assert.Equal(new Money(64_100m), taxpayer.TaxableIncome(IncomeKind.All));
    }

    [Fact]
    public void A_loss_is_not_set_against_other_income() {
        Taxpayer taxpayer = Fixtures.EarningTaxpayer(Fixtures.Salary(1_000m), new SelfEmploymentIncome(Fixtures.AnySource(), new Money(100m), new Money(5_000m)));

        Assert.Equal(new Money(1_000m), taxpayer.TaxableIncome(IncomeKind.All));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void Taxable_income_of_no_kind_or_an_unknown_kind_is_refused(int kinds) {
        Taxpayer anyTaxpayer = Any.Taxpayer().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyTaxpayer.TaxableIncome((IncomeKind)kinds));
    }

    [Fact]
    public void Income_adding_up_beyond_the_maximum_amount_is_refused() {
        Taxpayer taxpayer = Fixtures.EarningTaxpayer(
            new EmploymentIncome(Fixtures.AnySource(), new Money(Money.MaxAmount), Money.Zero),
            new EmploymentIncome(Fixtures.AnySource(), new Money(0.01m), Money.Zero));

        Assert.Throws<ArgumentOutOfRangeException>(() => taxpayer.TaxableIncome(IncomeKind.All));
    }

    [Fact]
    public void A_generated_taxpayer_is_always_constructible() {
        for (int i = 0; i < 100; i++) {
            Taxpayer anyTaxpayer = Any.Taxpayer().Generate();

            Assert.True(anyTaxpayer.DateOfBirth.Year <= 2000);
        }
    }

}

public sealed class BreakdownLineTests {

    private static BreakdownLine Line(
        string code, LineOutcome outcome = LineOutcome.Applied, RuleEffect effect = RuleEffect.Tax,
        decimal amount = 10m, decimal? taxableBase = null, IEnumerable<BreakdownLine>? children = null) {
        return new BreakdownLine(
            Fixtures.Code(code), AnyLabel.Create().Generate(), effect, outcome,
            taxableBase is null ? null : new Money((decimal)taxableBase), new Money(amount), children);
    }

    [Fact]
    public void A_line_keeps_what_it_was_built_with() {
        BreakdownLine child = Line("CHILD_RULE");

        BreakdownLine line = new BreakdownLine(Fixtures.Code("PARENT_RULE"), "Parent", RuleEffect.Tax, LineOutcome.CappedAtMaximum, new Money(5m), new Money(7m), [child]);

        Assert.Equal(Fixtures.Code("PARENT_RULE"), line.Code);
        Assert.Equal("Parent", line.Label);
        Assert.Equal(RuleEffect.Tax, line.Effect);
        Assert.Equal(LineOutcome.CappedAtMaximum, line.Outcome);
        Assert.Equal(new Money(5m), line.TaxableBase);
        Assert.Equal(new Money(7m), line.Amount);
        Assert.Equal([child], line.Children);
        Assert.Equal("PARENT_RULE CappedAtMaximum: 7.00", line.ToString());
    }

    [Fact]
    public void A_line_without_children_has_an_empty_list() {
        Assert.Empty(Line("LEAF_RULE").Children);
    }

    [Fact]
    public void A_line_that_does_not_apply_may_carry_nothing() {
        BreakdownLine line = Line("SKIPPED_RULE", LineOutcome.NotApplicable, amount: 0m);

        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void A_line_that_does_not_apply_cannot_carry_an_amount() {
        Assert.Throws<ArgumentException>(() => Line("SKIPPED_RULE", LineOutcome.NotApplicable, amount: 0.01m));
    }

    [Fact]
    public void A_line_that_does_not_apply_cannot_carry_a_base() {
        Assert.Throws<ArgumentException>(() => Line("SKIPPED_RULE", LineOutcome.NotApplicable, amount: 0m, taxableBase: 100m));
    }

    [Fact]
    public void A_line_that_does_not_apply_cannot_carry_children() {
        Assert.Throws<ArgumentException>(() => Line("SKIPPED_RULE", LineOutcome.NotApplicable, amount: 0m, children: [Line("CHILD_RULE")]));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(-1)]
    public void An_undefined_effect_is_refused(int effect) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Line("SOME_RULE", effect: (RuleEffect)effect));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    public void An_undefined_outcome_is_refused(int outcome) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Line("SOME_RULE", outcome: (LineOutcome)outcome));
    }

    [Fact]
    public void A_null_child_is_refused() {
        Assert.Throws<ArgumentException>(() => Line("SOME_RULE", children: [Line("CHILD_RULE"), null!]));
    }

    [Fact]
    public void A_missing_code_or_amount_is_refused() {
        string anyLabel = AnyLabel.Create().Generate();

        Assert.Throws<ArgumentNullException>(() => new BreakdownLine(null!, anyLabel, RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero));
        Assert.Throws<ArgumentNullException>(() => new BreakdownLine(Any.RuleCode().Generate(), anyLabel, RuleEffect.Tax, LineOutcome.Applied, null, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_label_is_refused(string label) {
        Assert.Throws<ArgumentException>(() => new BreakdownLine(Any.RuleCode().Generate(), label, RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero));
    }

    [Fact]
    public void A_label_longer_than_eighty_characters_is_refused() {
        RuleCode anyCode = Any.RuleCode().Generate();

        Assert.Equal(80, new BreakdownLine(anyCode, new string('l', 80), RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero).Label.Length);
        Assert.Throws<ArgumentException>(() => new BreakdownLine(anyCode, new string('l', 81), RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero));
    }

    [Fact]
    public void Find_returns_the_line_itself_or_a_nested_one() {
        BreakdownLine grandchild = Line("GRAND_RULE");
        BreakdownLine child      = Line("CHILD_RULE", children: [grandchild]);
        BreakdownLine root       = Line("ROOT_RULE", children: [child, Line("OTHER_RULE")]);

        Assert.Same(root, root.Find(Fixtures.Code("ROOT_RULE")));
        Assert.Same(child, root.Find(Fixtures.Code("CHILD_RULE")));
        Assert.Same(grandchild, root.Find(Fixtures.Code("GRAND_RULE")));
        Assert.Null(root.Find(Fixtures.Code("ABSENT_RULE")));
    }

    [Fact]
    public void Find_refuses_a_missing_code() {
        BreakdownLine anyLine = Line("SOME_RULE");

        Assert.Throws<ArgumentNullException>(() => anyLine.Find(null!));
    }

}

public sealed class TaxBreakdownTests {

    private static BreakdownLine Tax(string code, decimal amount, LineOutcome outcome = LineOutcome.Applied) {
        return new BreakdownLine(Fixtures.Code(code), AnyLabel.Create().Generate(), RuleEffect.Tax, outcome, null, new Money(amount));
    }

    private static BreakdownLine Credit(string code, decimal amount) {
        return new BreakdownLine(Fixtures.Code(code), AnyLabel.Create().Generate(), RuleEffect.Credit, LineOutcome.Applied, null, new Money(amount));
    }

    private static TaxBreakdown Breakdown(decimal income, params BreakdownLine[] lines) {
        return new TaxBreakdown(Any.TaxpayerId().Generate(), Any.JurisdictionCode().Generate(), 2024, new Money(income), lines);
    }

    [Fact]
    public void The_breakdown_keeps_who_where_and_when() {
        TaxpayerId       id           = new TaxpayerId("123456782");
        JurisdictionCode jurisdiction = new JurisdictionCode("FR");

        TaxBreakdown breakdown = new TaxBreakdown(id, jurisdiction, 2024, new Money(1_000m), [Tax("INCOME_TAX", 10m)]);

        Assert.Same(id, breakdown.TaxpayerId);
        Assert.Same(jurisdiction, breakdown.Jurisdiction);
        Assert.Equal(2024, breakdown.Year);
        Assert.Equal(new Money(1_000m), breakdown.TaxableIncome);
    }

    [Fact]
    public void Gross_tax_is_the_sum_of_the_tax_lines_and_credits_are_the_sum_of_the_credit_lines() {
        TaxBreakdown breakdown = Breakdown(10_000m, Tax("FIRST_TAX", 300m), Credit("FIRST_CREDIT", 50m), Tax("SECOND_TAX", 200m), Credit("SECOND_CREDIT", 25m));

        Assert.Equal(new Money(500m), breakdown.GrossTax);
        Assert.Equal(new Money(75m), breakdown.CreditsAvailable);
    }

    [Fact]
    public void Credits_are_set_against_the_tax() {
        TaxBreakdown breakdown = Breakdown(10_000m, Tax("INCOME_TAX", 500m), Credit("FAMILY_CREDIT", 120m));

        Assert.Equal(new Money(120m), breakdown.CreditsApplied);
        Assert.Equal(new Money(380m), breakdown.NetTax);
    }

    [Fact]
    public void Credits_above_the_tax_are_not_refunded() {
        TaxBreakdown breakdown = Breakdown(10_000m, Tax("INCOME_TAX", 100m), Credit("FAMILY_CREDIT", 400m));

        Assert.Equal(new Money(400m), breakdown.CreditsAvailable);
        Assert.Equal(new Money(100m), breakdown.CreditsApplied);
        Assert.True(breakdown.NetTax.IsZero);
    }

    [Fact]
    public void Credits_equal_to_the_tax_leave_nothing_to_pay() {
        TaxBreakdown breakdown = Breakdown(10_000m, Tax("INCOME_TAX", 100m), Credit("FAMILY_CREDIT", 100m));

        Assert.Equal(new Money(100m), breakdown.CreditsApplied);
        Assert.True(breakdown.NetTax.IsZero);
    }

    [Fact]
    public void Without_credits_the_net_tax_is_the_gross_tax() {
        TaxBreakdown breakdown = Breakdown(10_000m, Tax("INCOME_TAX", 100m));

        Assert.True(breakdown.CreditsAvailable.IsZero);
        Assert.True(breakdown.CreditsApplied.IsZero);
        Assert.Equal(new Money(100m), breakdown.NetTax);
    }

    [Fact]
    public void Credits_alone_apply_to_no_tax() {
        TaxBreakdown breakdown = Breakdown(10_000m, Credit("FAMILY_CREDIT", 100m));

        Assert.True(breakdown.GrossTax.IsZero);
        Assert.True(breakdown.CreditsApplied.IsZero);
        Assert.True(breakdown.NetTax.IsZero);
    }

    [Fact]
    public void A_breakdown_without_lines_owes_nothing() {
        TaxBreakdown breakdown = Breakdown(0m);

        Assert.True(breakdown.NetTax.IsZero);
        Assert.Empty(breakdown.Lines);
    }

    [Theory]
    [InlineData("1000", "100", "0.1")]
    [InlineData("3", "1", "0.3333")]
    [InlineData("3", "2", "0.6667")]
    [InlineData("100000", "1", "0.0000")]
    [InlineData("100000", "5", "0.0001")]
    public void The_effective_rate_is_net_tax_over_income_to_four_decimals(string income, string tax, string expected) {
        TaxBreakdown breakdown = Breakdown(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture), Tax("INCOME_TAX", decimal.Parse(tax, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), breakdown.EffectiveRate);
    }

    [Fact]
    public void The_effective_rate_rounds_half_away_from_zero() {
        // 1 / 20000 = 0.00005 -> 0.0001
        Assert.Equal(0.0001m, Breakdown(20_000m, Tax("INCOME_TAX", 1m)).EffectiveRate);
    }

    [Fact]
    public void The_effective_rate_is_zero_without_income() {
        Assert.Equal(0m, Breakdown(0m, Tax("INCOME_TAX", 50m)).EffectiveRate);
    }

    [Fact]
    public void The_effective_rate_uses_the_net_tax() {
        Assert.Equal(0.05m, Breakdown(1_000m, Tax("INCOME_TAX", 100m), Credit("FAMILY_CREDIT", 50m)).EffectiveRate);
    }

    [Fact]
    public void Find_returns_the_line_of_a_top_level_or_nested_rule() {
        BreakdownLine nested = Tax("NESTED_RULE", 5m);
        BreakdownLine parent = new BreakdownLine(Fixtures.Code("PARENT_RULE"), AnyLabel.Create().Generate(), RuleEffect.Tax, LineOutcome.Applied, null, new Money(5m), [nested]);
        TaxBreakdown  breakdown = Breakdown(100m, parent, Credit("SOME_CREDIT", 1m));

        Assert.Same(parent, breakdown.Find(Fixtures.Code("PARENT_RULE")));
        Assert.Same(nested, breakdown.Find(Fixtures.Code("NESTED_RULE")));
        Assert.NotNull(breakdown.Find(Fixtures.Code("SOME_CREDIT")));
        Assert.Null(breakdown.Find(Fixtures.Code("ABSENT_RULE")));
    }

    [Fact]
    public void A_not_applicable_line_adds_nothing() {
        TaxBreakdown breakdown = Breakdown(100m, Tax("INCOME_TAX", 10m), Tax("SKIPPED_TAX", 0m, LineOutcome.NotApplicable));

        Assert.Equal(new Money(10m), breakdown.GrossTax);
        Assert.Equal(2, breakdown.Lines.Count);
    }

    [Fact]
    public void A_missing_part_or_a_null_line_is_refused() {
        TaxpayerId       anyId           = Any.TaxpayerId().Generate();
        JurisdictionCode anyJurisdiction = Any.JurisdictionCode().Generate();

        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(null!, anyJurisdiction, 2024, Money.Zero, []));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(anyId, null!, 2024, Money.Zero, []));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(anyId, anyJurisdiction, 2024, null!, []));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(anyId, anyJurisdiction, 2024, Money.Zero, null!));
        Assert.Throws<ArgumentException>(() => new TaxBreakdown(anyId, anyJurisdiction, 2024, Money.Zero, [Tax("INCOME_TAX", 1m), null!]));
    }

    [Fact]
    public void The_lines_are_a_copy_of_the_list_that_was_given() {
        List<BreakdownLine> lines     = [Tax("INCOME_TAX", 10m)];
        TaxBreakdown        breakdown = new TaxBreakdown(Any.TaxpayerId().Generate(), Any.JurisdictionCode().Generate(), 2024, Money.Zero, lines);

        lines.Add(Tax("OTHER_TAX", 10m));

        Assert.Single(breakdown.Lines);
    }

}
