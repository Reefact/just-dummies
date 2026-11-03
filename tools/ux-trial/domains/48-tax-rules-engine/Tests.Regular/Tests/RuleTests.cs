using JustDummies;

using Xunit;

namespace TaxRules.Domain.Tests;

internal static class Rules {

    public const int AnyYear = 2024;

    // Born in 1990: rules with an age condition are about the taxpayer's age, so it is a literal, not a dummy.
    public static readonly DateOnly BornIn1990 = new DateOnly(1990, 6, 15);

    public static Taxpayer Earning(string gross) {
        return new AnyTaxpayer().WithDateOfBirth(BornIn1990).WithIncomes([new EmploymentIncome("ACME", Lit.M(gross), Lit.M("0"))]).Generate();
    }

    public static Taxpayer With(params IncomeComponent[] incomes) {
        return new AnyTaxpayer().WithDateOfBirth(BornIn1990).WithIncomes(incomes).Generate();
    }

    public static TaxBracket Bracket(string lower, string? upper, string rate) {
        return new TaxBracket(Lit.M(lower), upper is null ? null : Lit.M(upper), Lit.R(rate));
    }

    public static FlatRateRule Flat(string code, string rate, ICondition? condition = null, IncomeKind taxBase = IncomeKind.Employment) {
        return new AnyFlatRateRule().WithCode(Lit.C(code)).WithTaxBase(taxBase).WithRate(Lit.R(rate)).WithCondition(condition).Generate();
    }

    public static ProgressiveRule Scale(string code, params TaxBracket[] brackets) {
        return new AnyProgressiveRule().WithCode(Lit.C(code)).WithTaxBase(IncomeKind.Employment).WithBrackets(brackets).Generate();
    }

    public static TaxCreditRule Credit(string code, string baseAmount, string perMember, ICondition? condition = null) {
        return new AnyTaxCreditRule().WithCode(Lit.C(code)).WithBaseAmount(Lit.M(baseAmount)).WithPerAdditionalMember(Lit.M(perMember)).WithCondition(condition).Generate();
    }

}

public sealed class RuleBaseTests {

    [Fact]
    public void A_rule_keeps_its_code_label_and_condition() {
        // Arrange
        RuleCode   anyCode      = new AnyRuleCode().Generate();
        string     anyLabel     = Dummies.Label().Generate();
        ICondition anyCondition = Dummies.LeafCondition().Generate();

        // Act
        FlatRateRule rule = new AnyFlatRateRule().WithCode(anyCode).WithLabel(anyLabel).WithCondition(anyCondition).Generate();

        // Assert
        Assert.Same(anyCode, rule.Code);
        Assert.Equal(anyLabel, rule.Label);
        Assert.Same(anyCondition, rule.Condition);
        Assert.Equal(RuleEffect.Tax, rule.Effect);
        Assert.Empty(rule.Children);
    }

