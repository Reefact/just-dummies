using JustDummies;

using TaxRules.Domain;

namespace TaxRules.Tests.Discovery;

public sealed class TaxScheduleConstructionTests {

    [Fact]
    public void A_new_schedule_is_an_empty_draft() {
        // Arrange
        JurisdictionCode france = new JurisdictionCode("FR");

        // Act
        TaxSchedule schedule = new TaxSchedule(france, 2024);

        // Assert
        Assert.Same(france, schedule.Jurisdiction);
        Assert.Equal(2024, schedule.Year);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
        Assert.Empty(schedule.Rules);
    }

    [Theory]
    [InlineData(2000, true)]
    [InlineData(2099, true)]
    [InlineData(1999, false)]
    [InlineData(2100, false)]
    [InlineData(0, false)]
    public void A_schedule_covers_a_year_from_2000_to_2099(int year, bool accepted) {
        JurisdictionCode anyJurisdiction = Any.JurisdictionCode().Generate();

        if (accepted) {
            Assert.Equal(year, new TaxSchedule(anyJurisdiction, year).Year);
        } else {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TaxSchedule(anyJurisdiction, year));
        }
    }

    [Fact]
    public void A_missing_jurisdiction_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new TaxSchedule(null!, 2024));
    }

    [Fact]
    public void A_generated_schedule_is_a_draft_in_range() {
        for (int i = 0; i < 50; i++) {
            TaxSchedule anySchedule = Any.TaxSchedule().Generate();

            Assert.Equal(ScheduleStatus.Draft, anySchedule.Status);
            Assert.InRange(anySchedule.Year, TaxSchedule.FirstYear, TaxSchedule.LastYear);
        }
    }

}

public sealed class TaxScheduleRuleManagementTests {

    private static TaxSchedule Draft() => Any.TaxSchedule().WithJurisdiction(new JurisdictionCode("FR")).WithYear(2024).Generate();

    private static FlatRateRule Flat(string code) => Fixtures.FlatOn(IncomeKind.Employment, 0.1m, code);

    private static TaxScheduleException Refusal(Action action) => Assert.Throws<TaxScheduleException>(action);

    [Fact]
    public void Rules_are_kept_in_the_order_they_were_added() {
        // Arrange
        TaxSchedule schedule = Draft();
        FlatRateRule first  = Flat("FIRST_TAX");
        FlatRateRule second = Flat("SECOND_TAX");
        FlatRateRule third  = Flat("THIRD_TAX");

        // Act
        schedule.AddRule(second);
        schedule.AddRule(first);
        schedule.AddRule(third);

        // Assert
        Assert.Equal([second, first, third], schedule.Rules);
    }

