using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

/// <summary>The generators are code the suite owns: these check they only ever draw what the constructors accept.</summary>
public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void Every_generator_draws_only_values_its_constructor_accepts() {
        // Act
        Exception? refusal = Record.Exception(() => {
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
        Assert.Null(refusal);
    }

    [Fact]
    public void An_order_pinned_on_a_limit_alone_is_drawn_on_a_tick_that_admits_it() {
        // Act
        Exception? refusal = Record.Exception(() => {
            for (int draw = 0; draw < Draws; draw++) {
                Any.Order().WithLimitAt(10.37m).Generate();
                Any.Order().WithStopAt(0.0001m).Generate();
                Any.Order().WithStopAt(999_999.9998m).Generate();
            }
        });

        // Assert
        Assert.Null(refusal);
    }

}
