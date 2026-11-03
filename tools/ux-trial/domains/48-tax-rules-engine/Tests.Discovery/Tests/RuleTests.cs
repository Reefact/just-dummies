using JustDummies;

using TaxRules.Domain;

namespace TaxRules.Tests.Discovery;

public sealed class FlatRateRuleTests {

    private const int AnyYear = 2024;

    [Fact]
    public void The_tax_is_the_rate_applied_to_the_whole_base() {
        // Arrange
        FlatRateRule rule = Fixtures.FlatOn(IncomeKind.Employment, 0.125m);

        // Act
        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear);

        // Assert
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Equal(new Money(125m), line.Amount);
        Assert.Equal(new Money(1_000m), line.TaxableBase);
        Assert.Equal(RuleEffect.Tax, line.Effect);
        Assert.Empty(line.Children);
    }

    [Fact]
    public void The_line_carries_the_code_and_label_of_the_rule() {
        // Arrange
        FlatRateRule rule = Any.FlatRateRule().WithCode(Fixtures.Code("SALES_TAX")).WithLabel("Sales tax").Generate();

        // Act
        BreakdownLine line = rule.Evaluate(Any.Taxpayer().Generate(), AnyYear);

        // Assert
        Assert.Equal(Fixtures.Code("SALES_TAX"), line.Code);
        Assert.Equal("Sales tax", line.Label);
    }

    [Theory]
    [InlineData("100.03", "0.1234", "12.34")]
    [InlineData("0.05", "0.1", "0.01")]
    [InlineData("0.04", "0.1", "0.00")]
    [InlineData("333.33", "0.3333", "111.10")]
    public void The_amount_is_rounded_to_the_cent_half_away_from_zero(string income, string rate, string expected) {
        // Arrange
        FlatRateRule rule = Fixtures.FlatOn(IncomeKind.Employment, decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture));
        Taxpayer     taxpayer = Fixtures.TaxpayerEarning(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        BreakdownLine line = rule.Evaluate(taxpayer, AnyYear);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), line.Amount.Amount);
    }

    [Fact]
    public void Only_the_kinds_of_the_base_are_taxed() {
        // Arrange
        Taxpayer taxpayer = Fixtures.EarningTaxpayer(Fixtures.Salary(1_000m), Fixtures.Profit(2_000m), Fixtures.Interest(500m));

        // Act
        BreakdownLine employment = Fixtures.FlatOn(IncomeKind.Employment, 0.1m).Evaluate(taxpayer, AnyYear);
        BreakdownLine investment = Fixtures.FlatOn(IncomeKind.Investment, 0.1m).Evaluate(taxpayer, AnyYear);
        BreakdownLine both       = Fixtures.FlatOn(IncomeKind.Employment | IncomeKind.SelfEmployment, 0.1m).Evaluate(taxpayer, AnyYear);
        BreakdownLine all        = Fixtures.FlatOn(IncomeKind.All, 0.1m).Evaluate(taxpayer, AnyYear);

        // Assert
        Assert.Equal(new Money(100m), employment.Amount);
        Assert.Equal(new Money(50m), investment.Amount);
        Assert.Equal(new Money(300m), both.Amount);
        Assert.Equal(new Money(350m), all.Amount);
    }

    [Fact]
    public void A_taxpayer_without_income_of_the_base_owes_nothing_but_the_rule_applies() {
        // Arrange
        FlatRateRule rule = Fixtures.FlatOn(IncomeKind.Investment, 0.3m);

        // Act
        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(5_000m), AnyYear);

        // Assert
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.True(line.Amount.IsZero);
        Assert.Equal(Money.Zero, line.TaxableBase);
    }

    [Fact]
    public void A_condition_that_fails_makes_the_rule_not_applicable() {
        // Arrange
        FlatRateRule rule = Any.FlatRateRule()
                               .WithTaxBase(IncomeKind.Employment)
                               .WithRate(new Rate(0.2m))
                               .WithCondition(new HouseholdSizeAtLeastCondition(5))
                               .Generate();
        Taxpayer taxpayer = Any.Taxpayer().WithHouseholdSize(4).WithIncomes(new IncomeComponent[] { Fixtures.Salary(1_000m) }).Generate();

        // Act
        BreakdownLine line = rule.Evaluate(taxpayer, AnyYear);

        // Assert
        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.True(line.Amount.IsZero);
        Assert.Null(line.TaxableBase);
        Assert.Empty(line.Children);
    }

    [Fact]
    public void A_condition_that_holds_lets_the_rule_apply() {
        // Arrange
        FlatRateRule rule = Any.FlatRateRule()
                               .WithTaxBase(IncomeKind.Employment)
                               .WithRate(new Rate(0.2m))
                               .WithCondition(new HouseholdSizeAtLeastCondition(5))
                               .Generate();
        Taxpayer taxpayer = Any.Taxpayer().WithHouseholdSize(5).WithIncomes(new IncomeComponent[] { Fixtures.Salary(1_000m) }).Generate();

        // Act
        BreakdownLine line = rule.Evaluate(taxpayer, AnyYear);

        // Assert
        Assert.Equal(new Money(200m), line.Amount);
    }

    [Fact]
    public void A_rule_without_condition_always_applies() {
        FlatRateRule rule = Any.FlatRateRule().WithTaxBase(IncomeKind.All).Generate();

        Assert.Null(rule.Condition);
        Assert.Equal(LineOutcome.Applied, rule.Evaluate(Any.Taxpayer().Generate(), AnyYear).Outcome);
    }

    [Fact]
    public void A_flat_rule_is_a_leaf_that_taxes() {
        FlatRateRule anyRule = Any.FlatRateRule().Generate();

        Assert.Equal(RuleEffect.Tax, anyRule.Effect);
        Assert.Empty(anyRule.Children);
        Assert.Equal([anyRule], anyRule.SelfAndDescendants());
    }

    [Fact]
    public void A_zero_rate_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlatRateRule(Any.RuleCode().Generate(), AnyLabel.Create().Generate(), IncomeKind.All, Rate.Zero));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void A_base_with_no_kind_or_an_unknown_one_is_refused(int taxBase) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlatRateRule(Any.RuleCode().Generate(), AnyLabel.Create().Generate(), (IncomeKind)taxBase, new Rate(0.1m)));
    }

    [Fact]
    public void A_missing_code_or_rate_is_refused() {
        RuleCode anyCode = Any.RuleCode().Generate();
        string   anyLabel = AnyLabel.Create().Generate();

        Assert.Throws<ArgumentNullException>(() => new FlatRateRule(null!, anyLabel, IncomeKind.All, new Rate(0.1m)));
        Assert.Throws<ArgumentNullException>(() => new FlatRateRule(anyCode, anyLabel, IncomeKind.All, null!));
    }

    [Fact]
    public void Evaluating_for_a_missing_taxpayer_is_refused() {
        FlatRateRule anyRule = Any.FlatRateRule().Generate();

        Assert.Throws<ArgumentNullException>(() => anyRule.Evaluate(null!, AnyYear));
    }

}

