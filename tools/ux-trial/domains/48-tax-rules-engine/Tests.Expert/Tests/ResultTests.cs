using JustDummies;

using TaxRules.Domain;

using Xunit;

using static TaxRules.Tests.Build;

namespace TaxRules.Tests;

public sealed class BreakdownLineTests {

    private static AnyRuleCode AnyCode() => Any.RuleCode();

    private static BreakdownLine Line(string code, decimal amount, params BreakdownLine[] children) {
        return new BreakdownLine(Code(code), Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.Applied, null, M(amount), children);
    }

    [Fact]
    public void A_line_keeps_what_it_was_built_from() {
        RuleCode code = AnyCode().Generate();
        BreakdownLine child = TaxLine(1m);

        BreakdownLine line = new BreakdownLine(code, "Income tax", RuleEffect.Tax, LineOutcome.CappedAtMaximum, M(500m), M(50m), [child]);

        Assert.Same(code, line.Code);
        Assert.Equal("Income tax", line.Label);
        Assert.Equal(RuleEffect.Tax, line.Effect);
        Assert.Equal(LineOutcome.CappedAtMaximum, line.Outcome);
        Assert.Equal(M(500m), line.TaxableBase);
        Assert.Equal(M(50m), line.Amount);
        Assert.Same(child, Assert.Single(line.Children));
    }

    [Fact]
    public void A_line_without_children_given_has_none() {
        BreakdownLine line = new BreakdownLine(AnyCode().Generate(), Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.Applied, null, M(1m));

        Assert.Empty(line.Children);
    }

    [Fact]
    public void A_line_that_does_not_apply_may_carry_nothing_but_a_zero_amount() {
        BreakdownLine line = new BreakdownLine(AnyCode().Generate(), Plausible.Label().Generate(), RuleEffect.Credit, LineOutcome.NotApplicable, null, Money.Zero);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
    }

    [Fact]
    public void A_line_that_does_not_apply_cannot_carry_an_amount_a_base_or_children() {
        RuleCode code = AnyCode().Generate();
        string label = Plausible.Label().Generate();

        Assert.Throws<ArgumentException>(() => new BreakdownLine(code, label, RuleEffect.Tax, LineOutcome.NotApplicable, null, M(0.01m)));
        Assert.Throws<ArgumentException>(() => new BreakdownLine(code, label, RuleEffect.Tax, LineOutcome.NotApplicable, Money.Zero, Money.Zero));
        Assert.Throws<ArgumentException>(() => new BreakdownLine(code, label, RuleEffect.Tax, LineOutcome.NotApplicable, null, Money.Zero, [TaxLine(0m)]));
    }

