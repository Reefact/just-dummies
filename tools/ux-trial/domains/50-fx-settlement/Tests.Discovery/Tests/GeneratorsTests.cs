using JustDummies;
using Xunit;

namespace FxSettlement.Domain;

// The generators are part of the test code: this checks that each one only draws what its type accepts.
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Currency_generator_draws_valid_currencies() {
        for (int i = 0; i < Draws; i++) { Any.Currency().Generate(); }
    }

    [Fact]
    public void Money_generator_draws_valid_money() {
        for (int i = 0; i < Draws; i++) { Any.Money().Generate(); }
    }

    [Fact]
    public void CurrencyPair_generator_draws_valid_pairs() {
        for (int i = 0; i < Draws; i++) { Any.CurrencyPair().Generate(); }
    }

    [Fact]
    public void ExchangeRate_generator_draws_valid_rates() {
        for (int i = 0; i < Draws; i++) { Any.ExchangeRate().Generate(); }
    }

    [Fact]
    public void Lei_generator_draws_valid_leis() {
        for (int i = 0; i < Draws; i++) { Any.Lei().Generate(); }
    }

    [Fact]
    public void TradeId_generator_draws_valid_ids() {
        for (int i = 0; i < Draws; i++) { Any.TradeId().Generate(); }
    }

    [Fact]
    public void NetPosition_generator_draws_valid_positions() {
        for (int i = 0; i < Draws; i++) { Any.NetPosition().Generate(); }
    }

    [Fact]
    public void SettlementInstruction_generator_draws_valid_instructions() {
        for (int i = 0; i < Draws; i++) { Any.SettlementInstruction().Generate(); }
    }

    [Fact]
    public void RateSheet_generator_draws_valid_sheets() {
        for (int i = 0; i < Draws; i++) { Any.RateSheet().Generate(); }
    }

    [Fact]
    public void SpotTrade_generator_draws_valid_trades() {
        for (int i = 0; i < Draws; i++) { Any.SpotTrade().Generate(); }
    }

    [Fact]
    public void SettlementBatch_generator_draws_valid_batches() {
        for (int i = 0; i < Draws; i++) { Any.SettlementBatch().Generate(); }
    }

}