public sealed class RuleLabelTests {

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t ")]
    public void A_blank_label_is_refused(string label) {
        RuleCode anyCode = Any.RuleCode().Generate();

        Assert.Throws<ArgumentException>(() => new FlatRateRule(anyCode, label, IncomeKind.All, new Rate(0.1m)));
    }

    [Fact]
    public void A_missing_label_is_refused() {
        RuleCode anyCode = Any.RuleCode().Generate();

        Assert.Throws<ArgumentNullException>(() => new FlatRateRule(anyCode, null!, IncomeKind.All, new Rate(0.1m)));
    }

    [Fact]
    public void A_label_of_eighty_characters_is_accepted_and_one_more_is_refused() {
        RuleCode anyCode = Any.RuleCode().Generate();

        Assert.Equal(80, new FlatRateRule(anyCode, new string('l', 80), IncomeKind.All, new Rate(0.1m)).Label.Length);
        Assert.Throws<ArgumentException>(() => new FlatRateRule(anyCode, new string('l', 81), IncomeKind.All, new Rate(0.1m)));
    }

    [Fact]
    public void A_rule_describes_itself_by_code_and_label() {
        FlatRateRule rule = Any.FlatRateRule().WithCode(Fixtures.Code("SALES_TAX")).WithLabel("Sales tax").Generate();

        Assert.Equal("SALES_TAX (Sales tax)", rule.ToString());
    }

}

public sealed class ProgressiveRuleTests {

    private const int AnyYear = 2024;

    private static readonly TaxBracket[] ThreeBands = [
        Fixtures.Band(0m, 10_000m, 0m),
        Fixtures.Band(10_000m, 40_000m, 0.10m),
        Fixtures.Band(40_000m, null, 0.30m)
    ];

