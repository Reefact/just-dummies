namespace Trading.Tests.Regular;

public sealed class OrderLifecycleTests {

    /// <summary>An order of 100 shares that can be filled in pieces: the quantity and the time in force are what the fill rules are about.</summary>
    private static Order AnyPartiallyFillableOrder() {
        return Any.Order()
                  .WithQuantity(new Quantity(100))
                  .WithTimeInForce(TimeInForce.GoodTillCancelled)
                  .Generate();
    }

    private static Order AnyFilledOrder() {
        Order order = AnyPartiallyFillableOrder();
        order.Fill(100);

        return order;
    }

    private static Order AnyCancelledOrder() {
        Order order = Any.Order().Generate();
        order.Cancel();

        return order;
    }

    // ---------------------------------------------------------------- Fill

    [Fact]
    public void A_partial_fill_leaves_the_order_partially_filled() {
        // Arrange
        Order order = AnyPartiallyFillableOrder();

        // Act
        order.Fill(30);

        // Assert
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
        Assert.Equal(30, order.FilledShares);
        Assert.Equal(70, order.RemainingShares);
    }

    [Fact]
    public void Fills_accumulate_until_the_order_is_filled() {
        // Arrange
        Order order = AnyPartiallyFillableOrder();

        // Act
        order.Fill(30);
        order.Fill(50);
        OrderStatus afterTwo = order.Status;
        order.Fill(20);

        // Assert
        Assert.Equal(OrderStatus.PartiallyFilled, afterTwo);
        Assert.Equal(OrderStatus.Filled, order.Status);
        Assert.Equal(100, order.FilledShares);
        Assert.Equal(0, order.RemainingShares);
    }

    [Fact]
    public void Filling_every_share_at_once_fills_the_order() {
        // Arrange
        Order order = AnyPartiallyFillableOrder();

        // Act
        order.Fill(100);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
        Assert.Equal(0, order.RemainingShares);
    }

    [Fact]
    public void Filling_the_last_share_fills_the_order() {
        // Arrange
        Order order = AnyPartiallyFillableOrder();
        order.Fill(99);

        // Act
        order.Fill(1);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Fact]
    public void Filling_a_single_share_is_a_partial_fill() {
        // Arrange
        Order order = AnyPartiallyFillableOrder();

        // Act
        order.Fill(1);

        // Assert
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
        Assert.Equal(99, order.RemainingShares);
    }

