using JustDummies;

using TaxRules.Domain;

using Xunit;

using static TaxRules.Tests.Build;

namespace TaxRules.Tests;

public sealed class TaxScheduleTests {

    private static TaxSchedule AnyDraft() => Any.TaxSchedule().Generate();

    /// <summary>A schedule of the given country and year; the rules are the test's to add.</summary>
    private static TaxSchedule DraftFor(string jurisdiction, int year = 2024) {
        return Any.TaxSchedule().WithJurisdiction(Jurisdiction(jurisdiction)).WithYear(year).Generate();
    }

    /// <summary>A tax rule that always applies; which tax it is does not matter.</summary>
    private static FlatRateRule AnyTax() => Any.FlatRateRule().WithCondition(null).Generate();

    private static TaxCreditRule AnyCredit() => Any.TaxCreditRule().WithCondition(null).Generate();

    private static TaxSchedule ValidatedWith(params TaxRule[] rules) {
        TaxSchedule schedule = AnyDraft();
        foreach (TaxRule rule in rules) {
            schedule.AddRule(rule);
        }

        schedule.Validate();

        return schedule;
    }

    private static void AssertRefused(ScheduleRefusal reason, Action operation) {
        TaxScheduleException refusal = Assert.Throws<TaxScheduleException>(operation);

        Assert.Equal(reason, refusal.Reason);
    }

    // ---- creation -------------------------------------------------------------------------------------------