    private static ProgressiveRule ScaleOf(IEnumerable<TaxBracket> brackets) {
        return Any.ProgressiveRule().WithTaxBase(IncomeKind.Employment).WithBrackets(brackets).Generate();
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("5000", "0")]
    [InlineData("10000", "0")]
    [InlineData("10000.01", "0.00")]
    [InlineData("25000", "1500")]
    [InlineData("40000", "3000")]
    [InlineData("50000", "6000")]
    public void The_tax_is_the_sum_over_the_brackets(string income, string expected) {
        // Arrange
        ProgressiveRule rule = ScaleOf(ThreeBands);

        // Act
        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture)), AnyYear);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), line.Amount.Amount);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
    }

    [Fact]
    public void The_line_reports_the_base_the_scale_was_applied_to() {
        BreakdownLine line = ScaleOf(ThreeBands).Evaluate(Fixtures.TaxpayerEarning(25_000m), AnyYear);

        Assert.Equal(new Money(25_000m), line.TaxableBase);
    }

    [Fact]
    public void The_sum_is_rounded_once_at_the_end_and_not_bracket_by_bracket() {
        // Arrange: each bracket yields 0.005 for 1.00 of income; rounding each would give 0.02, rounding the sum 0.01.
        ProgressiveRule rule = ScaleOf([Fixtures.Band(0m, 1m, 0.005m), Fixtures.Band(1m, null, 0.005m)]);

        // Act
        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(2m), AnyYear);

        // Assert
        Assert.Equal(new Money(0.01m), line.Amount);
    }

    [Fact]
    public void Only_the_kinds_of_the_base_are_taxed() {
        ProgressiveRule rule     = ScaleOf([Fixtures.Band(0m, null, 0.5m)]);
        Taxpayer        taxpayer = Fixtures.EarningTaxpayer(Fixtures.Salary(100m), Fixtures.Profit(900m));

        Assert.Equal(new Money(50m), rule.Evaluate(taxpayer, AnyYear).Amount);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("9999.99", 0)]
    [InlineData("10000", 1)]
    [InlineData("39999.99", 1)]
    [InlineData("40000", 2)]
    [InlineData("999999999999.99", 2)]
    public void The_marginal_rate_is_the_rate_of_the_bracket_the_income_falls_in(string income, int bracketIndex) {
        ProgressiveRule rule = ScaleOf(ThreeBands);

        Rate marginal = rule.MarginalRateAt(new Money(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(ThreeBands[bracketIndex].Rate, marginal);
    }

    [Fact]
    public void A_single_open_ended_bracket_from_zero_is_a_valid_scale() {
        ProgressiveRule rule = ScaleOf([Fixtures.Band(0m, null, 0.2m)]);

        Assert.Single(rule.Brackets);
        Assert.Equal(new Money(200m), rule.Evaluate(Fixtures.TaxpayerEarning(1_000m), AnyYear).Amount);
    }

    [Fact]
    public void Equal_rates_in_consecutive_brackets_are_allowed() {
        ProgressiveRule rule = ScaleOf([Fixtures.Band(0m, 100m, 0.2m), Fixtures.Band(100m, null, 0.2m)]);

        Assert.Equal(2, rule.Brackets.Count);
    }

    [Fact]
    public void Twenty_brackets_are_the_most_a_scale_may_have() {
        Assert.Equal(20, ProgressiveRule.MaxBrackets);
        Assert.Equal(20, ScaleOf(ScaleOfBrackets(20)).Brackets.Count);
        Assert.Throws<ArgumentException>(() => ScaleOf(ScaleOfBrackets(21)));
    }

    private static List<TaxBracket> ScaleOfBrackets(int count) {
        List<TaxBracket> brackets = [];
        for (int i = 0; i < count; i++) {
            decimal lower = i * 1_000m;
            brackets.Add(Fixtures.Band(lower, i == count - 1 ? null : lower + 1_000m, 0.01m * (i + 1)));
        }

        return brackets;
    }

    [Fact]
    public void A_scale_without_brackets_is_refused() {
        Assert.Throws<ArgumentException>(() => ScaleOf([]));
    }

    [Fact]
    public void A_scale_with_a_null_bracket_is_refused() {
        Assert.Throws<ArgumentException>(() => ScaleOf([Fixtures.Band(0m, 100m, 0.1m), null!, Fixtures.Band(100m, null, 0.2m)]));
    }

    [Fact]
    public void A_missing_scale_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ProgressiveRule(Any.RuleCode().Generate(), AnyLabel.Create().Generate(), IncomeKind.All, null!));
    }

    [Fact]
    public void A_scale_must_start_at_zero() {
        Assert.Throws<ArgumentException>(() => ScaleOf([Fixtures.Band(0.01m, 100m, 0.1m), Fixtures.Band(100m, null, 0.2m)]));
    }

    [Fact]
    public void A_gap_between_two_brackets_is_refused() {
        Assert.Throws<ArgumentException>(() => ScaleOf([Fixtures.Band(0m, 100m, 0.1m), Fixtures.Band(100.01m, null, 0.2m)]));
    }

    [Fact]
    public void An_overlap_between_two_brackets_is_refused() {
        Assert.Throws<ArgumentException>(() => ScaleOf([Fixtures.Band(0m, 100m, 0.1m), Fixtures.Band(99.99m, null, 0.2m)]));
    }

    [Fact]
    public void Only_the_last_bracket_may_be_open_ended() {
        Assert.Throws<ArgumentException>(() => ScaleOf([Fixtures.Band(0m, null, 0.1m), Fixtures.Band(100m, null, 0.2m)]));
    }

    [Fact]
    public void The_last_bracket_must_be_open_ended() {
        Assert.Throws<ArgumentException>(() => ScaleOf([Fixtures.Band(0m, 100m, 0.1m), Fixtures.Band(100m, 200m, 0.2m)]));
        Assert.Throws<ArgumentException>(() => ScaleOf([Fixtures.Band(0m, 100m, 0.1m)]));
    }

    [Fact]
    public void A_rate_that_falls_is_refused() {
        Assert.Throws<ArgumentException>(() => ScaleOf([Fixtures.Band(0m, 100m, 0.2m), Fixtures.Band(100m, null, 0.1m)]));
        Assert.Throws<ArgumentException>(() => ScaleOf([Fixtures.Band(0m, 100m, 0.1m), Fixtures.Band(100m, 200m, 0.3m), Fixtures.Band(200m, null, 0.2m)]));
    }

    [Fact]
    public void The_scale_is_a_copy_of_the_brackets_it_was_given() {
        List<TaxBracket> brackets = [Fixtures.Band(0m, 100m, 0.1m), Fixtures.Band(100m, null, 0.2m)];
        ProgressiveRule  rule     = ScaleOf(brackets);

        brackets.Add(Fixtures.Band(200m, null, 0.3m));

        Assert.Equal(2, rule.Brackets.Count);
    }

    [Fact]
    public void A_condition_that_fails_makes_the_scale_not_applicable() {
        ProgressiveRule rule = Any.ProgressiveRule()
                                  .WithBrackets(ThreeBands)
                                  .WithCondition(new AgeAtLeastCondition(65))
                                  .Generate();
        Taxpayer taxpayer = Any.Taxpayer().WithDateOfBirth(new DateOnly(1990, 1, 1)).Generate();

        BreakdownLine line = rule.Evaluate(taxpayer, AnyYear);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void A_generated_scale_is_always_a_valid_one() {
        for (int i = 0; i < 100; i++) {
            ProgressiveRule anyRule = Any.ProgressiveRule().Generate();

            Assert.True(anyRule.Brackets[^1].IsOpenEnded);
        }
    }

}

