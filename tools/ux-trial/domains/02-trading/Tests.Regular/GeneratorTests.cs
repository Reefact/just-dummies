
namespace Trading.Tests.Regular;

public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void Every_scaffolded_generator_draws_values_its_constructor_accepts() {
        // Act
        Exception? failure = Record.Exception(() => {
            for (int draw = 0; draw < Draws; draw++) {
                Any.Ticker().Generate();
                Any.Price().Generate();
                Any.Quantity().Generate();
                Any.TickSize().Generate();
                Any.Order().Generate();
                Any.OrderBatch().Generate();
                Any.TradingSession().Generate();
            }
        });

        // Assert
        Assert.Null(failure);
    }

    [Fact]
    public void A_drawn_order_batch_holds_one_ticker_and_distinct_identifiers() {
        // Act
        Exception? failure = Record.Exception(() => {
            for (int draw = 0; draw < Draws; draw++) {
                OrderBatch anyBatch = Any.OrderBatch().Generate();

                Assert.Single(anyBatch.Orders.Select(order => order.Ticker).Distinct());
                Assert.Equal(anyBatch.Orders.Count, anyBatch.Orders.Select(order => order.Id).Distinct().Count());
            }
        });

        // Assert
        Assert.Null(failure);
    }

    [Fact]
    public void A_pinned_stop_of_null_gives_an_order_without_stop() {
        // Act
        Order anyOrder = Any.Order().WithoutStop().Generate();

        // Assert
        Assert.Null(anyOrder.Stop);
    }

    [Fact]
    public void A_generated_order_batch_can_be_pinned_to_a_given_ticker() {
        // Arrange
        Ticker apple = new Ticker("AAPL");

        // Act
        OrderBatch anyBatch = Any.OrderBatch().WithOrders(Any.ListOf(Any.Order().WithTicker(apple)).NonEmpty().WithMaxCount(5).DistinctBy(order => order.Id).Generate()).Generate();

        // Assert
        Assert.All(anyBatch.Orders, order => Assert.Equal(apple, order.Ticker));
    }

}
