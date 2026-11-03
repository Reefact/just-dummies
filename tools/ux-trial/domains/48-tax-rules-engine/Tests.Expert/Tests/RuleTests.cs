using JustDummies;

using TaxRules.Domain;

using Xunit;

using static TaxRules.Tests.Build;

namespace TaxRules.Tests;

/// <summary>What every rule does before its own arithmetic: the label, the condition, the nesting.</summary>
public sealed class TaxRuleCommonTests {

    [Fact]
    public void A_rule_with_no_condition_always_applies() {
        FlatRateRule rule = Any.FlatRateRule().WithCondition(null).Generate();
        Taxpayer anyTaxpayer = Any.Taxpayer().Generate();

        BreakdownLine line = rule.Evaluate(anyTaxpayer, 2020);

        Assert.NotEqual(LineOutcome.NotApplicable, line.Outcome);
    }

    [Fact]
    public void A_rule_whose_condition_fails_produces_nothing() {
        // Nobody who earns nothing is above a threshold of zero.
        FlatRateRule rule = Any.FlatRateRule().WithCondition(IncomeAboveNothing()).Generate();
        Taxpayer jobless = TaxpayerEarning();

        BreakdownLine line = rule.Evaluate(jobless, 2020);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.True(line.Amount.IsZero);
        Assert.Null(line.TaxableBase);
        Assert.Empty(line.Children);
        Assert.Equal(rule.Code, line.Code);
        Assert.Equal(rule.Label, line.Label);
        Assert.Equal(RuleEffect.Tax, line.Effect);
    }

    [Fact]
    public void A_rule_whose_condition_holds_computes_its_amount() {
        FlatRateRule rule = Any.FlatRateRule().WithTaxBase(IncomeKind.Employment).WithRate(R(0.1m)).WithCondition(IncomeAboveNothing()).Generate();
        Taxpayer taxpayer = TaxpayerEarning(Salary(1_000m));

        BreakdownLine line = rule.Evaluate(taxpayer, 2020);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(M(100m), line.Amount);
    }

