using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Discovery;

public sealed class PayslipLineTests {

    [Fact]
    public void A_valid_line_keeps_what_it_was_given() {
        PayslipLine line = new PayslipLine("Meal vouchers", LineKind.Bonus, 12.50m);

        Assert.Equal("Meal vouchers", line.Label);
        Assert.Equal(LineKind.Bonus, line.Kind);
        Assert.Equal(12.50m, line.Amount);
    }

    [Fact]
    public void The_maximum_label_length_is_forty() {
        Assert.Equal(40, PayslipLine.MaximumLabelLength);
    }

    [Fact]
    public void A_label_of_forty_characters_is_accepted() {
        string     label = new string('x', 40);
        LineKind   anyKind   = Any.Enum<LineKind>().Generate();
        decimal    anyAmount = Any.Decimal().Between(0.01m, 5_000m).WithScale(2).Generate();

        PayslipLine line = new PayslipLine(label, anyKind, anyAmount);

        Assert.Equal(label, line.Label);
    }

    [Fact]
    public void A_label_of_forty_one_characters_is_refused() {
        string   label     = new string('x', 41);
        LineKind anyKind   = Any.Enum<LineKind>().Generate();
        decimal  anyAmount = Any.Decimal().Between(0.01m, 5_000m).WithScale(2).Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PayslipLine(label, anyKind, anyAmount));

        Assert.Equal("label", exception.ParamName);
    }

    [Fact]
    public void A_null_label_is_refused() {
        LineKind anyKind   = Any.Enum<LineKind>().Generate();
        decimal  anyAmount = Any.Decimal().Between(0.01m, 5_000m).WithScale(2).Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new PayslipLine(null!, anyKind, anyAmount));

        Assert.Equal("label", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t ")]
    [InlineData("\n")]
    public void A_blank_label_is_refused(string label) {
        LineKind anyKind   = Any.Enum<LineKind>().Generate();
        decimal  anyAmount = Any.Decimal().Between(0.01m, 5_000m).WithScale(2).Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PayslipLine(label, anyKind, anyAmount));

        Assert.Equal("label", exception.ParamName);
    }

    public static TheoryData<decimal> NonPositiveAmounts => new TheoryData<decimal> { 0m, 0.00m, -0.01m, -100m };

    [Theory]
    [MemberData(nameof(NonPositiveAmounts))]
    public void An_amount_that_is_not_positive_is_refused(decimal amount) {
        string   anyLabel = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        LineKind anyKind  = Any.Enum<LineKind>().Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayslipLine(anyLabel, anyKind, amount));

        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("12.345")]
    public void An_amount_finer_than_a_cent_is_refused(string amountText) {
        decimal  amount   = decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture);
        string   anyLabel = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        LineKind anyKind  = Any.Enum<LineKind>().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PayslipLine(anyLabel, anyKind, amount));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void The_label_is_checked_before_the_amount() {
        LineKind anyKind = Any.Enum<LineKind>().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PayslipLine(" ", anyKind, 0m));

        Assert.Equal("label", exception.ParamName);
    }

    [Fact]
    public void A_bonus_adds_its_amount() {
        PayslipLine line = new PayslipLine("Bonus", LineKind.Bonus, 12.50m);

        Assert.Equal(12.50m, line.SignedAmount);
    }

    [Fact]
    public void A_deduction_subtracts_its_amount() {
        PayslipLine line = new PayslipLine("Union dues", LineKind.Deduction, 12.50m);

        Assert.Equal(-12.50m, line.SignedAmount);
    }

    [Fact]
    public void A_kind_is_not_checked_at_construction_but_an_undefined_one_has_no_signed_amount() {
        PayslipLine line = Any.PayslipLine().WithKind((LineKind)99).Generate();

        Assert.Throws<InvalidOperationException>(() => line.SignedAmount);
    }

}