    [Theory]
    [InlineData(TimeInForce.Day)]
    [InlineData(TimeInForce.GoodTillCancelled)]
    [InlineData(TimeInForce.ImmediateOrCancel)]
    public void An_order_that_is_not_fill_or_kill_accepts_a_partial_fill(TimeInForce timeInForce) {
        // Arrange
        Order order = Any.Order().WithQuantity(new Quantity(100)).WithTimeInForce(timeInForce).Generate();

        // Act
        order.Fill(40);

        // Assert
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_fill_of_nothing_or_less_is_refused(int shares) {
        // Arrange
        Order order = AnyPartiallyFillableOrder();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(shares));

        // Assert
        Assert.Equal("shares", refusal.ParamName);
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0, order.FilledShares);
    }

    [Fact]
    public void A_fill_above_the_remaining_shares_is_refused_and_changes_nothing() {
        // Arrange
        Order order = AnyPartiallyFillableOrder();
        order.Fill(60);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(41));

        // Assert
        Assert.Equal("shares", refusal.ParamName);
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
        Assert.Equal(60, order.FilledShares);
    }

    [Fact]
    public void A_fill_above_the_whole_quantity_is_refused() {
        // Arrange
        Order order = AnyPartiallyFillableOrder();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(101));
    }

    [Fact]
    public void A_filled_order_cannot_be_filled_again() {
        // Arrange
        Order order = AnyFilledOrder();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => order.Fill(1));

        // Assert
        Assert.Contains("Filled", refusal.Message);
        Assert.Equal(100, order.FilledShares);
    }

    [Fact]
    public void A_cancelled_order_cannot_be_filled() {
        // Arrange
        Order order = AnyCancelledOrder();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => order.Fill(1));

        // Assert
        Assert.Contains("Cancelled", refusal.Message);
        Assert.Equal(0, order.FilledShares);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10_000)]
    public void The_status_is_checked_before_the_number_of_shares(int shares) {
        // Arrange
        Order order = AnyFilledOrder();

        // Act & Assert: an invalid-operation error, not an out-of-range one
        Assert.Throws<InvalidOperationException>(() => order.Fill(shares));
    }

    [Fact]
    public void A_cancelled_partially_filled_order_cannot_be_filled_further() {
        // Arrange
        Order order = AnyPartiallyFillableOrder();
        order.Fill(10);
        order.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Fill(10));
    }

    // ---------------------------------------------------------------- fill or kill

    [Fact]
    public void A_fill_or_kill_order_accepts_a_complete_fill() {
        // Arrange
        Order order = Any.Order().WithQuantity(new Quantity(100)).WithTimeInForce(TimeInForce.FillOrKill).Generate();

        // Act
        order.Fill(100);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(99)]
    public void A_fill_or_kill_order_refuses_a_partial_fill_and_changes_nothing(int shares) {
        // Arrange
        Order order = Any.Order().WithQuantity(new Quantity(100)).WithTimeInForce(TimeInForce.FillOrKill).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => order.Fill(shares));

        // Assert
        Assert.Contains("fill-or-kill", refusal.Message);
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0, order.FilledShares);
    }

    [Fact]
    public void A_fill_or_kill_order_above_the_quantity_is_out_of_range_before_it_is_a_partial_fill_question() {
        // Arrange
        Order order = Any.Order().WithQuantity(new Quantity(100)).WithTimeInForce(TimeInForce.FillOrKill).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(101));
    }

    [Fact]
    public void A_fill_or_kill_order_of_a_single_share_is_filled_by_one_share() {
        // Arrange
        Order order = Any.Order().WithQuantity(new Quantity(1)).WithTimeInForce(TimeInForce.FillOrKill).Generate();

        // Act
        order.Fill(1);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    // ---------------------------------------------------------------- Cancel

    [Fact]
    public void An_open_order_can_be_cancelled() {
        // Arrange
        Order anyOrder = Any.Order().Generate();

        // Act
        anyOrder.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, anyOrder.Status);
    }

    [Fact]
    public void A_partially_filled_order_can_be_cancelled_and_keeps_what_was_filled() {
        // Arrange
        Order order = AnyPartiallyFillableOrder();
        order.Fill(40);

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(40, order.FilledShares);
        Assert.Equal(60, order.RemainingShares);
    }

    [Fact]
    public void A_filled_order_cannot_be_cancelled() {
        // Arrange
        Order order = AnyFilledOrder();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => order.Cancel());

        // Assert
        Assert.Contains("Filled", refusal.Message);
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Fact]
    public void A_cancelled_order_cannot_be_cancelled_again() {
        // Arrange
        Order order = AnyCancelledOrder();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => order.Cancel());

        // Assert
        Assert.Contains("Cancelled", refusal.Message);
    }

    // ---------------------------------------------------------------- IsTriggeredBy

    [Theory]
    [InlineData(-1000)]
    [InlineData(0)]
    [InlineData(0.0001)]
    [InlineData(100)]
    [InlineData(1_000_000_000)]
    public void An_order_without_a_stop_is_triggered_by_any_price(double lastTraded) {
        // Arrange
        Order order = Any.Order().WithoutStop().Generate();

        // Act
        bool triggered = order.IsTriggeredBy((decimal)lastTraded);

        // Assert
        Assert.True(triggered);
    }

    public static TheoryData<Side, decimal, bool> StopTriggers => new() {
        { Side.Buy, 99.99m, false },
        { Side.Buy, 100.00m, true },
        { Side.Buy, 100.01m, true },
        { Side.Buy, 150m, true },
        { Side.Buy, 0m, false },
        { Side.Sell, 100.01m, false },
        { Side.Sell, 100.00m, true },
        { Side.Sell, 99.99m, true },
        { Side.Sell, 0m, true },
        { Side.Sell, 150m, false }
    };

    [Theory]
    [MemberData(nameof(StopTriggers))]
    public void A_stop_is_triggered_inclusively_from_above_by_a_buy_and_from_below_by_a_sell(Side side, decimal lastTraded, bool expected) {
        // Arrange
        Order order = Any.Order()
                         .WithSide(side)
                         .WithTick(new TickSize(0.01m))
                         .WithLimit(new Price(110.00m))
                         .WithStop(new Price(100.00m))
                         .Generate();

        // Act
        bool triggered = order.IsTriggeredBy(lastTraded);

        // Assert
        Assert.Equal(expected, triggered);
    }

}
