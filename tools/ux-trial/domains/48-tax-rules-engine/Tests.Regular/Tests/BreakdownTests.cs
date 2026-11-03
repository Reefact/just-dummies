using JustDummies;

using Xunit;

namespace TaxRules.Domain.Tests;

public sealed class BreakdownLineTests {

    private static BreakdownLine Line(string code, string amount, RuleEffect effect = RuleEffect.Tax, LineOutcome outcome = LineOutcome.Applied,
                                      string? taxableBase = null, IEnumerable<BreakdownLine>? children = null, string label = "A line") {
        return new BreakdownLine(Lit.C(code), label, effect, outcome, taxableBase is null ? null : Lit.M(taxableBase), Lit.M(amount), children);
    }

    [Fact]
    public void A_line_keeps_what_it_was_given() {
        // Arrange
        RuleCode anyCode  = new AnyRuleCode().Generate();
        string   anyLabel = Dummies.Label().Generate();
        Money    anyBase  = new AnyMoney().Generate();
        Money    anyAmount = new AnyMoney().Generate();
        BreakdownLine child = Line("CHILD_LINE", "1");

        // Act
        BreakdownLine line = new BreakdownLine(anyCode, anyLabel, RuleEffect.Credit, LineOutcome.CappedAtMaximum, anyBase, anyAmount, [child]);

        // Assert
        Assert.Same(anyCode, line.Code);
        Assert.Equal(anyLabel, line.Label);
        Assert.Equal(RuleEffect.Credit, line.Effect);
        Assert.Equal(LineOutcome.CappedAtMaximum, line.Outcome);
        Assert.Same(anyBase, line.TaxableBase);
        Assert.Same(anyAmount, line.Amount);
        Assert.Same(child, Assert.Single(line.Children));
    }

    [Fact]
    public void Children_default_to_none() {
        Assert.Empty(Line("LINE_A", "1").Children);
    }

    [Theory]
    [InlineData(LineOutcome.Applied)]
    [InlineData(LineOutcome.CappedAtMaximum)]
    [InlineData(LineOutcome.RaisedToMinimum)]
    public void Any_outcome_other_than_not_applicable_may_carry_an_amount_a_base_and_children(LineOutcome outcome) {
        BreakdownLine line = Line("LINE_A", "10", outcome: outcome, taxableBase: "100", children: [Line("LINE_B", "5")]);

        Assert.Equal(outcome, line.Outcome);
    }

    [Fact]
    public void A_line_that_does_not_apply_may_be_empty() {
        BreakdownLine line = Line("LINE_A", "0", outcome: LineOutcome.NotApplicable);

        Assert.True(line.Amount.IsZero);
        Assert.Null(line.TaxableBase);
        Assert.Empty(line.Children);
    }

    [Fact]
    public void A_line_that_does_not_apply_produces_nothing() {
        Assert.Throws<ArgumentException>(() => Line("LINE_A", "0.01", outcome: LineOutcome.NotApplicable));
        Assert.Throws<ArgumentException>(() => Line("LINE_A", "0", outcome: LineOutcome.NotApplicable, taxableBase: "0"));
        Assert.Throws<ArgumentException>(() => Line("LINE_A", "0", outcome: LineOutcome.NotApplicable, children: [Line("LINE_B", "0")]));
    }

    [Fact]
    public void Undefined_enum_values_are_refused() {
        RuleCode anyCode = new AnyRuleCode().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new BreakdownLine(anyCode, "Line", (RuleEffect)9, LineOutcome.Applied, null, Money.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BreakdownLine(anyCode, "Line", RuleEffect.Tax, (LineOutcome)9, null, Money.Zero));
    }

    [Fact]
    public void A_null_child_is_refused() {
        Assert.Throws<ArgumentException>(() => Line("LINE_A", "1", children: [Line("LINE_B", "1"), null!]));
    }