    [Fact]
    public void A_missing_rule_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Draft().AddRule(null!));
    }

    [Fact]
    public void A_code_already_used_by_a_top_level_rule_is_refused() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Flat("SAME_CODE"));

        TaxScheduleException refusal = Refusal(() => schedule.AddRule(Flat("SAME_CODE")));

        Assert.Equal(ScheduleRefusal.DuplicateRuleCode, refusal.Reason);
        Assert.Contains("SAME_CODE", refusal.Message);
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void A_code_used_inside_the_new_tree_and_already_in_the_schedule_is_refused() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Flat("EXISTING_TAX"));
        CompositeRule newTree = new CompositeRule(Fixtures.Code("NEW_SUM"), AnyLabel.Create().Generate(), [Flat("EXISTING_TAX"), Flat("OTHER_TAX")]);

        TaxScheduleException refusal = Refusal(() => schedule.AddRule(newTree));

        Assert.Equal(ScheduleRefusal.DuplicateRuleCode, refusal.Reason);
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void A_top_level_code_equal_to_a_nested_code_already_in_the_schedule_is_refused() {
        TaxSchedule   schedule = Draft();
        CompositeRule existing = new CompositeRule(Fixtures.Code("OLD_SUM"), AnyLabel.Create().Generate(), [Flat("NESTED_TAX"), Flat("OTHER_TAX")]);
        schedule.AddRule(existing);

        Assert.Equal(ScheduleRefusal.DuplicateRuleCode, Refusal(() => schedule.AddRule(Flat("NESTED_TAX"))).Reason);
    }

    [Fact]
    public void A_nested_code_equal_to_a_top_level_code_already_in_the_schedule_is_refused() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Flat("TOP_TAX"));
        CompositeRule newTree = new CompositeRule(Fixtures.Code("NEW_SUM"), AnyLabel.Create().Generate(), [Flat("TOP_TAX"), Flat("OTHER_TAX")]);

        Assert.Equal(ScheduleRefusal.DuplicateRuleCode, Refusal(() => schedule.AddRule(newTree)).Reason);
    }

    [Fact]
    public void Sixty_four_top_level_rules_are_the_most_a_schedule_holds() {
        // Arrange
        TaxSchedule  schedule = Draft();
        List<RuleCode> codes  = Any.ListOf(new AnyRuleCode()).WithCount(65).DistinctBy(code => code.Value).Generate();

        // Act
        foreach (RuleCode code in codes.Take(TaxSchedule.MaxRules)) {
            schedule.AddRule(Any.FlatRateRule().WithCode(code).Generate());
        }

        TaxScheduleException refusal = Refusal(() => schedule.AddRule(Any.FlatRateRule().WithCode(codes[64]).Generate()));

        // Assert
        Assert.Equal(64, schedule.Rules.Count);
        Assert.Equal(ScheduleRefusal.TooManyRules, refusal.Reason);
    }

    [Fact]
    public void Removing_a_top_level_rule_takes_its_tree_with_it_and_frees_its_codes() {
        // Arrange
        TaxSchedule   schedule  = Draft();
        CompositeRule composite = new CompositeRule(Fixtures.Code("SUM_TAX"), AnyLabel.Create().Generate(), [Flat("PART_ONE"), Flat("PART_TWO")]);
        schedule.AddRule(composite);
        schedule.AddRule(Flat("PLAIN_TAX"));

        // Act
        schedule.RemoveRule(Fixtures.Code("SUM_TAX"));

        // Assert
        Assert.Equal([Fixtures.Code("PLAIN_TAX")], schedule.Rules.Select(r => r.Code));
        Assert.Null(schedule.Find(Fixtures.Code("PART_ONE")));
        schedule.AddRule(Flat("PART_ONE"));
        Assert.NotNull(schedule.Find(Fixtures.Code("PART_ONE")));
    }

    [Fact]
    public void A_nested_rule_cannot_be_removed_alone() {
        TaxSchedule   schedule  = Draft();
        CompositeRule composite = new CompositeRule(Fixtures.Code("SUM_TAX"), AnyLabel.Create().Generate(), [Flat("PART_ONE"), Flat("PART_TWO")]);
        schedule.AddRule(composite);

        TaxScheduleException refusal = Refusal(() => schedule.RemoveRule(Fixtures.Code("PART_ONE")));

        Assert.Equal(ScheduleRefusal.NestedRule, refusal.Reason);
        Assert.Contains("SUM_TAX", refusal.Message);
        Assert.Same(composite, Assert.Single(schedule.Rules));
    }

    [Fact]
    public void An_unknown_rule_cannot_be_removed() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Flat("SOME_TAX"));

        Assert.Equal(ScheduleRefusal.UnknownRule, Refusal(() => schedule.RemoveRule(Fixtures.Code("OTHER_TAX"))).Reason);
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void Removing_a_missing_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Draft().RemoveRule(null!));
    }

    [Fact]
    public void Find_returns_a_top_level_or_nested_rule() {
        TaxSchedule   schedule  = Draft();
        FlatRateRule  part      = Flat("PART_ONE");
        CompositeRule composite = new CompositeRule(Fixtures.Code("SUM_TAX"), AnyLabel.Create().Generate(), [part, Flat("PART_TWO")]);
        schedule.AddRule(composite);

        Assert.Same(composite, schedule.Find(Fixtures.Code("SUM_TAX")));
        Assert.Same(part, schedule.Find(Fixtures.Code("PART_ONE")));
        Assert.Null(schedule.Find(Fixtures.Code("ABSENT_TAX")));
        Assert.Throws<ArgumentNullException>(() => schedule.Find(null!));
    }

    [Fact]
    public void A_validated_schedule_accepts_neither_new_rules_nor_removals() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Flat("SOME_TAX"));
        schedule.Validate();

        Assert.Equal(ScheduleRefusal.AlreadyValidated, Refusal(() => schedule.AddRule(Flat("OTHER_TAX"))).Reason);
        Assert.Equal(ScheduleRefusal.AlreadyValidated, Refusal(() => schedule.RemoveRule(Fixtures.Code("SOME_TAX"))).Reason);
        Assert.Equal(ScheduleRefusal.AlreadyValidated, Refusal(() => schedule.RemoveRule(Fixtures.Code("ABSENT_TAX"))).Reason);
        Assert.Single(schedule.Rules);
    }

    [Fact]
    public void A_refusal_is_an_invalid_operation_carrying_its_reason() {
        TaxScheduleException refusal = Refusal(() => Draft().Validate());

        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
        Assert.Equal(ScheduleRefusal.NoRules, refusal.Reason);
    }

    [Fact]
    public void A_refusal_cannot_carry_an_undefined_reason() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaxScheduleException((ScheduleRefusal)99, "anything"));
    }

}