    [Fact]
    public void A_rule_may_have_no_condition() {
        FlatRateRule anyRule = new AnyFlatRateRule().WithCondition((ICondition?)null).Generate();

        Assert.Null(anyRule.Condition);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void A_blank_label_is_refused(string label) {
        Assert.Throws<ArgumentException>(() => new AnyFlatRateRule().WithLabel(label).Generate());
    }

    [Fact]
    public void A_null_label_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AnyFlatRateRule().WithLabel((string)null!).Generate());
    }

    [Fact]
    public void A_label_of_eighty_characters_is_accepted_and_eighty_one_refused() {
        string atLimit = Any.String().Alpha().WithLength(80).Generate();
        string tooLong = Any.String().Alpha().WithLength(81).Generate();

        Assert.Equal(atLimit, new AnyFlatRateRule().WithLabel(atLimit).Generate().Label);
        Assert.Throws<ArgumentException>(() => new AnyFlatRateRule().WithLabel(tooLong).Generate());
    }

    [Fact]
    public void A_null_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AnyFlatRateRule().WithCode((RuleCode)null!).Generate());
    }

    [Fact]
    public void ToString_shows_the_code_and_the_label() {
        FlatRateRule rule = new AnyFlatRateRule().WithCode(Lit.C("INCOME_TAX")).WithLabel("Income tax").Generate();

        Assert.Equal("INCOME_TAX (Income tax)", rule.ToString());
    }

    [Fact]
    public void Evaluate_refuses_a_null_taxpayer() {
        FlatRateRule anyRule = new AnyFlatRateRule().Generate();

        Assert.Throws<ArgumentNullException>(() => anyRule.Evaluate(null!, Rules.AnyYear));
    }

    [Fact]
    public void A_rule_whose_condition_fails_produces_nothing() {
        // Arrange
        ICondition   unsatisfied = new HouseholdSizeAtLeastCondition(10);
        FlatRateRule rule        = Rules.Flat("INCOME_TAX", "0.2", unsatisfied);
        Taxpayer     taxpayer    = new AnyTaxpayer().WithHouseholdSize(2).WithIncomes([new EmploymentIncome("ACME", Lit.M("50000"), Lit.M("0"))]).Generate();

        // Act
        BreakdownLine line = rule.Evaluate(taxpayer, Rules.AnyYear);

        // Assert
        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.True(line.Amount.IsZero);
        Assert.Null(line.TaxableBase);
        Assert.Empty(line.Children);
        Assert.Equal(rule.Code, line.Code);
        Assert.Equal(rule.Label, line.Label);
        Assert.Equal(RuleEffect.Tax, line.Effect);
    }

    [Fact]
    public void A_rule_whose_condition_holds_is_applied() {
        FlatRateRule rule     = Rules.Flat("INCOME_TAX", "0.2", new HouseholdSizeAtLeastCondition(2));
        Taxpayer     taxpayer = new AnyTaxpayer().WithHouseholdSize(2).WithIncomes([new EmploymentIncome("ACME", Lit.M("50000"), Lit.M("0"))]).Generate();

        BreakdownLine line = rule.Evaluate(taxpayer, Rules.AnyYear);

        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(Lit.M("10000"), line.Amount);
    }

    [Fact]
    public void SelfAndDescendants_lists_the_rule_then_its_nested_rules_depth_first() {
        // Arrange
        FlatRateRule a = Rules.Flat("RULE_A", "0.1");
        FlatRateRule b = Rules.Flat("RULE_B", "0.1");
        FlatRateRule c = Rules.Flat("RULE_C", "0.1");
        CompositeRule inner = new CompositeRule(Lit.C("INNER"), "Inner", [b, c]);
        CompositeRule outer = new CompositeRule(Lit.C("OUTER"), "Outer", [a, inner]);

        // Act
        List<string> order = outer.SelfAndDescendants().Select(r => r.Code.Value).ToList();

        // Assert
        Assert.Equal(["OUTER", "RULE_A", "INNER", "RULE_B", "RULE_C"], order);
    }

}

public sealed class FlatRateRuleTests {

    [Fact]
    public void The_amount_is_the_base_times_the_rate() {
        FlatRateRule  rule     = Rules.Flat("FLAT_TAX", "0.2");
        BreakdownLine line     = rule.Evaluate(Rules.Earning("40000"), Rules.AnyYear);

        Assert.Equal(Lit.M("8000"), line.Amount);
        Assert.Equal(Lit.M("40000"), line.TaxableBase);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(RuleEffect.Tax, line.Effect);
    }

    [Theory]
    [InlineData("0.05", "0.5", "0.03")]
    [InlineData("0.05", "0.49", "0.02")]
    [InlineData("0.03", "0.5", "0.02")]
    [InlineData("100", "0.0001", "0.01")]
    [InlineData("49.99", "0.0001", "0.00")]
    [InlineData("0", "0.5", "0.00")]
    public void The_amount_is_rounded_to_the_cent_half_away_from_zero(string gross, string rate, string expected) {
        FlatRateRule rule = Rules.Flat("FLAT_TAX", rate);

        BreakdownLine line = rule.Evaluate(Rules.Earning(gross), Rules.AnyYear);

        Assert.Equal(Lit.M(expected), line.Amount);
    }

    [Fact]
    public void Only_the_kinds_of_the_base_are_taxed() {
        // Arrange
        Taxpayer taxpayer = Rules.With(
            new EmploymentIncome("ACME", Lit.M("1000"), Lit.M("0")),
            new SelfEmploymentIncome("shop", Lit.M("2000"), Lit.M("0")),
            new InvestmentIncome("bank", Lit.M("4000"), InvestmentKind.Interest));

        // Act
        BreakdownLine employment = Rules.Flat("EMPL", "0.1", taxBase: IncomeKind.Employment).Evaluate(taxpayer, Rules.AnyYear);
        BreakdownLine selfAndInv = Rules.Flat("SELF_INV", "0.1", taxBase: IncomeKind.SelfEmployment | IncomeKind.Investment).Evaluate(taxpayer, Rules.AnyYear);
        BreakdownLine everything = Rules.Flat("ALL", "0.1", taxBase: IncomeKind.All).Evaluate(taxpayer, Rules.AnyYear);

        // Assert
        Assert.Equal(Lit.M("100"), employment.Amount);
        Assert.Equal(Lit.M("600"), selfAndInv.Amount);
        Assert.Equal(Lit.M("700"), everything.Amount);
        Assert.Equal(Lit.M("7000"), everything.TaxableBase);
    }