    [Fact]
    public void Undefined_effects_and_outcomes_are_refused() {
        RuleCode code = AnyCode().Generate();
        string label = Plausible.Label().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new BreakdownLine(code, label, (RuleEffect)2, LineOutcome.Applied, null, Money.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BreakdownLine(code, label, RuleEffect.Tax, (LineOutcome)4, null, Money.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BreakdownLine(code, label, RuleEffect.Tax, (LineOutcome)(-1), null, Money.Zero));
    }

    [Fact]
    public void A_line_needs_a_code_and_an_amount() {
        Assert.Throws<ArgumentNullException>(() => new BreakdownLine(null!, Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero));
        Assert.Throws<ArgumentNullException>(() => new BreakdownLine(AnyCode().Generate(), Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.Applied, null, null!));
    }

    [Fact]
    public void A_null_child_is_refused() {
        Assert.Throws<ArgumentException>(() => new BreakdownLine(AnyCode().Generate(), Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero, [TaxLine(1m), null!]));
    }

    [Fact]
    public void The_label_is_held_to_the_same_limits_as_a_rules() {
        RuleCode code = AnyCode().Generate();

        Assert.Equal(80, new BreakdownLine(code, new string('x', 80), RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero).Label.Length);
        Assert.Throws<ArgumentException>(() => new BreakdownLine(code, new string('x', 81), RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero));
        Assert.Throws<ArgumentException>(() => new BreakdownLine(code, "  ", RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero));
    }

    [Fact]
    public void A_line_is_found_by_its_own_code_or_by_the_code_of_a_line_nested_at_any_depth() {
        BreakdownLine leaf = Line("LEAF_RULE", 1m);
        BreakdownLine middle = Line("MIDDLE_RULE", 2m, leaf);
        BreakdownLine sibling = Line("SIBLING_RULE", 3m);
        BreakdownLine root = Line("ROOT_RULE", 6m, middle, sibling);

        Assert.Same(root, root.Find(Code("ROOT_RULE")));
        Assert.Same(middle, root.Find(Code("MIDDLE_RULE")));
        Assert.Same(leaf, root.Find(Code("LEAF_RULE")));
        Assert.Same(sibling, root.Find(Code("SIBLING_RULE")));
    }

    [Fact]
    public void A_code_absent_from_the_subtree_finds_nothing() {
        BreakdownLine root = Line("ROOT_RULE", 1m, Line("LEAF_RULE", 1m));

        Assert.Null(root.Find(Code("OTHER_RULE")));
    }

    [Fact]
    public void Finding_a_missing_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => TaxLine(1m).Find(null!));
    }

    [Fact]
    public void A_line_is_described_by_its_code_outcome_and_amount() {
        BreakdownLine line = new BreakdownLine(Code("INCOME_TAX"), Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.CappedAtMaximum, null, M(50m));

        Assert.Equal("INCOME_TAX CappedAtMaximum: 50.00", line.ToString());
    }

}

public sealed class TaxBreakdownTests {

    private static TaxBreakdown BreakdownOf(decimal income, params BreakdownLine[] lines) {
        return new TaxBreakdown(Any.TaxpayerId().Generate(), Any.JurisdictionCode().Generate(), Any.Int32().Between(2000, 2099).Generate(), M(income), lines);
    }

    [Fact]
    public void The_gross_tax_is_the_sum_of_the_tax_lines_and_the_credits_available_the_sum_of_the_credit_lines() {
        TaxBreakdown breakdown = BreakdownOf(50_000m, TaxLine(1_000m), TaxLine(250.50m), CreditLine(300m), CreditLine(99.99m));

        Assert.Equal(M(1_250.50m), breakdown.GrossTax);
        Assert.Equal(M(399.99m), breakdown.CreditsAvailable);
    }

    [Fact]
    public void Credits_are_set_against_the_tax_and_the_rest_is_the_net_tax() {
        TaxBreakdown breakdown = BreakdownOf(50_000m, TaxLine(1_000m), CreditLine(300m));

        Assert.Equal(M(300m), breakdown.CreditsApplied);
        Assert.Equal(M(700m), breakdown.NetTax);
    }

    [Fact]
    public void Credits_above_the_tax_are_not_refunded() {
        TaxBreakdown breakdown = BreakdownOf(50_000m, TaxLine(100m), CreditLine(300m));

        Assert.Equal(M(300m), breakdown.CreditsAvailable);
        Assert.Equal(M(100m), breakdown.CreditsApplied);
        Assert.True(breakdown.NetTax.IsZero);
    }

    [Fact]
    public void Credits_equal_to_the_tax_leave_no_net_tax() {
        TaxBreakdown breakdown = BreakdownOf(50_000m, TaxLine(300m), CreditLine(300m));

        Assert.Equal(M(300m), breakdown.CreditsApplied);
        Assert.True(breakdown.NetTax.IsZero);
    }

    [Fact]
    public void A_breakdown_without_lines_owes_and_credits_nothing() {
        TaxBreakdown breakdown = BreakdownOf(50_000m);

        Assert.True(breakdown.GrossTax.IsZero);
        Assert.True(breakdown.CreditsAvailable.IsZero);
        Assert.True(breakdown.CreditsApplied.IsZero);
        Assert.True(breakdown.NetTax.IsZero);
        Assert.Empty(breakdown.Lines);
    }

