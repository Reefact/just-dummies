using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

public sealed class OrderConstructionTests {

    [Fact]
    public void An_order_keeps_what_it_was_placed_with() {
        // Arrange
        Guid     id       = new Guid("6f1f5f4e-3c6b-4a39-9b5a-2f2b7c1d9e10");
        Ticker   ticker   = new Ticker("BRK.B");
        Quantity quantity = new Quantity(250);
        Price    limit    = new Price(10.05m);
        Price    stop     = new Price(9.95m);
        TickSize tick     = new TickSize(0.05m);
        DateTime placedAt = new DateTime(2026, 10, 27, 14, 30, 0, DateTimeKind.Utc);

        // Act
        Order order = new Order(id, ticker, Side.Sell, quantity, limit, stop, tick, TimeInForce.GoodTillCancelled, placedAt);

        // Assert
        Assert.Equal(id, order.Id);
        Assert.Equal(ticker, order.Ticker);
        Assert.Equal(Side.Sell, order.Side);
        Assert.Equal(quantity, order.Quantity);
        Assert.Equal(limit, order.Limit);
        Assert.Equal(stop, order.Stop);
        Assert.Equal(tick, order.Tick);
        Assert.Equal(TimeInForce.GoodTillCancelled, order.TimeInForce);
        Assert.Equal(placedAt, order.PlacedAt);
    }

    [Fact]
    public void A_new_order_is_open_with_nothing_filled_and_all_its_shares_remaining() {
        // Arrange
        // Act
        Order order = Any.Order().WithShares(250).Generate();

        // Assert
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0, order.FilledShares);
        Assert.Equal(250, order.RemainingShares);
    }

    [Fact]
    public void A_stop_is_optional() {
        // Act
        Order order = Any.Order().WithoutStop().Generate();

        // Assert
        Assert.Null(order.Stop);
    }

    [Fact]
    public void A_stop_one_tick_below_the_limit_is_accepted_on_either_side() {
        // Act
        Order buy  = Any.Order().WithSide(Side.Buy).WithTickOf(0.05m).WithLimitAt(10.00m).WithStopAt(9.95m).Generate();
        Order sell = Any.Order().WithSide(Side.Sell).WithTickOf(0.05m).WithLimitAt(10.00m).WithStopAt(9.95m).Generate();

        // Assert
        Assert.Equal(new Price(9.95m), buy.Stop);
        Assert.Equal(new Price(9.95m), sell.Stop);
    }

    [Fact]
    public void An_order_without_an_identifier_is_refused() {
        // Arrange
        OrderDraft draft = OrderDraft.Valid() with { Id = Guid.Empty };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => draft.Place());

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void An_order_without_a_ticker_is_refused() {
        // Arrange
        OrderDraft draft = OrderDraft.Valid() with { Ticker = null! };

        // Act & Assert
        Assert.Equal("ticker", Assert.Throws<ArgumentNullException>(() => draft.Place()).ParamName);
    }

    [Fact]
    public void An_order_without_a_quantity_is_refused() {
        // Arrange
        OrderDraft draft = OrderDraft.Valid() with { Quantity = null! };

        // Act & Assert
        Assert.Equal("quantity", Assert.Throws<ArgumentNullException>(() => draft.Place()).ParamName);
    }

    [Fact]
    public void An_order_without_a_limit_is_refused() {
        // Arrange
        OrderDraft draft = OrderDraft.Valid() with { Limit = null! };

        // Act & Assert
        Assert.Equal("limit", Assert.Throws<ArgumentNullException>(() => draft.Place()).ParamName);
    }

    [Fact]
    public void An_order_without_a_tick_is_refused() {
        // Arrange
        OrderDraft draft = OrderDraft.Valid() with { Tick = null! };

        // Act & Assert
        Assert.Equal("tick", Assert.Throws<ArgumentNullException>(() => draft.Place()).ParamName);
    }

    [Fact]
    public void The_identifier_is_checked_before_the_ticker() {
        // Arrange
        OrderDraft draft = OrderDraft.Valid() with { Id = Guid.Empty, Ticker = null! };

        // Act & Assert
        Assert.Equal("id", Assert.Throws<ArgumentException>(() => draft.Place()).ParamName);
    }

    [Fact]
    public void A_limit_between_two_ticks_is_refused() {
        // Arrange
        OrderDraft draft = OrderDraft.Valid() with { Tick = new TickSize(0.05m), Limit = new Price(10.03m), Stop = null };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => draft.Place());

        // Assert
        Assert.Equal("limit", refusal.ParamName);
    }

    [Fact]
    public void A_stop_between_two_ticks_is_refused() {
        // Arrange
        OrderDraft draft = OrderDraft.Valid() with { Tick = new TickSize(0.05m), Limit = new Price(10.00m), Stop = new Price(9.97m) };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => draft.Place());

        // Assert
        Assert.Equal("stop", refusal.ParamName);
    }

    [Theory]
    [InlineData(Side.Buy, "10.00")]
    [InlineData(Side.Sell, "10.00")]
    [InlineData(Side.Buy, "10.05")]
    [InlineData(Side.Sell, "10.05")]
    public void A_stop_at_or_above_the_limit_is_refused_on_either_side(Side side, string stop) {
        // Arrange
        OrderDraft draft = OrderDraft.Valid() with { Side = side, Tick = new TickSize(0.05m), Limit = new Price(10.00m), Stop = new Price(Literal.Decimal(stop)) };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => draft.Place());

        // Assert
        Assert.Equal("stop", refusal.ParamName);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void A_placement_time_that_is_not_utc_is_refused(DateTimeKind kind) {
        // Arrange
        DateTime anyInstant = Any.DateTime().Generate();
        OrderDraft draft    = OrderDraft.Valid() with { PlacedAt = DateTime.SpecifyKind(anyInstant, kind) };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => draft.Place());

        // Assert
        Assert.Equal("placedAt", refusal.ParamName);
    }

    [Fact]
    public void An_order_notional_is_its_whole_quantity_at_the_limit() {
        // Arrange
        Order order = Any.Order().WithShares(200).WithTickOf(0.05m).WithLimitAt(12.50m).Generate();

        // Act
        decimal notional = order.Notional();

        // Assert
        Assert.Equal(2500m, notional);
    }

    [Fact]
    public void The_notional_of_the_largest_order_the_exchange_allows_does_not_overflow() {
        // Arrange
        Order order = Any.Order().WithShares(Quantity.Maximum).WithTickOf(0.0001m).WithLimitAt(999_999.9999m).Generate();

        // Act
        decimal notional = order.Notional();

        // Assert
        Assert.Equal(999_999_999_900m, notional);
    }

}