    [Fact]
    public void A_credit_whose_condition_fails_is_still_a_credit_in_the_breakdown() {
        TaxCreditRule credit = Any.TaxCreditRule().WithCondition(IncomeAboveNothing()).Generate();
        Taxpayer jobless = TaxpayerEarning();

        BreakdownLine line = credit.Evaluate(jobless, 2020);

        Assert.Equal(RuleEffect.Credit, line.Effect);
        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void A_condition_that_depends_on_the_tax_year_is_given_the_year_of_the_evaluation() {
        FlatRateRule rule = Any.FlatRateRule().WithCondition(new AgeAtLeastCondition(30)).Generate();
        Taxpayer bornIn1990 = Any.Taxpayer().WithDateOfBirth(new DateOnly(1990, 3, 3)).Generate();

        Assert.Equal(LineOutcome.Applied, rule.Evaluate(bornIn1990, 2020).Outcome);
        Assert.Equal(LineOutcome.NotApplicable, rule.Evaluate(bornIn1990, 2019).Outcome);
    }

    [Fact]
    public void Evaluating_a_rule_requires_a_taxpayer() {
        FlatRateRule anyRule = Any.FlatRateRule().Generate();

        Assert.Throws<ArgumentNullException>(() => anyRule.Evaluate(null!, 2020));
    }

    [Fact]
    public void A_label_of_eighty_characters_is_accepted() {
        string eighty = new string('x', 80);

        FlatRateRule rule = Any.FlatRateRule().WithLabel(eighty).Generate();

        Assert.Equal(eighty, rule.Label);
    }

    [Fact]
    public void A_label_of_more_than_eighty_characters_is_refused_whatever_the_rule() {
        string tooLong = new string('x', 81);

        Assert.Throws<ArgumentException>(() => Any.FlatRateRule().WithLabel(tooLong).Generate());
        Assert.Throws<ArgumentException>(() => Any.ProgressiveRule().WithLabel(tooLong).Generate());
        Assert.Throws<ArgumentException>(() => Any.ThresholdExemptionRule().WithLabel(tooLong).Generate());
        Assert.Throws<ArgumentException>(() => Any.CappedRule().WithLabel(tooLong).Generate());
        Assert.Throws<ArgumentException>(() => Any.CompositeRule().WithLabel(tooLong).Generate());
        Assert.Throws<ArgumentException>(() => Any.TaxCreditRule().WithLabel(tooLong).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_label_is_refused_whatever_the_rule(string blank) {
        Assert.Throws<ArgumentException>(() => Any.FlatRateRule().WithLabel(blank).Generate());
        Assert.Throws<ArgumentException>(() => Any.TaxCreditRule().WithLabel(blank).Generate());
        Assert.Throws<ArgumentException>(() => Any.CappedRule().WithLabel(blank).Generate());
    }

    [Fact]
    public void A_rule_keeps_its_code_and_label_and_is_described_by_them() {
        FlatRateRule rule = Any.FlatRateRule().WithCode(Code("INCOME_TAX")).WithLabel("Income tax").Generate();

        Assert.Equal(Code("INCOME_TAX"), rule.Code);
        Assert.Equal("Income tax", rule.Label);
        Assert.Equal("INCOME_TAX (Income tax)", rule.ToString());
    }

    [Fact]
    public void A_rule_with_no_nested_rule_has_no_children_and_is_its_own_descendant() {
        FlatRateRule rule = Any.FlatRateRule().Generate();

        Assert.Empty(rule.Children);
        Assert.Same(rule, Assert.Single(rule.SelfAndDescendants()));
    }

    [Fact]
    public void A_tree_lists_itself_then_its_descendants_depth_first() {
        FlatRateRule a = Flat(IncomeKind.All, 0.1m, "A_RULE");
        FlatRateRule b = Flat(IncomeKind.All, 0.1m, "B_RULE");
        FlatRateRule c = Flat(IncomeKind.All, 0.1m, "C_RULE");
        CompositeRule inner = Any.CompositeRule().WithCode(Code("INNER")).WithComponents([a, b]).Generate();
        CompositeRule outer = Any.CompositeRule().WithCode(Code("OUTER")).WithComponents([inner, c]).Generate();

        string[] codes = outer.SelfAndDescendants().Select(r => r.Code.Value).ToArray();

        Assert.Equal(["OUTER", "INNER", "A_RULE", "B_RULE", "C_RULE"], codes);
    }

}

public sealed class FlatRateRuleTests {

    [Fact]
    public void The_amount_is_the_base_times_the_rate() {
        FlatRateRule rule = Any.FlatRateRule().WithTaxBase(IncomeKind.Employment).WithRate(R(0.172m)).WithCondition(null).Generate();
        Taxpayer taxpayer = TaxpayerEarning(Salary(10_000m));

        BreakdownLine line = rule.Evaluate(taxpayer, 2020);

        Assert.Equal(M(1_720m), line.Amount);
        Assert.Equal(M(10_000m), line.TaxableBase);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(RuleEffect.Tax, line.Effect);
        Assert.Empty(line.Children);
    }

    [Fact]
    public void The_amount_is_rounded_to_the_cent_half_away_from_zero() {
        FlatRateRule rule = Any.FlatRateRule().WithTaxBase(IncomeKind.Employment).WithRate(R(0.05m)).WithCondition(null).Generate();

        Assert.Equal(M(0.03m), rule.Evaluate(TaxpayerEarning(Salary(0.50m)), 2020).Amount);
        Assert.Equal(M(0.02m), rule.Evaluate(TaxpayerEarning(Salary(0.49m)), 2020).Amount);
    }

    [Fact]
    public void Only_the_kinds_of_its_base_are_taxed() {
        FlatRateRule rule = Any.FlatRateRule().WithTaxBase(IncomeKind.Employment | IncomeKind.Investment).WithRate(R(0.1m)).WithCondition(null).Generate();
        Taxpayer taxpayer = TaxpayerEarning(Salary(1_000m), Business(4_000m), Interest(200m), Dividend(100m));

        BreakdownLine line = rule.Evaluate(taxpayer, 2020);

        // 1000 salary + 200 interest + 60 taxable dividends; the business profit is not in the base.
        Assert.Equal(M(1_260m), line.TaxableBase);
        Assert.Equal(M(126m), line.Amount);
    }

    [Fact]
    public void A_taxpayer_with_no_income_of_the_base_owes_nothing_but_the_rule_still_applied() {
        FlatRateRule rule = Any.FlatRateRule().WithTaxBase(IncomeKind.Investment).WithCondition(null).Generate();
        Taxpayer taxpayer = TaxpayerEarning(Salary(1_000m));

        BreakdownLine line = rule.Evaluate(taxpayer, 2020);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.True(line.Amount.IsZero);
        Assert.True(line.TaxableBase!.IsZero);
    }

    [Fact]
    public void A_rate_of_zero_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.FlatRateRule().WithRate(Rate.Zero).Generate());
    }

    [Fact]
    public void The_smallest_rate_is_accepted() {
        FlatRateRule rule = Any.FlatRateRule().WithRate(R(0.0001m)).Generate();

        Assert.Equal(R(0.0001m), rule.Rate);
    }

    [Fact]
    public void A_base_naming_no_income_kind_or_an_unknown_one_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.FlatRateRule().WithTaxBase(IncomeKind.None).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.FlatRateRule().WithTaxBase((IncomeKind)8).Generate());
    }

    [Fact]
    public void A_rule_needs_its_rate() {
        Assert.Throws<ArgumentNullException>(() => new FlatRateRule(Any.RuleCode().Generate(), "Flat", IncomeKind.All, null!));
    }

}