    [Fact]
    public void A_null_code_or_amount_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new BreakdownLine(null!, "Line", RuleEffect.Tax, LineOutcome.Applied, null, Money.Zero));
        Assert.Throws<ArgumentNullException>(() => new BreakdownLine(Lit.C("LINE_A"), "Line", RuleEffect.Tax, LineOutcome.Applied, null, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_label_is_refused(string label) {
        Assert.Throws<ArgumentException>(() => Line("LINE_A", "1", label: label));
    }

    [Fact]
    public void A_label_over_eighty_characters_is_refused() {
        string anyTooLong = Any.String().Alpha().WithLength(81).Generate();

        Assert.Throws<ArgumentException>(() => Line("LINE_A", "1", label: anyTooLong));
    }

    [Fact]
    public void Find_returns_the_line_itself_a_nested_line_or_nothing() {
        // Arrange
        BreakdownLine deepest = Line("DEEPEST", "1");
        BreakdownLine middle  = Line("MIDDLE", "2", children: [deepest]);
        BreakdownLine sibling = Line("SIBLING", "3");
        BreakdownLine root    = Line("ROOT", "6", children: [sibling, middle]);

        // Act / Assert
        Assert.Same(root, root.Find(Lit.C("ROOT")));
        Assert.Same(sibling, root.Find(Lit.C("SIBLING")));
        Assert.Same(middle, root.Find(Lit.C("MIDDLE")));
        Assert.Same(deepest, root.Find(Lit.C("DEEPEST")));
        Assert.Null(root.Find(Lit.C("MISSING")));
        Assert.Null(deepest.Find(Lit.C("ROOT")));
    }

    [Fact]
    public void Find_refuses_a_null_code() {
        Assert.Throws<ArgumentNullException>(() => Line("LINE_A", "1").Find(null!));
    }

    [Fact]
    public void The_line_does_not_follow_later_changes_to_the_children_it_was_given() {
        List<BreakdownLine> children = [Line("LINE_B", "1")];
        BreakdownLine       line     = Line("LINE_A", "1", children: children);

        children.Add(Line("LINE_C", "1"));

        Assert.Single(line.Children);
    }

    [Fact]
    public void ToString_shows_code_outcome_and_amount() {
        Assert.Equal("LINE_A CappedAtMaximum: 12.50", Line("LINE_A", "12.5", outcome: LineOutcome.CappedAtMaximum).ToString());
    }

}

public sealed class TaxBreakdownTests {

    private static BreakdownLine Tax(string code, string amount) {
        return new BreakdownLine(Lit.C(code), "A tax", RuleEffect.Tax, LineOutcome.Applied, Lit.M("1"), Lit.M(amount));
    }

    private static BreakdownLine Credit(string code, string amount) {
        return new BreakdownLine(Lit.C(code), "A credit", RuleEffect.Credit, LineOutcome.Applied, null, Lit.M(amount));
    }

    private static TaxBreakdown Breakdown(string income, params BreakdownLine[] lines) {
        return new TaxBreakdown(new AnyTaxpayerId().Generate(), new AnyJurisdictionCode().Generate(), Rules.AnyYear, Lit.M(income), lines);
    }

    [Fact]
    public void A_breakdown_keeps_what_it_was_given() {
        // Arrange
        TaxpayerId       anyId           = new AnyTaxpayerId().Generate();
        JurisdictionCode anyJurisdiction = new AnyJurisdictionCode().Generate();
        int              anyYear         = Any.Int32().Between(TaxSchedule.FirstYear, TaxSchedule.LastYear).Generate();
        Money            anyIncome       = new AnyMoney().Generate();
        BreakdownLine    line            = Tax("TAX_A", "10");

        // Act
        TaxBreakdown breakdown = new TaxBreakdown(anyId, anyJurisdiction, anyYear, anyIncome, [line]);

        // Assert
        Assert.Same(anyId, breakdown.TaxpayerId);
        Assert.Same(anyJurisdiction, breakdown.Jurisdiction);
        Assert.Equal(anyYear, breakdown.Year);
        Assert.Same(anyIncome, breakdown.TaxableIncome);
        Assert.Same(line, Assert.Single(breakdown.Lines));
    }

    [Fact]
    public void Credits_are_set_against_the_gross_tax() {
        TaxBreakdown breakdown = Breakdown("10000", Tax("TAX_A", "1000"), Tax("TAX_B", "500"), Credit("CREDIT_A", "300"));

        Assert.Equal(Lit.M("1500"), breakdown.GrossTax);
        Assert.Equal(Lit.M("300"), breakdown.CreditsAvailable);
        Assert.Equal(Lit.M("300"), breakdown.CreditsApplied);
        Assert.Equal(Lit.M("1200"), breakdown.NetTax);
    }

    [Fact]
    public void Credits_are_not_refundable() {
        TaxBreakdown breakdown = Breakdown("10000", Tax("TAX_A", "1000"), Credit("CREDIT_A", "1500"), Credit("CREDIT_B", "500"));

        Assert.Equal(Lit.M("1000"), breakdown.GrossTax);
        Assert.Equal(Lit.M("2000"), breakdown.CreditsAvailable);
        Assert.Equal(Lit.M("1000"), breakdown.CreditsApplied);
        Assert.True(breakdown.NetTax.IsZero);
    }

    [Fact]
    public void Credits_equal_to_the_tax_cancel_it() {
        TaxBreakdown breakdown = Breakdown("10000", Tax("TAX_A", "1000"), Credit("CREDIT_A", "1000"));

        Assert.Equal(Lit.M("1000"), breakdown.CreditsApplied);
        Assert.True(breakdown.NetTax.IsZero);
    }

    [Fact]
    public void Without_credits_the_net_tax_is_the_gross_tax() {
        TaxBreakdown breakdown = Breakdown("10000", Tax("TAX_A", "1000"));

        Assert.True(breakdown.CreditsAvailable.IsZero);
        Assert.True(breakdown.CreditsApplied.IsZero);
        Assert.Equal(breakdown.GrossTax, breakdown.NetTax);
    }

    [Fact]
    public void Without_any_line_everything_is_zero() {
        TaxBreakdown breakdown = Breakdown("0");

        Assert.True(breakdown.GrossTax.IsZero);
        Assert.True(breakdown.CreditsAvailable.IsZero);
        Assert.True(breakdown.CreditsApplied.IsZero);
        Assert.True(breakdown.NetTax.IsZero);
    }

    [Fact]
    public void Only_top_level_lines_are_summed_not_the_nested_ones() {
        BreakdownLine nested = Tax("NESTED", "400");
        BreakdownLine parent = new BreakdownLine(Lit.C("PARENT"), "Parent", RuleEffect.Tax, LineOutcome.Applied, null, Lit.M("400"), [nested]);

        TaxBreakdown breakdown = Breakdown("10000", parent);

        Assert.Equal(Lit.M("400"), breakdown.GrossTax);
    }

    [Theory]
    [InlineData("10000", "1200", "0.12")]
    [InlineData("300", "100", "0.3333")]
    [InlineData("300", "200", "0.6667")]
    [InlineData("3", "2", "0.6667")]
    [InlineData("100000", "1", "0.0000")]
    [InlineData("100000", "5", "0.0001")]
    [InlineData("10000", "10000", "1.0000")]
    [InlineData("100", "250", "2.5")]
    public void The_effective_rate_is_the_net_tax_over_the_income_to_four_decimals(string income, string tax, string expected) {
        TaxBreakdown breakdown = Breakdown(income, Tax("TAX_A", tax));

        Assert.Equal(Lit.D(expected), breakdown.EffectiveRate);
    }

    [Fact]
    public void The_effective_rate_is_zero_without_income() {
        TaxBreakdown breakdown = Breakdown("0", Tax("TAX_A", "500"));

        Assert.Equal(0m, breakdown.EffectiveRate);
    }

    [Fact]
    public void The_effective_rate_uses_the_net_tax() {
        TaxBreakdown breakdown = Breakdown("10000", Tax("TAX_A", "2000"), Credit("CREDIT_A", "500"));

        Assert.Equal(0.15m, breakdown.EffectiveRate);
    }

    [Fact]
    public void Find_looks_in_nested_lines_too() {
        BreakdownLine nested = Tax("NESTED", "400");
        BreakdownLine parent = new BreakdownLine(Lit.C("PARENT"), "Parent", RuleEffect.Tax, LineOutcome.Applied, null, Lit.M("400"), [nested]);
        BreakdownLine other  = Tax("OTHER", "1");

        TaxBreakdown breakdown = Breakdown("10000", other, parent);

        Assert.Same(other, breakdown.Find(Lit.C("OTHER")));
        Assert.Same(parent, breakdown.Find(Lit.C("PARENT")));
        Assert.Same(nested, breakdown.Find(Lit.C("NESTED")));
        Assert.Null(breakdown.Find(Lit.C("MISSING")));
    }

    [Fact]
    public void Find_refuses_a_null_code() {
        Assert.Throws<ArgumentNullException>(() => Breakdown("0").Find(null!));
    }

    [Fact]
    public void A_null_line_or_collaborator_is_refused() {
        TaxpayerId       anyId        = new AnyTaxpayerId().Generate();
        JurisdictionCode anyJurisdiction = new AnyJurisdictionCode().Generate();

        Assert.Throws<ArgumentException>(() => new TaxBreakdown(anyId, anyJurisdiction, Rules.AnyYear, Money.Zero, [Tax("TAX_A", "1"), null!]));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(null!, anyJurisdiction, Rules.AnyYear, Money.Zero, []));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(anyId, null!, Rules.AnyYear, Money.Zero, []));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(anyId, anyJurisdiction, Rules.AnyYear, null!, []));
        Assert.Throws<ArgumentNullException>(() => new TaxBreakdown(anyId, anyJurisdiction, Rules.AnyYear, Money.Zero, null!));
    }

    [Fact]
    public void The_breakdown_does_not_follow_later_changes_to_the_lines_it_was_given() {
        List<BreakdownLine> lines = [Tax("TAX_A", "10")];
        TaxBreakdown breakdown = new TaxBreakdown(new AnyTaxpayerId().Generate(), new AnyJurisdictionCode().Generate(), Rules.AnyYear, Lit.M("100"), lines);

        lines.Add(Tax("TAX_B", "10"));

        Assert.Single(breakdown.Lines);
        Assert.Equal(Lit.M("10"), breakdown.GrossTax);
    }

}