    [Fact]
    public void A_zero_rate_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyFlatRateRule().WithRate(Rate.Zero).Generate());
    }

    [Fact]
    public void The_smallest_rate_is_accepted() {
        FlatRateRule rule = new AnyFlatRateRule().WithRate(Lit.R("0.0001")).Generate();

        Assert.Equal(Lit.R("0.0001"), rule.Rate);
    }

    [Fact]
    public void A_full_rate_takes_the_whole_base() {
        FlatRateRule rule = new AnyFlatRateRule().WithRate(Lit.R("1")).WithTaxBase(IncomeKind.Employment).Generate();

        Assert.Equal(Lit.M("123.45"), rule.Evaluate(Rules.Earning("123.45"), Rules.AnyYear).Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(16)]
    public void A_base_without_a_known_kind_is_refused(int taxBase) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyFlatRateRule().WithTaxBase((IncomeKind)taxBase).Generate());
    }

    [Fact]
    public void A_null_rate_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AnyFlatRateRule().WithRate((Rate)null!).Generate());
    }

    [Fact]
    public void A_generated_rule_keeps_its_base_and_rate() {
        FlatRateRule anyRule = new AnyFlatRateRule().Generate();

        Assert.NotEqual(IncomeKind.None, anyRule.TaxBase);
        Assert.False(anyRule.Rate.IsZero);
    }

}

public sealed class ProgressiveRuleTests {

    private static ProgressiveRule ThreeBands() {
        return Rules.Scale("INCOME_TAX",
                           Rules.Bracket("0", "10000", "0"),
                           Rules.Bracket("10000", "30000", "0.1"),
                           Rules.Bracket("30000", null, "0.3"));
    }

    [Theory]
    [InlineData("0", "0.00")]
    [InlineData("9999.99", "0.00")]
    [InlineData("10000", "0.00")]
    [InlineData("10000.10", "0.01")]
    [InlineData("20000", "1000.00")]
    [InlineData("30000", "2000.00")]
    [InlineData("50000", "8000.00")]
    [InlineData("50000.01", "8000.00")]
    [InlineData("50000.02", "8000.01")]
    public void The_tax_is_the_sum_over_the_brackets(string income, string expected) {
        BreakdownLine line = ThreeBands().Evaluate(Rules.Earning(income), Rules.AnyYear);

        Assert.Equal(Lit.M(expected), line.Amount);
        Assert.Equal(Lit.M(income), line.TaxableBase);
    }

    [Fact]
    public void The_tax_is_rounded_once_on_the_total_not_per_bracket() {
        // Each bracket owes 0.005; rounded once the total 0.015 is 0.02, rounded per bracket it would be 0.03.
        ProgressiveRule rule = Rules.Scale("SMALL_TAX",
                                           Rules.Bracket("0", "1", "0.005"),
                                           Rules.Bracket("1", "2", "0.005"),
                                           Rules.Bracket("2", null, "0.005"));

        BreakdownLine line = rule.Evaluate(Rules.Earning("3"), Rules.AnyYear);

        Assert.Equal(Lit.M("0.02"), line.Amount);
    }

    [Fact]
    public void One_open_ended_bracket_is_a_valid_scale() {
        ProgressiveRule rule = Rules.Scale("FLATISH", Rules.Bracket("0", null, "0.25"));

        Assert.Equal(Lit.M("250"), rule.Evaluate(Rules.Earning("1000"), Rules.AnyYear).Amount);
    }

    [Fact]
    public void Equal_consecutive_rates_are_allowed() {
        ProgressiveRule rule = Rules.Scale("SAME_RATE", Rules.Bracket("0", "100", "0.1"), Rules.Bracket("100", null, "0.1"));

        Assert.Equal(2, rule.Brackets.Count);
    }

