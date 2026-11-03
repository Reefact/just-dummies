using JustDummies;

using TaxRules.Domain;

namespace TaxRules.Tests.Discovery;

public sealed class CappedRuleTests {

    private const int AnyYear = 2024;

    // 10 % of 1 000 is 100.
    private static FlatRateRule TenPercent(string code = "INNER_TAX") => Fixtures.FlatOn(IncomeKind.Employment, 0.10m, code);

    private static CappedRule Cap(TaxRule inner, decimal maximum, decimal? minimum = null, string code = "CAPPED_TAX") {
        return new CappedRule(Fixtures.Code(code), AnyLabel.Create().Generate(), inner, new Money(maximum), minimum is null ? null : new Money((decimal)minimum));
    }

    [Fact]
    public void An_amount_above_the_maximum_is_capped() {
        BreakdownLine line = Cap(TenPercent(), 80m).Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear);

        Assert.Equal(LineOutcome.CappedAtMaximum, line.Outcome);
        Assert.Equal(new Money(80m), line.Amount);
    }

    [Fact]
    public void An_amount_equal_to_the_maximum_is_left_alone() {
        BreakdownLine line = Cap(TenPercent(), 100m).Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(new Money(100m), line.Amount);
    }

    [Fact]
    public void An_amount_one_cent_above_the_maximum_is_capped() {
        BreakdownLine line = Cap(TenPercent(), 99.99m).Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear);

        Assert.Equal(LineOutcome.CappedAtMaximum, line.Outcome);
        Assert.Equal(new Money(99.99m), line.Amount);
    }

    [Fact]
    public void An_amount_below_the_maximum_is_left_alone() {
        BreakdownLine line = Cap(TenPercent(), 500m).Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(new Money(100m), line.Amount);
    }

    [Fact]
    public void A_positive_amount_below_the_minimum_is_raised_to_it() {
        BreakdownLine line = Cap(TenPercent(), 500m, minimum: 150m).Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear);

        Assert.Equal(LineOutcome.RaisedToMinimum, line.Outcome);
        Assert.Equal(new Money(150m), line.Amount);
    }

    [Fact]
    public void An_amount_equal_to_the_minimum_is_left_alone() {
        BreakdownLine line = Cap(TenPercent(), 500m, minimum: 100m).Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(new Money(100m), line.Amount);
    }

    [Fact]
    public void A_zero_amount_stays_zero_whatever_the_minimum() {
        BreakdownLine line = Cap(TenPercent(), 500m, minimum: 150m).Evaluate(Fixtures.EarningTaxpayer(), AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void A_wrapped_rule_that_does_not_apply_gives_zero_and_is_not_raised() {
        FlatRateRule inner = Any.FlatRateRule()
                                .WithTaxBase(IncomeKind.Employment)
                                .WithRate(new Rate(0.1m))
                                .WithCondition(new HouseholdSizeAtLeastCondition(5))
                                .Generate();
        Taxpayer taxpayer = Any.Taxpayer().WithHouseholdSize(1).WithIncomes(new IncomeComponent[] { Fixtures.Salary(1_000m) }).Generate();

        BreakdownLine line = Cap(inner, 500m, minimum: 150m).Evaluate(taxpayer, AnyYear);

        Assert.True(line.Amount.IsZero);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(LineOutcome.NotApplicable, line.Children.Single().Outcome);
    }

    [Fact]
    public void A_minimum_equal_to_the_maximum_makes_the_amount_that_value_whenever_there_is_one() {
        CappedRule rule = Cap(TenPercent(), 120m, minimum: 120m);

        Assert.Equal(new Money(120m), rule.Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear).Amount);
        Assert.Equal(new Money(120m), rule.Evaluate(Fixtures.TaxpayerEarning(5_000m), AnyYear).Amount);
        Assert.True(rule.Evaluate(Fixtures.EarningTaxpayer(), AnyYear).Amount.IsZero);
    }

    [Fact]
    public void The_line_nests_the_line_of_the_wrapped_rule_and_has_no_base_of_its_own() {
        // Arrange
        CappedRule rule = Cap(TenPercent("INNER_TAX"), 80m, code: "CAPPED_TAX");

        // Act
        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear);

        // Assert
        Assert.Null(line.TaxableBase);
        BreakdownLine inner = Assert.Single(line.Children);
        Assert.Equal(Fixtures.Code("INNER_TAX"), inner.Code);
        Assert.Equal(new Money(100m), inner.Amount);
        Assert.Equal(new Money(1_000m), inner.TaxableBase);
        Assert.Same(inner, line.Find(Fixtures.Code("INNER_TAX")));
    }

    [Fact]
    public void The_minimum_defaults_to_zero() {
        Assert.True(Cap(TenPercent(), 500m).Minimum.IsZero);
    }

    [Fact]
    public void The_rule_exposes_its_wrapped_rule_as_its_only_child() {
        FlatRateRule inner = TenPercent();
        CappedRule   rule  = Cap(inner, 500m);

        Assert.Same(inner, rule.Inner);
        Assert.Equal([inner], rule.Children);
        Assert.Equal([rule, inner], rule.SelfAndDescendants());
        Assert.Equal(RuleEffect.Tax, rule.Effect);
    }

    [Fact]
    public void A_condition_that_fails_makes_the_cap_not_applicable_without_evaluating_its_inner_rule() {
        CappedRule rule = new CappedRule(Fixtures.Code("CAPPED_TAX"), AnyLabel.Create().Generate(), TenPercent(), new Money(500m), null, new AgeAtLeastCondition(65));
        Taxpayer   young = Any.Taxpayer().WithDateOfBirth(new DateOnly(1990, 1, 1)).WithIncomes(new IncomeComponent[] { Fixtures.Salary(1_000m) }).Generate();

        BreakdownLine line = rule.Evaluate(young, AnyYear);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.Empty(line.Children);
        Assert.Null(line.TaxableBase);
    }

    [Fact]
    public void A_cap_cannot_wrap_another_cap() {
        CappedRule inner = Cap(TenPercent(), 500m, code: "INNER_CAP");

        Assert.Throws<ArgumentException>(() => Cap(inner, 400m, code: "OUTER_CAP"));
    }

    [Fact]
    public void A_credit_cannot_be_capped() {
        TaxCreditRule credit = Fixtures.Credit("SOME_CREDIT", 100m);

        Assert.Throws<ArgumentException>(() => Cap(credit, 500m));
    }

    [Fact]
    public void A_cap_cannot_reuse_the_code_of_its_wrapped_rule() {
        Assert.Throws<ArgumentException>(() => Cap(TenPercent("SAME_CODE"), 500m, code: "SAME_CODE"));
    }

    [Fact]
    public void A_cap_cannot_reuse_a_code_deeper_in_the_wrapped_tree() {
        CompositeRule composite = new CompositeRule(
            Fixtures.Code("SUM_TAX"),
            AnyLabel.Create().Generate(),
            [TenPercent("FIRST_TAX"), Fixtures.FlatOn(IncomeKind.Investment, 0.1m, "SECOND_TAX")]);

        Assert.Throws<ArgumentException>(() => Cap(composite, 500m, code: "SECOND_TAX"));
        Assert.Equal(4, Cap(composite, 500m, code: "OTHER_TAX").SelfAndDescendants().Count());
    }

    [Fact]
    public void A_cap_may_wrap_a_composite_of_taxes() {
        CompositeRule composite = new CompositeRule(
            Fixtures.Code("SUM_TAX"),
            AnyLabel.Create().Generate(),
            [TenPercent("FIRST_TAX"), Fixtures.FlatOn(IncomeKind.Investment, 0.2m, "SECOND_TAX")]);
        Taxpayer taxpayer = Fixtures.EarningTaxpayer(Fixtures.Salary(1_000m), Fixtures.Interest(1_000m));

        BreakdownLine line = Cap(composite, 250m, code: "CAPPED_SUM").Evaluate(taxpayer, AnyYear);

        // 100 + 200 = 300, capped at 250.
        Assert.Equal(LineOutcome.CappedAtMaximum, line.Outcome);
        Assert.Equal(new Money(250m), line.Amount);
        Assert.Equal(new Money(300m), line.Children.Single().Amount);
    }

    [Fact]
    public void A_zero_maximum_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cap(TenPercent(), 0m));
    }

    [Fact]
    public void A_minimum_above_the_maximum_is_refused() {
        Assert.Throws<ArgumentException>(() => Cap(TenPercent(), 100m, minimum: 100.01m));
    }

    [Fact]
    public void A_missing_inner_rule_or_maximum_is_refused() {
        RuleCode anyCode  = Any.RuleCode().Generate();
        string   anyLabel = AnyLabel.Create().Generate();

        Assert.Throws<ArgumentNullException>(() => new CappedRule(anyCode, anyLabel, null!, new Money(1m)));
        Assert.Throws<ArgumentNullException>(() => new CappedRule(anyCode, anyLabel, TenPercent(), null!));
    }

    [Fact]
    public void A_generated_cap_is_always_constructible() {
        for (int i = 0; i < 100; i++) {
            CappedRule anyCap = Any.CappedRule().Generate();

            Assert.True(anyCap.Minimum.CompareTo(anyCap.Maximum) <= 0);
            Assert.DoesNotContain(anyCap.Inner.SelfAndDescendants(), r => r.Code.Equals(anyCap.Code));
        }
    }

}

