using JustDummies;

using Xunit;

namespace Trading.Domain;

public sealed class OrderTests {

    private static readonly TickSize Penny = new TickSize(0.01m);

    private static Order Build(Guid? id = null, Price? limit = null, Price? stop = null, TickSize? tick = null, DateTime? placedAt = null,
                               Side? side = null, int shares = 100, TimeInForce? timeInForce = null) {
        Order basis = Any.Order().Generate();

        return new Order(id ?? basis.Id,
                         basis.Ticker,
                         side ?? basis.Side,
                         new Quantity(shares),
                         limit ?? new Price(100m),
                         stop,
                         tick ?? Penny,
                         timeInForce ?? basis.TimeInForce,
                         placedAt ?? basis.PlacedAt);
    }

    private static Order BuildWith(TimeInForce timeInForce, int shares = 100) {
        return Build(timeInForce: timeInForce, shares: shares);
    }

    // ---- construction ----

    [Fact]
    public void A_new_order_is_open_with_nothing_filled() {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();

        // Act
        Order order = Any.Order().WithQuantity(anyQuantity).Generate();

        // Assert
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0, order.FilledShares);
        Assert.Equal(anyQuantity.Shares, order.RemainingShares);
    }

    [Fact]
    public void An_order_keeps_what_it_was_built_from() {
        // Arrange
        Guid        anyId          = Any.Guid().NonEmpty().Generate();
        Ticker      anyTicker      = Any.Ticker().Generate();
        Side        anySide        = Any.Enum<Side>().Generate();
        Quantity    anyQuantity    = Any.Quantity().Generate();
        TimeInForce anyTimeInForce = Any.Enum<TimeInForce>().Generate();
        DateTime    anyPlacedAt    = Any.DateTime().Generate();
        TickSize    tick           = new TickSize(0.05m);
        Price       limit          = new Price(20.15m);
        Price       stop           = new Price(19.90m);

        // Act
        Order order = new Order(anyId, anyTicker, anySide, anyQuantity, limit, stop, tick, anyTimeInForce, anyPlacedAt);

        // Assert
        Assert.Equal(anyId, order.Id);
        Assert.Equal(anyTicker, order.Ticker);
        Assert.Equal(anySide, order.Side);
        Assert.Equal(anyQuantity, order.Quantity);
        Assert.Equal(limit, order.Limit);
        Assert.Equal(stop, order.Stop);
        Assert.Equal(tick, order.Tick);
        Assert.Equal(anyTimeInForce, order.TimeInForce);
        Assert.Equal(anyPlacedAt, order.PlacedAt);
    }

    [Fact]
    public void An_order_without_a_stop_has_no_stop() {
        // Act
        Order order = Any.Order().WithoutStop().Generate();

        // Assert
        Assert.Null(order.Stop);
    }

    [Fact]
    public void An_order_needs_an_identifier() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(id: Guid.Empty));

        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void An_order_needs_a_ticker_a_quantity_a_limit_and_a_tick() {
        // Arrange
        Order basis = Any.Order().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Order(basis.Id, null!, basis.Side, basis.Quantity, basis.Limit, null, basis.Tick, basis.TimeInForce, basis.PlacedAt));
        Assert.Throws<ArgumentNullException>(() => new Order(basis.Id, basis.Ticker, basis.Side, null!, basis.Limit, null, basis.Tick, basis.TimeInForce, basis.PlacedAt));
        Assert.Throws<ArgumentNullException>(() => new Order(basis.Id, basis.Ticker, basis.Side, basis.Quantity, null!, null, basis.Tick, basis.TimeInForce, basis.PlacedAt));
        Assert.Throws<ArgumentNullException>(() => new Order(basis.Id, basis.Ticker, basis.Side, basis.Quantity, basis.Limit, null, null!, basis.TimeInForce, basis.PlacedAt));
    }

    [Fact]
    public void A_limit_off_the_tick_grid_is_refused() {
        // Arrange
        TickSize nickel = new TickSize(0.05m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(limit: new Price(10.03m), tick: nickel));

        // Assert
        Assert.Equal("limit", refusal.ParamName);
    }

    [Fact]
    public void A_limit_on_the_tick_grid_is_accepted() {
        // Arrange
        TickSize nickel = new TickSize(0.05m);

        // Act
        Order order = Build(limit: new Price(10.05m), tick: nickel);

        // Assert
        Assert.Equal(10.05m, order.Limit.Amount);
    }

    [Fact]
    public void A_stop_off_the_tick_grid_is_refused() {
        // Arrange
        TickSize nickel = new TickSize(0.05m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(limit: new Price(10.05m), stop: new Price(9.98m), tick: nickel));

        // Assert
        Assert.Equal("stop", refusal.ParamName);
    }

    [Theory]
    [InlineData("100.00", Side.Buy)]
    [InlineData("100.00", Side.Sell)]
    [InlineData("100.01", Side.Buy)]
    [InlineData("100.01", Side.Sell)]
    public void A_stop_at_or_above_the_limit_is_refused_whatever_the_side(string stop, Side side) {
        // Arrange
        Price limit = new Price(100m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(limit: limit, stop: new Price(decimal.Parse(stop, System.Globalization.CultureInfo.InvariantCulture)), side: side));

        // Assert
        Assert.Equal("stop", refusal.ParamName);
    }

    [Theory]
    [InlineData(Side.Buy)]
    [InlineData(Side.Sell)]
    public void A_stop_one_tick_below_the_limit_is_accepted(Side side) {
        // Act
        Order order = Build(limit: new Price(100m), stop: new Price(99.99m), side: side);

        // Assert
        Assert.Equal(99.99m, order.Stop!.Amount);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void A_timestamp_that_is_not_utc_is_refused(DateTimeKind kind) {
        // Arrange
        DateTime anyInstant = Any.DateTime().Between(new DateTime(2000, 1, 1), new DateTime(2100, 1, 1)).Generate();
        DateTime notUtc     = DateTime.SpecifyKind(anyInstant, kind);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(placedAt: notUtc));

        // Assert
        Assert.Equal("placedAt", refusal.ParamName);
    }

    [Fact]
    public void A_utc_timestamp_is_accepted() {
        // Arrange
        DateTime anyUtcInstant = Any.DateTime().Generate();

        // Act
        Order order = Build(placedAt: anyUtcInstant);

        // Assert
        Assert.Equal(DateTimeKind.Utc, order.PlacedAt.Kind);
    }

    [Fact]
    public void The_identifier_is_checked_before_the_limit_is_matched_to_the_tick() {
        // Arrange
        TickSize nickel = new TickSize(0.05m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(id: Guid.Empty, limit: new Price(10.03m), tick: nickel));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    // ---- computations ----

    [Fact]
    public void The_notional_is_the_whole_quantity_at_the_limit() {
        // Arrange
        Order order = Build(shares: 250, limit: new Price(12.35m));

        // Assert
        Assert.Equal(3_087.50m, order.Notional());
    }

    [Fact]
    public void The_notional_ignores_what_is_already_filled() {
        // Arrange
        Order order = Build(shares: 250, limit: new Price(12.35m), timeInForce: TimeInForce.GoodTillCancelled);

        // Act
        order.Fill(100);

        // Assert
        Assert.Equal(3_087.50m, order.Notional());
    }

    [Fact]
    public void The_notional_of_the_largest_order_is_not_truncated() {
        // Arrange
        Order order = Build(shares: Quantity.Maximum, limit: new Price(999_999.9999m), tick: new TickSize(0.0001m));

        // Assert
        Assert.Equal(999_999_999_900.0000m, order.Notional());
    }

    // ---- filling ----

    [Fact]
    public void A_partial_fill_leaves_the_order_partially_filled() {
        // Arrange
        Order order = BuildWith(TimeInForce.GoodTillCancelled);

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
        Order order = BuildWith(TimeInForce.GoodTillCancelled);

        // Act
        order.Fill(30);
        order.Fill(70);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
        Assert.Equal(100, order.FilledShares);
        Assert.Equal(0, order.RemainingShares);
    }

    [Theory]
    [InlineData(TimeInForce.Day)]
    [InlineData(TimeInForce.GoodTillCancelled)]
    [InlineData(TimeInForce.ImmediateOrCancel)]
    [InlineData(TimeInForce.FillOrKill)]
    public void A_fill_of_the_whole_quantity_fills_the_order_whatever_its_time_in_force(TimeInForce timeInForce) {
        // Arrange
        Order order = BuildWith(timeInForce);

        // Act
        order.Fill(100);

        // Assert
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Theory]
    [InlineData(TimeInForce.Day)]
    [InlineData(TimeInForce.GoodTillCancelled)]
    [InlineData(TimeInForce.ImmediateOrCancel)]
    public void A_partial_fill_is_accepted_unless_the_order_is_fill_or_kill(TimeInForce timeInForce) {
        // Arrange
        Order order = BuildWith(timeInForce);

        // Act
        order.Fill(1);

        // Assert
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
    }

    [Fact]
    public void A_fill_or_kill_order_refuses_a_partial_fill_and_stays_untouched() {
        // Arrange
        Order order = BuildWith(TimeInForce.FillOrKill);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => order.Fill(99));
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0, order.FilledShares);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_fill_of_no_shares_or_fewer_is_refused(int shares) {
        // Arrange
        Order order = BuildWith(TimeInForce.GoodTillCancelled);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(shares));
    }

    [Fact]
    public void A_fill_beyond_the_remaining_shares_is_refused() {
        // Arrange
        Order order = BuildWith(TimeInForce.GoodTillCancelled);
        order.Fill(40);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(61));
        Assert.Equal(40, order.FilledShares);
    }

    [Fact]
    public void An_oversized_fill_of_a_fill_or_kill_order_is_refused_as_out_of_range_not_as_a_partial_fill() {
        // Arrange
        Order order = BuildWith(TimeInForce.FillOrKill);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(101));
    }

    [Fact]
    public void A_filled_order_cannot_be_filled_again_whatever_the_shares() {
        // Arrange
        Order order = BuildWith(TimeInForce.GoodTillCancelled);
        order.Fill(100);
        int anyShares = Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(100).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => order.Fill(anyShares));
        Assert.Throws<InvalidOperationException>(() => order.Fill(0));
    }

    [Fact]
    public void A_cancelled_order_cannot_be_filled() {
        // Arrange
        Order order = BuildWith(TimeInForce.GoodTillCancelled);
        order.Cancel();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => order.Fill(1));
    }

    // ---- cancelling ----

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
    public void A_partially_filled_order_can_be_cancelled_and_keeps_its_fills() {
        // Arrange
        Order order = BuildWith(TimeInForce.GoodTillCancelled);
        order.Fill(10);

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, order.FilledShares);
    }

    [Fact]
    public void A_filled_order_cannot_be_cancelled() {
        // Arrange
        Order order = BuildWith(TimeInForce.GoodTillCancelled);
        order.Fill(100);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Fact]
    public void A_cancelled_order_cannot_be_cancelled_twice() {
        // Arrange
        Order order = Any.Order().Generate();
        order.Cancel();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
    }

    // ---- stop triggers ----

    [Fact]
    public void An_order_without_a_stop_is_triggered_by_any_price() {
        // Arrange
        Order   order         = Any.Order().WithoutStop().Generate();
        decimal anyLastTraded = Any.Decimal().Generate();

        // Assert
        Assert.True(order.IsTriggeredBy(anyLastTraded));
    }

    [Theory]
    [InlineData("99.99", false)]
    [InlineData("100.00", true)]
    [InlineData("100.01", true)]
    public void A_buy_stop_is_triggered_when_the_last_traded_price_reaches_it_from_below(string lastTraded, bool expected) {
        // Arrange
        Order order = Build(side: Side.Buy, limit: new Price(105m), stop: new Price(100m));

        // Act
        bool triggered = order.IsTriggeredBy(decimal.Parse(lastTraded, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(expected, triggered);
    }

    [Theory]
    [InlineData("99.99", true)]
    [InlineData("100.00", true)]
    [InlineData("100.01", false)]
    public void A_sell_stop_is_triggered_when_the_last_traded_price_falls_to_it(string lastTraded, bool expected) {
        // Arrange
        Order order = Build(side: Side.Sell, limit: new Price(105m), stop: new Price(100m));

        // Act
        bool triggered = order.IsTriggeredBy(decimal.Parse(lastTraded, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(expected, triggered);
    }

}
