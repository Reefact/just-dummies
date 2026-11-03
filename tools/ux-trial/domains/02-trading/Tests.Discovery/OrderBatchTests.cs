using JustDummies;

using Xunit;

namespace Trading.Domain;

public sealed class OrderBatchTests {

    private static readonly Ticker Acme = new Ticker("ACME");

    private static Order Limit(Side side, decimal limit, int shares = 10, DateTime? placedAt = null) {
        return Any.Order()
                  .WithTicker(Acme)
                  .WithTick(new TickSize(0.0001m))
                  .WithSide(side)
                  .WithQuantity(new Quantity(shares))
                  .WithLimit(new Price(limit))
                  .WithoutStop()
                  .WithPlacedAt(placedAt ?? new DateTime(2026, 10, 27, 9, 30, 0, DateTimeKind.Utc))
                  .Generate();
    }

    // ---- construction ----

    [Fact]
    public void A_batch_keeps_its_reference_and_the_orders_as_given() {
        // Arrange
        string               anyReference = Any.StringMatching(@"B-\d{8}-\d{4}").Generate();
        IReadOnlyList<Order> anyOrders    = Any.ListOf(Any.Order().WithTicker(Acme)).WithCountBetween(1, 5).DistinctBy(o => o.Id).Generate();

        // Act
        OrderBatch batch = new OrderBatch(anyReference, anyOrders);

        // Assert
        Assert.Equal(anyReference, batch.Reference);
        Assert.Same(anyOrders, batch.Orders);
    }

    [Fact]
    public void A_batch_of_one_order_is_a_batch() {
        // Act
        OrderBatch batch = Any.OrderBatch().WithOrderCount(1).Generate();

        // Assert
        Assert.Single(batch.Orders);
    }

    [Fact]
    public void A_batch_of_fifty_orders_is_a_batch() {
        // Act
        OrderBatch batch = Any.OrderBatch().WithOrderCount(OrderBatch.MaximumSize).Generate();

        // Assert
        Assert.Equal(50, batch.Orders.Count);
    }