public sealed class CompositeRuleTests {

    private const int AnyYear = 2024;

    private static FlatRateRule Flat(string code, IncomeKind taxBase, decimal rate) => Fixtures.FlatOn(taxBase, rate, code);

    private static CompositeRule Composite(string code, params TaxRule[] components) {
        return new CompositeRule(Fixtures.Code(code), AnyLabel.Create().Generate(), components);
    }

    [Fact]
    public void The_amount_is_the_sum_of_the_component_amounts() {
        // Arrange
        CompositeRule rule     = Composite("SUM_TAX", Flat("EMPLOY_TAX", IncomeKind.Employment, 0.10m), Flat("INVEST_TAX", IncomeKind.Investment, 0.20m));
        Taxpayer      taxpayer = Fixtures.EarningTaxpayer(Fixtures.Salary(1_000m), Fixtures.Interest(500m));

        // Act
        BreakdownLine line = rule.Evaluate(taxpayer, AnyYear);

        // Assert
        Assert.Equal(new Money(200m), line.Amount);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Null(line.TaxableBase);
        Assert.Equal(RuleEffect.Tax, line.Effect);
    }

    [Fact]
    public void The_line_nests_one_line_per_component_in_order() {
        CompositeRule rule = Composite("SUM_TAX", Flat("FIRST_TAX", IncomeKind.Employment, 0.1m), Flat("SECOND_TAX", IncomeKind.Employment, 0.2m), Flat("THIRD_TAX", IncomeKind.Employment, 0.3m));

        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(100m), AnyYear);

