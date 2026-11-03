namespace TokenLedger.Domain.Tests.Regular;

public sealed class LedgerQueryTests : LedgerTestBase {

    [Fact]
    public void An_unseen_address_has_no_balance_no_nonce_and_no_allowance() {
        // Arrange
        Ledger  ledger        = Any.Ledger().Generate();
        Address anyStranger   = Any.Address().Generate();
        Address anyOtherOwner = Any.Address().Generate();

        // Assert
        Assert.Equal(TokenAmount.Zero, ledger.BalanceOf(anyStranger));
        Assert.Equal(UInt128.Zero, ledger.NextNonceOf(anyStranger));
        Assert.Equal(TokenAmount.Zero, ledger.AllowanceOf(anyStranger, anyOtherOwner));
        Assert.DoesNotContain(ledger.Accounts, a => a.Address.Equals(anyStranger));
    }

    [Fact]
    public void The_queries_refuse_null_arguments() {
        // Arrange
        Ledger  ledger     = Any.Ledger().Generate();
        Address anyAddress = Any.Address().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ledger.BalanceOf(null!));
        Assert.Throws<ArgumentNullException>(() => ledger.NextNonceOf(null!));
        Assert.Throws<ArgumentNullException>(() => ledger.AllowanceOf(null!, anyAddress));
        Assert.Throws<ArgumentNullException>(() => ledger.AllowanceOf(anyAddress, null!));
        Assert.Throws<ArgumentNullException>(() => ledger.ValueOf(anyAddress, null!));
        Assert.Throws<ArgumentNullException>(() => ledger.MarketCapitalisation(null!));
    }

    [Fact]
    public void A_balance_is_reported_as_an_exact_human_amount() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 12_345_678)
                            .WithToken(t => t.WithDecimals(4))
                            .Generate();

        // Assert
        Assert.Equal(ExactDecimal.Parse("1234.5678"), ledger.HumanBalanceOf(anyMinter));
        Assert.Equal(ExactDecimal.Zero, ledger.HumanBalanceOf(Any.Address().Generate()));
    }

    [Fact]
    public void An_address_is_valued_at_its_balance_times_the_price() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 12_345_678)
                            .WithToken(t => t.WithDecimals(4))
                            .Generate();
        TokenPrice price = Any.TokenPrice().WithPerToken(ExactDecimal.Parse("2.5")).Generate();

        // Act
        ExactDecimal value = ledger.ValueOf(anyMinter, price);

        // Assert: 1234.5678 tokens at 2.5
        Assert.Equal(ExactDecimal.Parse("3086.4195"), value);
    }

    [Fact]
    public void The_market_capitalisation_is_the_total_supply_times_the_price() {
        // Arrange
        Ledger ledger = FeelessLedger(Any.Address().Generate(), Any.Address().Generate(), 2_000_000)
                        .WithToken(t => t.WithDecimals(6))
                        .Generate();
        TokenPrice price = Any.TokenPrice().WithPerToken(ExactDecimal.Parse("0.25")).Generate();

        // Act
        ExactDecimal capitalisation = ledger.MarketCapitalisation(price);

        // Assert: 2 tokens at 0.25
        Assert.Equal(ExactDecimal.Parse("0.5"), capitalisation);
    }

    [Fact]
    public void Advancing_the_chain_moves_the_current_block() {
        // Arrange
        Ledger ledger = Any.Ledger().WithGenesisBlock(100).Generate();

        // Act
        ledger.AdvanceBlocks(5);
        ledger.AdvanceBlocks(1);

        // Assert
        Assert.Equal(106UL, ledger.CurrentBlock);
    }

    [Fact]
    public void The_chain_cannot_advance_by_zero_blocks() {
        // Arrange
        Ledger ledger = Any.Ledger().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => ledger.AdvanceBlocks(0));
    }

    [Fact]
    public void The_chain_can_advance_up_to_the_last_block_but_not_past_it() {
        // Arrange
        Ledger ledger = Any.Ledger().WithGenesisBlock(ulong.MaxValue - 10).Generate();

        // Act
        ledger.AdvanceBlocks(10);

        // Assert
        Assert.Equal(ulong.MaxValue, ledger.CurrentBlock);
        Assert.Throws<ArgumentOutOfRangeException>(() => ledger.AdvanceBlocks(1));
        Assert.Equal(ulong.MaxValue, ledger.CurrentBlock);
    }

    [Fact]
    public void An_advance_that_would_pass_the_last_block_changes_nothing() {
        // Arrange
        Ledger ledger = Any.Ledger().WithGenesisBlock(ulong.MaxValue - 10).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => ledger.AdvanceBlocks(11));
        Assert.Equal(ulong.MaxValue - 10, ledger.CurrentBlock);
    }

}
