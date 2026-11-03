using JustDummies;

using TaxRules.Domain;

using Xunit;

using static TaxRules.Tests.Build;

namespace TaxRules.Tests;

public sealed class CappedRuleTests {

    // A flat 10 % on employment, so that a salary of 1 000 owes 100: the inner amount is the number the tests move.
    private static FlatRateRule TenPercentOnSalary(string? code = null) => Flat(IncomeKind.Employment, 0.1m, code);

    private static Taxpayer EarningSalary(decimal gross) => TaxpayerEarning(Salary(gross));

    private static AnyCappedRule AnyCapOver(TaxRule inner, decimal maximum) {
        return Any.CappedRule().WithInner(inner).WithMaximum(M(maximum)).WithCondition(null);
    }

    [Fact]
    public void An_amount_below_the_maximum_passes_through_unchanged() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 500m).WithoutMinimum().Generate();

        BreakdownLine line = rule.Evaluate(EarningSalary(1_000m), 2020);

        Assert.Equal(M(100m), line.Amount);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
    }

    [Fact]
    public void An_amount_above_the_maximum_is_brought_down_to_it() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 50m).WithoutMinimum().Generate();

        BreakdownLine line = rule.Evaluate(EarningSalary(1_000m), 2020);

        Assert.Equal(M(50m), line.Amount);
        Assert.Equal(LineOutcome.CappedAtMaximum, line.Outcome);
    }

    [Fact]
    public void An_amount_equal_to_the_maximum_is_not_reported_as_capped() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 100m).WithoutMinimum().Generate();

        BreakdownLine line = rule.Evaluate(EarningSalary(1_000m), 2020);

        Assert.Equal(M(100m), line.Amount);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
    }

    [Fact]
    public void A_positive_amount_below_the_minimum_is_raised_to_it() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 500m).WithMinimum(M(250m)).Generate();

        BreakdownLine line = rule.Evaluate(EarningSalary(1_000m), 2020);

        Assert.Equal(M(250m), line.Amount);
        Assert.Equal(LineOutcome.RaisedToMinimum, line.Outcome);
    }

    [Fact]
    public void An_amount_equal_to_the_minimum_is_not_reported_as_raised() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 500m).WithMinimum(M(100m)).Generate();

        BreakdownLine line = rule.Evaluate(EarningSalary(1_000m), 2020);

        Assert.Equal(M(100m), line.Amount);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
    }

    [Fact]
    public void An_amount_of_zero_stays_zero_whatever_the_minimum() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 500m).WithMinimum(M(250m)).Generate();

        BreakdownLine line = rule.Evaluate(TaxpayerEarning(), 2020);

        Assert.True(line.Amount.IsZero);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
    }

    [Fact]
    public void A_cap_whose_minimum_equals_its_maximum_fixes_the_amount_of_any_positive_inner() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 300m).WithMinimum(M(300m)).Generate();

        Assert.Equal(M(300m), rule.Evaluate(EarningSalary(100m), 2020).Amount);
        Assert.Equal(LineOutcome.RaisedToMinimum, rule.Evaluate(EarningSalary(100m), 2020).Outcome);
        Assert.Equal(M(300m), rule.Evaluate(EarningSalary(100_000m), 2020).Amount);
        Assert.Equal(LineOutcome.CappedAtMaximum, rule.Evaluate(EarningSalary(100_000m), 2020).Outcome);
    }

    [Fact]
    public void Without_a_minimum_the_minimum_is_zero() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 500m).WithoutMinimum().Generate();

        Assert.True(rule.Minimum.IsZero);
        Assert.Equal(M(500m), rule.Maximum);
    }

    [Fact]
    public void A_cap_reports_the_line_of_the_rule_it_bounds_and_has_no_base_of_its_own() {
        FlatRateRule inner = TenPercentOnSalary("SALARY_TAX");
        CappedRule rule = AnyCapOver(inner, 50m).WithCode(Code("CAPPED_TAX")).WithoutMinimum().Generate();

        BreakdownLine line = rule.Evaluate(EarningSalary(1_000m), 2020);

        Assert.Equal(Code("CAPPED_TAX"), line.Code);
        Assert.Null(line.TaxableBase);
        BreakdownLine child = Assert.Single(line.Children);
        Assert.Equal(Code("SALARY_TAX"), child.Code);
        Assert.Equal(M(100m), child.Amount);
        Assert.Same(inner, Assert.Single(rule.Children));
        Assert.Same(inner, rule.Inner);
    }

    [Fact]
    public void A_cap_whose_condition_fails_produces_nothing_and_does_not_evaluate_its_inner_rule() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 50m).WithMinimum(M(10m)).WithCondition(IncomeAboveNothing()).Generate();

        BreakdownLine line = rule.Evaluate(TaxpayerEarning(), 2020);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.Empty(line.Children);
        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void An_inner_rule_that_does_not_apply_gives_a_zero_amount_that_is_still_not_raised() {
        FlatRateRule innerNotApplying = Any.FlatRateRule().WithCondition(IncomeAboveNothing()).Generate();
        CappedRule rule = AnyCapOver(innerNotApplying, 500m).WithMinimum(M(250m)).Generate();

        BreakdownLine line = rule.Evaluate(TaxpayerEarning(), 2020);

        Assert.True(line.Amount.IsZero);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(LineOutcome.NotApplicable, Assert.Single(line.Children).Outcome);
    }

    [Fact]
    public void A_cap_can_bound_a_composite() {
        CompositeRule composite = CompositeOf(TenPercentOnSalary(), Flat(IncomeKind.Employment, 0.2m));
        CappedRule rule = AnyCapOver(composite, 200m).WithoutMinimum().Generate();

        BreakdownLine line = rule.Evaluate(EarningSalary(1_000m), 2020);

        Assert.Equal(M(200m), line.Amount);
        Assert.Equal(LineOutcome.CappedAtMaximum, line.Outcome);
    }

    [Fact]
    public void A_cap_does_not_wrap_another_cap() {
        CappedRule innerCap = AnyCapOver(TenPercentOnSalary(), 50m).Generate();

        Assert.Throws<ArgumentException>(() => AnyCapOver(innerCap, 40m).Generate());
    }

    [Fact]
    public void A_cap_does_not_wrap_a_credit() {
        TaxCreditRule credit = Any.TaxCreditRule().WithCondition(null).Generate();

        Assert.Throws<ArgumentException>(() => AnyCapOver(credit, 40m).Generate());
    }

    [Fact]
    public void A_cap_cannot_share_its_code_with_the_rule_it_wraps() {
        FlatRateRule inner = TenPercentOnSalary("SHARED_CODE");

        Assert.Throws<ArgumentException>(() => AnyCapOver(inner, 50m).WithCode(Code("SHARED_CODE")).Generate());
    }

    [Fact]
    public void A_cap_cannot_share_its_code_with_a_rule_nested_deeper_in_the_wrapped_tree() {
        CompositeRule inner = Any.CompositeRule()
                                 .WithComponents([TenPercentOnSalary("DEEP_CODE"), Flat(IncomeKind.Employment, 0.2m)])
                                 .Generate();

        Assert.Throws<ArgumentException>(() => AnyCapOver(inner, 50m).WithCode(Code("DEEP_CODE")).Generate());
    }

    [Fact]
    public void A_maximum_of_zero_is_refused_as_an_exemption_in_disguise() {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyCapOver(TenPercentOnSalary(), 50m).WithMaximum(Money.Zero).Generate());
    }

    [Fact]
    public void A_minimum_above_the_maximum_is_refused() {
        Assert.Throws<ArgumentException>(() => AnyCapOver(TenPercentOnSalary(), 50m).WithMinimum(M(50.01m)).Generate());
    }

    [Fact]
    public void A_minimum_of_zero_is_accepted() {
        CappedRule rule = AnyCapOver(TenPercentOnSalary(), 50m).WithMinimum(Money.Zero).Generate();

        Assert.True(rule.Minimum.IsZero);
    }

    [Fact]
    public void A_cap_needs_a_rule_and_a_maximum() {
        Assert.Throws<ArgumentNullException>(() => new CappedRule(Any.RuleCode().Generate(), "Cap", null!, M(1m)));
        Assert.Throws<ArgumentNullException>(() => new CappedRule(Any.RuleCode().Generate(), "Cap", TenPercentOnSalary(), null!));
    }

}