public sealed class TaxScheduleValidationTests {

    private static TaxSchedule Draft(string jurisdiction = "FR") {
        return Any.TaxSchedule().WithJurisdiction(new JurisdictionCode(jurisdiction)).WithYear(2024).Generate();
    }

    private static FlatRateRule Flat(string code, ICondition? condition = null) {
        return Any.FlatRateRule().WithCode(Fixtures.Code(code)).WithCondition(condition).Generate();
    }

    private static TaxScheduleException Refusal(Action action) => Assert.Throws<TaxScheduleException>(action);

    [Fact]
    public void An_empty_schedule_cannot_be_validated() {
        TaxSchedule schedule = Draft();

        Assert.Equal(ScheduleRefusal.NoRules, Refusal(schedule.Validate).Reason);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
    }

    [Fact]
    public void A_schedule_of_credits_alone_cannot_be_validated() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Fixtures.Credit("FIRST_CREDIT", 10m));
        schedule.AddRule(Fixtures.Credit("SECOND_CREDIT", 10m));

        Assert.Equal(ScheduleRefusal.NoTaxRule, Refusal(schedule.Validate).Reason);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
    }

    [Fact]
    public void One_tax_rule_among_credits_is_enough() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Fixtures.Credit("SOME_CREDIT", 10m));
        schedule.AddRule(Flat("SOME_TAX"));

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void A_single_tax_rule_is_a_valid_schedule() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Flat("SOME_TAX"));

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void Validating_twice_is_refused() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Flat("SOME_TAX"));
        schedule.Validate();

        Assert.Equal(ScheduleRefusal.AlreadyValidated, Refusal(schedule.Validate).Reason);
    }

    [Fact]
    public void A_residency_condition_naming_another_country_is_refused() {
        TaxSchedule schedule = Draft("FR");
        schedule.AddRule(Flat("SOME_TAX", new ResidentOfCondition(new JurisdictionCode("DE"))));

        TaxScheduleException refusal = Refusal(schedule.Validate);

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, refusal.Reason);
        Assert.Contains("SOME_TAX", refusal.Message);
        Assert.Contains("DE", refusal.Message);
        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
    }

    [Fact]
    public void A_residency_condition_naming_a_subdivision_of_another_country_is_refused() {
        TaxSchedule schedule = Draft("FR");
        schedule.AddRule(Flat("SOME_TAX", new ResidentOfCondition(new JurisdictionCode("DE-BY"))));

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, Refusal(schedule.Validate).Reason);
    }

    [Fact]
    public void A_residency_condition_naming_the_schedules_own_country_or_one_of_its_subdivisions_is_accepted() {
        TaxSchedule schedule = Draft("US-CA");
        schedule.AddRule(Flat("COUNTRY_TAX", new ResidentOfCondition(new JurisdictionCode("US"))));
        schedule.AddRule(Flat("OTHER_STATE", new ResidentOfCondition(new JurisdictionCode("US-NY"))));
        schedule.AddRule(Flat("SAME_STATE", new ResidentOfCondition(new JurisdictionCode("US-CA"))));

        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void A_foreign_residency_buried_in_a_combination_is_found() {
        ICondition buried = new AndCondition([
            new AgeAtLeastCondition(18),
            new OrCondition([new HouseholdSizeAtLeastCondition(3), new NotCondition(new ResidentOfCondition(new JurisdictionCode("DE")))])
        ]);
        TaxSchedule schedule = Draft("FR");
        schedule.AddRule(Flat("SOME_TAX", buried));

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, Refusal(schedule.Validate).Reason);
    }

    [Fact]
    public void A_foreign_residency_on_a_nested_rule_is_found() {
        TaxSchedule schedule = Draft("FR");
        CompositeRule composite = new CompositeRule(
            Fixtures.Code("SUM_TAX"),
            AnyLabel.Create().Generate(),
            [Flat("PLAIN_TAX"), Flat("NESTED_TAX", new ResidentOfCondition(new JurisdictionCode("IT")))]);
        schedule.AddRule(composite);

        TaxScheduleException refusal = Refusal(schedule.Validate);

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, refusal.Reason);
        Assert.Contains("NESTED_TAX", refusal.Message);
    }

    [Fact]
    public void A_foreign_residency_on_the_rule_wrapped_by_a_cap_is_found() {
        TaxSchedule schedule = Draft("FR");
        CappedRule capped = new CappedRule(
            Fixtures.Code("CAP_ONE"),
            AnyLabel.Create().Generate(),
            Flat("INNER_TAX", new ResidentOfCondition(new JurisdictionCode("ES"))),
            new Money(100m));
        schedule.AddRule(capped);

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, Refusal(schedule.Validate).Reason);
    }

    [Fact]
    public void A_foreign_residency_on_a_credit_is_found() {
        TaxSchedule schedule = Draft("FR");
        schedule.AddRule(Flat("SOME_TAX"));
        schedule.AddRule(Any.TaxCreditRule().WithBaseAmount(new Money(10m)).WithCondition(new ResidentOfCondition(new JurisdictionCode("BE"))).Generate());

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, Refusal(schedule.Validate).Reason);
    }

    [Fact]
    public void A_condition_on_a_cap_itself_naming_a_foreign_country_is_found() {
        TaxSchedule schedule = Draft("FR");
        CappedRule capped = new CappedRule(
            Fixtures.Code("CAP_ONE"),
            AnyLabel.Create().Generate(),
            Flat("INNER_TAX"),
            new Money(100m),
            null,
            new ResidentOfCondition(new JurisdictionCode("ES")));
        schedule.AddRule(capped);

        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, Refusal(schedule.Validate).Reason);
    }

    [Fact]
    public void A_schedule_refused_at_validation_can_be_amended_and_validated_again() {
        TaxSchedule schedule = Draft("FR");
        schedule.AddRule(Flat("FOREIGN_TAX", new ResidentOfCondition(new JurisdictionCode("DE"))));
        Assert.Equal(ScheduleRefusal.ForeignResidencyCondition, Refusal(schedule.Validate).Reason);

        schedule.RemoveRule(Fixtures.Code("FOREIGN_TAX"));
        schedule.AddRule(Flat("DOMESTIC_TAX"));
        schedule.Validate();

        Assert.Equal(ScheduleStatus.Validated, schedule.Status);
    }

    [Fact]
    public void Reopening_a_validated_schedule_returns_it_to_draft_with_its_rules() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Flat("SOME_TAX"));
        schedule.Validate();

        schedule.Reopen();

        Assert.Equal(ScheduleStatus.Draft, schedule.Status);
        Assert.Single(schedule.Rules);
        schedule.AddRule(Flat("OTHER_TAX"));
        schedule.Validate();
        Assert.Equal(2, schedule.Rules.Count);
    }

    [Fact]
    public void A_draft_cannot_be_reopened() {
        TaxSchedule schedule = Draft();

        Assert.Equal(ScheduleRefusal.NotValidated, Refusal(schedule.Reopen).Reason);
    }

}

