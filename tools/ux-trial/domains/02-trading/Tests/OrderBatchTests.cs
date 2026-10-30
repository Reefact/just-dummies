using JustDummies;
using JustDummies.Xunit;

using Trading.Domain;

using Xunit;

namespace Trading.Tests;

[Reproducible]
public sealed class OrderBatchTests {

    private static AnyPattern AnyBatchReference() {
        return Any.StringMatching(@"^B-\d{8}-\d{4}$");
    }

    [Fact]
    public void The_total_notional_of_a_batch_adds_up_its_orders() {
        // Arrange
        Ticker   anyTicker = new AnyTicker().Generate();
        AnyOrder anyOrder  = Arbitrary.Order().WithTicker(anyTicker);
        Order[]  orders    = [
            anyOrder.WithQuantity(new Quantity(100)).WithLimit(new Price(20.00m)).Generate(),
            anyOrder.WithQuantity(new Quantity(50)).WithLimit(new Price(10.50m)).Generate()
        ];
        OrderBatch anyBatch = Arbitrary.OrderBatch().WithOrders(orders).Generate();

        // Act
        decimal total = anyBatch.TotalNotional();

        // Assert
        Assert.Equal(2_525m, total);
    }

    [Fact]
    public void Bids_are_ranked_highest_limit_first_and_offers_are_left_out() {
        // Arrange
        AnyOrder anyOrder = Arbitrary.Order().WithTicker(new AnyTicker().Generate());
        Order    bidAt10  = anyOrder.WithSide(Side.Buy).WithLimit(new Price(10.00m)).Generate();
        Order    bidAt11  = anyOrder.WithSide(Side.Buy).WithLimit(new Price(11.00m)).Generate();
        Order    offer    = anyOrder.WithSide(Side.Sell).Generate();
        OrderBatch anyBatch = Arbitrary.OrderBatch().WithOrders([bidAt10, offer, bidAt11]).Generate();

        // Act
        IReadOnlyList<Order> ranked = anyBatch.BidsByPriority();

        // Assert
        Assert.Equal([bidAt11, bidAt10], ranked);
    }

    [Fact]
    public void A_batch_refuses_orders_for_two_different_tickers() {
        // Arrange
        Ticker[] twoTickers = Any.SetOf(new AnyTicker()).WithCount(2).Generate().ToArray();
        Order[]  orders     = [
            Arbitrary.Order().WithTicker(twoTickers[0]).Generate(),
            Arbitrary.Order().WithTicker(twoTickers[1]).Generate()
        ];
        string anyReference = AnyBatchReference().Generate();

        // Act
        OrderBatch Submit() => new(anyReference, orders);

        // Assert
        Assert.Throws<ArgumentException>(Submit);
    }

    [Fact]
    public void A_batch_refuses_more_than_fifty_orders() {
        // Arrange
        AnyOrder     anyOrderOnOneTicker = Arbitrary.Order().WithTicker(new AnyTicker().Generate());
        List<Order>  fiftyOneOrders      = Any.ListOf(anyOrderOnOneTicker).WithCount(51).Generate();
        string       anyReference        = AnyBatchReference().Generate();

        // Act
        OrderBatch Submit() => new(anyReference, fiftyOneOrders);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(Submit);
    }

}
