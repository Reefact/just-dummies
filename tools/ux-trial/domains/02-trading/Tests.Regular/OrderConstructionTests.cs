namespace Trading.Tests.Regular;

public sealed class OrderConstructionTests {

    private static readonly DateTime Utc         = new DateTime(2026, 10, 27, 14, 30, 0, DateTimeKind.Utc);
    private static readonly TickSize Cent        = new TickSize(0.01m);
    private static readonly Price    OneHundred  = new Price(100.00m);
    private static readonly Price    NinetyNine  = new Price(99.99m);

    private static Order Construct(Guid      id,
                                   Ticker?   ticker,
                                   Quantity? quantity,
                                   Price?    limit,
                                   Price?    stop,
                                   TickSize? tick,
                                   DateTime  placedAt) {
        Side        anySide        = Any.Enum<Side>().Generate();
        TimeInForce anyTimeInForce = Any.Enum<TimeInForce>().Generate();

        return new Order(id, ticker!, anySide, quantity!, limit!, stop, tick!, anyTimeInForce, placedAt);
    }

    [Fact]
    public void An_order_keeps_everything_it_was_built_with() {
        // Arrange
        Guid     id       = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");
        Ticker   ticker   = new Ticker("AAPL");
        Quantity quantity = new Quantity(250);
        Price    limit    = new Price(187.25m);
        Price    stop     = new Price(185.00m);
        TickSize tick     = new TickSize(0.25m);

        // Act
        Order order = new Order(id, ticker, Side.Sell, quantity, limit, stop, tick, TimeInForce.GoodTillCancelled, Utc);

        // Assert
        Assert.Equal(id, order.Id);
        Assert.Same(ticker, order.Ticker);
        Assert.Equal(Side.Sell, order.Side);
        Assert.Same(quantity, order.Quantity);
        Assert.Same(limit, order.Limit);
        Assert.Same(stop, order.Stop);
        Assert.Same(tick, order.Tick);
        Assert.Equal(TimeInForce.GoodTillCancelled, order.TimeInForce);
        Assert.Equal(Utc, order.PlacedAt);
    }