public sealed class CompositeRuleTests {

    private static CompositeRule SumOfTenAndTwentyPercentOnSalary(ICondition? condition = null) {
        return Any.CompositeRule()
                  .WithComponents([Flat(IncomeKind.Employment, 0.1m), Flat(IncomeKind.Employment, 0.2m)])
                  .WithCondition(condition)
                  .Generate();
    }

    [Fact]
    public void The_amount_is_the_sum_of_the_components_and_the_lines_come_in_order() {
        FlatRateRule tenPercent = Flat(IncomeKind.Employment, 0.1m, "TEN_PERCENT");
        FlatRateRule twentyPercent = Flat(IncomeKind.Employment, 0.2m, "TWENTY_PERCENT");
        CompositeRule rule = Any.CompositeRule().WithComponents([tenPercent, twentyPercent]).WithCondition(null).Generate();

        BreakdownLine line = rule.Evaluate(TaxpayerEarning(Salary(1_000m)), 2020);

        Assert.Equal(M(300m), line.Amount);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Null(line.TaxableBase);
        Assert.Equal(["TEN_PERCENT", "TWENTY_PERCENT"], line.Children.Select(c => c.Code.Value));
        Assert.Equal([M(100m), M(200m)], line.Children.Select(c => c.Amount));
    }

