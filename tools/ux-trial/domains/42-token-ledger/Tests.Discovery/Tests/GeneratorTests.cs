using JustDummies;

using TokenLedger.Domain;

using Xunit;

namespace TokenLedger.Tests.Discovery;

// The scaffolded generators are the base of every other test: each must only ever draw what its constructor accepts.
public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void The_address_generator_draws_only_well_formed_non_zero_addresses() {
        for (int i = 0; i < Draws; i++) {
            Address anyAddress = Any.Address().NonZero().Generate();

            Assert.False(anyAddress.IsZero);
        }
    }

    [Fact]
    public void The_token_amount_generator_draws_amounts_the_type_holds() {
        for (int i = 0; i < Draws; i++) { Any.TokenAmount().Generate(); }
    }

    [Fact]
    public void The_exact_decimal_generator_draws_normalised_decimals() {
        for (int i = 0; i < Draws; i++) {
            ExactDecimal anyDecimal = Any.ExactDecimal().Generate();

            Assert.Equal(anyDecimal, ExactDecimal.Parse(anyDecimal.ToString()));
        }
    }

    [Fact]
    public void The_token_definition_generator_draws_definitions_with_a_whole_supply_cap() {
        for (int i = 0; i < Draws; i++) { Any.TokenDefinition().Generate(); }
    }

    [Fact]
    public void The_token_price_generator_draws_positive_prices() {
        for (int i = 0; i < Draws; i++) { Any.TokenPrice().Generate(); }
    }

    [Fact]
    public void The_gas_schedule_generator_draws_schedules_the_constructor_accepts() {
        for (int i = 0; i < Draws; i++) { Any.GasSchedule().Generate(); }
    }

    [Fact]
    public void The_envelope_and_receipt_generators_keep_the_fee_within_256_bits() {
        for (int i = 0; i < Draws; i++) {
            Any.TransactionEnvelope().Generate();
            Any.Receipt().Generate();
        }
    }

    [Fact]
    public void The_transfer_record_and_account_generators_draw_valid_values() {
        for (int i = 0; i < Draws; i++) {
            Any.TransferRecord().Generate();
            Any.Account().Generate();
        }
    }

    [Fact]
    public void The_ledger_generator_draws_ledgers_the_constructor_accepts() {
        for (int i = 0; i < Draws; i++) { Any.Ledger().Generate(); }
    }

}
