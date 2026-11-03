using JustDummies;

using Xunit;

namespace TaxRules.Domain.Tests;

public sealed class CappedRuleTests {

    // The wrapped rule: 20 % of employment income.
    private static FlatRateRule Inner() {
        return Rules.Flat("INNER_TAX", "0.2");
    }

    private static CappedRule Cap(string maximum, string? minimum = null, ICondition? condition = null, TaxRule? inner = null, string code = "CAPPED_TAX") {
        return new CappedRule(Lit.C(code), "Capped tax", inner ?? Inner(), Lit.M(maximum), minimum is null ? null : Lit.M(minimum), condition);
    }

    [Fact]
    public void An_inner_amount_above_the_maximum_is_capped() {
        BreakdownLine line = Cap("5000").Evaluate(Rules.Earning("40000"), Rules.AnyYear);

        Assert.Equal(LineOutcome.CappedAtMaximum, line.Outcome);
        Assert.Equal(Lit.M("5000"), line.Amount);
    }

    [Fact]
    public void An_inner_amount_equal_to_the_maximum_is_not_capped() {
        BreakdownLine line = Cap("8000").Evaluate(Rules.Earning("40000"), Rules.AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(Lit.M("8000"), line.Amount);
    }

    [Fact]
    public void An_inner_amount_between_the_bounds_passes_through() {
        BreakdownLine line = Cap("5000", "10").Evaluate(Rules.Earning("1000"), Rules.AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(Lit.M("200"), line.Amount);
    }

    [Fact]
    public void A_positive_inner_amount_below_the_minimum_is_raised_to_it() {
        BreakdownLine line = Cap("5000", "50").Evaluate(Rules.Earning("100"), Rules.AnyYear);

        Assert.Equal(LineOutcome.RaisedToMinimum, line.Outcome);
        Assert.Equal(Lit.M("50"), line.Amount);
    }

    [Fact]
    public void An_inner_amount_equal_to_the_minimum_is_not_raised() {
        BreakdownLine line = Cap("5000", "20").Evaluate(Rules.Earning("100"), Rules.AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(Lit.M("20"), line.Amount);
    }

    [Fact]
    public void A_zero_inner_amount_stays_zero_even_with_a_minimum() {
        BreakdownLine line = Cap("5000", "50").Evaluate(Rules.Earning("0"), Rules.AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void An_inner_rule_that_does_not_apply_gives_zero_even_with_a_minimum() {
        FlatRateRule inner = Rules.Flat("INNER_TAX", "0.2", new AgeAtLeastCondition(120));

        BreakdownLine line = Cap("5000", "50", inner: inner).Evaluate(Rules.Earning("10000"), Rules.AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.True(line.Amount.IsZero);
        Assert.Equal(LineOutcome.NotApplicable, Assert.Single(line.Children).Outcome);
    }

    [Fact]
    public void The_default_minimum_is_zero() {
        Assert.True(Cap("5000").Minimum.IsZero);
    }

    [Fact]
    public void The_line_carries_the_inner_line_and_no_base() {
        // Arrange
        CappedRule rule = Cap("5000");

        // Act
        BreakdownLine line = rule.Evaluate(Rules.Earning("40000"), Rules.AnyYear);

        // Assert
        Assert.Null(line.TaxableBase);
        BreakdownLine inner = Assert.Single(line.Children);
        Assert.Equal(Lit.C("INNER_TAX"), inner.Code);
        Assert.Equal(Lit.M("8000"), inner.Amount);
        Assert.Equal(RuleEffect.Tax, line.Effect);
        Assert.Same(rule.Inner, Assert.Single(rule.Children));
    }

    [Fact]
    public void A_condition_that_fails_leaves_the_cap_unapplied_without_evaluating_the_inner_rule() {
        CappedRule rule = Cap("5000", condition: new AgeAtLeastCondition(120));

        BreakdownLine line = rule.Evaluate(Rules.Earning("40000"), Rules.AnyYear);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.Empty(line.Children);
    }

    [Fact]
    public void A_cap_cannot_wrap_another_cap() {
        CappedRule innerCap = Cap("5000");

        Assert.Throws<ArgumentException>(() => Cap("1000", inner: innerCap, code: "OUTER_CAP"));
    }

    [Fact]
    public void A_cap_may_wrap_a_composite_that_contains_a_cap() {
        CappedRule inside    = Cap("5000", code: "INSIDE_CAP", inner: Rules.Flat("FLAT_A", "0.1"));
        CompositeRule composite = new CompositeRule(Lit.C("COMBO"), "Combo", [inside, Rules.Flat("FLAT_B", "0.1")]);

        CappedRule outer = Cap("9000", inner: composite, code: "OUTER_CAP");

        Assert.Same(composite, outer.Inner);
    }

    [Fact]
    public void A_credit_cannot_be_capped() {
        TaxCreditRule credit = Rules.Credit("FAMILY", "100", "50");

        Assert.Throws<ArgumentException>(() => Cap("5000", inner: credit));
    }

    [Fact]
    public void A_cap_cannot_reuse_the_code_of_the_wrapped_rule() {
        Assert.Throws<ArgumentException>(() => Cap("5000", code: "INNER_TAX"));
    }

    [Fact]
    public void A_cap_cannot_reuse_a_code_deeper_in_the_wrapped_tree() {
        CompositeRule composite = new CompositeRule(Lit.C("COMBO"), "Combo", [Rules.Flat("FLAT_A", "0.1"), Rules.Flat("FLAT_B", "0.1")]);

        Assert.Throws<ArgumentException>(() => Cap("5000", inner: composite, code: "FLAT_B"));
    }

    [Fact]
    public void A_zero_maximum_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cap("0"));
    }

    [Fact]
    public void The_smallest_maximum_is_one_cent() {
        Assert.Equal(Lit.M("0.01"), Cap("0.01").Maximum);
    }

    [Fact]
    public void A_minimum_above_the_maximum_is_refused() {
        Assert.Throws<ArgumentException>(() => Cap("100", "100.01"));
    }

    [Fact]
    public void A_minimum_equal_to_the_maximum_is_accepted_and_pins_every_positive_amount() {
        CappedRule rule = Cap("100", "100");

        Assert.Equal(Lit.M("100"), rule.Evaluate(Rules.Earning("1"), Rules.AnyYear).Amount);
        Assert.Equal(Lit.M("100"), rule.Evaluate(Rules.Earning("1000000"), Rules.AnyYear).Amount);
    }

    [Fact]
    public void Null_collaborators_are_refused() {
        Assert.Throws<ArgumentNullException>(() => new CappedRule(Lit.C("CAPPED_TAX"), "Capped", null!, Lit.M("1")));
        Assert.Throws<ArgumentNullException>(() => new CappedRule(Lit.C("CAPPED_TAX"), "Capped", Inner(), null!));
        Assert.Throws<ArgumentNullException>(() => new CappedRule(null!, "Capped", Inner(), Lit.M("1")));
    }

    [Fact]
    public void A_blank_label_is_refused() {
        Assert.Throws<ArgumentException>(() => new CappedRule(Lit.C("CAPPED_TAX"), " ", Inner(), Lit.M("1")));
    }

}

public sealed class CompositeRuleTests {

    private static CompositeRule Composite(string code, params TaxRule[] components) {
        return new CompositeRule(Lit.C(code), "Composite", components);
    }

    [Fact]
    public void The_amount_is_the_sum_of_the_components() {
        // Arrange
        CompositeRule rule = Composite("SOCIAL", Rules.Flat("PENSION", "0.1"), Rules.Flat("HEALTH", "0.05"), Rules.Flat("UNEMP", "0.01"));

        // Act
        BreakdownLine line = rule.Evaluate(Rules.Earning("10000"), Rules.AnyYear);

        // Assert
        Assert.Equal(Lit.M("1600"), line.Amount);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Null(line.TaxableBase);
        Assert.Equal(["PENSION", "HEALTH", "UNEMP"], line.Children.Select(c => c.Code.Value).ToList());
        Assert.Equal([Lit.M("1000"), Lit.M("500"), Lit.M("100")], line.Children.Select(c => c.Amount).ToList());
    }

    [Fact]
    public void The_components_are_rounded_each_before_they_are_summed() {
        // Each flat rule owes 0.005 on 0.05 income at 10 %: rounded one by one 0.01 + 0.01.
        CompositeRule rule = Composite("TWO_SMALL", Rules.Flat("PART_A", "0.1"), Rules.Flat("PART_B", "0.1"));

        BreakdownLine line = rule.Evaluate(Rules.Earning("0.05"), Rules.AnyYear);

        Assert.Equal(Lit.M("0.02"), line.Amount);
    }

    [Fact]
    public void A_component_that_does_not_apply_contributes_zero_but_is_listed() {
        CompositeRule rule = Composite("SOCIAL", Rules.Flat("PENSION", "0.1"), Rules.Flat("SENIOR", "0.5", new AgeAtLeastCondition(120)));

        BreakdownLine line = rule.Evaluate(Rules.Earning("1000"), Rules.AnyYear);

        Assert.Equal(Lit.M("100"), line.Amount);
        Assert.Equal(LineOutcome.NotApplicable, line.Children[1].Outcome);
    }

    [Fact]
    public void A_condition_that_fails_leaves_the_composite_unapplied() {
        CompositeRule rule = new CompositeRule(Lit.C("SOCIAL"), "Social", [Rules.Flat("PENSION", "0.1"), Rules.Flat("HEALTH", "0.1")], new AgeAtLeastCondition(120));

        BreakdownLine line = rule.Evaluate(Rules.Earning("1000"), Rules.AnyYear);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.Empty(line.Children);
    }

    [Fact]
    public void Composites_nest() {
        // Arrange
        CompositeRule inner = Composite("INNER", Rules.Flat("PART_B", "0.1"), Rules.Flat("PART_C", "0.2"));
        CompositeRule outer = Composite("OUTER", Rules.Flat("PART_A", "0.1"), inner);

        // Act
        BreakdownLine line = outer.Evaluate(Rules.Earning("1000"), Rules.AnyYear);

        // Assert
        Assert.Equal(Lit.M("400"), line.Amount);
        Assert.Equal(Lit.M("300"), line.Children[1].Amount);
        Assert.Equal(Lit.M("200"), line.Find(Lit.C("PART_C"))!.Amount);
    }

    [Fact]
    public void The_children_are_the_components_in_order() {
        FlatRateRule a = Rules.Flat("PART_A", "0.1");
        FlatRateRule b = Rules.Flat("PART_B", "0.1");

        CompositeRule rule = Composite("COMBO", a, b);

        Assert.Equal([a, b], rule.Children);
        Assert.Equal(RuleEffect.Tax, rule.Effect);
    }

    [Fact]
    public void Two_to_sixteen_components_are_accepted() {
        // Arrange
        IReadOnlyList<RuleCode> codes = Dummies.DistinctCodes(18);
        List<TaxRule> sixteen = codes.Skip(1).Take(16).Select(c => (TaxRule)new AnyFlatRateRule().WithCode(c).Generate()).ToList();

        // Act
        CompositeRule small = new CompositeRule(codes[0], "Small", sixteen.Take(2));
        CompositeRule large = new CompositeRule(codes[17], "Large", sixteen);

        // Assert
        Assert.Equal(2, small.Children.Count);
        Assert.Equal(16, large.Children.Count);
    }

    [Fact]
    public void One_component_or_seventeen_are_refused() {
        // Arrange
        IReadOnlyList<RuleCode> codes = Dummies.DistinctCodes(18);
        List<TaxRule> seventeen = codes.Skip(1).Select(c => (TaxRule)new AnyFlatRateRule().WithCode(c).Generate()).ToList();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Composite("SINGLE", Rules.Flat("ONLY_ONE", "0.1")));
        Assert.Throws<ArgumentException>(() => Composite("NONE"));
        Assert.Equal(17, seventeen.Count);
        Assert.Throws<ArgumentException>(() => new CompositeRule(codes[0], "Too many", seventeen));
    }

    [Fact]
    public void A_credit_component_is_refused() {
        Assert.Throws<ArgumentException>(() => Composite("MIXED", Rules.Flat("PART_A", "0.1"), Rules.Credit("FAMILY", "10", "0")));
    }

    [Fact]
    public void A_null_component_or_list_is_refused() {
        Assert.Throws<ArgumentException>(() => Composite("HAS_NULL", Rules.Flat("PART_A", "0.1"), null!));
        Assert.Throws<ArgumentNullException>(() => new CompositeRule(Lit.C("NO_LIST"), "No list", null!));
    }

    [Fact]
    public void A_component_cannot_reuse_the_composite_own_code() {
        Assert.Throws<ArgumentException>(() => Composite("PART_A", Rules.Flat("PART_A", "0.1"), Rules.Flat("PART_B", "0.1")));
    }

    [Fact]
    public void Two_components_cannot_share_a_code() {
        Assert.Throws<ArgumentException>(() => Composite("COMBO", Rules.Flat("PART_A", "0.1"), Rules.Flat("PART_A", "0.2")));
    }

    [Fact]
    public void A_code_cannot_appear_twice_at_different_depths() {
        CompositeRule inner = Composite("INNER", Rules.Flat("PART_B", "0.1"), Rules.Flat("PART_C", "0.1"));

        Assert.Throws<ArgumentException>(() => Composite("OUTER", Rules.Flat("PART_B", "0.1"), inner));
        Assert.Throws<ArgumentException>(() => Composite("OUTER", inner, Rules.Flat("PART_C", "0.1")));
        Assert.Throws<ArgumentException>(() => Composite("INNER", inner, Rules.Flat("PART_A", "0.1")));
    }

    [Fact]
    public void The_same_rule_cannot_be_a_component_twice() {
        FlatRateRule shared = Rules.Flat("SHARED", "0.1");

        Assert.Throws<ArgumentException>(() => Composite("COMBO", shared, shared));
    }

    [Fact]
    public void A_cap_inside_a_composite_is_listed_with_its_own_children() {
        CappedRule cap = new CappedRule(Lit.C("CAPPED"), "Capped", Rules.Flat("INNER_TAX", "0.5"), Lit.M("100"));

        CompositeRule rule = Composite("COMBO", cap, Rules.Flat("OTHER", "0.1"));
        BreakdownLine line = rule.Evaluate(Rules.Earning("1000"), Rules.AnyYear);

        Assert.Equal(Lit.M("200"), line.Amount);
        Assert.Equal(LineOutcome.CappedAtMaximum, line.Children[0].Outcome);
        Assert.Equal(Lit.M("500"), line.Find(Lit.C("INNER_TAX"))!.Amount);
    }

}
