using JustDummies;

using Xunit;

namespace TaxRules.Domain.Tests;

public sealed class TaxScheduleTests {

    private static TaxSchedule French2024() {
        return new AnyTaxSchedule().WithJurisdiction(new JurisdictionCode("FR")).WithYear(2024).Generate();
    }

    private static TaxScheduleException Refusal(Action action) {
        return Assert.Throws<TaxScheduleException>(action);
    }

    private static FlatRateRule Flat(string code, ICondition? condition = null) {
        return Rules.Flat(code, "0.1", condition);
    }

    // ---- construction

    [Fact]
    public void A_new_schedule_is_a_draft_without_rules() {
        // Arrange
        JurisdictionCode anyJurisdiction = new AnyJurisdictionCode().Generate();
        int              anyYear         = Any.Int32().Between(TaxSchedule.FirstYear, TaxSchedule.LastYear).Generate();

        // Act
        TaxSchedule schedule = new TaxSchedule(anyJurisdiction, anyYear);

        // Assert
        Assert.Same(anyJurisdiction, schedule.Jurisdiction);
        Assert.Equal(anyYear, schedule.Year);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
        Assert.Empty(schedule.Rules);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2099)]
    public void The_first_and_last_years_are_accepted(int year) {
        Assert.Equal(year, new AnyTaxSchedule().WithYear(year).Generate().Year);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2100)]
    [InlineData(0)]
    [InlineData(-2024)]
    public void Other_years_are_refused(int year) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyTaxSchedule().WithYear(year).Generate());
    }

    [Fact]
    public void A_null_jurisdiction_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new TaxSchedule(null!, 2024));
    }

    // ---- AddRule / RemoveRule / Find

    [Fact]
    public void Rules_are_kept_in_the_order_they_were_added() {
        // Arrange
        TaxSchedule schedule = French2024();
        FlatRateRule a = Flat("RULE_A");
        FlatRateRule b = Flat("RULE_B");
        FlatRateRule c = Flat("RULE_C");

        // Act
        schedule.AddRule(b);
        schedule.AddRule(a);
        schedule.AddRule(c);

        // Assert
        Assert.Equal([b, a, c], schedule.Rules);
    }

    [Fact]
    public void A_null_rule_is_refused() {
        Assert.Throws<ArgumentNullException>(() => French2024().AddRule(null!));
    }

    [Fact]
    public void A_rule_whose_code_is_already_a_top_level_code_is_refused() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));

        TaxScheduleException refusal = Refusal(() => schedule.AddRule(Flat("RULE_A")));

        Assert.Equal(ScheduleRefusal.DuplicateRuleCode, refusal.Reason);
        Assert.Contains("RULE_A", refusal.Message);
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void A_new_rule_cannot_reuse_a_code_nested_in_an_existing_tree() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(new CompositeRule(Lit.C("SOCIAL"), "Social", [Flat("PENSION"), Flat("HEALTH")]));

        TaxScheduleException refusal = Refusal(() => schedule.AddRule(Flat("HEALTH")));

        Assert.Equal(ScheduleRefusal.DuplicateRuleCode, refusal.Reason);
    }

    [Fact]
    public void A_new_tree_cannot_contain_a_code_already_used_at_top_level() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("PENSION"));

        TaxScheduleException refusal = Refusal(() => schedule.AddRule(new CompositeRule(Lit.C("SOCIAL"), "Social", [Flat("PENSION"), Flat("HEALTH")])));

        Assert.Equal(ScheduleRefusal.DuplicateRuleCode, refusal.Reason);
        Assert.Contains("PENSION", refusal.Message);
    }

    [Fact]
    public void Two_trees_cannot_share_a_nested_code() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(new CompositeRule(Lit.C("SOCIAL_A"), "Social A", [Flat("PENSION"), Flat("HEALTH")]));

        TaxScheduleException refusal = Refusal(() => schedule.AddRule(new CompositeRule(Lit.C("SOCIAL_B"), "Social B", [Flat("UNEMP"), Flat("HEALTH")])));

        Assert.Equal(ScheduleRefusal.DuplicateRuleCode, refusal.Reason);
    }

    [Fact]
    public void Distinct_generated_codes_never_clash() {
        TaxSchedule schedule = French2024();
        IReadOnlyList<RuleCode> codes = Dummies.DistinctCodes(10);

        foreach (RuleCode code in codes) {
            schedule.AddRule(new AnyFlatRateRule().WithCode(code).Generate());
        }

        Assert.Equal(10, schedule.Rules.Count);
    }

    [Fact]
    public void Sixty_four_top_level_rules_are_accepted_and_the_sixty_fifth_is_refused() {
        // Arrange
        TaxSchedule schedule = French2024();
        IReadOnlyList<RuleCode> codes = Dummies.DistinctCodes(TaxSchedule.MaxRules + 1);
        for (int i = 0; i < TaxSchedule.MaxRules; i++) {
            schedule.AddRule(new AnyFlatRateRule().WithCode(codes[i]).Generate());
        }

        // Act
        TaxScheduleException refusal = Refusal(() => schedule.AddRule(new AnyFlatRateRule().WithCode(codes[TaxSchedule.MaxRules]).Generate()));

        // Assert
        Assert.Equal(ScheduleRefusal.TooManyRules, refusal.Reason);
        Assert.Equal(TaxSchedule.MaxRules, schedule.Rules.Count);
    }

    [Fact]
    public void The_limit_counts_top_level_rules_not_nested_ones() {
        // Arrange
        TaxSchedule schedule = French2024();
        IReadOnlyList<RuleCode> codes = Dummies.DistinctCodes(64 + 17);
        List<TaxRule> components = codes.Skip(1).Take(16).Select(c => (TaxRule)new AnyFlatRateRule().WithCode(c).Generate()).ToList();
        schedule.AddRule(new CompositeRule(codes[0], "Big", components));

        // Act
        for (int i = 17; i < 17 + 63; i++) {
            schedule.AddRule(new AnyFlatRateRule().WithCode(codes[i]).Generate());
        }

        // Assert
        Assert.Equal(64, schedule.Rules.Count);
    }

    [Fact]
    public void Too_many_rules_is_reported_before_a_duplicate_code() {
        // Arrange
        TaxSchedule schedule = French2024();
        IReadOnlyList<RuleCode> codes = Dummies.DistinctCodes(TaxSchedule.MaxRules);
        foreach (RuleCode code in codes) {
            schedule.AddRule(new AnyFlatRateRule().WithCode(code).Generate());
        }

        // Act
        TaxScheduleException refusal = Refusal(() => schedule.AddRule(new AnyFlatRateRule().WithCode(codes[0]).Generate()));

        // Assert
        Assert.Equal(ScheduleRefusal.TooManyRules, refusal.Reason);
    }

    [Fact]
    public void A_top_level_rule_is_removed_with_its_whole_tree_and_its_codes_become_free_again() {
        // Arrange
        TaxSchedule schedule = French2024();
        schedule.AddRule(new CompositeRule(Lit.C("SOCIAL"), "Social", [Flat("PENSION"), Flat("HEALTH")]));
        schedule.AddRule(Flat("OTHER"));

        // Act
        schedule.RemoveRule(Lit.C("SOCIAL"));

        // Assert
        Assert.Equal(["OTHER"], schedule.Rules.Select(r => r.Code.Value).ToList());
        Assert.Null(schedule.Find(Lit.C("PENSION")));
        schedule.AddRule(Flat("PENSION"));
        Assert.Equal(2, schedule.Rules.Count);
    }

    [Fact]
    public void A_nested_rule_cannot_be_removed_alone() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(new CompositeRule(Lit.C("SOCIAL"), "Social", [Flat("PENSION"), Flat("HEALTH")]));

        TaxScheduleException refusal = Refusal(() => schedule.RemoveRule(Lit.C("PENSION")));

        Assert.Equal(ScheduleRefusal.NestedRule, refusal.Reason);
        Assert.Contains("SOCIAL", refusal.Message);
        Assert.NotNull(schedule.Find(Lit.C("PENSION")));
    }

    [Fact]
    public void An_unknown_rule_cannot_be_removed() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));

        TaxScheduleException refusal = Refusal(() => schedule.RemoveRule(Lit.C("RULE_Z")));

        Assert.Equal(ScheduleRefusal.UnknownRule, refusal.Reason);
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void Removal_refuses_a_null_code() {
        Assert.Throws<ArgumentNullException>(() => French2024().RemoveRule(null!));
    }

    [Fact]
    public void Find_reaches_top_level_and_nested_rules() {
        // Arrange
        TaxSchedule schedule = French2024();
        FlatRateRule pension = Flat("PENSION");
        CompositeRule social = new CompositeRule(Lit.C("SOCIAL"), "Social", [pension, Flat("HEALTH")]);
        CappedRule capped = new CappedRule(Lit.C("CAPPED"), "Capped", Flat("INNER_TAX"), Lit.M("100"));
        schedule.AddRule(social);
        schedule.AddRule(capped);

        // Act / Assert
        Assert.Same(social, schedule.Find(Lit.C("SOCIAL")));
        Assert.Same(pension, schedule.Find(Lit.C("PENSION")));
        Assert.Same(capped.Inner, schedule.Find(Lit.C("INNER_TAX")));
        Assert.Null(schedule.Find(Lit.C("MISSING")));
        Assert.Throws<ArgumentNullException>(() => schedule.Find(null!));
    }

    // ---- Validate / Reopen

    [Fact]
    public void An_empty_schedule_cannot_be_validated() {
        TaxSchedule schedule = French2024();

        TaxScheduleException refusal = Refusal(() => schedule.Validate());

        Assert.Equal(ScheduleRefusal.NoRules, refusal.Reason);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
    }

    [Fact]
    public void A_schedule_of_credits_alone_cannot_be_validated() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Rules.Credit("FAMILY", "100", "10"));
        schedule.AddRule(Rules.Credit("BASIC", "50", "0"));

        TaxScheduleException refusal = Refusal(() => schedule.Validate());

        Assert.Equal(ScheduleRefusal.NoTaxRule, refusal.Reason);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
    }

    [Fact]
    public void One_tax_rule_among_credits_is_enough() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Rules.Credit("FAMILY", "100", "10"));
        schedule.AddRule(Flat("INCOME_TAX"));

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void A_validated_schedule_refuses_every_change() {
        // Arrange
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();

        // Act / Assert
        Assert.Equal(ScheduleRefusal.AlreadyValidated, Refusal(() => schedule.AddRule(Flat("RULE_B"))).Reason);
        Assert.Equal(ScheduleRefusal.AlreadyValidated, Refusal(() => schedule.RemoveRule(Lit.C("RULE_A"))).Reason);
        Assert.Equal(ScheduleRefusal.AlreadyValidated, Refusal(() => schedule.RemoveRule(Lit.C("RULE_Z"))).Reason);
        Assert.Equal(ScheduleRefusal.AlreadyValidated, Refusal(() => schedule.Validate()).Reason);
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void A_validated_schedule_reports_already_validated_before_a_duplicate_code() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();

        TaxScheduleException refusal = Refusal(() => schedule.AddRule(Flat("RULE_A")));

        Assert.Equal(ScheduleRefusal.AlreadyValidated, refusal.Reason);
    }

    [Fact]
    public void A_residency_condition_on_another_country_is_refused() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A", new ResidentOfCondition(new JurisdictionCode("DE"))));

        TaxScheduleException refusal = Refusal(() => schedule.Validate());

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, refusal.Reason);
        Assert.Contains("RULE_A", refusal.Message);
        Assert.Contains("DE", refusal.Message);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
    }

    [Fact]
    public void A_residency_condition_on_the_own_country_or_one_of_its_subdivisions_is_accepted() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A", new ResidentOfCondition(new JurisdictionCode("FR"))));
        schedule.AddRule(Flat("RULE_B", new ResidentOfCondition(new JurisdictionCode("FR-75"))));

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void A_subdivision_schedule_accepts_residency_in_a_sibling_subdivision_of_the_same_country() {
        TaxSchedule schedule = new AnyTaxSchedule().WithJurisdiction(new JurisdictionCode("US-CA")).WithYear(2024).Generate();
        schedule.AddRule(Flat("RULE_A", new ResidentOfCondition(new JurisdictionCode("US-NY"))));

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void A_foreign_residency_is_found_wherever_the_condition_is_nested() {
        // Arrange
        ICondition foreign = new ResidentOfCondition(new JurisdictionCode("DE"));
        ICondition[] conditions = [
            new AndCondition([new AgeAtLeastCondition(18), foreign]),
            new OrCondition([foreign, new AgeAtLeastCondition(18)]),
            new NotCondition(foreign),
            new AndCondition([new AgeAtLeastCondition(18), new OrCondition([new AgeAtLeastCondition(65), new NotCondition(foreign)])]),
        ];

        foreach (ICondition condition in conditions) {
            TaxSchedule schedule = French2024();
            schedule.AddRule(Flat("RULE_A", condition));

            // Act
            TaxScheduleException refusal = Refusal(() => schedule.Validate());

            // Assert
            Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, refusal.Reason);
        }
    }

    [Fact]
    public void A_foreign_residency_is_found_in_the_condition_of_a_nested_rule() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(new CompositeRule(Lit.C("SOCIAL"), "Social", [Flat("PENSION"), Flat("HEALTH", new ResidentOfCondition(new JurisdictionCode("BE")))]));

        TaxScheduleException refusal = Refusal(() => schedule.Validate());

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, refusal.Reason);
        Assert.Contains("HEALTH", refusal.Message);
    }

    [Fact]
    public void A_foreign_residency_is_found_in_the_condition_of_a_credit() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("INCOME_TAX"));
        schedule.AddRule(Rules.Credit("FAMILY", "100", "0", new ResidentOfCondition(new JurisdictionCode("IT"))));

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, Refusal(() => schedule.Validate()).Reason);
    }

    [Fact]
    public void A_foreign_residency_is_found_in_the_condition_of_a_cap_but_not_looked_for_in_the_wrapped_rule_twice() {
        TaxSchedule schedule = French2024();
        CappedRule capped = new CappedRule(Lit.C("CAPPED"), "Capped", Flat("INNER_TAX"), Lit.M("100"), null, new ResidentOfCondition(new JurisdictionCode("ES")));
        schedule.AddRule(capped);

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, Refusal(() => schedule.Validate()).Reason);
    }

    [Fact]
    public void A_refused_validation_leaves_the_schedule_amendable() {
        // Arrange
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A", new ResidentOfCondition(new JurisdictionCode("DE"))));
        Refusal(() => schedule.Validate());

        // Act
        schedule.RemoveRule(Lit.C("RULE_A"));
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();

        // Assert
        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void Reopening_a_draft_is_refused() {
        TaxSchedule schedule = French2024();

        TaxScheduleException refusal = Refusal(() => schedule.Reopen());

        Assert.Equal(ScheduleRefusal.NotValidated, refusal.Reason);
    }

    [Fact]
    public void A_reopened_schedule_can_be_amended_and_validated_again() {
        // Arrange
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();

        // Act
        schedule.Reopen();
        schedule.AddRule(Flat("RULE_B"));
        schedule.Validate();

        // Assert
        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
        Assert.Equal(2, schedule.Rules.Count);
    }

    [Fact]
    public void A_reopened_schedule_cannot_be_evaluated_until_it_is_validated_again() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();
        schedule.Reopen();

        TaxScheduleException refusal = Refusal(() => schedule.Evaluate(Rules.Earning("100")));

        Assert.Equal(ScheduleRefusal.NotValidated, refusal.Reason);
    }

    // ---- Evaluate

    [Fact]
    public void A_draft_cannot_evaluate_a_taxpayer() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));

        TaxScheduleException refusal = Refusal(() => schedule.Evaluate(Rules.Earning("100")));

        Assert.Equal(ScheduleRefusal.NotValidated, refusal.Reason);
    }

    [Fact]
    public void Evaluation_refuses_a_null_taxpayer() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();

        Assert.Throws<ArgumentNullException>(() => schedule.Evaluate(null!));
    }

    [Fact]
    public void A_taxpayer_born_after_the_schedule_year_is_refused() {
        // Arrange
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();
        Taxpayer newborn = new AnyTaxpayer().WithDateOfBirth(new DateOnly(2025, 1, 1)).Generate();

        // Act
        TaxScheduleException refusal = Refusal(() => schedule.Evaluate(newborn));

        // Assert
        Assert.Equal(ScheduleRefusal.TaxpayerNotBorn, refusal.Reason);
        Assert.Contains(newborn.Id.Value, refusal.Message);
    }

    [Fact]
    public void A_taxpayer_born_during_the_schedule_year_is_evaluated() {
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();
        Taxpayer newborn = new AnyTaxpayer().WithDateOfBirth(new DateOnly(2024, 12, 31)).WithIncomes([]).Generate();

        TaxBreakdown breakdown = schedule.Evaluate(newborn);

        Assert.True(breakdown.NetTax.IsZero);
    }

    [Fact]
    public void Evaluation_gives_one_line_per_top_level_rule_in_order() {
        // Arrange
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_B"));
        schedule.AddRule(Rules.Credit("FAMILY", "10", "0"));
        schedule.AddRule(new CompositeRule(Lit.C("SOCIAL"), "Social", [Flat("PENSION"), Flat("HEALTH")]));
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();
        Taxpayer taxpayer = new AnyTaxpayer().Generate();

        // Act
        TaxBreakdown breakdown = schedule.Evaluate(taxpayer);

        // Assert
        Assert.Equal(["RULE_B", "FAMILY", "SOCIAL", "RULE_A"], breakdown.Lines.Select(l => l.Code.Value).ToList());
        Assert.Same(taxpayer.Id, breakdown.TaxpayerId);
        Assert.Same(schedule.Jurisdiction, breakdown.Jurisdiction);
        Assert.Equal(2024, breakdown.Year);
    }

    [Fact]
    public void The_breakdown_income_is_the_taxable_income_of_every_kind_whatever_the_rules_tax() {
        // Arrange
        TaxSchedule schedule = French2024();
        schedule.AddRule(Flat("RULE_A"));
        schedule.Validate();
        Taxpayer taxpayer = Rules.With(
            new EmploymentIncome("ACME", Lit.M("1000"), Lit.M("0")),
            new SelfEmploymentIncome("shop", Lit.M("500"), Lit.M("0")),
            new InvestmentIncome("fund", Lit.M("100"), InvestmentKind.Dividend));

        // Act
        TaxBreakdown breakdown = schedule.Evaluate(taxpayer);

        // Assert
        Assert.Equal(Lit.M("1560"), breakdown.TaxableIncome);
    }

    [Fact]
    public void A_whole_schedule_computes_a_realistic_breakdown() {
        // Arrange: income tax on a scale, a capped solidarity levy, a flat levy on investments for over-sixties only,
        // a family credit, and a rule that tests residence in a subdivision.
        TaxSchedule schedule = French2024();
        schedule.AddRule(Rules.Scale("INCOME_TAX",
                                     Rules.Bracket("0", "10000", "0"),
                                     Rules.Bracket("10000", "40000", "0.11"),
                                     Rules.Bracket("40000", null, "0.30")));
        schedule.AddRule(new CappedRule(Lit.C("SOLIDARITY"), "Solidarity levy",
                                        Rules.Flat("SOLIDARITY_BASE", "0.05", taxBase: IncomeKind.All), Lit.M("2000"), Lit.M("100")));
        schedule.AddRule(Rules.Flat("INVEST_LEVY", "0.17", new AgeAtLeastCondition(60), IncomeKind.Investment));
        schedule.AddRule(Rules.Credit("FAMILY", "200", "150", new HouseholdSizeAtLeastCondition(2)));
        schedule.AddRule(Rules.Flat("PARIS_TAX", "0.01", new ResidentOfCondition(new JurisdictionCode("FR-75"))));
        schedule.Validate();

        Taxpayer taxpayer = new AnyTaxpayer()
            .WithDateOfBirth(new DateOnly(1960, 5, 5))
            .WithResidence(new JurisdictionCode("FR-75"))
            .WithHouseholdSize(3)
            .WithIncomes([
                new EmploymentIncome("ACME", Lit.M("60000"), Lit.M("6000")),
                new SelfEmploymentIncome("consulting", Lit.M("20000"), Lit.M("5000")),
                new InvestmentIncome("fund", Lit.M("5000"), InvestmentKind.Dividend)
            ]).Generate();

        // Act
        TaxBreakdown breakdown = schedule.Evaluate(taxpayer);

        // Assert: employment 54 000, self-employment 15 000, dividends 3 000 taxable -> 72 000 in all.
        // INCOME_TAX taxes employment only: 30 000 x 11 % + 14 000 x 30 % = 3 300 + 4 200.
        Assert.Equal(Lit.M("72000"), breakdown.TaxableIncome);
        Assert.Equal(Lit.M("7500"), breakdown.Find(Lit.C("INCOME_TAX"))!.Amount);
        Assert.Equal(Lit.M("2000"), breakdown.Find(Lit.C("SOLIDARITY"))!.Amount);
        Assert.Equal(LineOutcome.CappedAtMaximum, breakdown.Find(Lit.C("SOLIDARITY"))!.Outcome);
        Assert.Equal(Lit.M("3600"), breakdown.Find(Lit.C("SOLIDARITY_BASE"))!.Amount);
        Assert.Equal(Lit.M("510"), breakdown.Find(Lit.C("INVEST_LEVY"))!.Amount);
        Assert.Equal(Lit.M("500"), breakdown.Find(Lit.C("FAMILY"))!.Amount);
        Assert.Equal(Lit.M("540"), breakdown.Find(Lit.C("PARIS_TAX"))!.Amount);
        Assert.Equal(Lit.M("10550"), breakdown.GrossTax);
        Assert.Equal(Lit.M("500"), breakdown.CreditsApplied);
        Assert.Equal(Lit.M("10050"), breakdown.NetTax);
        Assert.Equal(0.1396m, breakdown.EffectiveRate);
    }

}
