using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

public sealed class OrderCancelTests {

    [Fact]
    public void An_open_order_can_be_cancelled() {
        // Arrange
        Order order = Any.Order().Generate();

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void A_partially_filled_order_can_be_cancelled_and_keeps_what_it_filled() {
        // Arrange
        Order order = Any.Order().WithShares(100).AllowingPartialFills().Generate();
        order.Fill(30);

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(30, order.FilledShares);
    }

    [Fact]
    public void A_filled_order_cannot_be_cancelled() {
        // Arrange
        Order order = Any.Order().WithShares(100).Generate();
        order.Fill(100);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Fact]
    public void A_cancelled_order_cannot_be_cancelled_again() {
        // Arrange
        Order order = Any.Order().Generate();
        order.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

}

public sealed class OrderTriggerTests {

    [Fact]
    public void An_order_without_a_stop_is_triggered_by_any_last_traded_price() {
        // Arrange
        Order   order         = Any.Order().WithoutStop().Generate();
        decimal anyLastTraded = Any.Price().Generate().Amount;

        // Act
        bool triggered = order.IsTriggeredBy(anyLastTraded);

        // Assert
        Assert.True(triggered);
    }

    [Theory]
    [InlineData("9.90", false)]
    [InlineData("9.94", false)]
    [InlineData("9.95", true)]
    [InlineData("9.96", true)]
    [InlineData("10.50", true)]
    public void A_buy_stop_is_triggered_once_the_last_traded_price_reaches_it_from_below(string lastTraded, bool expected) {
        // Arrange
        Order order = Any.Order().WithSide(Side.Buy).WithTickOf(0.05m).WithLimitAt(10.00m).WithStopAt(9.95m).Generate();

        // Act
        bool triggered = order.IsTriggeredBy(Literal.Decimal(lastTraded));

        // Assert
        Assert.Equal(expected, triggered);
    }

    [Theory]
    [InlineData("10.50", false)]
    [InlineData("9.96", false)]
    [InlineData("9.95", true)]
    [InlineData("9.94", true)]
    [InlineData("9.00", true)]
    public void A_sell_stop_is_triggered_once_the_last_traded_price_falls_to_it(string lastTraded, bool expected) {
        // Arrange
        Order order = Any.Order().WithSide(Side.Sell).WithTickOf(0.05m).WithLimitAt(10.00m).WithStopAt(9.95m).Generate();

        // Act
        bool triggered = order.IsTriggeredBy(Literal.Decimal(lastTraded));

        // Assert
        Assert.Equal(expected, triggered);
    }

}
