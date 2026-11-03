namespace TokenLedger.Domain.Tests.Regular;

public sealed class LedgerScenarioTests : LedgerTestBase {

    [Fact]
    public void After_a_mix_of_operations_the_balances_still_add_up_to_the_supply() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();
        Address anyAlice     = Any.Address().Generate();
        Address anyBob       = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyMinter, anyCollector, 5_000_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 2, gasLimit: 100_000), anyAlice, Units(1_000_000));
        ledger.Mint(Tx(ledger, anyMinter, gasPrice: 2, gasLimit: 100_000), anyBob, Units(500_000));
        ledger.Approve(Tx(ledger, anyAlice, gasPrice: 2, gasLimit: 100_000), anyBob, Units(300_000));
        ledger.TransferFrom(Tx(ledger, anyBob, gasPrice: 2, gasLimit: 100_000), anyAlice, anyCollector, Units(200_000));
        ledger.Burn(Tx(ledger, anyAlice, gasPrice: 2, gasLimit: 100_000), Units(100_000));
        ledger.AdvanceBlocks(3);
        ledger.IncreaseAllowance(Tx(ledger, anyBob, gasPrice: 2, gasLimit: 100_000), anyAlice, Units(10));

        // Assert
        Assert.Equal(Units(5_000_000 + 500_000 - 100_000), ledger.TotalSupply);
        Assert.Equal(ledger.TotalSupply, SumOfBalances(ledger));
        Assert.Equal(6, ledger.Receipts.Count);
    }

    [Fact]
    public void Receipts_are_kept_in_the_order_the_transactions_were_accepted() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(10));
        ledger.Approve(Tx(ledger, anyAlice), anyMinter, Units(5));
        ledger.Burn(Tx(ledger, anyMinter), Units(1));

        // Assert
        Assert.Equal(new[] { TransactionKind.Transfer, TransactionKind.Approve, TransactionKind.Burn },
                     ledger.Receipts.Select(r => r.Kind).ToArray());
        Assert.Equal(new[] { anyMinter, anyAlice, anyMinter }, ledger.Receipts.Select(r => r.Sender).ToArray());
    }

    [Fact]
    public void Receipts_carry_the_block_in_which_they_were_accepted() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).WithGenesisBlock(1).Generate();

        // Act
        ledger.Burn(Tx(ledger, anyMinter), Units(1));
        ledger.AdvanceBlocks(9);
        ledger.Burn(Tx(ledger, anyMinter), Units(1));

        // Assert
        Assert.Equal(new[] { 1UL, 10UL }, ledger.Receipts.Select(r => r.BlockNumber).ToArray());
    }

    [Fact]
    public void An_account_that_receives_a_token_appears_among_the_accounts() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter), anyRecipient, Units(1));

        // Assert
        Account recipient = ledger.Accounts.Single(a => a.Address.Equals(anyRecipient));
        Assert.Equal(Units(1), recipient.Balance);
        Assert.True(recipient.IsHolder);
        Assert.Equal(UInt128.Zero, recipient.NextNonce);
    }

    [Fact]
    public void Ledgers_do_not_share_state() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        Ledger  other     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Burn(Tx(ledger, anyMinter), Units(1_000));

        // Assert
        Assert.Equal(Units(1_000), other.BalanceOf(anyMinter));
        Assert.Equal(UInt128.Zero, other.NextNonceOf(anyMinter));
    }

    [Fact]
    public void The_largest_possible_supply_can_be_held_and_moved_without_wrapping() {
        // Arrange: a token with no decimals whose cap is the largest amount.
        Address         anyMinter    = Any.Address().Generate();
        Address         anyRecipient = Any.Address().Generate();
        TokenDefinition token        = Any.TokenDefinition().WithDecimals(0).WithSupplyCap(TokenAmount.Max).Generate();
        Ledger          ledger       = Any.Ledger()
                                          .WithToken(token)
                                          .WithMinter(anyMinter)
                                          .WithInitialSupply(TokenAmount.Max)
                                          .WithMinimumGasPrice(TokenAmount.Zero)
                                          .Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter), anyRecipient, TokenAmount.Max);

        // Assert
        Assert.Equal(TokenAmount.Max, ledger.BalanceOf(anyRecipient));
        Assert.True(ledger.BalanceOf(anyMinter).IsZero);
        Assert.True(ledger.RemainingMintable.IsZero);
    }

}
