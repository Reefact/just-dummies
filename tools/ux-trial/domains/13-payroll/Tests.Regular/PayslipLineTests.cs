using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Regular;

public sealed class PayslipLineTests {

    public static TheoryData<decimal> AmountsNotInWholeCents => new TheoryData<decimal> { 10.005m, 0.001m, 99.999m };

    [Fact]
    public void A_line_keeps_what_it_was_built_from() {
        // Act
        PayslipLine line = new PayslipLine("Transport allowance", LineKind.Bonus, 45.30m);

        // Assert
        Assert.Equal("Transport allowance", line.Label);
        Assert.Equal(LineKind.Bonus, line.Kind);
        Assert.Equal(45.30m, line.Amount);
    }

    [Fact]
    public void A_bonus_adds_its_amount_to_the_net_pay() {
        // Arrange
        PayslipLine anyBonus = Any.PayslipLine().WithKind(LineKind.Bonus).WithAmount(25.50m).Generate();

        // Act
        decimal signedAmount = anyBonus.SignedAmount;

        // Assert
        Assert.Equal(25.50m, signedAmount);
    }

    [Fact]
    public void A_deduction_takes_its_amount_off_the_net_pay() {
        // Arrange
        PayslipLine anyDeduction = Any.PayslipLine().WithKind(LineKind.Deduction).WithAmount(25.50m).Generate();

        // Act
        decimal signedAmount = anyDeduction.SignedAmount;

        // Assert
        Assert.Equal(-25.50m, signedAmount);
    }

    [Fact]
    public void A_line_of_an_undefined_kind_can_be_built_but_has_no_signed_amount() {
        // Arrange
        PayslipLine anyLine = Any.PayslipLine().WithKind((LineKind)99).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyLine.SignedAmount);
    }

    [Fact]
    public void A_null_label_is_refused() {
        // Arrange
        LineKind anyKind   = Any.Enum<LineKind>().Generate();
        decimal  anyAmount = Any.PayslipLine().Generate().Amount;

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new PayslipLine(null!, anyKind, anyAmount));

        // Assert
        Assert.Equal("label", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData(" \r\n ")]
    public void A_blank_label_is_refused(string label) {
        // Arrange
        LineKind anyKind   = Any.Enum<LineKind>().Generate();
        decimal  anyAmount = Any.PayslipLine().Generate().Amount;

        // Act
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() => new PayslipLine(label, anyKind, anyAmount));

        // Assert
        Assert.Equal("label", exception.ParamName);
    }

    [Fact]
    public void A_label_of_the_maximum_length_is_accepted() {
        // Arrange
        string anyLongestLabel = Any.String().Alpha().WithLength(PayslipLine.MaximumLabelLength).Generate();

        // Act
        PayslipLine line = new PayslipLine(anyLongestLabel, LineKind.Bonus, 10m);

        // Assert
        Assert.Equal(PayslipLine.MaximumLabelLength, line.Label.Length);
    }

    [Fact]
    public void A_label_one_character_too_long_is_refused() {
        // Arrange
        string anyTooLongLabel = Any.String().Alpha().WithLength(PayslipLine.MaximumLabelLength + 1).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PayslipLine(anyTooLongLabel, LineKind.Bonus, 10m));

        // Assert
        Assert.Equal("label", exception.ParamName);
    }

    [Fact]
    public void The_maximum_label_length_is_forty() {
        Assert.Equal(40, PayslipLine.MaximumLabelLength);
    }

    [Fact]
    public void A_zero_amount_is_refused() {
        // Arrange
        string anyLabel = Any.PayslipLine().Generate().Label;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayslipLine(anyLabel, LineKind.Deduction, 0m));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        string  anyLabel          = Any.PayslipLine().Generate().Label;
        decimal anyNegativeAmount = Any.Decimal().Negative().WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new PayslipLine(anyLabel, LineKind.Bonus, anyNegativeAmount));
    }

    [Theory]
    [MemberData(nameof(AmountsNotInWholeCents))]
    public void An_amount_that_is_not_in_whole_cents_is_refused(decimal amount) {
        // Arrange
        string anyLabel = Any.PayslipLine().Generate().Label;

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PayslipLine(anyLabel, LineKind.Bonus, amount));

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void The_smallest_amount_is_one_cent() {
        // Act
        PayslipLine line = Any.PayslipLine().WithKind(LineKind.Bonus).WithAmount(0.01m).Generate();

        // Assert
        Assert.Equal(0.01m, line.SignedAmount);
    }

}