public sealed class TaxScheduleEvaluationTests {

    private static TaxSchedule Draft(string jurisdiction = "FR", int year = 2024) {
        return Any.TaxSchedule().WithJurisdiction(new JurisdictionCode(jurisdiction)).WithYear(year).Generate();
    }

    private static TaxSchedule Validated(params TaxRule[] rules) {
        TaxSchedule schedule = Draft();
        foreach (TaxRule rule in rules) { schedule.AddRule(rule); }

        schedule.Validate();
        return schedule;
    }

    private static TaxScheduleException Refusal(Action action) => Assert.Throws<TaxScheduleException>(action);

    [Fact]
    public void A_draft_cannot_evaluate_a_taxpayer() {
        TaxSchedule schedule = Draft();
        schedule.AddRule(Fixtures.FlatOn(IncomeKind.All, 0.1m));

        Assert.Equal(ScheduleRefusal.NotValidated, Refusal(() => schedule.Evaluate(Any.Taxpayer().Generate())).Reason);
    }

    [Fact]
    public void A_reopened_schedule_cannot_evaluate_a_taxpayer() {
        TaxSchedule schedule = Validated(Fixtures.FlatOn(IncomeKind.All, 0.1m));
        schedule.Reopen();

        Assert.Equal(ScheduleRefusal.NotValidated, Refusal(() => schedule.Evaluate(Any.Taxpayer().Generate())).Reason);
    }

