using JustDummies;

using Xunit;

namespace Shop.Domain.Tests;

public sealed class OrderLifecycleTests {

    public static TheoryData<OrderStatus> AllStatuses => new TheoryData<OrderStatus> {
        OrderStatus.Placed, OrderStatus.Paid, OrderStatus.Shipped, OrderStatus.Cancelled
    };

    public static TheoryData<OrderStatus> StatusesOtherThanPlaced => new TheoryData<OrderStatus> {
        OrderStatus.Paid, OrderStatus.Shipped, OrderStatus.Cancelled
    };

    public static TheoryData<OrderStatus> StatusesOtherThanPaid => new TheoryData<OrderStatus> {
        OrderStatus.Placed, OrderStatus.Shipped, OrderStatus.Cancelled
    };

    public static TheoryData<OrderStatus> StatusesThatCanBeCancelled => new TheoryData<OrderStatus> {
        OrderStatus.Placed, OrderStatus.Paid, OrderStatus.Cancelled
    };

    #region MarkPaid

    [Fact]
    public void A_placed_order_can_be_paid() {
        // Arrange
        Order order = OrderBuilders.OrderIn(OrderStatus.Placed);

        // Act
        order.MarkPaid();

        // Assert
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Theory]
    [MemberData(nameof(StatusesOtherThanPlaced))]
    public void An_order_that_is_not_placed_cannot_be_paid(OrderStatus status) {
        // Arrange
        Order order = OrderBuilders.OrderIn(status);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => order.MarkPaid());

        // Assert
        Assert.Contains(status.ToString(), error.Message);
        Assert.Equal(status, order.Status);
    }

    #endregion

    #region Ship

    [Fact]
    public void A_paid_order_can_be_shipped() {
        // Arrange
        Order order = OrderBuilders.OrderIn(OrderStatus.Paid);

        // Act
        order.Ship();

        // Assert
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Theory]
    [MemberData(nameof(StatusesOtherThanPaid))]
    public void An_order_that_is_not_paid_cannot_be_shipped(OrderStatus status) {
        // Arrange
        Order order = OrderBuilders.OrderIn(status);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => order.Ship());

        // Assert
        Assert.Contains(status.ToString(), error.Message);
        Assert.Equal(status, order.Status);
    }

    #endregion

    #region Cancel

    [Theory]
    [MemberData(nameof(StatusesThatCanBeCancelled))]
    public void An_order_that_has_not_shipped_can_be_cancelled(OrderStatus status) {
        // Arrange
        Order order = OrderBuilders.OrderIn(status);

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void A_shipped_order_cannot_be_cancelled() {
        // Arrange
        Order order = OrderBuilders.OrderIn(OrderStatus.Shipped);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Cancelling_twice_is_accepted() {
        // Arrange
        Order order = OrderBuilders.OrderIn(OrderStatus.Cancelled);

        // Act
        Exception? error = Record.Exception(() => order.Cancel());

        // Assert
        Assert.Null(error);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    #endregion

    #region The whole way

    [Fact]
    public void An_order_goes_from_placed_to_paid_to_shipped() {
        // Arrange
        Order anyOrder = Any.Order().Generate();

        // Act
        anyOrder.MarkPaid();
        anyOrder.Ship();

        // Assert
        Assert.Equal(OrderStatus.Shipped, anyOrder.Status);
    }

    [Fact]
    public void A_cancelled_order_cannot_come_back_to_life() {
        // Arrange
        Order order = OrderBuilders.OrderIn(OrderStatus.Cancelled);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => order.MarkPaid());
        Assert.Throws<InvalidOperationException>(() => order.Ship());
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    #endregion

}