    [Fact]
    public void The_marginal_rate_is_the_rate_of_the_bracket_the_income_falls_in() {
        ProgressiveRule rule = ThreeBands();

        Assert.Equal(Lit.R("0"), rule.MarginalRateAt(Lit.M("0")));
        Assert.Equal(Lit.R("0"), rule.MarginalRateAt(Lit.M("9999.99")));
        Assert.Equal(Lit.R("0.1"), rule.MarginalRateAt(Lit.M("10000")));
        Assert.Equal(Lit.R("0.1"), rule.MarginalRateAt(Lit.M("29999.99")));
        Assert.Equal(Lit.R("0.3"), rule.MarginalRateAt(Lit.M("30000")));
        Assert.Equal(Lit.R("0.3"), rule.MarginalRateAt(new Money(Money.MaxAmount)));
    }

    [Fact]
    public void The_marginal_rate_refuses_a_null_income() {
        Assert.Throws<ArgumentNullException>(() => ThreeBands().MarginalRateAt(null!));
    }

    [Fact]
    public void A_generated_scale_is_always_a_valid_progressive_scale() {
        ProgressiveRule anyRule = new AnyProgressiveRule().Generate();

        Assert.True(anyRule.Brackets[0].Lower.IsZero);
        Assert.True(anyRule.Brackets[^1].IsOpenEnded);
    }

    [Fact]
    public void An_empty_scale_is_refused() {
        Assert.Throws<ArgumentException>(() => Rules.Scale("EMPTY"));
    }

    [Fact]
    public void Twenty_brackets_are_accepted_and_twenty_one_refused() {
        // Arrange
        List<TaxBracket> twenty = Enumerable.Range(0, 20)
                                            .Select(i => new TaxBracket(new Money(i * 100m), i == 19 ? null : new Money((i + 1) * 100m), Lit.R("0.1")))
                                            .ToList();
        List<TaxBracket> twentyOne = Enumerable.Range(0, 21)
                                               .Select(i => new TaxBracket(new Money(i * 100m), i == 20 ? null : new Money((i + 1) * 100m), Lit.R("0.1")))
                                               .ToList();

        // Act / Assert
        Assert.Equal(20, Rules.Scale("TWENTY", twenty.ToArray()).Brackets.Count);
        Assert.Throws<ArgumentException>(() => Rules.Scale("TWENTY_ONE", twentyOne.ToArray()));
    }

    [Fact]
    public void A_null_bracket_is_refused() {
        Assert.Throws<ArgumentException>(() => Rules.Scale("HAS_NULL", Rules.Bracket("0", "10", "0.1"), null!, Rules.Bracket("10", null, "0.2")));
    }

    [Fact]
    public void A_null_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AnyProgressiveRule().WithBrackets((IEnumerable<TaxBracket>)null!).Generate());
    }

    [Fact]
    public void The_first_bracket_must_start_at_zero() {
        Assert.Throws<ArgumentException>(() => Rules.Scale("LATE_START", Rules.Bracket("0.01", "100", "0.1"), Rules.Bracket("100", null, "0.2")));
    }

    [Fact]
    public void A_gap_between_brackets_is_refused() {
        Assert.Throws<ArgumentException>(() => Rules.Scale("GAP", Rules.Bracket("0", "100", "0.1"), Rules.Bracket("100.01", null, "0.2")));
    }

    [Fact]
    public void An_overlap_between_brackets_is_refused() {
        Assert.Throws<ArgumentException>(() => Rules.Scale("OVERLAP", Rules.Bracket("0", "100", "0.1"), Rules.Bracket("99.99", null, "0.2")));
    }

    [Fact]
    public void Only_the_last_bracket_may_be_open_ended() {
        Assert.Throws<ArgumentException>(() => Rules.Scale("EARLY_OPEN", Rules.Bracket("0", null, "0.1"), Rules.Bracket("100", null, "0.2")));
    }

    [Fact]
    public void The_last_bracket_must_be_open_ended() {
        Assert.Throws<ArgumentException>(() => Rules.Scale("CLOSED_END", Rules.Bracket("0", "100", "0.1"), Rules.Bracket("100", "200", "0.2")));
    }

    [Fact]
    public void A_single_closed_bracket_is_refused() {
        Assert.Throws<ArgumentException>(() => Rules.Scale("CLOSED_ONLY", Rules.Bracket("0", "100", "0.1")));
    }

    [Fact]
    public void A_rate_that_decreases_is_refused() {
        Assert.Throws<ArgumentException>(() => Rules.Scale("FALLING", Rules.Bracket("0", "100", "0.2"), Rules.Bracket("100", null, "0.1999")));
    }

    [Fact]
    public void A_scale_does_not_follow_later_changes_to_the_list_it_was_given() {
        List<TaxBracket> brackets = [Rules.Bracket("0", null, "0.1")];
        ProgressiveRule  rule     = new AnyProgressiveRule().WithBrackets(brackets).Generate();

        brackets.Add(Rules.Bracket("100", null, "0.2"));

        Assert.Single(rule.Brackets);
    }

    [Fact]
    public void A_condition_that_fails_leaves_the_scale_unapplied() {
        ProgressiveRule rule = new AnyProgressiveRule().WithBrackets([Rules.Bracket("0", null, "0.1")])
                                                       .WithCondition(new AgeAtLeastCondition(120)).Generate();

        BreakdownLine line = rule.Evaluate(Rules.Earning("1000"), Rules.AnyYear);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
    }

}