public sealed class ThresholdExemptionRuleTests {

    private const int AnyYear = 2024;

    private static ThresholdExemptionRule RuleOf(decimal threshold, decimal rate, decimal? ceiling = null) {
        return Any.ThresholdExemptionRule()
                  .WithTaxBase(IncomeKind.Employment)
                  .WithThreshold(new Money(threshold))
                  .WithRate(new Rate(rate))
                  .WithWithdrawalCeiling(ceiling is null ? null : new Money((decimal)ceiling))
                  .Generate();
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("8000", "0")]
    [InlineData("10000", "0")]
    [InlineData("10000.01", "0.00")]
    [InlineData("15000", "1000")]
    [InlineData("100000", "18000")]
    public void Income_up_to_the_threshold_is_exempt_and_the_excess_is_taxed(string income, string expected) {
        // Arrange
        ThresholdExemptionRule rule = RuleOf(10_000m, 0.20m);

        // Act
        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture)), AnyYear);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), line.Amount.Amount);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
    }

    [Theory]
    [InlineData("19999.99", "2000.00")]
    [InlineData("20000", "4000")]
    [InlineData("20000.01", "4000.00")]
    [InlineData("50000", "10000")]
    public void A_base_reaching_the_withdrawal_ceiling_is_taxed_in_full(string income, string expected) {
        // Arrange
        ThresholdExemptionRule rule = RuleOf(10_000m, 0.20m, ceiling: 20_000m);

        // Act
        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture)), AnyYear);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), line.Amount.Amount);
    }

    [Fact]
    public void The_exemption_is_withdrawn_at_the_ceiling_and_not_below_it() {
        ThresholdExemptionRule rule = RuleOf(10_000m, 0.20m, ceiling: 20_000m);

        Assert.False(rule.IsExemptionWithdrawnAt(new Money(19_999.99m)));
        Assert.True(rule.IsExemptionWithdrawnAt(new Money(20_000m)));
        Assert.True(rule.IsExemptionWithdrawnAt(new Money(20_000.01m)));
    }

    [Fact]
    public void Without_a_ceiling_the_exemption_is_never_withdrawn() {
        ThresholdExemptionRule rule = RuleOf(10_000m, 0.20m);

        Assert.False(rule.IsExemptionWithdrawnAt(new Money(Money.MaxAmount)));
        Assert.Null(rule.WithdrawalCeiling);
    }

    [Fact]
    public void The_line_reports_the_whole_base_whether_or_not_it_was_exempt() {
        ThresholdExemptionRule rule = RuleOf(10_000m, 0.20m);

        Assert.Equal(new Money(8_000m), rule.Evaluate(Fixtures.TaxpayerEarning(8_000m), AnyYear).TaxableBase);
        Assert.Equal(new Money(15_000m), rule.Evaluate(Fixtures.TaxpayerEarning(15_000m), AnyYear).TaxableBase);
    }

    [Fact]
    public void The_tax_is_rounded_to_the_cent() {
        ThresholdExemptionRule rule = RuleOf(10m, 0.0333m);

        BreakdownLine line = rule.Evaluate(Fixtures.TaxpayerEarning(100.03m), AnyYear);

        // 90.03 * 0.0333 = 2.998 -> 3.00
        Assert.Equal(new Money(3.00m), line.Amount);
    }

    [Fact]
    public void A_zero_threshold_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => RuleOf(0m, 0.2m));
    }

    [Fact]
    public void A_zero_rate_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => RuleOf(10m, 0m));
    }

    [Theory]
    [InlineData("10000")]
    [InlineData("9999.99")]
    [InlineData("1")]
    public void A_ceiling_not_above_the_threshold_is_refused(string ceiling) {
        // The constructor is called directly: the generator repairs a ceiling that breaks the rule instead of passing it on.
        Assert.Throws<ArgumentException>(() => new ThresholdExemptionRule(
            Any.RuleCode().Generate(),
            AnyLabel.Create().Generate(),
            IncomeKind.Employment,
            new Money(10_000m),
            new Rate(0.2m),
            new Money(decimal.Parse(ceiling, System.Globalization.CultureInfo.InvariantCulture))));
    }

    [Fact]
    public void A_ceiling_one_cent_above_the_threshold_is_accepted() {
        Assert.Equal(new Money(10_000.01m), RuleOf(10_000m, 0.2m, ceiling: 10_000.01m).WithdrawalCeiling);
    }

    [Fact]
    public void A_missing_threshold_or_rate_is_refused() {
        RuleCode anyCode  = Any.RuleCode().Generate();
        string   anyLabel = AnyLabel.Create().Generate();

        Assert.Throws<ArgumentNullException>(() => new ThresholdExemptionRule(anyCode, anyLabel, IncomeKind.All, null!, new Rate(0.1m)));
        Assert.Throws<ArgumentNullException>(() => new ThresholdExemptionRule(anyCode, anyLabel, IncomeKind.All, new Money(1m), null!));
    }

    [Fact]
    public void A_condition_that_fails_makes_the_rule_not_applicable() {
        ThresholdExemptionRule rule = Any.ThresholdExemptionRule()
                                         .WithThreshold(new Money(10m))
                                         .WithWithdrawalCeiling(new Money(20m))
                                         .WithCondition(new NotCondition(new HouseholdSizeAtLeastCondition(2)))
                                         .Generate();
        Taxpayer taxpayer = Any.Taxpayer().WithHouseholdSize(3).Generate();

        Assert.Equal(LineOutcome.NotApplicable, rule.Evaluate(taxpayer, AnyYear).Outcome);
    }

    [Fact]
    public void A_generated_rule_always_has_a_ceiling_above_its_threshold() {
        for (int i = 0; i < 100; i++) {
            ThresholdExemptionRule anyRule = Any.ThresholdExemptionRule().Generate();

            Assert.True(anyRule.WithdrawalCeiling is null || anyRule.WithdrawalCeiling.CompareTo(anyRule.Threshold) > 0);
        }
    }

}