public sealed class ProgressiveRuleTests {

    private static TaxBracket[] ThreeBrackets() => [
        new TaxBracket(M(0m), M(10_000m), R(0.10m)),
        new TaxBracket(M(10_000m), M(30_000m), R(0.20m)),
        new TaxBracket(M(30_000m), null, R(0.30m))
    ];

    private static AnyProgressiveRule AnyScaleTaxingEmployment(params TaxBracket[] brackets) {
        return Any.ProgressiveRule().WithTaxBase(IncomeKind.Employment).WithBrackets(brackets).WithCondition(null);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("5000", "500")]
    [InlineData("10000", "1000")]
    [InlineData("10000.01", "1000.002")]
    [InlineData("30000", "5000")]
    [InlineData("45000", "9500")]
    public void The_tax_is_the_sum_over_the_brackets_of_the_rate_on_the_part_of_the_income_in_each(string income, string expectedTax) {
        ProgressiveRule rule = AnyScaleTaxingEmployment(ThreeBrackets()).Generate();
        Taxpayer taxpayer = TaxpayerEarning(Salary(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture)));

        BreakdownLine line = rule.Evaluate(taxpayer, 2020);

        Assert.Equal(Money.RoundedFrom(decimal.Parse(expectedTax, System.Globalization.CultureInfo.InvariantCulture)), line.Amount);
        Assert.Equal(M(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture)), line.TaxableBase);
    }

    [Fact]
    public void The_sum_is_rounded_once_at_the_end_not_bracket_by_bracket() {
        // Each bracket owes 0.004; rounded one by one they would owe nothing, together 0.008 rounds to a cent.
        ProgressiveRule rule = AnyScaleTaxingEmployment(new TaxBracket(M(0m), M(0.01m), R(0.4m)),
                                                       new TaxBracket(M(0.01m), null, R(0.4m))).Generate();

        BreakdownLine line = rule.Evaluate(TaxpayerEarning(Salary(0.02m)), 2020);

        Assert.Equal(M(0.01m), line.Amount);
    }

    [Fact]
    public void The_marginal_rate_is_the_rate_of_the_bracket_the_income_falls_in() {
        ProgressiveRule rule = AnyScaleTaxingEmployment(ThreeBrackets()).Generate();

        Assert.Equal(R(0.10m), rule.MarginalRateAt(M(0m)));
        Assert.Equal(R(0.10m), rule.MarginalRateAt(M(9_999.99m)));
        Assert.Equal(R(0.20m), rule.MarginalRateAt(M(10_000m)));
        Assert.Equal(R(0.20m), rule.MarginalRateAt(M(29_999.99m)));
        Assert.Equal(R(0.30m), rule.MarginalRateAt(M(30_000m)));
        Assert.Equal(R(0.30m), rule.MarginalRateAt(M(Money.MaxAmount)));
    }

    [Fact]
    public void The_marginal_rate_of_nothing_is_refused() {
        ProgressiveRule anyRule = Any.ProgressiveRule().Generate();

        Assert.Throws<ArgumentNullException>(() => anyRule.MarginalRateAt(null!));
    }

    [Fact]
    public void A_single_open_ended_bracket_from_zero_is_a_flat_scale() {
        ProgressiveRule rule = AnyScaleTaxingEmployment(new TaxBracket(M(0m), null, R(0.25m))).Generate();

        Assert.Equal(M(250m), rule.Evaluate(TaxpayerEarning(Salary(1_000m)), 2020).Amount);
        Assert.Single(rule.Brackets);
    }

    [Fact]
    public void Equal_rates_in_successive_brackets_are_accepted() {
        ProgressiveRule rule = AnyScaleTaxingEmployment(new TaxBracket(M(0m), M(100m), R(0.1m)), new TaxBracket(M(100m), null, R(0.1m))).Generate();

        Assert.Equal(2, rule.Brackets.Count);
    }

    [Fact]
    public void Twenty_brackets_are_accepted_and_twenty_one_refused() {
        Assert.Equal(20, ProgressiveRule.MaxBrackets);
        Assert.Equal(20, AnyScaleTaxingEmployment(ScaleOf(20)).Generate().Brackets.Count);
        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(ScaleOf(21)).Generate());
    }

    [Fact]
    public void A_scale_without_brackets_is_refused() {
        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment().Generate());
    }

    [Fact]
    public void A_scale_with_a_missing_bracket_is_refused() {
        TaxBracket[] withNull = [new TaxBracket(M(0m), M(100m), R(0.1m)), null!, new TaxBracket(M(100m), null, R(0.2m))];

        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(withNull).Generate());
    }

    [Fact]
    public void A_scale_must_start_at_zero() {
        TaxBracket[] startsLate = [new TaxBracket(M(0.01m), M(100m), R(0.1m)), new TaxBracket(M(100m), null, R(0.2m))];

        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(startsLate).Generate());
    }

    [Fact]
    public void Brackets_with_a_gap_between_them_are_refused() {
        TaxBracket[] gap = [new TaxBracket(M(0m), M(100m), R(0.1m)), new TaxBracket(M(100.01m), null, R(0.2m))];

        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(gap).Generate());
    }

    [Fact]
    public void Brackets_that_overlap_are_refused() {
        TaxBracket[] overlap = [new TaxBracket(M(0m), M(100m), R(0.1m)), new TaxBracket(M(99.99m), null, R(0.2m))];

        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(overlap).Generate());
    }

    [Fact]
    public void Brackets_given_out_of_order_are_refused() {
        TaxBracket[] outOfOrder = [new TaxBracket(M(100m), null, R(0.2m)), new TaxBracket(M(0m), M(100m), R(0.1m))];

        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(outOfOrder).Generate());
    }

    [Fact]
    public void Only_the_last_bracket_may_be_open_ended() {
        TaxBracket[] openInTheMiddle = [new TaxBracket(M(0m), null, R(0.1m)), new TaxBracket(M(100m), null, R(0.2m))];

        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(openInTheMiddle).Generate());
    }

    [Fact]
    public void The_last_bracket_must_be_open_ended() {
        TaxBracket[] closedAtTheEnd = [new TaxBracket(M(0m), M(100m), R(0.1m)), new TaxBracket(M(100m), M(200m), R(0.2m))];

        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(closedAtTheEnd).Generate());
        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(new TaxBracket(M(0m), M(100m), R(0.1m))).Generate());
    }

    [Fact]
    public void A_rate_that_falls_from_one_bracket_to_the_next_is_refused() {
        TaxBracket[] decreasing = [new TaxBracket(M(0m), M(100m), R(0.2m)), new TaxBracket(M(100m), null, R(0.1999m))];

        Assert.Throws<ArgumentException>(() => AnyScaleTaxingEmployment(decreasing).Generate());
    }

    [Fact]
    public void A_scale_needs_its_brackets_and_a_base() {
        Assert.Throws<ArgumentNullException>(() => new ProgressiveRule(Any.RuleCode().Generate(), "Scale", IncomeKind.All, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyScaleTaxingEmployment(ThreeBrackets()).WithTaxBase(IncomeKind.None).Generate());
    }

    private static TaxBracket[] ScaleOf(int count) {
        List<TaxBracket> brackets = [];
        for (int i = 0; i < count; i++) {
            Money lower = M(i * 100m);
            Money? upper = i == count - 1 ? null : M((i + 1) * 100m);
            brackets.Add(new TaxBracket(lower, upper, R(0.01m * (i + 1) / 2m)));
        }

        return brackets.ToArray();
    }

}