public sealed class ThresholdExemptionRuleTests {

    private static ThresholdExemptionRule Exemption(string? ceiling = "50000") {
        return new AnyThresholdExemptionRule().WithCode(Lit.C("EXEMPT_TAX"))
                                              .WithTaxBase(IncomeKind.Employment)
                                              .WithThreshold(Lit.M("10000"))
                                              .WithRate(Lit.R("0.2"))
                                              .WithWithdrawalCeiling(ceiling is null ? null : Lit.M(ceiling))
                                              .WithCondition((ICondition?)null)
                                              .Generate();
    }

    [Theory]
    [InlineData("0", "0.00")]
    [InlineData("8000", "0.00")]
    [InlineData("10000", "0.00")]
    [InlineData("10000.05", "0.01")]
    [InlineData("30000", "4000.00")]
    [InlineData("49999.99", "8000.00")]
    public void Below_the_ceiling_only_the_excess_over_the_threshold_is_taxed(string income, string expected) {
        BreakdownLine line = Exemption().Evaluate(Rules.Earning(income), Rules.AnyYear);

        Assert.Equal(Lit.M(expected), line.Amount);
        Assert.Equal(Lit.M(income), line.TaxableBase);
    }

    [Theory]
    [InlineData("50000", "10000.00")]
    [InlineData("60000", "12000.00")]
    public void At_or_above_the_ceiling_the_whole_base_is_taxed(string income, string expected) {
        BreakdownLine line = Exemption().Evaluate(Rules.Earning(income), Rules.AnyYear);

        Assert.Equal(Lit.M(expected), line.Amount);
    }

    [Fact]
    public void Without_a_ceiling_the_exemption_is_never_withdrawn() {
        BreakdownLine line = Exemption(ceiling: null).Evaluate(Rules.Earning("1000000"), Rules.AnyYear);

        Assert.Equal(Lit.M("198000"), line.Amount);
    }

    [Fact]
    public void The_withdrawal_starts_exactly_at_the_ceiling() {
        ThresholdExemptionRule rule = Exemption();

        Assert.False(rule.IsExemptionWithdrawnAt(Lit.M("49999.99")));
        Assert.True(rule.IsExemptionWithdrawnAt(Lit.M("50000")));
        Assert.True(rule.IsExemptionWithdrawnAt(Lit.M("50000.01")));
        Assert.False(Exemption(ceiling: null).IsExemptionWithdrawnAt(new Money(Money.MaxAmount)));
    }

    [Fact]
    public void Withdrawal_refuses_a_null_income() {
        Assert.Throws<ArgumentNullException>(() => Exemption().IsExemptionWithdrawnAt(null!));
    }

    [Fact]
    public void A_generated_rule_has_a_ceiling_above_its_threshold_when_it_has_one() {
        ThresholdExemptionRule anyRule = new AnyThresholdExemptionRule().Generate();

        Assert.True(anyRule.WithdrawalCeiling is null || anyRule.WithdrawalCeiling.CompareTo(anyRule.Threshold) > 0);
    }

