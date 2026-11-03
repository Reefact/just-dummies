using JustDummies;

using TokenLedger.Domain;
using TokenLedger.Tests.Discovery.Support;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class LedgerQueryTests {

    // Four decimals and a supply of 1234.5678 tokens: the figures the human readings below are checked against.
    private static Ledger ExampleLedger() {
        TokenDefinition token = Any.TokenDefinition()
                                   .WithName("Example Token")
                                   .WithSymbol("EXT")
                                   .WithDecimals(4)
                                   .WithSupplyCap(new TokenAmount(1_000_000_0000))
                                   .Generate();

        return Any.Ledger().WithToken(token).WithInitialSupply(new TokenAmount(12_345_678)).WithMinimumGasPrice(TokenAmount.Zero).Generate();
    }

    [Fact]
    public void An_address_the_ledger_has_never_seen_has_nothing() {
        Ledger  ledger     = Chain.NewLedger();
        Address anyStranger = Chain.NewAddress();

        Assert.Equal(TokenAmount.Zero, ledger.BalanceOf(anyStranger));
        Assert.Equal(UInt128.Zero, ledger.NextNonceOf(anyStranger));
        Assert.Equal(TokenAmount.Zero, ledger.AllowanceOf(anyStranger, ledger.Minter));
    }

    [Fact]
    public void The_zero_address_has_no_balance_and_no_nonce() {
        Ledger ledger = Chain.NewLedger();

        Assert.Equal(TokenAmount.Zero, ledger.BalanceOf(Address.Zero));
        Assert.Equal(UInt128.Zero, ledger.NextNonceOf(Address.Zero));
    }

    [Fact]
    public void Queries_refuse_a_null_address() {
        Ledger  ledger  = Chain.NewLedger();
        Address anyone  = Chain.NewAddress();

        Assert.Throws<ArgumentNullException>(() => ledger.BalanceOf(null!));
        Assert.Throws<ArgumentNullException>(() => ledger.NextNonceOf(null!));
        Assert.Throws<ArgumentNullException>(() => ledger.AllowanceOf(null!, anyone));
        Assert.Throws<ArgumentNullException>(() => ledger.AllowanceOf(anyone, null!));
    }

    [Fact]
    public void A_balance_reads_back_as_an_exact_human_amount() {
        Ledger ledger = ExampleLedger();

        Assert.Equal(ExactDecimal.Parse("1234.5678"), ledger.HumanBalanceOf(ledger.Minter));
        Assert.Equal(ExactDecimal.Zero, ledger.HumanBalanceOf(Chain.NewAddress()));
    }

    [Fact]
    public void The_value_of_a_balance_is_the_tokens_held_times_the_price() {
        Ledger     ledger = ExampleLedger();
        TokenPrice price  = new TokenPrice(ExactDecimal.Parse("0.5"), "EUR");

        Assert.Equal(ExactDecimal.Parse("617.2839"), ledger.ValueOf(ledger.Minter, price));
        Assert.Equal(ExactDecimal.Zero, ledger.ValueOf(Chain.NewAddress(), price));
    }

    [Fact]
    public void The_market_capitalisation_is_the_total_supply_times_the_price() {
        Ledger     ledger = ExampleLedger();
        TokenPrice price  = new TokenPrice(ExactDecimal.Parse("2"), "USD");

        Assert.Equal(ExactDecimal.Parse("2469.1356"), ledger.MarketCapitalisation(price));
    }

    [Fact]
    public void Valuation_refuses_a_null_price() {
        Ledger  ledger = Chain.NewLedger();
        Address anyone = Chain.NewAddress();

        Assert.Throws<ArgumentNullException>(() => ledger.ValueOf(anyone, null!));
        Assert.Throws<ArgumentNullException>(() => ledger.MarketCapitalisation(null!));
    }

    [Fact]
    public void Advancing_moves_the_current_block_by_the_count() {
        // Arrange
        ulong  anyGenesis = Any.UInt64().Between(0, 1_000_000).Generate();
        Ledger ledger     = Any.Ledger().WithGenesisBlock(anyGenesis).Generate();

        // Act
        ledger.AdvanceBlocks(1);
        ledger.AdvanceBlocks(9);

        // Assert
        Assert.Equal(anyGenesis + 10, ledger.CurrentBlock);
    }

    [Fact]
    public void Advancing_by_nothing_is_refused() {
        Ledger ledger = Chain.NewLedger();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => ledger.AdvanceBlocks(0));

        Assert.Equal("count", refusal.ParamName);
    }

    [Fact]
    public void Advancing_may_reach_the_last_block_but_not_pass_it() {
        Ledger ledger = Any.Ledger().WithGenesisBlock(ulong.MaxValue - 10).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => ledger.AdvanceBlocks(11));
        Assert.Equal(ulong.MaxValue - 10, ledger.CurrentBlock);

        ledger.AdvanceBlocks(10);

        Assert.Equal(ulong.MaxValue, ledger.CurrentBlock);
        Assert.Throws<ArgumentOutOfRangeException>(() => ledger.AdvanceBlocks(1));
    }

    [Fact]
    public void Advancing_by_the_largest_count_from_a_later_block_is_refused() {
        ulong  anyGenesis = Any.UInt64().Between(1, 1_000_000).Generate();
        Ledger ledger     = Any.Ledger().WithGenesisBlock(anyGenesis).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => ledger.AdvanceBlocks(ulong.MaxValue));
    }

    [Fact]
    public void Advancing_by_the_largest_count_from_block_zero_reaches_the_last_block() {
        Ledger ledger = Any.Ledger().WithGenesisBlock(0).Generate();

        ledger.AdvanceBlocks(ulong.MaxValue);

        Assert.Equal(ulong.MaxValue, ledger.CurrentBlock);
    }

}
