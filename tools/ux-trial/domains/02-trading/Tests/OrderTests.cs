using JustDummies;
using JustDummies.Xunit;

using Trading.Domain;

using Xunit;

namespace Trading.Tests;

[Reproducible]
public sealed class OrderTests {

    private static readonly TickSize Cent = new(0.01m);

    [Fact]
    public void The_notional_of_an_order_is_its_quantity_times_its_limit() {
        // Arrange
        Order anyOrderOf300At12_50 = Arbitrary.Order()
                                              .WithQuantity(new Quantity(300))
                                              .WithLimit(new Price(12.50m))
                                              .Generate();

        // Act
        decimal notional = anyOrderOf300At12_50.Notional();

        // Assert
        Assert.Equal(3_750m, notional);
    }

    [Fact]
    public void A_partial_fill_leaves_the_rest_of_the_order_open() {
        // Arrange
        Order anyOrderOf500 = Arbitrary.Order()
                                       .WithQuantity(new Quantity(500))
                                       .WithTimeInForce(timeInForce => timeInForce.DifferentFrom(TimeInForce.FillOrKill))
                                       .Generate();

        // Act
        anyOrderOf500.Fill(200);

        // Assert
        Assert.Equal(OrderStatus.PartiallyFilled, anyOrderOf500.Status);
        Assert.Equal(300, anyOrderOf500.RemainingShares);
    }

    [Fact]
    public void Filling_the_whole_quantity_fills_the_order() {
        // Arrange
        Order anyOrderOf500 = Arbitrary.Order().WithQuantity(new Quantity(500)).Generate();

        // Act
        anyOrderOf500.Fill(500);

        // Assert
        Assert.Equal(OrderStatus.Filled, anyOrderOf500.Status);
    }

    [Fact]
    public void A_fill_or_kill_order_refuses_a_partial_fill() {
        // Arrange
        Order anyFillOrKillOf500 = Arbitrary.Order()
                                            .WithQuantity(new Quantity(500))
                                            .WithTimeInForce(TimeInForce.FillOrKill)
                                            .Generate();

        // Act
        void PartialFill() => anyFillOrKillOf500.Fill(499);

        // Assert
        Assert.Throws<InvalidOperationException>(PartialFill);
        Assert.Equal(OrderStatus.Open, anyFillOrKillOf500.Status);
    }

    [Fact]
    public void A_filled_order_cannot_be_cancelled() {
        // Arrange
        Order anyOrderOf100 = Arbitrary.Order().WithQuantity(new Quantity(100)).Generate();
        anyOrderOf100.Fill(100);

        // Act
        void Cancel() => anyOrderOf100.Cancel();

        // Assert
        Assert.Throws<InvalidOperationException>(Cancel);
    }

    [Fact]
    public void A_buy_stop_triggers_once_the_market_trades_at_the_stop() {
        // Arrange
        Order anyBuyStopAt10 = Arbitrary.Order()
                                        .WithSide(Side.Buy)
                                        .WithTick(Cent)
                                        .WithLimit(new Price(10.50m))
                                        .WithStop(new Price(10.00m))
                                        .Generate();

        // Act
        bool triggered = anyBuyStopAt10.IsTriggeredBy(10.00m);

        // Assert
        Assert.True(triggered);
    }

    [Fact]
    public void A_sell_stop_does_not_trigger_while_the_market_trades_above_it() {
        // Arrange
        Order anySellStopAt10 = Arbitrary.Order()
                                         .WithSide(Side.Sell)
                                         .WithTick(Cent)
                                         .WithLimit(new Price(10.50m))
                                         .WithStop(new Price(10.00m))
                                         .Generate();

        // Act
        bool triggered = anySellStopAt10.IsTriggeredBy(10.01m);

        // Assert
        Assert.False(triggered);
    }

    [Fact]
    public void An_order_refuses_a_limit_off_the_tick_grid() {
        // Arrange
        AnyOrder anyOrderOnANickelGrid = Arbitrary.Order().WithTick(new TickSize(0.05m));

        // Act
        void Place() => anyOrderOnANickelGrid.WithLimit(new Price(10.03m)).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(Place);
        Assert.Equal("limit", refusal.ParamName);
    }

    [Fact]
    public void An_order_refuses_a_stop_at_its_limit() {
        // Arrange
        AnyOrder anyOrderLimitedAt10 = Arbitrary.Order().WithTick(Cent).WithLimit(new Price(10.00m));

        // Act
        void Place() => anyOrderLimitedAt10.WithStop(new Price(10.00m)).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(Place);
        Assert.Equal("stop", refusal.ParamName);
    }

    [Theory]
    [InlineData(Side.Buy, 10.05)]
    [InlineData(Side.Sell, 10.10)]
    public void Rounding_to_the_tick_never_worsens_the_order(Side side, decimal expected) {
        // Arrange
        TickSize nickel = new(0.05m);

        // Act
        decimal rounded = nickel.Round(10.07m, side);

        // Assert
        Assert.Equal(expected, rounded);
    }

}
