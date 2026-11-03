using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class ContractualFlowTests {

    [Fact]
    public void An_inflow_is_signed_positive() {
        // Arrange
        ContractualFlow inflow = Any.ContractualFlow().WithAmount(125.50m).WithDirection(FlowDirection.Inflow).Generate();

        // Assert
        Assert.Equal(125.50m, inflow.Amount);
        Assert.Equal(125.50m, inflow.SignedAmount);
    }

    [Fact]
    public void An_outflow_is_signed_negative_while_its_amount_stays_positive() {
        // Arrange
        ContractualFlow outflow = Any.ContractualFlow().WithAmount(125.50m).WithDirection(FlowDirection.Outflow).Generate();

        // Assert
        Assert.Equal(125.50m, outflow.Amount);
        Assert.Equal(-125.50m, outflow.SignedAmount);
    }

    [Fact]
    public void A_flow_keeps_what_it_was_built_with() {
        // Arrange
        DateOnly valueDate = new DateOnly(2026, 3, 31);
        Currency usd       = Ladders.Usd;

        // Act
        ContractualFlow flow = new ContractualFlow("F-1", valueDate, 10m, usd, FlowDirection.Outflow);

        // Assert
        Assert.Equal("F-1", flow.FlowId);
        Assert.Equal(valueDate, flow.ValueDate);
        Assert.Same(usd, flow.Currency);
        Assert.Equal(FlowDirection.Outflow, flow.Direction);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-1000")]
    public void A_flow_amount_that_is_not_strictly_positive_is_refused(string amount) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.ContractualFlow().WithAmount(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)).Generate());

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void The_smallest_cent_is_a_valid_amount() {
        // Act
        ContractualFlow flow = Any.ContractualFlow().WithAmount(0.01m).Generate();

        // Assert
        Assert.Equal(0.01m, flow.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_flow_id_is_refused(string flowId) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.ContractualFlow().WithFlowId(flowId).Generate());

        // Assert
        Assert.Equal("flowId", refusal.ParamName);
    }

    [Fact]
    public void An_unknown_direction_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.ContractualFlow().WithDirection((FlowDirection)42).Generate());

        // Assert
        Assert.Equal("direction", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Any.ContractualFlow().WithCurrency((Currency)null!).Generate());

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

}
