using JustDummies;
using JustDummies.Xunit;

using Trading.Domain;

using Xunit;

namespace Trading.Tests;

/// <summary>Harness checks: each scaffolded generator, as resolved by hand, never builds a value its constructor rejects.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void AlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyTicker_always_builds_a_valid_Ticker() {
        AlwaysBuilds(new AnyTicker());
    }

    [Fact]
    public void Scaffolded_AnyTickSize_always_builds_a_valid_TickSize() {
        AlwaysBuilds(new AnyTickSize());
    }

    [Fact]
    public void Scaffolded_AnyPrice_always_builds_a_valid_Price() {
        AlwaysBuilds(new AnyPrice());
    }

    [Fact]
    public void Scaffolded_AnyQuantity_always_builds_a_valid_Quantity() {
        AlwaysBuilds(new AnyQuantity());
    }

    [Fact]
    public void Scaffolded_AnyOrder_always_builds_a_valid_Order() {
        AlwaysBuilds(Arbitrary.Order());
    }

    [Fact]
    public void Scaffolded_AnyOrderBatch_always_builds_a_valid_OrderBatch() {
        AlwaysBuilds(Arbitrary.OrderBatch());
    }

    [Fact]
    public void Scaffolded_AnyTradingSession_always_builds_a_valid_TradingSession() {
        AlwaysBuilds(Arbitrary.TradingSession());
    }

}
