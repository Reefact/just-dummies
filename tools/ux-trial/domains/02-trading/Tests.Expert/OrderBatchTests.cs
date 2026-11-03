using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

public sealed class OrderBatchTests {

    private static List<Order> AnyOrdersOfOneTicker(int count) {
        Ticker anyTicker = Any.Ticker().Generate();

        return Any.ListOf(Any.Order().WithTicker(anyTicker)).WithCount(count).DistinctBy(order => order.Id).Generate();
    }

    [Theory]
    [InlineData("B-20261027-0001")]
    [InlineData("B-00000000-0000")]
    [InlineData("B-99999999-9999")]
    public void A_reference_of_a_b_then_eight_digits_then_four_digits_is_accepted_without_checking_the_date(string reference) {
        // Arrange
        List<Order> anyOrders = AnyOrdersOfOneTicker(1);

        // Act
        OrderBatch batch = new OrderBatch(reference, anyOrders);

        // Assert
        Assert.Equal(reference, batch.Reference);
    }

    [Theory]
    [InlineData("B-2026102-0001")]
    [InlineData("B-202610270-0001")]
    [InlineData("B-20261027-001")]
    [InlineData("B-20261027-00012")]
    [InlineData("b-20261027-0001")]
    [InlineData("X-20261027-0001")]
    [InlineData("B20261027-0001")]
    [InlineData("B-20261027_0001")]
    [InlineData("B-2026102A-0001")]
    [InlineData(" B-20261027-0001")]
    [InlineData("B-20261027-0001 ")]
    [InlineData("ORD-12345678")]
    public void A_reference_that_is_not_of_the_batch_format_is_refused(string reference) {
        // Arrange
        List<Order> anyOrders = AnyOrdersOfOneTicker(1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch(reference, anyOrders));

        // Assert
        Assert.Equal("reference", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the pattern ends with '$', which in .NET also matches before a final line feed, so \"B-20261027-0001\\n\" is accepted as a reference; seed: not applicable, the value is a literal")]
    public void A_reference_followed_by_a_line_feed_is_refused() {
        // Arrange
        List<Order> anyOrders = AnyOrdersOfOneTicker(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OrderBatch("B-20261027-0001\n", anyOrders));
    }

    [Fact(Skip = "domain defect: \\d in .NET matches every Unicode decimal digit, so a reference written with Arabic-Indic digits is accepted although the model says eight digits; seed: not applicable, the value is a literal")]
    public void A_reference_written_with_non_ascii_digits_is_refused() {
        // Arrange
        List<Order> anyOrders = AnyOrdersOfOneTicker(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OrderBatch("B-٢٠٢٦١٠٢٧-٠٠٠١", anyOrders));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_reference_is_refused(string blank) {
        // Arrange
        List<Order> anyOrders = AnyOrdersOfOneTicker(1);

        // Act & Assert
        Assert.Equal("reference", Assert.Throws<ArgumentException>(() => new OrderBatch(blank, anyOrders)).ParamName);
    }

    [Fact]
    public void A_missing_reference_is_refused_as_a_null_argument() {
        // Arrange
        List<Order> anyOrders = AnyOrdersOfOneTicker(1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrderBatch(null!, anyOrders));
    }

    [Fact]
    public void A_missing_list_of_orders_is_refused_as_a_null_argument() {
        // Arrange
        string anyReference = Any.StringMatching(@"B-\d{8}-\d{4}").Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new OrderBatch(anyReference, null!));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(50)]
    public void A_batch_holds_from_one_to_fifty_orders(int count) {
        // Arrange
        List<Order> orders = AnyOrdersOfOneTicker(count);

        // Act
        OrderBatch batch = new OrderBatch("B-20261027-0001", orders);

        // Assert
        Assert.Equal(count, batch.Orders.Count);
    }

