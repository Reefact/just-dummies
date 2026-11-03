using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

public sealed class OrderFillTests {

    [Fact]
    public void A_partial_fill_leaves_the_order_partially_filled_with_the_rest_remaining() {
        // Arrange
        Order order = Any.Order().WithShares(100).AllowingPartialFills().Generate();

        // Act
        order.Fill(30);

        // Assert
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
        Assert.Equal(30, order.FilledShares);
        Assert.Equal(70, order.RemainingShares);
    }

    [Fact]
    public void Successive_fills_add_up_until_the_order_is_filled() {
        // Arrange
        Order order = Any.Order().WithShares(100).AllowingPartialFills().Generate();

        // Act
        order.Fill(30);
        order.Fill(70);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
        Assert.Equal(100, order.FilledShares);
        Assert.Equal(0, order.RemainingShares);
    }

    [Fact]
    public void Filling_every_share_at_once_fills_the_order_whatever_its_time_in_force() {
        // Arrange
        Order order = Any.Order().WithShares(100).Generate();

        // Act
        order.Fill(100);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
        Assert.Equal(0, order.RemainingShares);
    }

    [Fact]
    public void A_fill_of_a_single_share_is_accepted() {
        // Arrange
        Order order = Any.Order().WithShares(100).AllowingPartialFills().Generate();

        // Act
        order.Fill(1);

        // Assert
        Assert.Equal(99, order.RemainingShares);
    }

    [Fact]
    public void A_one_share_order_is_filled_by_its_only_share() {
        // Arrange
        Order order = Any.Order().WithShares(1).Generate();

        // Act
        order.Fill(1);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_fill_of_no_share_or_fewer_is_refused(int shares) {
        // Arrange
        Order order = Any.Order().WithShares(100).AllowingPartialFills().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(shares));

        // Assert
        Assert.Equal("shares", refusal.ParamName);
    }

    [Fact]
    public void A_fill_beyond_the_shares_remaining_is_refused_and_changes_nothing() {
        // Arrange
        Order order = Any.Order().WithShares(100).AllowingPartialFills().Generate();
        order.Fill(30);

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(71));

        // Assert
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
        Assert.Equal(30, order.FilledShares);
    }

    [Fact]
    public void A_fill_of_exactly_the_shares_remaining_is_accepted() {
        // Arrange
        Order order = Any.Order().WithShares(100).AllowingPartialFills().Generate();
        order.Fill(30);

        // Act
        order.Fill(70);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Fact]
    public void A_fill_of_more_than_the_whole_quantity_is_refused() {
        // Arrange
        Order order = Any.Order().WithShares(100).AllowingPartialFills().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(101));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(101)]
    public void A_filled_order_refuses_any_further_fill_before_looking_at_its_size(int shares) {
        // Arrange
        Order order = Any.Order().WithShares(100).Generate();
        order.Fill(100);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Fill(shares));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(101)]
    public void A_cancelled_order_refuses_any_fill_before_looking_at_its_size(int shares) {
        // Arrange
        Order order = Any.Order().WithShares(100).Generate();
        order.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Fill(shares));
    }

    [Fact]
    public void A_partially_filled_order_that_was_cancelled_cannot_be_filled_any_more() {
        // Arrange
        Order order = Any.Order().WithShares(100).AllowingPartialFills().Generate();
        order.Fill(30);
        order.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Fill(10));
        Assert.Equal(30, order.FilledShares);
    }

    [Fact]
    public void A_fill_or_kill_order_refuses_a_partial_fill_and_changes_nothing() {
        // Arrange
        Order order = Any.Order().WithShares(100).WithTimeInForce(TimeInForce.FillOrKill).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => order.Fill(99));

        // Assert
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0, order.FilledShares);
    }

    [Fact]
    public void A_fill_or_kill_order_accepts_a_complete_fill() {
        // Arrange
        Order order = Any.Order().WithShares(100).WithTimeInForce(TimeInForce.FillOrKill).Generate();

        // Act
        order.Fill(100);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Fact]
    public void A_fill_or_kill_order_checks_the_size_of_a_fill_before_its_completeness() {
        // Arrange
        Order order = Any.Order().WithShares(100).WithTimeInForce(TimeInForce.FillOrKill).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(101));
    }

    [Theory]
    [InlineData(TimeInForce.Day)]
    [InlineData(TimeInForce.GoodTillCancelled)]
    [InlineData(TimeInForce.ImmediateOrCancel)]
    public void Every_time_in_force_but_fill_or_kill_accepts_a_partial_fill(TimeInForce timeInForce) {
        // Arrange
        Order order = Any.Order().WithShares(100).WithTimeInForce(timeInForce).Generate();

        // Act
        order.Fill(40);

        // Assert
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
    }

    [Fact]
    public void Filling_does_not_change_the_notional_which_is_always_the_whole_quantity() {
        // Arrange
        Order order = Any.Order().WithShares(200).AllowingPartialFills().WithTickOf(0.05m).WithLimitAt(12.50m).Generate();
        order.Fill(50);

        // Act
        decimal notional = order.Notional();

        // Assert
        Assert.Equal(2500m, notional);
    }

}