    [Fact]
    public void Each_component_rounds_its_own_amount_before_the_sum() {
        // 0.50 x 5 % rounds to 0.03 in each: the sum is 0.06, not the 0.05 of rounding 0.025 + 0.025 once.
        CompositeRule rule = Any.CompositeRule()
                                .WithComponents([Flat(IncomeKind.Employment, 0.05m), Flat(IncomeKind.Employment, 0.05m)])
                                .WithCondition(null)
                                .Generate();

        Assert.Equal(M(0.06m), rule.Evaluate(TaxpayerEarning(Salary(0.50m)), 2020).Amount);
    }

    [Fact]
    public void A_component_that_does_not_apply_adds_nothing_but_stays_in_the_lines() {
        FlatRateRule notApplying = Any.FlatRateRule().WithTaxBase(IncomeKind.Employment).WithRate(R(0.5m)).WithCondition(IncomeAboveNothing()).Generate();
        CompositeRule rule = Any.CompositeRule().WithComponents([Flat(IncomeKind.Investment, 0.1m), notApplying]).WithCondition(null).Generate();

        BreakdownLine line = rule.Evaluate(TaxpayerEarning(), 2020);

        Assert.True(line.Amount.IsZero);
        Assert.Equal([LineOutcome.Applied, LineOutcome.NotApplicable], line.Children.Select(c => c.Outcome));
    }

    [Fact]
    public void A_composite_whose_condition_fails_produces_nothing() {
        CompositeRule rule = SumOfTenAndTwentyPercentOnSalary(IncomeAboveNothing());

        BreakdownLine line = rule.Evaluate(TaxpayerEarning(), 2020);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.Empty(line.Children);
        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void The_components_of_a_composite_are_its_children() {
        FlatRateRule first = Flat(IncomeKind.All, 0.1m);
        FlatRateRule second = Flat(IncomeKind.All, 0.2m);

        CompositeRule rule = Any.CompositeRule().WithComponents([first, second]).Generate();

        Assert.Equal(new TaxRule[] { first, second }, rule.Children);
    }

    [Fact]
    public void Two_components_is_the_smallest_composite_and_sixteen_the_largest() {
        Assert.Equal(16, CompositeRule.MaxComponents);
        Assert.Equal(2, Any.CompositeRule().WithComponents(DistinctFlatRules(2)).Generate().Children.Count);
        Assert.Equal(16, Any.CompositeRule().WithComponents(DistinctFlatRules(16)).Generate().Children.Count);
    }

    [Fact]
    public void Fewer_than_two_or_more_than_sixteen_components_are_refused() {
        Assert.Throws<ArgumentException>(() => Any.CompositeRule().WithComponents([]).Generate());
        Assert.Throws<ArgumentException>(() => Any.CompositeRule().WithComponents(DistinctFlatRules(1)).Generate());
        Assert.Throws<ArgumentException>(() => Any.CompositeRule().WithComponents(DistinctFlatRules(17)).Generate());
    }

    [Fact]
    public void A_missing_component_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.CompositeRule().WithComponents([Flat(IncomeKind.All, 0.1m), null!]).Generate());
    }

    [Fact]
    public void A_composite_needs_its_components() {
        Assert.Throws<ArgumentNullException>(() => new CompositeRule(Any.RuleCode().Generate(), "Composite", null!));
    }

    [Fact]
    public void A_credit_cannot_be_a_component() {
        TaxCreditRule credit = Any.TaxCreditRule().WithCondition(null).Generate();

        Assert.Throws<ArgumentException>(() => Any.CompositeRule().WithComponents([Flat(IncomeKind.All, 0.1m), credit]).Generate());
    }

    [Fact]
    public void Two_components_cannot_share_a_code() {
        Assert.Throws<ArgumentException>(() => Any.CompositeRule()
                                                  .WithComponents([Flat(IncomeKind.All, 0.1m, "SAME_CODE"), Flat(IncomeKind.All, 0.2m, "SAME_CODE")])
                                                  .Generate());
    }

    [Fact]
    public void A_component_cannot_share_the_code_of_the_composite_itself() {
        Assert.Throws<ArgumentException>(() => Any.CompositeRule()
                                                  .WithCode(Code("OWN_CODE"))
                                                  .WithComponents([Flat(IncomeKind.All, 0.1m, "OWN_CODE"), Flat(IncomeKind.All, 0.2m)])
                                                  .Generate());
    }

    [Fact]
    public void A_code_cannot_repeat_between_a_component_and_something_nested_in_another() {
        CompositeRule nested = Any.CompositeRule().WithComponents([Flat(IncomeKind.All, 0.1m, "NESTED_CODE"), Flat(IncomeKind.All, 0.2m)]).Generate();

        Assert.Throws<ArgumentException>(() => Any.CompositeRule()
                                                  .WithComponents([nested, Flat(IncomeKind.All, 0.3m, "NESTED_CODE")])
                                                  .Generate());
    }

    [Fact]
    public void A_code_used_by_a_rule_wrapped_in_a_cap_cannot_be_reused_by_a_sibling_component() {
        CappedRule cap = Any.CappedRule().WithInner(Flat(IncomeKind.All, 0.1m, "WRAPPED_CODE")).WithCondition(null).Generate();

        Assert.Throws<ArgumentException>(() => Any.CompositeRule()
                                                  .WithComponents([cap, Flat(IncomeKind.All, 0.3m, "WRAPPED_CODE")])
                                                  .Generate());
    }

    [Fact]
    public void A_composite_can_nest_composites_and_caps_with_distinct_codes() {
        CompositeRule inner = Any.CompositeRule().WithComponents([Flat(IncomeKind.All, 0.1m), Flat(IncomeKind.All, 0.2m)]).Generate();
        CappedRule cap = Any.CappedRule().WithInner(Flat(IncomeKind.All, 0.3m)).WithCondition(null).Generate();

        CompositeRule outer = Any.CompositeRule().WithComponents([inner, cap]).Generate();

        Assert.Equal(6, outer.SelfAndDescendants().Count());
    }

    private static List<TaxRule> DistinctFlatRules(int count) {
        return Any.SetOf(Any.RuleCode()).WithCount(count).Generate()
                  .Select(code => (TaxRule)Any.FlatRateRule().WithCode(code).Generate())
                  .ToList();
    }

}