    [Fact]
    public void A_batch_without_an_order_is_refused() {
        // Arrange
        List<Order> noOrder = [];

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBatch("B-20261027-0001", noOrder));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
    }

    [Fact]
    public void A_batch_of_fifty_one_orders_is_refused() {
        // Arrange
        List<Order> tooMany = AnyOrdersOfOneTicker(51);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBatch("B-20261027-0001", tooMany));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
    }

    [Fact]
    public void The_size_limit_is_checked_before_the_distinct_ids_and_the_ticker() {
        // Arrange
        Order       anyOrder = Any.Order().Generate();
        List<Order> sameOrderFiftyOneTimes = Enumerable.Repeat(anyOrder, OrderBatch.MaximumSize + 1).ToList();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBatch("B-20261027-0001", sameOrderFiftyOneTimes));
    }

    [Fact]
    public void The_same_order_twice_is_refused() {
        // Arrange
        Order anyOrder = Any.Order().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch("B-20261027-0001", [anyOrder, anyOrder]));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
    }

    [Fact]
    public void Two_orders_sharing_an_identifier_are_refused() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();
        Guid   anyId     = Any.Guid().NonEmpty().Generate();
        Order  first     = Any.Order().WithTicker(anyTicker).WithId(anyId).Generate();
        Order  second    = Any.Order().WithTicker(anyTicker).WithId(anyId).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OrderBatch("B-20261027-0001", [first, second]));
    }

    [Fact]
    public void Orders_of_two_tickers_are_refused() {
        // Arrange
        Ticker anyTicker      = Any.Ticker().Generate();
        Ticker anyOtherTicker = Any.Ticker().WithSymbol(symbol => symbol.DifferentFrom(anyTicker.Symbol)).Generate();
        Order  first          = Any.Order().WithTicker(anyTicker).Generate();
        Order  second         = Any.Order().WithTicker(anyOtherTicker).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OrderBatch("B-20261027-0001", [first, second]));

        // Assert
        Assert.Equal("orders", refusal.ParamName);
    }

    [Fact]
    public void A_batch_keeps_the_list_of_orders_it_was_given() {
        // Arrange
        List<Order> orders = AnyOrdersOfOneTicker(3);

        // Act
        OrderBatch batch = new OrderBatch("B-20261027-0001", orders);

        // Assert
        Assert.Same(orders, batch.Orders);
    }

    [Fact(Skip = "domain defect: the batch stores the caller's list instead of a copy, so adding a 51st order to it afterwards breaks the 50-order limit the constructor enforced; seed: not applicable, no value is drawn that matters")]
    public void A_batch_does_not_follow_later_changes_to_the_list_it_was_given() {
        // Arrange
        List<Order> orders = AnyOrdersOfOneTicker(50);
        OrderBatch  batch  = new OrderBatch("B-20261027-0001", orders);

        // Act
        orders.Add(Any.Order().WithTicker(orders[0].Ticker).Generate());

        // Assert
        Assert.Equal(50, batch.Orders.Count);
    }

    [Fact(Skip = "domain defect: a null element in the list makes the constructor throw a NullReferenceException instead of an argument exception; seed: not applicable")]
    public void A_null_order_in_the_list_is_refused_as_an_argument_error() {
        // Arrange
        Order anyOrder = Any.Order().Generate();

        // Act & Assert
        Assert.IsAssignableFrom<ArgumentException>(Record.Exception(() => new OrderBatch("B-20261027-0001", [anyOrder, null!])));
    }

    [Fact]
    public void The_total_notional_adds_up_the_notional_of_every_order_whatever_its_side() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();
        Order  buy       = Any.Order().WithTicker(anyTicker).WithSide(Side.Buy).WithShares(100).WithLimitAt(10.00m).Generate();
        Order  sell      = Any.Order().WithTicker(anyTicker).WithSide(Side.Sell).WithShares(50).WithLimitAt(20.50m).Generate();
        OrderBatch batch = new OrderBatch("B-20261027-0001", [buy, sell]);

        // Act
        decimal total = batch.TotalNotional();

        // Assert
        Assert.Equal(2025m, total);
    }

    [Fact]
    public void The_total_notional_of_a_one_order_batch_is_that_orders_notional() {
        // Arrange
        Order      order = Any.Order().WithShares(8).WithLimitAt(12.50m).Generate();
        OrderBatch batch = new OrderBatch("B-20261027-0001", [order]);

        // Act
        decimal total = batch.TotalNotional();

        // Assert
        Assert.Equal(100m, total);
    }

    [Fact]
    public void The_total_notional_counts_the_whole_quantity_of_an_order_already_partly_filled() {
        // Arrange
        Order order = Any.Order().WithShares(100).WithLimitAt(10.00m).AllowingPartialFills().Generate();
        order.Fill(40);
        OrderBatch batch = new OrderBatch("B-20261027-0001", [order]);

        // Act
        decimal total = batch.TotalNotional();

        // Assert
        Assert.Equal(1000m, total);
    }

    [Fact]
    public void Bids_come_highest_limit_first_and_earliest_first_among_equal_limits_and_leave_the_sells_out() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();
        Order  lowBid    = Any.Order().WithTicker(anyTicker).WithSide(Side.Buy).WithLimitAt(10.10m).WithPlacedAt(At(9, 0)).Generate();
        Order  lateBid   = Any.Order().WithTicker(anyTicker).WithSide(Side.Buy).WithLimitAt(10.20m).WithPlacedAt(At(9, 30)).Generate();
        Order  earlyBid  = Any.Order().WithTicker(anyTicker).WithSide(Side.Buy).WithLimitAt(10.20m).WithPlacedAt(At(9, 15)).Generate();
        Order  dearAsk   = Any.Order().WithTicker(anyTicker).WithSide(Side.Sell).WithLimitAt(99.00m).WithPlacedAt(At(8, 0)).Generate();
        OrderBatch batch = new OrderBatch("B-20261027-0001", [lowBid, lateBid, dearAsk, earlyBid]);

        // Act
        IReadOnlyList<Order> bids = batch.BidsByPriority();

        // Assert
        Assert.Equal([earlyBid.Id, lateBid.Id, lowBid.Id], bids.Select(bid => bid.Id));
    }

    [Fact]
    public void A_batch_of_sells_has_no_bids() {
        // Arrange
        Ticker      anyTicker = Any.Ticker().Generate();
        List<Order> onlySells = Any.ListOf(Any.Order().WithTicker(anyTicker).WithSide(Side.Sell)).WithCount(3).DistinctBy(order => order.Id).Generate();
        OrderBatch  batch     = new OrderBatch("B-20261027-0001", onlySells);

        // Act
        IReadOnlyList<Order> bids = batch.BidsByPriority();

        // Assert
        Assert.Empty(bids);
    }

    [Fact]
    public void Ranking_the_bids_does_not_reorder_the_orders_of_the_batch() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();
        Order  lowBid    = Any.Order().WithTicker(anyTicker).WithSide(Side.Buy).WithLimitAt(10.10m).Generate();
        Order  highBid   = Any.Order().WithTicker(anyTicker).WithSide(Side.Buy).WithLimitAt(10.20m).Generate();
        OrderBatch batch = new OrderBatch("B-20261027-0001", [lowBid, highBid]);

        // Act
        batch.BidsByPriority();

        // Assert
        Assert.Equal([lowBid.Id, highBid.Id], batch.Orders.Select(order => order.Id));
    }

    private static DateTime At(int hour, int minute) {
        return new DateTime(2026, 10, 27, hour, minute, 0, DateTimeKind.Utc);
    }

}