    [Fact]
    public void A_schedule_starts_as_a_draft_without_rules() {
        JurisdictionCode jurisdiction = Any.JurisdictionCode().Generate();

        TaxSchedule schedule = new TaxSchedule(jurisdiction, 2024);

        Assert.Same(jurisdiction, schedule.Jurisdiction);
        Assert.Equal(2024, schedule.Year);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
        Assert.Empty(schedule.Rules);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2099)]
    public void The_first_and_last_year_a_schedule_covers_are_accepted(int year) {
        Assert.Equal(year, Any.TaxSchedule().WithYear(year).Generate().Year);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2100)]
    [InlineData(0)]
    public void A_year_outside_the_century_is_refused(int year) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.TaxSchedule().WithYear(year).Generate());
    }

    [Fact]
    public void A_schedule_needs_a_jurisdiction() {
        Assert.Throws<ArgumentNullException>(() => Any.TaxSchedule().WithJurisdiction((JurisdictionCode)null!).Generate());
    }

    // ---- adding and removing ---------------------------------------------------------------------------------

    [Fact]
    public void Rules_are_kept_in_the_order_they_were_added() {
        TaxSchedule schedule = AnyDraft();
        FlatRateRule first = AnyTax();
        TaxCreditRule second = AnyCredit();
        FlatRateRule third = AnyTax();

        schedule.AddRule(first);
        schedule.AddRule(second);
        schedule.AddRule(third);

        Assert.Equal(new TaxRule[] { first, second, third }, schedule.Rules);
    }

    [Fact]
    public void Adding_nothing_is_refused() {
        TaxSchedule schedule = AnyDraft();

        Assert.Throws<ArgumentNullException>(() => schedule.AddRule(null!));
    }

    [Fact]
    public void A_rule_whose_code_is_already_a_top_level_code_is_refused() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(Flat(IncomeKind.All, 0.1m, "INCOME_TAX"));

        AssertRefused(ScheduleRefusal.DuplicateRuleCode, () => schedule.AddRule(Flat(IncomeKind.All, 0.2m, "INCOME_TAX")));
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void A_rule_whose_code_is_already_nested_in_another_rule_is_refused() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(CompositeOf(Flat(IncomeKind.All, 0.1m, "NESTED_CODE"), Flat(IncomeKind.All, 0.2m)));

        AssertRefused(ScheduleRefusal.DuplicateRuleCode, () => schedule.AddRule(Flat(IncomeKind.All, 0.3m, "NESTED_CODE")));
    }

    [Fact]
    public void A_rule_with_a_code_nested_deep_in_its_tree_that_the_schedule_already_uses_is_refused() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(Flat(IncomeKind.All, 0.1m, "TAKEN_CODE"));
        CompositeRule newcomer = CompositeOf(Flat(IncomeKind.All, 0.2m), Flat(IncomeKind.All, 0.3m, "TAKEN_CODE"));

        AssertRefused(ScheduleRefusal.DuplicateRuleCode, () => schedule.AddRule(newcomer));
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void A_rule_wrapped_in_a_cap_counts_for_the_codes_of_the_schedule() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(Any.CappedRule().WithInner(Flat(IncomeKind.All, 0.1m, "WRAPPED_CODE")).WithCondition(null).Generate());

        AssertRefused(ScheduleRefusal.DuplicateRuleCode, () => schedule.AddRule(Flat(IncomeKind.All, 0.2m, "WRAPPED_CODE")));
    }

    [Fact]
    public void Sixty_four_top_level_rules_are_accepted_and_the_sixty_fifth_refused() {
        TaxSchedule schedule = AnyDraft();
        List<RuleCode> codes = Any.SetOf(Any.RuleCode()).WithCount(65).Generate().ToList();

        foreach (RuleCode code in codes.Take(TaxSchedule.MaxRules)) {
            schedule.AddRule(Any.FlatRateRule().WithCode(code).Generate());
        }

        AssertRefused(ScheduleRefusal.TooManyRules, () => schedule.AddRule(Any.FlatRateRule().WithCode(codes[64]).Generate()));
        Assert.Equal(64, schedule.Rules.Count);
    }

    [Fact]
    public void The_limit_counts_top_level_rules_not_the_rules_nested_in_them() {
        TaxSchedule schedule = AnyDraft();
        List<RuleCode> codes = Any.SetOf(Any.RuleCode()).WithCount(2 * 63 + 2).Generate().ToList();

        for (int i = 0; i < 63; i++) {
            TaxRule component1 = Any.FlatRateRule().WithCode(codes[2 * i]).Generate();
            TaxRule component2 = Any.FlatRateRule().WithCode(codes[2 * i + 1]).Generate();
            schedule.AddRule(Any.CompositeRule().WithComponents([component1, component2]).Generate());
        }

        schedule.AddRule(Any.FlatRateRule().WithCode(codes[126]).Generate());

        Assert.Equal(64, schedule.Rules.Count);
        Assert.Equal(63 * 3 + 1, schedule.Rules.SelectMany(r => r.SelfAndDescendants()).Count());
    }

    [Fact]
    public void A_top_level_rule_is_removed_with_everything_nested_in_it_and_its_codes_become_free() {
        TaxSchedule schedule = AnyDraft();
        CompositeRule composite = Any.CompositeRule()
                                     .WithCode(Code("COMPOSITE_RULE"))
                                     .WithComponents([Flat(IncomeKind.All, 0.1m, "FIRST_PART"), Flat(IncomeKind.All, 0.2m, "SECOND_PART")])
                                     .Generate();
        FlatRateRule other = AnyTax();
        schedule.AddRule(composite);
        schedule.AddRule(other);

        schedule.RemoveRule(Code("COMPOSITE_RULE"));

        Assert.Same(other, Assert.Single(schedule.Rules));
        Assert.Null(schedule.Find(Code("FIRST_PART")));
        schedule.AddRule(Flat(IncomeKind.All, 0.3m, "FIRST_PART"));
        Assert.NotNull(schedule.Find(Code("FIRST_PART")));
    }

    [Fact]
    public void A_nested_rule_cannot_be_removed_on_its_own() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(Any.CompositeRule().WithCode(Code("COMPOSITE_RULE")).WithComponents([Flat(IncomeKind.All, 0.1m, "NESTED_PART"), AnyTax()]).Generate());

        TaxScheduleException refusal = Assert.Throws<TaxScheduleException>(() => schedule.RemoveRule(Code("NESTED_PART")));

        Assert.Equal(ScheduleRefusal.NestedRule, refusal.Reason);
        Assert.Contains("COMPOSITE_RULE", refusal.Message);
        Assert.NotNull(schedule.Find(Code("NESTED_PART")));
    }

    [Fact]
    public void A_rule_wrapped_in_a_cap_cannot_be_removed_on_its_own_either() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(Any.CappedRule().WithCode(Code("CAP_RULE")).WithInner(Flat(IncomeKind.All, 0.1m, "WRAPPED_RULE")).WithCondition(null).Generate());

        AssertRefused(ScheduleRefusal.NestedRule, () => schedule.RemoveRule(Code("WRAPPED_RULE")));
    }

    [Fact]
    public void Removing_a_code_the_schedule_does_not_know_is_refused() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(Flat(IncomeKind.All, 0.1m, "KNOWN_RULE"));

        AssertRefused(ScheduleRefusal.UnknownRule, () => schedule.RemoveRule(Code("UNKNOWN_RULE")));
    }

    [Fact]
    public void Removing_nothing_is_refused() {
        TaxSchedule schedule = AnyDraft();

        Assert.Throws<ArgumentNullException>(() => schedule.RemoveRule(null!));
    }

    [Fact]
    public void A_rule_is_found_whether_top_level_or_nested_and_a_missing_one_finds_nothing() {
        TaxSchedule schedule = AnyDraft();
        FlatRateRule nested = Flat(IncomeKind.All, 0.1m, "NESTED_PART");
        CompositeRule composite = Any.CompositeRule().WithCode(Code("COMPOSITE_RULE")).WithComponents([nested, AnyTax()]).Generate();
        FlatRateRule topLevel = Flat(IncomeKind.All, 0.2m, "TOP_LEVEL");
        schedule.AddRule(composite);
        schedule.AddRule(topLevel);

        Assert.Same(composite, schedule.Find(Code("COMPOSITE_RULE")));
        Assert.Same(nested, schedule.Find(Code("NESTED_PART")));
        Assert.Same(topLevel, schedule.Find(Code("TOP_LEVEL")));
        Assert.Null(schedule.Find(Code("ABSENT_RULE")));
        Assert.Throws<ArgumentNullException>(() => schedule.Find(null!));
    }

    // ---- validating and reopening -----------------------------------------------------------------------------

    [Fact]
    public void A_schedule_with_a_tax_rule_validates_and_is_then_frozen() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(AnyTax());

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void An_empty_schedule_cannot_be_validated() {
        TaxSchedule schedule = AnyDraft();

        AssertRefused(ScheduleRefusal.NoRules, schedule.Validate);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
    }

    [Fact]
    public void A_schedule_of_credits_alone_cannot_be_validated() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(AnyCredit());
        schedule.AddRule(AnyCredit());

        AssertRefused(ScheduleRefusal.NoTaxRule, schedule.Validate);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
    }

    [Fact]
    public void One_tax_rule_among_credits_is_enough() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(AnyCredit());
        schedule.AddRule(AnyTax());

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void A_tax_rule_that_never_applies_still_counts_as_a_tax_rule() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(Any.FlatRateRule().WithCondition(IncomeAboveNothing()).Generate());

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void Validating_twice_is_refused() {
        TaxSchedule schedule = ValidatedWith(AnyTax());

        AssertRefused(ScheduleRefusal.AlreadyValidated, schedule.Validate);
    }

    [Fact]
    public void A_validated_schedule_accepts_neither_new_rules_nor_removals() {
        TaxSchedule schedule = ValidatedWith(Flat(IncomeKind.All, 0.1m, "KNOWN_RULE"));

        AssertRefused(ScheduleRefusal.AlreadyValidated, () => schedule.AddRule(AnyTax()));
        AssertRefused(ScheduleRefusal.AlreadyValidated, () => schedule.RemoveRule(Code("KNOWN_RULE")));
        AssertRefused(ScheduleRefusal.AlreadyValidated, () => schedule.RemoveRule(Code("UNKNOWN_RULE")));
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void A_validated_schedule_still_finds_its_rules() {
        TaxSchedule schedule = ValidatedWith(Flat(IncomeKind.All, 0.1m, "KNOWN_RULE"));

        Assert.NotNull(schedule.Find(Code("KNOWN_RULE")));
    }

    [Fact]
    public void A_residency_condition_naming_another_country_is_refused() {
        TaxSchedule schedule = DraftFor("FR");
        schedule.AddRule(Any.FlatRateRule().WithCondition(new ResidentOfCondition(Jurisdiction("DE"))).Generate());

        AssertRefused(ScheduleRefusal.ForeignResidencyCondition, schedule.Validate);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
    }

    [Fact]
    public void A_residency_condition_naming_the_country_or_one_of_its_subdivisions_is_accepted() {
        TaxSchedule schedule = DraftFor("FR");
        schedule.AddRule(Any.FlatRateRule().WithCondition(new ResidentOfCondition(Jurisdiction("FR"))).Generate());
        schedule.AddRule(Any.FlatRateRule().WithCondition(new ResidentOfCondition(Jurisdiction("FR-75"))).Generate());

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void A_state_schedule_accepts_a_residency_condition_on_another_state_of_the_same_country() {
        TaxSchedule schedule = DraftFor("US-CA");
        schedule.AddRule(Any.FlatRateRule().WithCondition(new ResidentOfCondition(Jurisdiction("US-NY"))).Generate());

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void A_state_schedule_refuses_a_residency_condition_on_another_country() {
        TaxSchedule schedule = DraftFor("US-CA");
        schedule.AddRule(Any.FlatRateRule().WithCondition(new ResidentOfCondition(Jurisdiction("CA-ON"))).Generate());

        AssertRefused(ScheduleRefusal.ForeignResidencyCondition, schedule.Validate);
    }

    [Fact]
    public void A_foreign_residency_condition_is_found_at_any_depth_of_the_condition() {
        ICondition foreign = new ResidentOfCondition(Jurisdiction("DE"));
        ICondition anyLeaf = Any.Condition().Leaves().Generate();
        ICondition[] buried = [
            new AndCondition([anyLeaf, foreign]),
            new OrCondition([foreign, anyLeaf]),
            new NotCondition(foreign),
            new AndCondition([anyLeaf, new OrCondition([anyLeaf, new NotCondition(foreign)])])
        ];

        foreach (ICondition condition in buried) {
            TaxSchedule schedule = DraftFor("FR");
            schedule.AddRule(Any.FlatRateRule().WithCondition(condition).Generate());

            AssertRefused(ScheduleRefusal.ForeignResidencyCondition, schedule.Validate);
        }
    }

    [Fact]
    public void A_foreign_residency_condition_is_found_on_a_nested_rule() {
        ICondition foreign = new ResidentOfCondition(Jurisdiction("DE"));
        TaxSchedule schedule = DraftFor("FR");
        schedule.AddRule(CompositeOf(AnyTax(), Any.FlatRateRule().WithCondition(foreign).Generate()));
        TaxSchedule otherSchedule = DraftFor("FR");
        otherSchedule.AddRule(Any.CappedRule().WithInner(Any.FlatRateRule().WithCondition(foreign).Generate()).WithCondition(null).Generate());

        AssertRefused(ScheduleRefusal.ForeignResidencyCondition, schedule.Validate);
        AssertRefused(ScheduleRefusal.ForeignResidencyCondition, otherSchedule.Validate);
    }

    [Fact]
    public void A_foreign_residency_condition_is_found_on_a_credit() {
        TaxSchedule schedule = DraftFor("FR");
        schedule.AddRule(AnyTax());
        schedule.AddRule(Any.TaxCreditRule().WithCondition(new ResidentOfCondition(Jurisdiction("DE"))).Generate());

        AssertRefused(ScheduleRefusal.ForeignResidencyCondition, schedule.Validate);
    }

    [Fact]
    public void Conditions_without_any_residency_are_not_a_problem_at_any_depth() {
        TaxSchedule schedule = DraftFor("FR");
        ICondition deepWithoutResidency = new AndCondition([new AgeAtLeastCondition(18), new OrCondition([new HouseholdSizeAtLeastCondition(3), new NotCondition(new IncomeAboveCondition(IncomeKind.All, M(10m)))])]);
        schedule.AddRule(Any.FlatRateRule().WithCondition(deepWithoutResidency).Generate());

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void Reopening_a_validated_schedule_makes_it_a_draft_again() {
        TaxSchedule schedule = ValidatedWith(AnyTax());

        schedule.Reopen();

        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
        schedule.AddRule(AnyTax());
        Assert.Equal(2, schedule.Rules.Count);
    }

    [Fact]
    public void A_draft_cannot_be_reopened() {
        TaxSchedule schedule = AnyDraft();

        AssertRefused(ScheduleRefusal.NotValidated, schedule.Reopen);
    }

    [Fact]
    public void A_reopened_schedule_is_validated_again_from_scratch() {
        TaxSchedule schedule = ValidatedWith(Flat(IncomeKind.All, 0.1m, "ONLY_RULE"));
        schedule.Reopen();
        schedule.RemoveRule(Code("ONLY_RULE"));

        AssertRefused(ScheduleRefusal.NoRules, schedule.Validate);
    }

    // ---- evaluating --------------------------------------------------------------------------------------------

    [Fact]
    public void A_draft_cannot_evaluate_a_taxpayer() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(AnyTax());
        Taxpayer anyTaxpayer = Any.Taxpayer().Generate();

        AssertRefused(ScheduleRefusal.NotValidated, () => schedule.Evaluate(anyTaxpayer));
    }

    [Fact]
    public void A_reopened_schedule_cannot_evaluate_a_taxpayer() {
        TaxSchedule schedule = ValidatedWith(AnyTax());
        schedule.Reopen();
        Taxpayer anyTaxpayer = Any.Taxpayer().Generate();

        AssertRefused(ScheduleRefusal.NotValidated, () => schedule.Evaluate(anyTaxpayer));
    }

    [Fact]
    public void Evaluating_nobody_is_refused() {
        TaxSchedule schedule = ValidatedWith(AnyTax());

        Assert.Throws<ArgumentNullException>(() => schedule.Evaluate(null!));
    }

    [Fact]
    public void A_taxpayer_born_after_the_year_of_the_schedule_is_refused() {
        TaxSchedule schedule = Any.TaxSchedule().WithYear(2024).Generate();
        schedule.AddRule(AnyTax());
        schedule.Validate();
        Taxpayer bornNextYear = Any.Taxpayer().WithDateOfBirth(new DateOnly(2025, 1, 1)).Generate();

        AssertRefused(ScheduleRefusal.TaxpayerNotBorn, () => schedule.Evaluate(bornNextYear));
    }

    [Fact]
    public void A_taxpayer_born_during_the_year_of_the_schedule_is_evaluated() {
        TaxSchedule schedule = Any.TaxSchedule().WithYear(2024).Generate();
        schedule.AddRule(AnyTax());
        schedule.Validate();
        Taxpayer bornOnTheLastDay = Any.Taxpayer().WithDateOfBirth(new DateOnly(2024, 12, 31)).Generate();
        Taxpayer bornOnTheFirstDay = Any.Taxpayer().WithDateOfBirth(new DateOnly(2024, 1, 1)).Generate();

        Assert.Equal(2024, schedule.Evaluate(bornOnTheLastDay).Year);
        Assert.Equal(2024, schedule.Evaluate(bornOnTheFirstDay).Year);
    }

    [Fact]
    public void The_breakdown_names_the_taxpayer_the_jurisdiction_and_the_year_and_totals_every_kind_of_income() {
        TaxSchedule schedule = DraftFor("FR", 2024);
        schedule.AddRule(AnyTax());
        schedule.Validate();
        Taxpayer taxpayer = TaxpayerEarning(Salary(20_000m, 1_000m), Business(5_000m, 6_000m), Interest(300m), Dividend(1_000m));

        TaxBreakdown breakdown = schedule.Evaluate(taxpayer);

        Assert.Same(taxpayer.Id, breakdown.TaxpayerId);
        Assert.Equal(Jurisdiction("FR"), breakdown.Jurisdiction);
        Assert.Equal(2024, breakdown.Year);
        Assert.Equal(M(19_900m), breakdown.TaxableIncome);
    }

    [Fact]
    public void The_breakdown_has_one_line_per_top_level_rule_in_the_order_they_were_added() {
        TaxSchedule schedule = AnyDraft();
        schedule.AddRule(Flat(IncomeKind.All, 0.1m, "SECOND_ADDED_FIRST"));
        schedule.AddRule(AnyCredit());
        schedule.AddRule(CompositeOf(Flat(IncomeKind.All, 0.1m, "NESTED_PART"), AnyTax()));
        schedule.AddRule(Flat(IncomeKind.All, 0.1m, "LAST_ADDED"));
        schedule.Validate();

        TaxBreakdown breakdown = schedule.Evaluate(Any.Taxpayer().Generate());

        Assert.Equal(schedule.Rules.Select(r => r.Code), breakdown.Lines.Select(l => l.Code));
        Assert.NotNull(breakdown.Find(Code("NESTED_PART")));
    }

    [Fact]
    public void A_schedule_that_applies_nothing_to_a_taxpayer_owes_nothing() {
        TaxSchedule schedule = ValidatedWith(Any.FlatRateRule().WithCondition(IncomeAboveNothing()).Generate());

        TaxBreakdown breakdown = schedule.Evaluate(TaxpayerEarning());

        Assert.True(breakdown.NetTax.IsZero);
        Assert.Equal(LineOutcome.NotApplicable, Assert.Single(breakdown.Lines).Outcome);
        Assert.Equal(0m, breakdown.EffectiveRate);
    }

    [Fact]
    public void A_whole_schedule_computes_the_tax_line_by_line_and_sets_the_credit_against_it() {
        TaxSchedule schedule = DraftFor("FR", 2024);
        schedule.AddRule(Any.ProgressiveRule()
                            .WithCode(Code("INCOME_TAX"))
                            .WithTaxBase(IncomeKind.Employment | IncomeKind.SelfEmployment)
                            .WithBrackets([new TaxBracket(M(0m), M(10_000m), R(0m)),
                                           new TaxBracket(M(10_000m), M(30_000m), R(0.10m)),
                                           new TaxBracket(M(30_000m), null, R(0.30m))])
                            .WithCondition(null)
                            .Generate());
        schedule.AddRule(Flat(IncomeKind.Investment, 0.172m, "INVESTMENT_TAX"));
        schedule.AddRule(Any.CappedRule()
                            .WithCode(Code("HOUSING_TAX"))
                            .WithInner(Flat(IncomeKind.All, 0.01m, "HOUSING_BASE"))
                            .WithMaximum(M(500m))
                            .WithMinimum(M(100m))
                            .WithCondition(null)
                            .Generate());
        schedule.AddRule(Any.TaxCreditRule()
                            .WithCode(Code("FAMILY_CREDIT"))
                            .WithBaseAmount(M(100m))
                            .WithPerAdditionalMember(M(50m))
                            .WithCondition(new HouseholdSizeAtLeastCondition(2))
                            .Generate());
        schedule.Validate();
        Taxpayer taxpayer = Any.Taxpayer()
                               .WithHouseholdSize(3)
                               .WithIncomes([Salary(40_000m, 4_000m), Business(5_000m, 2_000m), Dividend(1_000m), Interest(400m)])
                               .Generate();

        TaxBreakdown breakdown = schedule.Evaluate(taxpayer);

        // Employment 36 000 + business 3 000 = 39 000: 20 000 x 10 % + 9 000 x 30 % = 4 700.
        Assert.Equal(M(4_700m), breakdown.Find(Code("INCOME_TAX"))!.Amount);
        // Investments: 60 % of the 1 000 dividend + 400 interest = 1 000, at 17.2 %.
        Assert.Equal(M(1_000m), breakdown.Find(Code("INVESTMENT_TAX"))!.TaxableBase);
        Assert.Equal(M(172m), breakdown.Find(Code("INVESTMENT_TAX"))!.Amount);
        // 1 % of 40 000 is 400: between the minimum and the maximum of the cap.
        Assert.Equal(M(400m), breakdown.Find(Code("HOUSING_TAX"))!.Amount);
        Assert.Equal(M(400m), breakdown.Find(Code("HOUSING_BASE"))!.Amount);
        // 100 + 2 additional members x 50.
        Assert.Equal(M(200m), breakdown.Find(Code("FAMILY_CREDIT"))!.Amount);
        Assert.Equal(M(40_000m), breakdown.TaxableIncome);
        Assert.Equal(M(5_272m), breakdown.GrossTax);
        Assert.Equal(M(200m), breakdown.CreditsApplied);
        Assert.Equal(M(5_072m), breakdown.NetTax);
        Assert.Equal(0.1268m, breakdown.EffectiveRate);
    }

    [Fact]
    public void A_taxpayer_for_whom_the_condition_of_the_credit_fails_gets_no_credit() {
        TaxSchedule schedule = DraftFor("FR", 2024);
        schedule.AddRule(Flat(IncomeKind.Employment, 0.1m, "INCOME_TAX"));
        schedule.AddRule(Any.TaxCreditRule().WithCode(Code("FAMILY_CREDIT")).WithCondition(new HouseholdSizeAtLeastCondition(2)).Generate());
        schedule.Validate();
        Taxpayer single = Any.Taxpayer().WithHouseholdSize(1).WithIncomes([Salary(10_000m)]).Generate();

        TaxBreakdown breakdown = schedule.Evaluate(single);

        Assert.Equal(LineOutcome.NotApplicable, breakdown.Find(Code("FAMILY_CREDIT"))!.Outcome);
        Assert.Equal(M(1_000m), breakdown.NetTax);
    }

    [Fact]
    public void Evaluating_does_not_change_the_schedule() {
        TaxSchedule schedule = ValidatedWith(AnyTax());
        Taxpayer anyTaxpayer = Any.Taxpayer().Generate();

        schedule.Evaluate(anyTaxpayer);

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
        Assert.Single(schedule.Rules);
    }

}

public sealed class TaxScheduleExceptionTests {

    [Fact]
    public void A_refusal_carries_its_reason_and_its_message_and_is_an_invalid_operation() {
        TaxScheduleException refusal = new TaxScheduleException(ScheduleRefusal.NoRules, "An empty schedule cannot be validated.");

        Assert.Equal(ScheduleRefusal.NoRules, refusal.Reason);
        Assert.Equal("An empty schedule cannot be validated.", refusal.Message);
        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_reason_the_schedule_does_not_know_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaxScheduleException((ScheduleRefusal)99, "Unknown."));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaxScheduleException((ScheduleRefusal)(-1), "Unknown."));
    }

}
