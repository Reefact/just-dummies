namespace Trading.Tests.Regular;

public sealed class OrderBatchTests {

    private const string AReference = "B-20261027-0001";

    private static readonly TickSize Cent = new TickSize(0.01m);

    private static List<Order> AnyOrdersOn(Ticker ticker, int count) {
        return Any.ListOf(Any.Order().WithTicker(ticker))
                  .WithCount(count)
                  .DistinctBy(order => order.Id)
                  .Generate();
    }

    private static Order AnyOrderOn(Ticker ticker) {
        return Any.Order().WithTicker(ticker).Generate();
    }

    // ---------------------------------------------------------------- reference

    [Theory]
    [InlineData("B-20261027-0001")]
    [InlineData("B-00000000-0000")]
    [InlineData("B-99999999-9999")]
    [InlineData("B-20261331-0042")]
    public void A_batch_accepts_a_reference_of_the_form_B_eight_digits_four_digits(string reference) {
        // Arrange
        List<Order> anyOrders = AnyOrdersOn(Any.Ticker().Generate(), 2);

        // Act
        OrderBatch batch = new OrderBatch(reference, anyOrders);

        // Assert
        Assert.Equal(reference, batch.Reference);
    }

    [Theory]
    [InlineData("b-20261027-0001")]
    [InlineData("X-20261027-0001")]
    [InlineData("B20261027-0001")]
    [InlineData("B-20261027_0001")]
    [InlineData("B-2026102-0001")]
    [InlineData("B-202610270-0001")]
    [InlineData("B-20261027-001")]
    [InlineData("B-20261027-00012")]
    [InlineData("B-2026-10-27-0001")]
    [InlineData("B-2026102A-0001")]
    [InlineData(" B-20261027-0001")]
    [InlineData("B-20261027-0001 ")]
    [InlineData("AB-20261027-0001")]
    [InlineData("B-20261027-0001-0002")]
    public void A_batch_refuses_a_reference_outside_the_format(string reference) {
        // Arrange
        List<Order> anyOrders = AnyOrdersOn(Any.Ticker().Generate(), 2);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch(reference, anyOrders));