    [Fact]
    public void A_new_order_is_open_with_nothing_filled() {
        // Act
        Order order = Any.Order().WithQuantity(new Quantity(100)).Generate();

        // Assert
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0, order.FilledShares);
        Assert.Equal(100, order.RemainingShares);
    }

    [Fact]
    public void An_order_without_a_stop_has_none() {
        // Act
        Order order = Any.Order().WithoutStop().Generate();

        // Assert
        Assert.Null(order.Stop);
    }

    [Fact]
    public void An_order_refuses_an_empty_identifier() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Construct(Guid.Empty, Any.Ticker().Generate(), new Quantity(10), OneHundred, null, Cent, Utc));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void An_order_checks_its_identifier_before_anything_else() {
        // Act: an empty identifier and every other argument wrong as well
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Construct(Guid.Empty, null, null, null, null, null, DateTime.Now));

        // Assert: a plain argument error naming the id, not the null the ticker would raise
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void An_order_refuses_a_missing_ticker() {
        // Arrange
        Guid anyId = Any.Guid().NonEmpty().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Construct(anyId, null, new Quantity(10), OneHundred, null, Cent, Utc));

        // Assert
        Assert.Equal("ticker", refusal.ParamName);
    }

    [Fact]
    public void An_order_refuses_a_missing_quantity() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Construct(anyId, anyTicker, null, OneHundred, null, Cent, Utc));

        // Assert
        Assert.Equal("quantity", refusal.ParamName);
    }

    [Fact]
    public void An_order_refuses_a_missing_limit() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Construct(anyId, anyTicker, new Quantity(10), null, null, Cent, Utc));

        // Assert
        Assert.Equal("limit", refusal.ParamName);
    }

    [Fact]
    public void An_order_refuses_a_missing_tick() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Construct(anyId, anyTicker, new Quantity(10), OneHundred, null, null, Utc));

        // Assert
        Assert.Equal("tick", refusal.ParamName);
    }

    [Fact]
    public void An_order_refuses_a_limit_off_the_tick_grid() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Construct(anyId, anyTicker, new Quantity(10), new Price(100.01m), null, new TickSize(0.05m), Utc));

        // Assert
        Assert.Equal("limit", refusal.ParamName);
        Assert.Contains("0.05", refusal.Message);
    }

    [Fact]
    public void An_order_accepts_a_limit_on_the_tick_grid() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act
        Order order = Construct(anyId, anyTicker, new Quantity(10), new Price(100.05m), null, new TickSize(0.05m), Utc);

        // Assert
        Assert.Equal(100.05m, order.Limit.Amount);
    }

    [Fact]
    public void An_order_checks_the_limit_grid_before_the_timestamp() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act: both the limit and the timestamp are wrong
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Construct(anyId, anyTicker, new Quantity(10), new Price(100.01m), null, new TickSize(0.05m), DateTime.Now));

        // Assert
        Assert.Equal("limit", refusal.ParamName);
    }

    [Fact]
    public void An_order_refuses_a_stop_off_the_tick_grid() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Construct(anyId, anyTicker, new Quantity(10), OneHundred, new Price(95.03m), new TickSize(0.05m), Utc));

        // Assert
        Assert.Equal("stop", refusal.ParamName);
        Assert.Contains("multiple of the tick", refusal.Message);
    }

    [Theory]
    [InlineData("100.00")]
    [InlineData("100.01")]
    [InlineData("250.00")]
    public void An_order_refuses_a_stop_equal_to_or_above_the_limit(string stopAmount) {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();
        Price  stop      = new Price(decimal.Parse(stopAmount, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Construct(anyId, anyTicker, new Quantity(10), OneHundred, stop, Cent, Utc));

        // Assert
        Assert.Equal("stop", refusal.ParamName);
        Assert.Contains("strictly below", refusal.Message);
    }

    [Theory]
    [InlineData(Side.Buy)]
    [InlineData(Side.Sell)]
    public void The_stop_must_be_below_the_limit_on_both_sides(Side side) {
        // Arrange
        Guid        anyId          = Any.Guid().NonEmpty().Generate();
        Ticker      anyTicker      = Any.Ticker().Generate();
        TimeInForce anyTimeInForce = Any.Enum<TimeInForce>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Order(anyId, anyTicker, side, new Quantity(10), OneHundred, OneHundred, Cent, anyTimeInForce, Utc));

        // Assert
        Assert.Equal("stop", refusal.ParamName);
    }

    [Fact]
    public void An_order_accepts_a_stop_one_tick_below_the_limit() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act
        Order order = Construct(anyId, anyTicker, new Quantity(10), OneHundred, NinetyNine, Cent, Utc);

        // Assert
        Assert.Equal(99.99m, order.Stop!.Amount);
    }

    [Fact]
    public void An_order_checks_the_stop_grid_before_the_stop_ordering() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act: off the grid and above the limit
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Construct(anyId, anyTicker, new Quantity(10), OneHundred, new Price(100.123m), Cent, Utc));

        // Assert
        Assert.Contains("multiple of the tick", refusal.Message);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void An_order_refuses_a_timestamp_that_is_not_utc(DateTimeKind kind) {
        // Arrange
        Guid     anyId     = Any.Guid().NonEmpty().Generate();
        Ticker   anyTicker = Any.Ticker().Generate();
        DateTime placedAt  = new DateTime(2026, 10, 27, 14, 30, 0, kind);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Construct(anyId, anyTicker, new Quantity(10), OneHundred, null, Cent, placedAt));

        // Assert
        Assert.Equal("placedAt", refusal.ParamName);
    }

    [Fact]
    public void An_order_does_not_check_that_side_and_time_in_force_are_defined_values() {
        // Arrange
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Ticker anyTicker = Any.Ticker().Generate();

        // Act
        Order order = new Order(anyId, anyTicker, (Side)42, new Quantity(10), OneHundred, null, Cent, (TimeInForce)42, Utc);

        // Assert
        Assert.Equal((Side)42, order.Side);
        Assert.Equal((TimeInForce)42, order.TimeInForce);
    }

    [Fact]
    public void The_notional_is_the_full_quantity_times_the_limit() {
        // Arrange
        Order order = Any.Order()
                         .WithQuantity(new Quantity(250))
                         .WithTick(Cent)
                         .WithLimit(new Price(12.34m))
                         .WithoutStop()
                         .Generate();

        // Act
        decimal notional = order.Notional();

        // Assert
        Assert.Equal(3085.00m, notional);
    }

    [Fact]
    public void The_notional_stays_the_full_quantity_after_a_partial_fill() {
        // Arrange
        Order order = Any.Order()
                         .WithQuantity(new Quantity(250))
                         .WithTick(Cent)
                         .WithLimit(new Price(12.34m))
                         .WithoutStop()
                         .WithTimeInForce(TimeInForce.GoodTillCancelled)
                         .Generate();
        order.Fill(100);

        // Act
        decimal notional = order.Notional();

        // Assert
        Assert.Equal(3085.00m, notional);
    }

    [Fact]
    public void The_notional_of_the_largest_possible_order_does_not_overflow() {
        // Arrange
        Order order = Any.Order()
                         .WithQuantity(new Quantity(Quantity.Maximum))
                         .WithTick(new TickSize(0.0001m))
                         .WithLimit(new Price(999_999.9999m))
                         .WithoutStop()
                         .Generate();

        // Act
        decimal notional = order.Notional();

        // Assert
        Assert.Equal(999_999_999_900.0000m, notional);
    }

}
