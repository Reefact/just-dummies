using System.Numerics;

using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class LedgerQueryTests {

    // ---- balances, nonces, allowances of addresses nobody has used -------------------------------------------------

    [Fact]
    public void An_address_the_ledger_has_never_seen_holds_nothing() {
        // Arrange
        Ledger  anyLedger  = Any.Ledger().Generate();
        Address anyStranger = Any.Address().Generate();

        // Assert
        Assert.Equal(TokenAmount.Zero, anyLedger.BalanceOf(anyStranger));
        Assert.True(anyLedger.HumanBalanceOf(anyStranger).IsZero);
    }

    [Fact]
    public void An_address_the_ledger_has_never_seen_expects_nonce_zero() {
        // Arrange
        Ledger  anyLedger   = Any.Ledger().Generate();
        Address anyStranger = Any.Address().Generate();

        // Assert
        Assert.Equal(UInt128.Zero, anyLedger.NextNonceOf(anyStranger));
    }

    [Fact]
    public void An_allowance_nobody_set_is_zero() {
        // Arrange
        Ledger  anyLedger = Any.Ledger().Generate();
        Address anyOwner  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();

        // Assert
        Assert.Equal(TokenAmount.Zero, anyLedger.AllowanceOf(anyOwner, anySpender));
    }

    [Fact]
    public void An_allowance_belongs_to_the_pair_it_was_set_for_and_to_no_other() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        Assert.Equal(Amount.Of(500), chain.Ledger.AllowanceOf(chain.Alice, chain.Bob));
        Assert.Equal(TokenAmount.Zero, chain.Ledger.AllowanceOf(chain.Bob, chain.Alice));
        Assert.Equal(TokenAmount.Zero, chain.Ledger.AllowanceOf(chain.Alice, chain.Carol));
        Assert.Equal(TokenAmount.Zero, chain.Ledger.AllowanceOf(chain.Carol, chain.Bob));
    }

    [Fact]
    public void Asking_about_nobody_is_refused_as_a_missing_argument() {
        // Arrange
        Ledger  anyLedger  = Any.Ledger().Generate();
        Address anyAddress = Any.Address().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyLedger.BalanceOf(null!));
        Assert.Throws<ArgumentNullException>(() => anyLedger.NextNonceOf(null!));
        Assert.Throws<ArgumentNullException>(() => anyLedger.AllowanceOf(null!, anyAddress));
        Assert.Throws<ArgumentNullException>(() => anyLedger.AllowanceOf(anyAddress, null!));
    }

    // ---- human balance and value ----------------------------------------------------------------------------------

    [Fact]
    public void A_balance_reads_as_its_exact_human_value() {
        // Arrange
        Chain chain = Chain.Start(decimals: 4).FundAlice(12_345_678);

        // Assert
        Assert.Equal(ExactDecimal.Parse("1234.5678"), chain.Ledger.HumanBalanceOf(chain.Alice));
    }

    [Fact]
    public void A_balance_is_valued_at_the_price_of_a_whole_token() {
        // Arrange
        Chain      chain = Chain.Start(decimals: 4).FundAlice(12_345_678);
        TokenPrice price = new TokenPrice(ExactDecimal.Parse("2.5"), "USD");

        // Assert
        Assert.Equal(ExactDecimal.Parse("3086.4195"), chain.Ledger.ValueOf(chain.Alice, price));
    }

    [Fact]
    public void The_market_capitalisation_values_the_whole_supply() {
        // Arrange: 1 token + 0.5 token = 1.5 tokens at 4 decimals, at 2 per token.
        Chain      chain = Chain.Start(decimals: 4).FundAlice(10_000).FundBob(5_000);
        TokenPrice price = new TokenPrice(ExactDecimal.Parse("2"), "USD");

        // Assert
        Assert.Equal(ExactDecimal.Parse("3"), chain.Ledger.MarketCapitalisation(price));
    }

    [Fact]
    public void Valuing_without_a_price_is_refused_as_a_missing_argument() {
        // Arrange
        Ledger  anyLedger  = Any.Ledger().Generate();
        Address anyAddress = Any.Address().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyLedger.ValueOf(anyAddress, null!));
        Assert.Throws<ArgumentNullException>(() => anyLedger.MarketCapitalisation(null!));
    }

    // ---- advancing the chain --------------------------------------------------------------------------------------

    [Fact]
    public void Advancing_moves_the_current_block_forward_by_the_count() {
        // Arrange
        Ledger anyLedger = Any.Ledger().WithGenesisBlock(100).Generate();

        // Act
        anyLedger.AdvanceBlocks(25);

        // Assert
        Assert.Equal(125UL, anyLedger.CurrentBlock);
    }

    [Fact]
    public void Advancing_by_nothing_is_refused() {
        // Arrange
        Ledger anyLedger = Any.Ledger().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyLedger.AdvanceBlocks(0));
    }

    [Fact]
    public void Advancing_may_land_exactly_on_the_last_block_number() {
        // Arrange
        Ledger anyLedger = Any.Ledger().WithGenesisBlock(ulong.MaxValue - 5).Generate();

        // Act
        anyLedger.AdvanceBlocks(5);

        // Assert
        Assert.Equal(ulong.MaxValue, anyLedger.CurrentBlock);
    }

    [Fact]
    public void Advancing_past_the_last_block_number_is_refused_and_leaves_the_block_where_it_was() {
        // Arrange
        Ledger anyLedger = Any.Ledger().WithGenesisBlock(ulong.MaxValue - 5).Generate();

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => anyLedger.AdvanceBlocks(6));

        // Assert
        Assert.Equal(ulong.MaxValue - 5, anyLedger.CurrentBlock);
    }

}