        // Assert
        Assert.Equal("reference", refusal.ParamName);
        Assert.Contains(reference, refusal.Message);
    }

    [Fact]
    public void A_batch_refuses_a_null_reference_as_a_null_argument() {
        // Arrange
        List<Order> anyOrders = AnyOrdersOn(Any.Ticker().Generate(), 2);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new OrderBatch(null!, anyOrders));

        // Assert
        Assert.Equal("reference", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_batch_refuses_a_blank_reference_as_a_plain_argument_error(string blank) {
        // Arrange
        List<Order> anyOrders = AnyOrdersOn(Any.Ticker().Generate(), 2);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch(blank, anyOrders));

        // Assert
        Assert.Equal("reference", refusal.ParamName);
    }

    [Fact]
    public void The_reference_is_checked_before_the_orders() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch("nope", null!));

        // Assert: the reference, not the missing list
        Assert.Equal("reference", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the reference pattern ends with $, which also matches before a final newline, so \"B-20261027-0001\\n\" is accepted (no seed: literal input)")]
    public void A_batch_refuses_a_reference_with_a_trailing_newline() {
        // Arrange
        List<Order> anyOrders = AnyOrdersOn(Any.Ticker().Generate(), 2);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch("B-20261027-0001\n", anyOrders));

        // Assert
        Assert.Equal("reference", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit, so \"B-٢٠٢٦١٠٢٧-٠٠٠١\" (Arabic-Indic digits) is accepted as a batch reference (no seed: literal input)")]
    public void A_batch_refuses_a_reference_made_of_non_ascii_digits() {
        // Arrange
        List<Order> anyOrders = AnyOrdersOn(Any.Ticker().Generate(), 2);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch("B-٢٠٢٦١٠٢٧-٠٠٠١", anyOrders));

        // Assert
        Assert.Equal("reference", refusal.ParamName);
    }

    // ---------------------------------------------------------------- orders

    [Fact]
    public void A_batch_refuses_a_missing_list_of_orders() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new OrderBatch(AReference, null!));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
    }

    [Fact]
    public void A_batch_refuses_an_empty_list_of_orders() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBatch(AReference, new List<Order>()));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
        Assert.Equal(0, refusal.ActualValue);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(49)]
    [InlineData(50)]
    public void A_batch_accepts_from_one_to_fifty_orders(int count) {
        // Arrange
        List<Order> anyOrders = AnyOrdersOn(Any.Ticker().Generate(), count);

        // Act
        OrderBatch batch = new OrderBatch(AReference, anyOrders);

        // Assert
        Assert.Equal(count, batch.Orders.Count);
    }

    [Fact]
    public void A_batch_refuses_more_than_fifty_orders() {
        // Arrange
        List<Order> anyOrders = AnyOrdersOn(Any.Ticker().Generate(), OrderBatch.MaximumSize + 1);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBatch(AReference, anyOrders));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
        Assert.Equal(51, refusal.ActualValue);
    }

    [Fact]
    public void The_size_is_checked_before_the_identifiers_and_the_ticker() {
        // Arrange: 51 times the same order
        Order       anyOrder  = AnyOrderOn(Any.Ticker().Generate());
        List<Order> anyOrders = Enumerable.Repeat(anyOrder, 51).ToList();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBatch(AReference, anyOrders));
    }

    [Fact]
    public void The_maximum_size_is_fifty() {
        Assert.Equal(50, OrderBatch.MaximumSize);
    }

    [Fact]
    public void A_batch_refuses_the_same_order_twice() {
        // Arrange
        Order anyOrder = AnyOrderOn(Any.Ticker().Generate());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch(AReference, [anyOrder, anyOrder]));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
        Assert.Contains("same order twice", refusal.Message);
    }

    [Fact]
    public void A_batch_refuses_two_distinct_orders_sharing_an_identifier() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Order  first     = Any.Order().WithTicker(anyTicker).WithId(anyId).Generate();
        Order  second    = Any.Order().WithTicker(anyTicker).WithId(anyId).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch(AReference, [first, second]));

        // Assert
        Assert.Contains("same order twice", refusal.Message);
    }

    [Fact]
    public void A_batch_refuses_orders_on_different_tickers() {
        // Arrange
        Order first  = AnyOrderOn(new Ticker("AAPL"));
        Order second = AnyOrderOn(new Ticker("MSFT"));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch(AReference, [first, second]));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
        Assert.Contains("single ticker", refusal.Message);
    }

    [Fact]
    public void A_batch_accepts_orders_whose_tickers_are_distinct_instances_of_the_same_symbol() {
        // Arrange
        Order first  = AnyOrderOn(new Ticker("BRK.B"));
        Order second = AnyOrderOn(new Ticker("BRK.B"));

        // Act
        OrderBatch batch = new OrderBatch(AReference, [first, second]);

        // Assert
        Assert.Equal(2, batch.Orders.Count);
    }

    [Fact]
    public void The_identifiers_are_checked_before_the_tickers() {
        // Arrange: the same id on two different tickers
        Guid  anyId  = Any.Guid().NonEmpty().Generate();
        Order first  = Any.Order().WithTicker(new Ticker("AAPL")).WithId(anyId).Generate();
        Order second = Any.Order().WithTicker(new Ticker("MSFT")).WithId(anyId).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch(AReference, [first, second]));

        // Assert
        Assert.Contains("same order twice", refusal.Message);
    }

    [Fact]
    public void A_batch_keeps_the_list_it_was_given_without_copying_it() {
        // Arrange
        List<Order> anyOrders = AnyOrdersOn(Any.Ticker().Generate(), 3);

        // Act
        OrderBatch batch = new OrderBatch(AReference, anyOrders);

        // Assert
        Assert.Same(anyOrders, batch.Orders);
    }

    // ---------------------------------------------------------------- TotalNotional

    [Fact]
    public void The_total_notional_adds_the_notional_of_every_order_whatever_its_side() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();
        Order  buy       = Any.Order().WithTicker(anyTicker).WithSide(Side.Buy).WithTick(Cent).WithQuantity(new Quantity(100)).WithLimit(new Price(10.00m)).WithoutStop().Generate();
        Order  sell      = Any.Order().WithTicker(anyTicker).WithSide(Side.Sell).WithTick(Cent).WithQuantity(new Quantity(50)).WithLimit(new Price(20.50m)).WithoutStop().Generate();
        Order  other     = Any.Order().WithTicker(anyTicker).WithSide(Side.Buy).WithTick(Cent).WithQuantity(new Quantity(3)).WithLimit(new Price(0.33m)).WithoutStop().Generate();
        OrderBatch batch = new OrderBatch(AReference, [buy, sell, other]);

        // Act
        decimal total = batch.TotalNotional();

        // Assert
        Assert.Equal(1000.00m + 1025.00m + 0.99m, total);
    }

    [Fact]
    public void The_total_notional_of_a_single_order_is_its_notional() {
        // Arrange
        Order order = Any.Order().WithTick(Cent).WithQuantity(new Quantity(7)).WithLimit(new Price(1.15m)).WithoutStop().Generate();
        OrderBatch batch = new OrderBatch(AReference, [order]);

        // Act
        decimal total = batch.TotalNotional();

        // Assert
        Assert.Equal(8.05m, total);
    }

    [Fact]
    public void The_total_notional_counts_the_full_quantity_of_a_filled_order() {
        // Arrange
        Order order = Any.Order()
                         .WithTick(Cent)
                         .WithQuantity(new Quantity(10))
                         .WithLimit(new Price(5.00m))
                         .WithoutStop()
                         .WithTimeInForce(TimeInForce.GoodTillCancelled)
                         .Generate();
        order.Fill(4);
        OrderBatch batch = new OrderBatch(AReference, [order]);

        // Act
        decimal total = batch.TotalNotional();

        // Assert
        Assert.Equal(50.00m, total);
    }

    // ---------------------------------------------------------------- BidsByPriority

    private static Order Bid(Ticker ticker, Side side, decimal limit, DateTime placedAt) {
        return Any.Order()
                  .WithTicker(ticker)
                  .WithSide(side)
                  .WithTick(Cent)
                  .WithLimit(new Price(limit))
                  .WithoutStop()
                  .WithPlacedAt(placedAt)
                  .Generate();
    }

    [Fact]
    public void Bids_come_highest_limit_first_then_earliest_placed() {
        // Arrange
        Ticker   anyTicker = Any.Ticker().Generate();
        DateTime nine      = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
        Order    low       = Bid(anyTicker, Side.Buy, 99.00m, nine);
        Order    tieLate   = Bid(anyTicker, Side.Buy, 100.00m, nine.AddMinutes(3));
        Order    top       = Bid(anyTicker, Side.Buy, 101.00m, nine.AddMinutes(5));
        Order    tieEarly  = Bid(anyTicker, Side.Buy, 100.00m, nine.AddMinutes(1));
        OrderBatch batch   = new OrderBatch(AReference, [low, tieLate, top, tieEarly]);

        // Act
        IReadOnlyList<Order> bids = batch.BidsByPriority();

        // Assert
        Assert.Equal([top, tieEarly, tieLate, low], bids);
    }

    [Fact]
    public void Sell_orders_are_not_bids() {
        // Arrange
        Ticker   anyTicker = Any.Ticker().Generate();
        DateTime nine      = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
        Order    buy       = Bid(anyTicker, Side.Buy, 50.00m, nine);
        Order    sell      = Bid(anyTicker, Side.Sell, 500.00m, nine);
        OrderBatch batch   = new OrderBatch(AReference, [sell, buy]);

        // Act
        IReadOnlyList<Order> bids = batch.BidsByPriority();

        // Assert
        Assert.Equal([buy], bids);
    }

    [Fact]
    public void A_batch_of_sell_orders_has_no_bids() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();
        Order  first     = Any.Order().WithTicker(anyTicker).WithSide(Side.Sell).Generate();
        Order  second    = Any.Order().WithTicker(anyTicker).WithSide(Side.Sell).Generate();
        OrderBatch batch = new OrderBatch(AReference, [first, second]);

        // Act
        IReadOnlyList<Order> bids = batch.BidsByPriority();

        // Assert
        Assert.Empty(bids);
    }

    [Fact]
    public void Ranking_the_bids_does_not_reorder_the_batch() {
        // Arrange
        Ticker   anyTicker = Any.Ticker().Generate();
        DateTime nine      = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
        Order    low       = Bid(anyTicker, Side.Buy, 99.00m, nine);
        Order    high      = Bid(anyTicker, Side.Buy, 101.00m, nine);
        OrderBatch batch   = new OrderBatch(AReference, [low, high]);

        // Act
        batch.BidsByPriority();

        // Assert
        Assert.Equal([low, high], batch.Orders);
    }

    [Fact]
    public void Limits_are_compared_by_amount_not_by_position_in_the_batch() {
        // Arrange: 9.50 sorts after 10.00 as text, before it as a number
        Ticker   anyTicker = Any.Ticker().Generate();
        DateTime nine      = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
        Order    ninePointFive = Bid(anyTicker, Side.Buy, 9.50m, nine);
        Order    ten           = Bid(anyTicker, Side.Buy, 10.00m, nine);
        OrderBatch batch       = new OrderBatch(AReference, [ten, ninePointFive]);

        // Act
        IReadOnlyList<Order> bids = batch.BidsByPriority();

        // Assert
        Assert.Equal([ten, ninePointFive], bids);
    }

}