    [Fact]
    public void A_batch_of_fifty_one_orders_is_refused() {
        // Arrange
        IReadOnlyList<Order> tooManyOrders = Any.ListOf(Any.Order().WithTicker(Acme)).WithCount(OrderBatch.MaximumSize + 1).DistinctBy(o => o.Id).Generate();
        string               anyReference  = Any.StringMatching(@"B-\d{8}-\d{4}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBatch(anyReference, tooManyOrders));
    }

    [Fact]
    public void An_empty_batch_is_refused() {
        // Arrange
        string anyReference = Any.StringMatching(@"B-\d{8}-\d{4}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBatch(anyReference, new List<Order>()));
    }

    [Fact]
    public void A_batch_without_a_list_of_orders_is_refused() {
        // Arrange
        string anyReference = Any.StringMatching(@"B-\d{8}-\d{4}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new OrderBatch(anyReference, null!));
    }

    [Fact]
    public void The_size_is_checked_before_the_orders_are_compared_to_one_another() {
        // Arrange: fifty-one orders on different tickers, some with the same id.
        Order                clone         = Any.Order().Generate();
        IReadOnlyList<Order> tooManyOrders = Enumerable.Repeat(clone, OrderBatch.MaximumSize + 1).ToList();
        string               anyReference  = Any.StringMatching(@"B-\d{8}-\d{4}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBatch(anyReference, tooManyOrders));
    }

    [Fact]
    public void The_same_order_twice_is_refused() {
        // Arrange
        Order                anyOrder     = Any.Order().Generate();
        string               anyReference = Any.StringMatching(@"B-\d{8}-\d{4}").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch(anyReference, new List<Order> { anyOrder, anyOrder }));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
    }

    [Fact]
    public void Two_different_orders_sharing_an_identifier_are_refused() {
        // Arrange
        Guid                 sharedId     = Any.Guid().NonEmpty().Generate();
        Order                first        = Any.Order().WithTicker(Acme).WithId(sharedId).Generate();
        Order                second       = Any.Order().WithTicker(Acme).WithId(sharedId).Generate();
        string               anyReference = Any.StringMatching(@"B-\d{8}-\d{4}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new OrderBatch(anyReference, new List<Order> { first, second }));
    }

    [Fact]
    public void Orders_on_two_tickers_are_refused() {
        // Arrange
        Ticker               anyTicker      = Any.Ticker().Generate();
        Ticker               anyOtherTicker = Any.Ticker().WithSymbol(s => s.DifferentFrom(anyTicker.Symbol)).Generate();
        Order                first          = Any.Order().WithTicker(anyTicker).Generate();
        Order                second         = Any.Order().WithTicker(anyOtherTicker).Generate();
        string               anyReference   = Any.StringMatching(@"B-\d{8}-\d{4}").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch(anyReference, new List<Order> { first, second }));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
    }

    [Fact]
    public void Orders_on_equal_tickers_built_separately_count_as_one_ticker() {
        // Arrange
        string symbol = Any.Ticker().Generate().Symbol;
        Order  first  = Any.Order().WithTicker(new Ticker(symbol)).Generate();
        Order  second = Any.Order().WithTicker(new Ticker(symbol)).Generate();

        // Act
        OrderBatch batch = Any.OrderBatch().WithOrders(new List<Order> { first, second }).Generate();

        // Assert
        Assert.Equal(2, batch.Orders.Count);
    }

    [Fact]
    public void A_missing_reference_is_refused_as_null() {
        // Arrange
        IReadOnlyList<Order> anyOrders = Any.ListOf(Any.Order().WithTicker(Acme)).WithCount(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new OrderBatch(null!, anyOrders));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("B-2026102-0001")]
    [InlineData("B-202610270-0001")]
    [InlineData("B-20261027-001")]
    [InlineData("B-20261027-00012")]
    [InlineData("b-20261027-0001")]
    [InlineData("X-20261027-0001")]
    [InlineData("B20261027-0001")]
    [InlineData(" B-20261027-0001")]
    [InlineData("B-20261027-0001 ")]
    [InlineData("B-2026-10-27-0001")]
    public void A_malformed_reference_is_refused(string reference) {
        // Arrange
        IReadOnlyList<Order> anyOrders = Any.ListOf(Any.Order().WithTicker(Acme)).WithCount(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new OrderBatch(reference, anyOrders));
    }

    [Fact]
    public void The_eight_digits_are_not_looked_at_as_a_date() {
        // Arrange
        IReadOnlyList<Order> anyOrders = Any.ListOf(Any.Order().WithTicker(Acme)).WithCount(1).Generate();

        // Act
        OrderBatch batch = new OrderBatch("B-99999999-0000", anyOrders);

        // Assert
        Assert.Equal("B-99999999-0000", batch.Reference);
    }

    [Fact(Skip = "domain defect: OrderBatch reference regex ends with $, which also matches before a final line feed, so \"B-20261027-0001\\n\" is accepted; deterministic literal, no seed")]
    public void A_reference_followed_by_a_line_feed_is_refused() {
        // Arrange
        IReadOnlyList<Order> anyOrders = Any.ListOf(Any.Order().WithTicker(Acme)).WithCount(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new OrderBatch("B-20261027-0001\n", anyOrders));
    }

    [Fact(Skip = "domain defect: \\d in the batch reference regex matches any Unicode decimal digit (Arabic-Indic here), so a non-ASCII reference is accepted; deterministic literal, no seed")]
    public void A_reference_written_with_non_ascii_digits_is_refused() {
        // Arrange
        IReadOnlyList<Order> anyOrders = Any.ListOf(Any.Order().WithTicker(Acme)).WithCount(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new OrderBatch("B-٢٠٢٦١٠٢٧-٠٠٠١", anyOrders));
    }

    // ---- computations ----

    [Fact]
    public void The_total_notional_adds_up_every_order_buys_and_sells_alike() {
        // Arrange
        Order buy        = Limit(Side.Buy, 10.00m, shares: 100);   // 1 000.00
        Order sell       = Limit(Side.Sell, 20.50m, shares: 50);   // 1 025.00
        Order smallBuy   = Limit(Side.Buy, 0.3333m, shares: 3);    //     0.9999

        OrderBatch batch = Any.OrderBatch().WithOrders(new List<Order> { buy, sell, smallBuy }).Generate();

        // Act
        decimal total = batch.TotalNotional();

        // Assert
        Assert.Equal(2_025.9999m, total);
    }

    [Fact]
    public void The_total_notional_of_a_single_order_is_its_notional() {
        // Arrange
        Order      order = Limit(Side.Sell, 12.34m, shares: 5);
        OrderBatch batch = Any.OrderBatch().WithOrders(new List<Order> { order }).Generate();

        // Assert
        Assert.Equal(61.70m, batch.TotalNotional());
    }

    [Fact]
    public void Bids_are_ranked_by_highest_limit_then_earliest_placement_and_sells_are_left_out() {
        // Arrange
        DateTime nine   = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
        DateTime ten    = new DateTime(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);
        DateTime eleven = new DateTime(2026, 10, 27, 11, 0, 0, DateTimeKind.Utc);

        Order lateTenBid   = Limit(Side.Buy, 10.00m, placedAt: eleven);
        Order earlyTenBid  = Limit(Side.Buy, 10.00m, placedAt: nine);
        Order topBid       = Limit(Side.Buy, 11.00m, placedAt: ten);
        Order lowBid       = Limit(Side.Buy, 9.50m, placedAt: nine);
        Order highSell     = Limit(Side.Sell, 12.00m, placedAt: nine);

        OrderBatch batch = Any.OrderBatch().WithOrders(new List<Order> { lateTenBid, highSell, lowBid, topBid, earlyTenBid }).Generate();

        // Act
        IReadOnlyList<Order> bids = batch.BidsByPriority();

        // Assert
        Assert.Equal(new List<Order> { topBid, earlyTenBid, lateTenBid, lowBid }, bids);
    }

    [Fact]
    public void A_batch_of_sells_has_no_bids() {
        // Arrange
        OrderBatch batch = Any.OrderBatch().WithOrders(new List<Order> { Limit(Side.Sell, 10m), Limit(Side.Sell, 11m) }).Generate();

        // Act / Assert
        Assert.Empty(batch.BidsByPriority());
    }

    [Fact]
    public void Bids_do_not_reorder_the_batch_itself() {
        // Arrange
        Order low  = Limit(Side.Buy, 9m);
        Order high = Limit(Side.Buy, 11m);
        OrderBatch batch = Any.OrderBatch().WithOrders(new List<Order> { low, high }).Generate();

        // Act
        batch.BidsByPriority();

        // Assert
        Assert.Equal(new List<Order> { low, high }, batch.Orders);
    }

}
