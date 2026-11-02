namespace Payroll.Tests.Expert;

public sealed class PayslipLineTests {

    [Fact]
    public void A_payslip_line_keeps_its_label_kind_and_amount() {
        // Act
        PayslipLine line = new PayslipLine("Transport allowance", LineKind.Bonus, 84.60m);

        // Assert
        Assert.Equal("Transport allowance", line.Label);
        Assert.Equal(LineKind.Bonus, line.Kind);
        Assert.Equal(84.60m, line.Amount);
    }

    [Fact]
    public void A_label_of_the_maximum_length_is_accepted() {
        // Arrange
        string anyLabelAtTheLimit = Any.String().Printable().NotBlank().WithLength(PayslipLine.MaximumLabelLength).Generate();

        // Act
        PayslipLine line = Any.PayslipLine().WithLabel(anyLabelAtTheLimit).Generate();

        // Assert
        Assert.Equal(40, line.Label.Length);
    }

    [Fact]
    public void A_label_one_character_over_the_maximum_length_is_refused() {
        // Arrange
        string anyLabelTooLong = Any.String().Printable().NotBlank().WithLength(PayslipLine.MaximumLabelLength + 1).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.PayslipLine().WithLabel(anyLabelTooLong).Generate());

        // Assert
        Assert.Equal("label", refusal.ParamName);
    }

    [Fact]
    public void A_null_label_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.PayslipLine().WithLabel((string)null!).Generate());

        // Assert
        Assert.Equal("label", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  \t\r\n ")]
    public void A_blank_label_is_refused(string blankLabel) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.PayslipLine().WithLabel(blankLabel).Generate());

        // Assert
        Assert.Equal("label", refusal.ParamName);
    }

    [Fact]
    public void A_label_is_not_trimmed() {
        // Act
        PayslipLine line = new PayslipLine("  Night shift ", LineKind.Bonus, 10m);

        // Assert
        Assert.Equal("  Night shift ", line.Label);
    }

    [Fact]
    public void An_amount_of_one_cent_is_accepted() {
        // Act
        PayslipLine line = Any.PayslipLine().WithAmount(0.01m).Generate();

        // Assert
        Assert.Equal(0.01m, line.Amount);
    }

    [Fact]
    public void An_amount_of_zero_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.PayslipLine().WithAmount(0m).Generate());

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        decimal anyNegativeAmount = Any.Decimal().Negative().WithScale(2).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.PayslipLine().WithAmount(anyNegativeAmount).Generate());

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData(1.005)]    // half a cent
    [InlineData(0.001)]
    [InlineData(100.999)]
    public void An_amount_with_a_fraction_of_a_cent_is_refused(double euros) {
        // Arrange
        decimal amount = (decimal)euros;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.PayslipLine().WithAmount(amount).Generate());

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void A_bonus_adds_its_amount_to_the_net_pay() {
        // Arrange
        PayslipLine bonus = new PayslipLine("Night shift", LineKind.Bonus, 75.25m);

        // Act & Assert
        Assert.Equal(75.25m, bonus.SignedAmount);
    }

    [Fact]
    public void A_deduction_takes_its_amount_off_the_net_pay() {
        // Arrange
        PayslipLine deduction = new PayslipLine("Health insurance", LineKind.Deduction, 75.25m);

        // Act & Assert
        Assert.Equal(-75.25m, deduction.SignedAmount);
    }

    [Fact]
    public void A_line_of_an_undefined_kind_is_built_but_has_no_signed_amount() {
        // Arrange
        PayslipLine line = Any.PayslipLine().WithKind((LineKind)99).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => line.SignedAmount);
    }

}