    [Fact]
    public void A_missing_taxpayer_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Validated(Fixtures.FlatOn(IncomeKind.All, 0.1m)).Evaluate(null!));
    }

    [Fact]
    public void A_taxpayer_born_after_the_year_cannot_be_evaluated() {
        TaxSchedule schedule = Validated(Fixtures.FlatOn(IncomeKind.All, 0.1m));
        Taxpayer    unborn   = Any.Taxpayer().WithDateOfBirth(new DateOnly(2025, 1, 1)).Generate();

        Assert.Equal(ScheduleRefusal.TaxpayerNotBorn, Refusal(() => schedule.Evaluate(unborn)).Reason);
    }

    [Fact]
    public void A_taxpayer_born_during_the_year_can_be_evaluated() {
        TaxSchedule schedule = Validated(Fixtures.FlatOn(IncomeKind.All, 0.1m));
        Taxpayer    newborn  = Any.Taxpayer().WithDateOfBirth(new DateOnly(2024, 12, 31)).Generate();

        TaxBreakdown breakdown = schedule.Evaluate(newborn);

        Assert.Equal(2024, breakdown.Year);
    }

    [Fact]
    public void The_breakdown_names_the_taxpayer_the_jurisdiction_and_the_year_and_totals_all_income() {
        // Arrange
        TaxSchedule schedule = Validated(Fixtures.FlatOn(IncomeKind.Employment, 0.1m));
        Taxpayer    taxpayer = Fixtures.EarningTaxpayer(Fixtures.Salary(1_000m), Fixtures.Profit(500m), Fixtures.Interest(250m));

        // Act
        TaxBreakdown breakdown = schedule.Evaluate(taxpayer);

        // Assert
        Assert.Same(taxpayer.Id, breakdown.TaxpayerId);
        Assert.Equal(new JurisdictionCode("FR"), breakdown.Jurisdiction);
        Assert.Equal(2024, breakdown.Year);
        Assert.Equal(new Money(1_750m), breakdown.TaxableIncome);
    }

    [Fact]
    public void There_is_one_line_per_top_level_rule_in_the_order_of_the_schedule() {
        TaxSchedule schedule = Validated(
            Fixtures.FlatOn(IncomeKind.All, 0.1m, "SECOND_TAX"),
            Fixtures.Credit("FIRST_CREDIT", 10m),
            Fixtures.FlatOn(IncomeKind.All, 0.1m, "THIRD_TAX"));

        TaxBreakdown breakdown = schedule.Evaluate(Any.Taxpayer().Generate());

        Assert.Equal(
            [Fixtures.Code("SECOND_TAX"), Fixtures.Code("FIRST_CREDIT"), Fixtures.Code("THIRD_TAX")],
            breakdown.Lines.Select(l => l.Code));
    }

    [Fact]
    public void A_complete_schedule_is_evaluated_into_an_explained_breakdown() {
        // Arrange: a progressive tax on everything earned, a capped surtax on investment, a family credit.
        ProgressiveRule scale = Any.ProgressiveRule()
                                   .WithCode(Fixtures.Code("INCOME_TAX"))
                                   .WithTaxBase(IncomeKind.Employment | IncomeKind.SelfEmployment)
                                   .WithBrackets(new[] { Fixtures.Band(0m, 10_000m, 0m), Fixtures.Band(10_000m, 40_000m, 0.10m), Fixtures.Band(40_000m, null, 0.30m) })
                                   .Generate();
        CappedRule surtax = new CappedRule(
            Fixtures.Code("INVEST_CAP"),
            AnyLabel.Create().Generate(),
            Fixtures.FlatOn(IncomeKind.Investment, 0.20m, "INVEST_TAX"),
            new Money(150m));
        TaxCreditRule family = Fixtures.Credit("FAMILY_CREDIT", 100m, 50m);

        TaxSchedule schedule = Validated(scale, surtax, family);
        Taxpayer taxpayer = Any.Taxpayer()
                               .WithHouseholdSize(3)
                               .WithIncomes(new IncomeComponent[] { Fixtures.Salary(50_000m), Fixtures.Profit(5_000m), Fixtures.Interest(1_000m) })
                               .Generate();

        // Act
        TaxBreakdown breakdown = schedule.Evaluate(taxpayer);

        // Assert
        // Scale on 55 000: 0 + 3 000 + 15 000 * 0.30 = 7 500. Surtax: 20 % of 1 000 = 200, capped at 150. Credit: 100 + 2 * 50 = 200.
        Assert.Equal(new Money(7_500m), breakdown.Find(Fixtures.Code("INCOME_TAX"))!.Amount);
        Assert.Equal(LineOutcome.CappedAtMaximum, breakdown.Find(Fixtures.Code("INVEST_CAP"))!.Outcome);
        Assert.Equal(new Money(200m), breakdown.Find(Fixtures.Code("INVEST_TAX"))!.Amount);
        Assert.Equal(new Money(150m), breakdown.Find(Fixtures.Code("INVEST_CAP"))!.Amount);
        Assert.Equal(new Money(200m), breakdown.Find(Fixtures.Code("FAMILY_CREDIT"))!.Amount);
        Assert.Equal(new Money(7_650m), breakdown.GrossTax);
        Assert.Equal(new Money(200m), breakdown.CreditsApplied);
        Assert.Equal(new Money(7_450m), breakdown.NetTax);
        Assert.Equal(new Money(56_000m), breakdown.TaxableIncome);
        Assert.Equal(0.133m, breakdown.EffectiveRate);
    }

    [Fact]
    public void Rules_whose_condition_fails_appear_as_not_applicable_and_do_not_count() {
        // Arrange
        FlatRateRule seniorsOnly = Any.FlatRateRule()
                                      .WithCode(Fixtures.Code("SENIOR_TAX"))
                                      .WithTaxBase(IncomeKind.All)
                                      .WithRate(new Rate(0.5m))
                                      .WithCondition(new AgeAtLeastCondition(65))
                                      .Generate();
        TaxSchedule schedule = Validated(Fixtures.FlatOn(IncomeKind.All, 0.1m, "GENERAL_TAX"), seniorsOnly);
        Taxpayer young = Any.Taxpayer()
                            .WithDateOfBirth(new DateOnly(1990, 1, 1))
                            .WithIncomes(new IncomeComponent[] { Fixtures.Salary(1_000m) })
                            .Generate();
        Taxpayer senior = Any.Taxpayer()
                             .WithDateOfBirth(new DateOnly(1950, 1, 1))
                             .WithIncomes(new IncomeComponent[] { Fixtures.Salary(1_000m) })
                             .Generate();

        // Act
        TaxBreakdown forYoung  = schedule.Evaluate(young);
        TaxBreakdown forSenior = schedule.Evaluate(senior);

        // Assert
        Assert.Equal(LineOutcome.NotApplicable, forYoung.Find(Fixtures.Code("SENIOR_TAX"))!.Outcome);
        Assert.Equal(new Money(100m), forYoung.GrossTax);
        Assert.Equal(LineOutcome.Applied, forSenior.Find(Fixtures.Code("SENIOR_TAX"))!.Outcome);
        Assert.Equal(new Money(600m), forSenior.GrossTax);
    }

    [Fact]
    public void The_conditions_see_the_year_of_the_schedule() {
        // Arrange: aged 40 at the end of 2024 (born 1984), but only 30 at the end of 2014.
        FlatRateRule rule = Any.FlatRateRule()
                               .WithCode(Fixtures.Code("ADULT_TAX"))
                               .WithTaxBase(IncomeKind.All)
                               .WithRate(new Rate(0.1m))
                               .WithCondition(new AgeAtLeastCondition(40))
                               .Generate();
        Taxpayer taxpayer = Any.Taxpayer().WithDateOfBirth(new DateOnly(1984, 5, 5)).WithIncomes(new IncomeComponent[] { Fixtures.Salary(1_000m) }).Generate();

        TaxSchedule in2024 = Draft(year: 2024);
        in2024.AddRule(rule);
        in2024.Validate();

        TaxSchedule in2014 = Draft(year: 2014);
        in2014.AddRule(Any.FlatRateRule().WithCode(Fixtures.Code("ADULT_TAX")).WithTaxBase(IncomeKind.All).WithRate(new Rate(0.1m)).WithCondition(new AgeAtLeastCondition(40)).Generate());
        in2014.Validate();

        // Act and assert
        Assert.Equal(LineOutcome.Applied, in2024.Evaluate(taxpayer).Lines.Single().Outcome);
        Assert.Equal(LineOutcome.NotApplicable, in2014.Evaluate(taxpayer).Lines.Single().Outcome);
    }

    [Fact]
    public void A_schedule_can_be_amended_after_reopening_and_evaluates_the_new_rules() {
        TaxSchedule schedule = Validated(Fixtures.FlatOn(IncomeKind.Employment, 0.1m, "FIRST_TAX"));
        Taxpayer    taxpayer = Fixtures.TaxpayerEarning(1_000m);
        Assert.Equal(new Money(100m), schedule.Evaluate(taxpayer).GrossTax);

        schedule.Reopen();
        schedule.AddRule(Fixtures.FlatOn(IncomeKind.Employment, 0.2m, "SECOND_TAX"));
        schedule.Validate();

        Assert.Equal(new Money(300m), schedule.Evaluate(taxpayer).GrossTax);
    }

    [Fact]
    public void Evaluating_a_taxpayer_resident_elsewhere_is_not_refused() {
        // The schedule does not test where the taxpayer lives unless a rule says so.
        TaxSchedule schedule = Validated(Fixtures.FlatOn(IncomeKind.Employment, 0.1m));
        Taxpayer    abroad   = Any.Taxpayer().WithResidence(new JurisdictionCode("JP")).WithIncomes(new IncomeComponent[] { Fixtures.Salary(1_000m) }).Generate();

        Assert.Equal(new Money(100m), schedule.Evaluate(abroad).GrossTax);
    }

    [Fact]
    public void Evaluating_a_taxpayer_whose_total_income_exceeds_the_maximum_amount_is_refused() {
        TaxSchedule schedule = Validated(Fixtures.FlatOn(IncomeKind.Employment, 0.1m));
        Taxpayer huge = Fixtures.EarningTaxpayer(
            new EmploymentIncome(Fixtures.AnySource(), new Money(Money.MaxAmount), Money.Zero),
            new SelfEmploymentIncome(Fixtures.AnySource(), new Money(1m), Money.Zero));

        Assert.Throws<ArgumentOutOfRangeException>(() => schedule.Evaluate(huge));
    }

}