public sealed class TaxCreditRuleTests {

    [Theory]
    [InlineData(1, "300.00")]
    [InlineData(2, "400.00")]
    [InlineData(4, "600.00")]
    [InlineData(20, "2200.00")]
    public void The_credit_is_the_base_amount_plus_the_amount_per_member_beyond_the_taxpayer(int householdSize, string expected) {
        TaxCreditRule rule = Any.TaxCreditRule().WithBaseAmount(M(300m)).WithPerAdditionalMember(M(100m)).WithCondition(null).Generate();
        Taxpayer taxpayer = Any.Taxpayer().WithHouseholdSize(householdSize).Generate();

        BreakdownLine line = rule.Evaluate(taxpayer, 2020);

        Assert.Equal(M(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)), line.Amount);
        Assert.Equal(RuleEffect.Credit, line.Effect);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Null(line.TaxableBase);
        Assert.Empty(line.Children);
    }

    [Fact]
    public void A_credit_with_only_a_per_member_amount_grants_nothing_to_a_single_taxpayer() {
        TaxCreditRule rule = Any.TaxCreditRule().WithBaseAmount(Money.Zero).WithPerAdditionalMember(M(50m)).WithCondition(null).Generate();

        Assert.True(rule.Evaluate(Any.Taxpayer().WithHouseholdSize(1).Generate(), 2020).Amount.IsZero);
        Assert.Equal(M(150m), rule.Evaluate(Any.Taxpayer().WithHouseholdSize(4).Generate(), 2020).Amount);
    }

    [Fact]
    public void A_credit_with_only_a_base_amount_ignores_the_household() {
        TaxCreditRule rule = Any.TaxCreditRule().WithBaseAmount(M(300m)).WithPerAdditionalMember(Money.Zero).WithCondition(null).Generate();

        Assert.Equal(M(300m), rule.Evaluate(Any.Taxpayer().WithHouseholdSize(1).Generate(), 2020).Amount);
        Assert.Equal(M(300m), rule.Evaluate(Any.Taxpayer().WithHouseholdSize(20).Generate(), 2020).Amount);
    }

    [Fact]
    public void A_credit_granting_nothing_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.TaxCreditRule().WithBaseAmount(Money.Zero).WithPerAdditionalMember(Money.Zero).Generate());
    }

    [Fact]
    public void A_credit_exposes_what_it_grants() {
        TaxCreditRule rule = Any.TaxCreditRule().WithBaseAmount(M(300m)).WithPerAdditionalMember(M(100m)).Generate();

        Assert.Equal(M(300m), rule.BaseAmount);
        Assert.Equal(M(100m), rule.PerAdditionalMember);
        Assert.Equal(RuleEffect.Credit, rule.Effect);
        Assert.Empty(rule.Children);
    }

    [Fact]
    public void A_credit_needs_its_amounts() {
        Assert.Throws<ArgumentNullException>(() => new TaxCreditRule(Any.RuleCode().Generate(), "Credit", null!, Money.Zero));
        Assert.Throws<ArgumentNullException>(() => new TaxCreditRule(Any.RuleCode().Generate(), "Credit", Money.Zero, null!));
    }

}