    [Fact]
    public void A_zero_threshold_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyThresholdExemptionRule().WithThreshold(Money.Zero).WithoutWithdrawalCeiling().Generate());
    }

    [Fact]
    public void A_zero_rate_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyThresholdExemptionRule().WithRate(Rate.Zero).Generate());
    }

    [Fact]
    public void A_ceiling_equal_to_the_threshold_is_refused() {
        Money anyBound = new Money(Any.Decimal().Between(0.01m, 1_000_000m).WithScale(2).Generate());

        Assert.Throws<ArgumentException>(() => new AnyThresholdExemptionRule().WithThreshold(anyBound).WithWithdrawalCeiling(anyBound).Generate());
    }

    [Fact]
    public void A_ceiling_below_the_threshold_is_refused() {
        Assert.Throws<ArgumentException>(() => new AnyThresholdExemptionRule().WithThreshold(Lit.M("100")).WithWithdrawalCeiling(Lit.M("99.99")).Generate());
    }

    [Fact]
    public void A_ceiling_one_cent_above_the_threshold_is_accepted() {
        ThresholdExemptionRule rule = new AnyThresholdExemptionRule().WithThreshold(Lit.M("100")).WithWithdrawalCeiling(Lit.M("100.01")).Generate();

        Assert.Equal(Lit.M("100.01"), rule.WithdrawalCeiling);
    }

    [Fact]
    public void Null_threshold_or_rate_are_refused() {
        Assert.Throws<ArgumentNullException>(() => new AnyThresholdExemptionRule().WithThreshold((Money)null!).WithoutWithdrawalCeiling().Generate());
        Assert.Throws<ArgumentNullException>(() => new AnyThresholdExemptionRule().WithRate((Rate)null!).Generate());
    }

}

public sealed class TaxCreditRuleTests {

    [Theory]
    [InlineData(1, "100.00")]
    [InlineData(2, "150.00")]
    [InlineData(4, "250.00")]
    [InlineData(20, "1050.00")]
    public void The_credit_is_the_base_plus_an_amount_per_additional_member(int household, string expected) {
        TaxCreditRule rule     = Rules.Credit("FAMILY", "100", "50");
        Taxpayer      taxpayer = new AnyTaxpayer().WithHouseholdSize(household).Generate();

        BreakdownLine line = rule.Evaluate(taxpayer, Rules.AnyYear);

        Assert.Equal(Lit.M(expected), line.Amount);
        Assert.Equal(RuleEffect.Credit, line.Effect);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Null(line.TaxableBase);
    }

    [Fact]
    public void A_credit_may_have_only_a_base() {
        TaxCreditRule rule = Rules.Credit("BASIC", "75.50", "0");

        BreakdownLine line = rule.Evaluate(new AnyTaxpayer().WithHouseholdSize(7).Generate(), Rules.AnyYear);

        Assert.Equal(Lit.M("75.50"), line.Amount);
    }

    [Fact]
    public void A_credit_may_have_only_an_amount_per_member() {
        TaxCreditRule rule = Rules.Credit("PER_HEAD", "0", "40");

        Assert.True(rule.Evaluate(new AnyTaxpayer().WithHouseholdSize(1).Generate(), Rules.AnyYear).Amount.IsZero);
        Assert.Equal(Lit.M("120"), rule.Evaluate(new AnyTaxpayer().WithHouseholdSize(4).Generate(), Rules.AnyYear).Amount);
    }

    [Fact]
    public void A_credit_granting_nothing_is_refused() {
        Assert.Throws<ArgumentException>(() => Rules.Credit("NOTHING", "0", "0"));
    }

    [Fact]
    public void The_effect_is_credit_even_when_the_rule_does_not_apply() {
        TaxCreditRule rule = Rules.Credit("ELDERLY", "100", "0", new AgeAtLeastCondition(120));

        BreakdownLine line = rule.Evaluate(new AnyTaxpayer().WithDateOfBirth(new DateOnly(1980, 1, 1)).Generate(), Rules.AnyYear);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.Equal(RuleEffect.Credit, line.Effect);
        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void A_generated_credit_grants_something() {
        TaxCreditRule anyRule = new AnyTaxCreditRule().Generate();

        Assert.False(anyRule.BaseAmount.IsZero && anyRule.PerAdditionalMember.IsZero);
        Assert.Equal(RuleEffect.Credit, anyRule.Effect);
    }

    [Fact]
    public void Null_amounts_are_refused() {
        Assert.Throws<ArgumentNullException>(() => new AnyTaxCreditRule().WithBaseAmount((Money)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => new AnyTaxCreditRule().WithPerAdditionalMember((Money)null!).Generate());
    }

}
