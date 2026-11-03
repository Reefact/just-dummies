namespace LiquidityGap.Domain.Tests.Expert.Tests;

public sealed class ContractualFlowTests {

    [Fact]
    public void An_inflow_is_signed_positive_from_the_banks_point_of_view() {
        // Arrange
        ContractualFlow anyInflow = Any.ContractualFlow().WithAmount(125.50m).WithDirection(FlowDirection.Inflow).Generate();

        // Assert
        Assert.Equal(125.50m, anyInflow.SignedAmount);
        Assert.Equal(125.50m, anyInflow.Amount);
    }

    [Fact]
    public void An_outflow_is_signed_negative_but_keeps_a_positive_amount() {
        // Arrange
        ContractualFlow anyOutflow = Any.ContractualFlow().WithAmount(125.50m).WithDirection(FlowDirection.Outflow).Generate();

        // Assert
        Assert.Equal(-125.50m, anyOutflow.SignedAmount);
        Assert.Equal(125.50m, anyOutflow.Amount);
    }

    [Fact]
    public void The_smallest_amount_is_accepted() {
        // Act
        ContractualFlow anyFlow = Any.ContractualFlow().WithAmount(0.000001m).Generate();

        // Assert
        Assert.Equal(0.000001m, anyFlow.Amount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-1000")]
    public void A_zero_or_negative_amount_is_refused(string amount) {
        // Arrange
        decimal notPositive = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.ContractualFlow().WithAmount(notPositive).Generate());

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_identifier_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.ContractualFlow().WithFlowId(blank).Generate());

        // Assert
        Assert.Equal("flowId", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.ContractualFlow().WithCurrency((Currency)null!).Generate());

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void An_undefined_direction_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.ContractualFlow().WithDirection((FlowDirection)42).Generate());

        // Assert
        Assert.Equal("direction", refusal.ParamName);
    }

}