public sealed class ThresholdExemptionRuleTests {

    private static AnyThresholdExemptionRule AnyExemptionOnEmployment(decimal threshold, decimal rate, decimal? ceiling = null) {
        AnyThresholdExemptionRule generator = Any.ThresholdExemptionRule()
                                                 .WithTaxBase(IncomeKind.Employment)
                                                 .WithThreshold(M(threshold))
                                                 .WithRate(R(rate))
                                                 .WithCondition(null);

        return ceiling is null ? generator.WithoutWithdrawalCeiling() : generator.WithWithdrawalCeiling(M(ceiling.Value));
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("9999.99", "0")]
    [InlineData("10000", "0")]
    [InlineData("10000.10", "0.01")]
    [InlineData("15000", "500")]
    [InlineData("1000000", "99000")]
    public void Only_the_excess_over_the_threshold_is_taxed(string income, string expectedTax) {
        ThresholdExemptionRule rule = AnyExemptionOnEmployment(10_000m, 0.1m).Generate();

        BreakdownLine line = rule.Evaluate(TaxpayerEarning(Salary(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture))), 2020);

        Assert.Equal(M(decimal.Parse(expectedTax, System.Globalization.CultureInfo.InvariantCulture)), line.Amount);
        Assert.Equal(M(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture)), line.TaxableBase);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
    }

    [Theory]
    [InlineData("10000", "0", false)]
    [InlineData("19999.99", "999.999", false)]
    [InlineData("20000", "2000", true)]
    [InlineData("20000.01", "2000.001", true)]
    public void A_base_at_or_above_the_withdrawal_ceiling_loses_the_exemption_and_is_taxed_in_full(string income, string expectedTax, bool withdrawn) {
        ThresholdExemptionRule rule = AnyExemptionOnEmployment(10_000m, 0.1m, ceiling: 20_000m).Generate();
        Money earned = M(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture));

        BreakdownLine line = rule.Evaluate(TaxpayerEarning(Salary(earned.Amount)), 2020);

        Assert.Equal(Money.RoundedFrom(decimal.Parse(expectedTax, System.Globalization.CultureInfo.InvariantCulture)), line.Amount);
        Assert.Equal(withdrawn, rule.IsExemptionWithdrawnAt(earned));
    }

    [Fact]
    public void Without_a_ceiling_the_exemption_is_never_withdrawn() {
        ThresholdExemptionRule rule = AnyExemptionOnEmployment(10_000m, 0.1m).Generate();

        Assert.False(rule.IsExemptionWithdrawnAt(M(Money.MaxAmount)));
        Assert.Null(rule.WithdrawalCeiling);
    }

    [Fact]
    public void A_ceiling_one_cent_above_the_threshold_is_accepted() {
        ThresholdExemptionRule rule = AnyExemptionOnEmployment(10_000m, 0.1m, ceiling: 10_000.01m).Generate();

        Assert.Equal(M(10_000.01m), rule.WithdrawalCeiling);
    }

    [Fact]
    public void A_ceiling_at_or_below_the_threshold_is_refused() {
        Assert.Throws<ArgumentException>(() => AnyExemptionOnEmployment(10_000m, 0.1m, ceiling: 10_000m).Generate());
        Assert.Throws<ArgumentException>(() => AnyExemptionOnEmployment(10_000m, 0.1m, ceiling: 9_999.99m).Generate());
    }

    [Fact]
    public void A_threshold_of_zero_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyExemptionOnEmployment(10_000m, 0.1m).WithThreshold(Money.Zero).Generate());
    }

    [Fact]
    public void A_rate_of_zero_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyExemptionOnEmployment(10_000m, 0.1m).WithRate(Rate.Zero).Generate());
    }

    [Fact]
    public void A_base_naming_no_income_kind_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyExemptionOnEmployment(10_000m, 0.1m).WithTaxBase(IncomeKind.None).Generate());
    }

    [Fact]
    public void Asking_whether_the_exemption_is_withdrawn_for_no_income_is_refused() {
        ThresholdExemptionRule anyRule = Any.ThresholdExemptionRule().Generate();

        Assert.Throws<ArgumentNullException>(() => anyRule.IsExemptionWithdrawnAt(null!));
    }

}