        Assert.Equal(
            [Fixtures.Code("FIRST_TAX"), Fixtures.Code("SECOND_TAX"), Fixtures.Code("THIRD_TAX")],
            line.Children.Select(c => c.Code));
        Assert.Equal([new Money(10m), new Money(20m), new Money(30m)], line.Children.Select(c => c.Amount));
        Assert.Equal(new Money(60m), line.Amount);
    }

    [Fact]
    public void Each_component_is_rounded_before_the_sum() {
        // 0.05 * 0.1 = 0.005 -> 0.01 each; two of them make 0.02 (unrounded sum 0.01).
        CompositeRule rule = Composite("SUM_TAX", Flat("FIRST_TAX", IncomeKind.Employment, 0.1m), Flat("SECOND_TAX", IncomeKind.Employment, 0.1m));

        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(0.05m), AnyYear);

        Assert.Equal(new Money(0.02m), line.Amount);
    }

    [Fact]
    public void A_component_that_does_not_apply_contributes_nothing() {
        FlatRateRule conditional = Any.FlatRateRule()
                                      .WithCode(Fixtures.Code("COND_TAX"))
                                      .WithTaxBase(IncomeKind.Employment)
                                      .WithRate(new Rate(0.5m))
                                      .WithCondition(new HouseholdSizeAtLeastCondition(10))
                                      .Generate();
        CompositeRule rule = Composite("SUM_TAX", conditional, Flat("PLAIN_TAX", IncomeKind.Employment, 0.1m));
        Taxpayer taxpayer = Any.Taxpayer().WithHouseholdSize(2).WithIncomes(new IncomeComponent[] { Fixtures.Salary(1_000m) }).Generate();

        BreakdownLine line = rule.Evaluate(taxpayer, AnyYear);

        Assert.Equal(new Money(100m), line.Amount);
        Assert.Equal(LineOutcome.NotApplicable, line.Find(Fixtures.Code("COND_TAX"))!.Outcome);
    }

    [Fact]
    public void A_condition_that_fails_makes_the_composite_not_applicable_without_nested_lines() {
        CompositeRule rule = new CompositeRule(
            Fixtures.Code("SUM_TAX"),
            AnyLabel.Create().Generate(),
            [Flat("FIRST_TAX", IncomeKind.All, 0.1m), Flat("SECOND_TAX", IncomeKind.All, 0.1m)],
            new HouseholdSizeAtLeastCondition(10));

        BreakdownLine line = rule.Evaluate(Any.Taxpayer().WithHouseholdSize(2).Generate(), AnyYear);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.Empty(line.Children);
        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void The_rule_exposes_its_components_as_children_and_walks_its_tree_depth_first() {
        FlatRateRule first  = Flat("FIRST_TAX", IncomeKind.All, 0.1m);
        FlatRateRule second = Flat("SECOND_TAX", IncomeKind.All, 0.1m);
        CompositeRule inner = Composite("INNER_SUM", first, second);
        FlatRateRule third = Flat("THIRD_TAX", IncomeKind.All, 0.1m);
        CompositeRule outer = Composite("OUTER_SUM", inner, third);

        Assert.Equal([inner, third], outer.Children);
        Assert.Equal([outer, inner, first, second, third], outer.SelfAndDescendants());
    }

    [Fact]
    public void Two_to_sixteen_components_are_allowed() {
        Assert.Equal(16, CompositeRule.MaxComponents);
        Assert.Equal(2, Composite("TWO_TAX", Flat("AAA", IncomeKind.All, 0.1m), Flat("BBB", IncomeKind.All, 0.1m)).Children.Count);
        Assert.Equal(16, new CompositeRule(Fixtures.Code("MANY_TAX"), AnyLabel.Create().Generate(), ComponentsNamed(16)).Children.Count);
    }

    [Fact]
    public void Fewer_than_two_or_more_than_sixteen_components_are_refused() {
        Assert.Throws<ArgumentException>(() => new CompositeRule(Fixtures.Code("NONE_TAX"), AnyLabel.Create().Generate(), ComponentsNamed(0)));
        Assert.Throws<ArgumentException>(() => new CompositeRule(Fixtures.Code("ONE_TAX"), AnyLabel.Create().Generate(), ComponentsNamed(1)));
        Assert.Throws<ArgumentException>(() => new CompositeRule(Fixtures.Code("TOO_MANY"), AnyLabel.Create().Generate(), ComponentsNamed(17)));
    }

    private static List<TaxRule> ComponentsNamed(int count) {
        return Enumerable.Range(0, count).Select(i => (TaxRule)Flat($"PART_{i}", IncomeKind.All, 0.01m)).ToList();
    }

    [Fact]
    public void A_null_component_is_refused() {
        Assert.Throws<ArgumentException>(() => Composite("SUM_TAX", Flat("FIRST_TAX", IncomeKind.All, 0.1m), null!));
    }

    [Fact]
    public void A_missing_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CompositeRule(Fixtures.Code("SUM_TAX"), AnyLabel.Create().Generate(), null!));
    }

    [Fact]
    public void A_credit_cannot_be_a_component() {
        Assert.Throws<ArgumentException>(() => Composite("SUM_TAX", Flat("FIRST_TAX", IncomeKind.All, 0.1m), Fixtures.Credit("SOME_CREDIT", 10m)));
    }

    [Fact]
    public void The_same_code_cannot_appear_twice_among_the_components() {
        Assert.Throws<ArgumentException>(() => Composite("SUM_TAX", Flat("SAME_CODE", IncomeKind.All, 0.1m), Flat("SAME_CODE", IncomeKind.Employment, 0.2m)));
    }

    [Fact]
    public void A_composite_cannot_reuse_its_own_code_for_a_component() {
        Assert.Throws<ArgumentException>(() => Composite("SUM_TAX", Flat("SUM_TAX", IncomeKind.All, 0.1m), Flat("OTHER_TAX", IncomeKind.All, 0.1m)));
    }

    [Fact]
    public void A_code_repeated_in_two_nested_trees_is_refused() {
        CompositeRule left  = Composite("LEFT_SUM", Flat("SHARED_TAX", IncomeKind.All, 0.1m), Flat("LEFT_ONLY", IncomeKind.All, 0.1m));
        CompositeRule right = Composite("RIGHT_SUM", Flat("SHARED_TAX", IncomeKind.All, 0.1m), Flat("RIGHT_ONLY", IncomeKind.All, 0.1m));

        Assert.Throws<ArgumentException>(() => Composite("TOP_SUM", left, right));
    }

    [Fact]
    public void A_code_reused_by_a_cap_inside_a_component_is_refused() {
        CappedRule capped = new CappedRule(Fixtures.Code("CAP_ONE"), AnyLabel.Create().Generate(), Flat("INNER_ONE", IncomeKind.All, 0.1m), new Money(10m));

        Assert.Throws<ArgumentException>(() => Composite("TOP_SUM", capped, Flat("INNER_ONE", IncomeKind.All, 0.1m)));
        Assert.Throws<ArgumentException>(() => Composite("TOP_SUM", capped, Flat("CAP_ONE", IncomeKind.All, 0.1m)));
    }

    [Fact]
    public void A_composite_may_contain_a_cap_and_the_cap_applies_inside_the_sum() {
        CappedRule capped = new CappedRule(Fixtures.Code("CAP_ONE"), AnyLabel.Create().Generate(), Flat("INNER_ONE", IncomeKind.Employment, 0.1m), new Money(50m));
        CompositeRule rule = Composite("TOP_SUM", capped, Flat("PLAIN_TAX", IncomeKind.Employment, 0.2m));

        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear);

        // 100 capped at 50, plus 200.
        Assert.Equal(new Money(250m), line.Amount);
        Assert.Equal(LineOutcome.CappedAtMaximum, line.Find(Fixtures.Code("CAP_ONE"))!.Outcome);
    }

    [Fact]
    public void The_components_are_a_copy_of_the_list_that_was_given() {
        List<TaxRule> components = [Flat("FIRST_TAX", IncomeKind.All, 0.1m), Flat("SECOND_TAX", IncomeKind.All, 0.1m)];
        CompositeRule rule       = new CompositeRule(Fixtures.Code("SUM_TAX"), AnyLabel.Create().Generate(), components);

        components.Add(Flat("THIRD_TAX", IncomeKind.All, 0.1m));

        Assert.Equal(2, rule.Children.Count);
    }

    [Fact]
    public void A_generated_composite_is_always_constructible() {
        for (int i = 0; i < 100; i++) {
            CompositeRule anyComposite = Any.CompositeRule().Generate();

            Assert.InRange(anyComposite.Children.Count, 2, CompositeRule.MaxComponents);
        }
    }

}
