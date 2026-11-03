using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class ContractualFlowTests {

    [Fact]
    public void An_inflow_is_signed_positive() {
        // Arrange
        ContractualFlow inflow = Any.ContractualFlow()
                                    .WithAmount(125.40m)
                                    .WithDirection(FlowDirection.Inflow)
                                    .Generate();

        // Act
        decimal signedAmount = inflow.SignedAmount;

        // Assert
        Assert.Equal(125.40m, signedAmount);
    }

    [Fact]
    public void An_outflow_is_signed_negative() {
        // Arrange
        ContractualFlow outflow = Any.ContractualFlow()
                                     .WithAmount(125.40m)
                                     .WithDirection(FlowDirection.Outflow)
                                     .Generate();

        // Act
        decimal signedAmount = outflow.SignedAmount;

        // Assert
        Assert.Equal(-125.40m, signedAmount);
    }

    [Fact]
    public void A_flow_keeps_its_amount_unsigned_whatever_its_direction() {
        // Arrange
        ContractualFlow anyFlow = Any.ContractualFlow().WithAmount(10m).Generate();

        // Act
        decimal amount = anyFlow.Amount;

        // Assert
        Assert.Equal(10m, amount);
    }

    [Fact]
    public void A_flow_keeps_the_identity_it_was_built_with() {
        // Arrange
        Currency   currency = new Currency("CHF");
        DateOnly   date     = new DateOnly(2026, 3, 9);
        ContractualFlow flow = new ContractualFlow("LN-7", date, 1m, currency, FlowDirection.Outflow);

        // Act & Assert
        Assert.Equal("LN-7", flow.FlowId);
        Assert.Equal(date, flow.ValueDate);
        Assert.Equal(currency, flow.Currency);
        Assert.Equal(FlowDirection.Outflow, flow.Direction);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    [InlineData(null)]
    public void A_blank_identifier_is_refused(string? blankId) {
        // Arrange
        DateOnly        anyDate      = Any.DateOnly().Generate();
        Currency        anyCurrency  = Any.Currency().Generate();
        FlowDirection   anyDirection = Any.Enum<FlowDirection>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContractualFlow(blankId!, anyDate, 1m, anyCurrency, anyDirection));

        // Assert
        Assert.Equal("flowId", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-1000")]
    public void An_amount_that_is_not_strictly_positive_is_refused(string invalidAmount) {
        // Arrange
        string        anyId        = Any.String().NotBlank().Generate();
        DateOnly      anyDate      = Any.DateOnly().Generate();
        Currency      anyCurrency  = Any.Currency().Generate();
        FlowDirection anyDirection = Any.Enum<FlowDirection>().Generate();
        decimal       amount       = decimal.Parse(invalidAmount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ContractualFlow(anyId, anyDate, amount, anyCurrency, anyDirection));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void The_smallest_cent_is_an_acceptable_amount() {
        // Act
        ContractualFlow flow = Any.ContractualFlow().WithAmount(0.01m).Generate();

        // Assert
        Assert.Equal(0.01m, flow.Amount);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(100)]
    public void A_direction_that_is_not_declared_is_refused(int undeclared) {
        // Arrange
        string   anyId       = Any.String().NotBlank().Generate();
        DateOnly anyDate     = Any.DateOnly().Generate();
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ContractualFlow(anyId, anyDate, 1m, anyCurrency, (FlowDirection)undeclared));

        // Assert
        Assert.Equal("direction", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Arrange
        string        anyId        = Any.String().NotBlank().Generate();
        DateOnly      anyDate      = Any.DateOnly().Generate();
        FlowDirection anyDirection = Any.Enum<FlowDirection>().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new ContractualFlow(anyId, anyDate, 1m, null!, anyDirection));

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

}