public sealed class TaxCreditRuleTests {

    private const int AnyYear = 2024;

    [Theory]
    [InlineData(1, "100")]
    [InlineData(2, "150")]
    [InlineData(3, "200")]
    [InlineData(20, "1050")]
    public void A_credit_is_a_base_plus_an_amount_for_each_member_beyond_the_taxpayer(int household, string expected) {
        // Arrange
        TaxCreditRule rule     = Fixtures.Credit("FAMILY_CREDIT", 100m, 50m);
        Taxpayer      taxpayer = Any.Taxpayer().WithHouseholdSize(household).Generate();

        // Act
        BreakdownLine line = rule.Evaluate(taxpayer, AnyYear);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), line.Amount.Amount);
        Assert.Equal(RuleEffect.Credit, line.Effect);
        Assert.Equal(LineOutcome.Applied, line.Outcome);
        Assert.Null(line.TaxableBase);
    }

    [Fact]
    public void A_credit_with_only_a_per_member_amount_is_nil_for_a_household_of_one() {
        TaxCreditRule rule = Fixtures.Credit("CHILD_CREDIT", 0m, 75m);

        Assert.True(rule.Evaluate(Any.Taxpayer().WithHouseholdSize(1).Generate(), AnyYear).Amount.IsZero);
        Assert.Equal(new Money(225m), rule.Evaluate(Any.Taxpayer().WithHouseholdSize(4).Generate(), AnyYear).Amount);
    }

    [Fact]
    public void A_credit_with_only_a_base_amount_ignores_the_household() {
        TaxCreditRule rule = Fixtures.Credit("BASE_CREDIT", 100m);

        Assert.Equal(new Money(100m), rule.Evaluate(Any.Taxpayer().WithHouseholdSize(1).Generate(), AnyYear).Amount);
        Assert.Equal(new Money(100m), rule.Evaluate(Any.Taxpayer().WithHouseholdSize(9).Generate(), AnyYear).Amount);
    }

    [Fact]
    public void A_credit_that_grants_nothing_is_refused() {
        // The constructor is called directly: the generator repairs a credit that grants nothing instead of passing it on.
        Assert.Throws<ArgumentException>(() => new TaxCreditRule(Any.RuleCode().Generate(), AnyLabel.Create().Generate(), Money.Zero, Money.Zero));
    }

    [Fact]
    public void A_missing_amount_is_refused() {
        RuleCode anyCode  = Any.RuleCode().Generate();
        string   anyLabel = AnyLabel.Create().Generate();

        Assert.Throws<ArgumentNullException>(() => new TaxCreditRule(anyCode, anyLabel, null!, Money.Zero));
        Assert.Throws<ArgumentNullException>(() => new TaxCreditRule(anyCode, anyLabel, Money.Zero, null!));
    }

    [Fact]
    public void A_credit_is_a_credit_and_a_leaf() {
        TaxCreditRule anyCredit = Any.TaxCreditRule().Generate();

        Assert.Equal(RuleEffect.Credit, anyCredit.Effect);
        Assert.Empty(anyCredit.Children);
    }

    [Fact]
    public void A_condition_that_fails_makes_the_credit_not_applicable() {
        TaxCreditRule rule = Any.TaxCreditRule()
                                .WithBaseAmount(new Money(100m))
                                .WithCondition(new AgeAtLeastCondition(65))
                                .Generate();
        Taxpayer young = Any.Taxpayer().WithDateOfBirth(new DateOnly(1990, 1, 1)).Generate();

        BreakdownLine line = rule.Evaluate(young, AnyYear);

        Assert.Equal(LineOutcome.NotApplicable, line.Outcome);
        Assert.Equal(RuleEffect.Credit, line.Effect);
        Assert.True(line.Amount.IsZero);
    }

    [Fact]
    public void A_credit_beyond_the_maximum_amount_is_refused() {
        TaxCreditRule rule = Fixtures.Credit("HUGE_CREDIT", Money.MaxAmount, 1m);

        Assert.Throws<ArgumentOutOfRangeException>(() => rule.Evaluate(Any.Taxpayer().WithHouseholdSize(2).Generate(), AnyYear));
    }

}