    [Fact]
    public void A_line_that_does_not_apply_adds_nothing_to_either_total() {
        BreakdownLine notApplying = new BreakdownLine(Any.RuleCode().Generate(), Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.NotApplicable, null, Money.Zero);

        TaxBreakdown breakdown = BreakdownOf(1_000m, TaxLine(10m), notApplying);

        Assert.Equal(M(10m), breakdown.GrossTax);
    }

    [Theory]
    [InlineData("1000", "100", "0.1")]
    [InlineData("300", "100", "0.3333")]
    [InlineData("300", "200", "0.6667")]
    [InlineData("32", "1", "0.0313")]
    [InlineData("3", "0.01", "0.0033")]
    [InlineData("1000", "0", "0")]
    [InlineData("100", "100", "1")]
    public void The_effective_rate_is_the_net_tax_over_the_income_to_four_decimals_half_away_from_zero(string income, string netTax, string expected) {
        TaxBreakdown breakdown = BreakdownOf(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture),
                                             TaxLine(decimal.Parse(netTax, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), breakdown.EffectiveRate);
    }

    [Fact]
    public void The_effective_rate_is_zero_without_income() {
        TaxBreakdown breakdown = BreakdownOf(0m, TaxLine(100m));

        Assert.Equal(0m, breakdown.EffectiveRate);
    }

    [Fact]
    public void The_effective_rate_is_computed_on_the_net_tax_not_the_gross() {
        TaxBreakdown breakdown = BreakdownOf(1_000m, TaxLine(200m), CreditLine(100m));

        Assert.Equal(0.1m, breakdown.EffectiveRate);
    }

    [Fact]
    public void A_breakdown_keeps_who_where_and_when_it_is_about() {
        TaxpayerId id = Any.TaxpayerId().Generate();
        JurisdictionCode jurisdiction = Any.JurisdictionCode().Generate();

        TaxBreakdown breakdown = new TaxBreakdown(id, jurisdiction, 2024, M(1_000m), []);

        Assert.Same(id, breakdown.TaxpayerId);
        Assert.Same(jurisdiction, breakdown.Jurisdiction);
        Assert.Equal(2024, breakdown.Year);
        Assert.Equal(M(1_000m), breakdown.TaxableIncome);
    }

    [Fact]
    public void A_breakdown_finds_the_line_of_a_rule_at_any_depth_or_nothing() {
        BreakdownLine nested = new BreakdownLine(Code("NESTED_RULE"), Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.Applied, null, M(1m));
        BreakdownLine parent = new BreakdownLine(Code("PARENT_RULE"), Plausible.Label().Generate(), RuleEffect.Tax, LineOutcome.Applied, null, M(1m), [nested]);
        BreakdownLine other = new BreakdownLine(Code("OTHER_RULE"), Plausible.Label().Generate(), RuleEffect.Credit, LineOutcome.Applied, null, M(1m));
        TaxBreakdown breakdown = BreakdownOf(1_000m, parent, other);

        Assert.Same(parent, breakdown.Find(Code("PARENT_RULE")));
        Assert.Same(nested, breakdown.Find(Code("NESTED_RULE")));
        Assert.Same(other, breakdown.Find(Code("OTHER_RULE")));
        Assert.Null(breakdown.Find(Code("ABSENT_RULE")));
        Assert.Throws<ArgumentNullException>(() => breakdown.Find(null!));
    }

    [Fact]
    public void A_breakdown_needs_all_its_parts_and_no_null_line() {
        TaxpayerId id = Any.TaxpayerId().Generate();
        JurisdictionCode jurisdiction = Any.JurisdictionCode().Generate();

        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(null!, jurisdiction, 2024, Money.Zero, []));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(id, null!, 2024, Money.Zero, []));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(id, jurisdiction, 2024, null!, []));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(id, jurisdiction, 2024, Money.Zero, null!));
        Assert.Throws<ArgumentException>(() => new TaxBreakdown(id, jurisdiction, 2024, Money.Zero, [TaxLine(1m), null!]));
    }

}
