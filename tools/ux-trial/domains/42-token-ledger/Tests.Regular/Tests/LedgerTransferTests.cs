namespace TokenLedger.Domain.Tests.Regular;

public sealed class LedgerTransferTests : LedgerTestBase {

    [Fact]
    public void A_transfer_moves_the_amount_from_the_sender_to_the_recipient() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter), anyRecipient, Units(300));

        // Assert
        Assert.Equal(Units(700), ledger.BalanceOf(anyMinter));
        Assert.Equal(Units(300), ledger.BalanceOf(anyRecipient));
        Assert.Equal(Units(1_000), ledger.TotalSupply);
    }

    [Fact]
    public void A_transfer_is_logged_as_a_movement_at_the_current_block() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).WithGenesisBlock(50).Generate();
        ledger.AdvanceBlocks(7);

        // Act
        ledger.Transfer(Tx(ledger, anyMinter), anyRecipient, Units(300));

        // Assert: the genesis mint, then the movement (the fee is zero, so it logs nothing).
        Assert.Equal(2, ledger.Transfers.Count);
        TransferRecord movement = ledger.Transfers[1];
        Assert.Equal(anyMinter, movement.From);
        Assert.Equal(anyRecipient, movement.To);
        Assert.Equal(Units(300), movement.Amount);
        Assert.Equal(57UL, movement.BlockNumber);
        Assert.False(movement.IsMint);
        Assert.False(movement.IsBurn);
    }

    [Fact]
    public void A_transfer_returns_a_receipt_stamped_with_the_current_block() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000_000).WithGenesisBlock(10).Generate();
        ledger.AdvanceBlocks(5);

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 3, gasLimit: 100_000), Any.Address().Generate(), Units(300));

        // Assert
        Assert.Equal(TransactionKind.Transfer, receipt.Kind);
        Assert.Equal(anyMinter, receipt.Sender);
        Assert.Equal(UInt128.Zero, receipt.Nonce);
        Assert.Equal(15UL, receipt.BlockNumber);
        Assert.Equal(55_000UL, receipt.GasUsed);
        Assert.Equal(Units(3), receipt.GasPrice);
        Assert.Equal(Units(165_000), receipt.Fee);
        Assert.Same(receipt, Assert.Single(ledger.Receipts));
    }

    [Fact]
    public void A_transfer_to_an_existing_holder_pays_no_new_holder_surcharge() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Address anyHolder = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyHolder, Units(10));

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter), anyHolder, Units(10));

        // Assert
        Assert.Equal(35_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_transfer_to_an_account_that_never_held_anything_pays_the_surcharge() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000_000).Generate();

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter), Any.Address().Generate(), Units(10));

        // Assert
        Assert.Equal(55_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_transfer_to_an_account_emptied_again_pays_the_surcharge_too() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Address anyHolder = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyHolder, Units(10));
        ledger.Transfer(Tx(ledger, anyHolder), anyMinter, Units(10));

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter), anyHolder, Units(10));

        // Assert: the balance was zero again, so the slot is "fresh" once more.
        Assert.Equal(55_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Transferring_nothing_to_a_new_account_pays_no_surcharge() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyMinter, Any.Address().Generate(), 1_000_000).Generate();

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter), anyRecipient, TokenAmount.Zero);

        // Assert
        Assert.Equal(35_000UL, receipt.GasUsed);
        Assert.True(ledger.BalanceOf(anyRecipient).IsZero);
        Assert.Equal(Units(1_000_000), ledger.BalanceOf(anyMinter));
    }

    [Fact]
    public void A_transfer_of_nothing_is_still_logged() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter), Any.Address().Generate(), TokenAmount.Zero);

        // Assert
        Assert.Equal(2, ledger.Transfers.Count);
        Assert.True(ledger.Transfers[1].Amount.IsZero);
    }

    [Fact]
    public void A_transfer_to_oneself_changes_no_balance() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter), anyMinter, Units(400));

        // Assert
        Assert.Equal(Units(1_000), ledger.BalanceOf(anyMinter));
        Assert.Equal(Units(1_000), ledger.TotalSupply);
        Assert.Equal(UInt128.One, ledger.NextNonceOf(anyMinter));
    }

    [Fact]
    public void A_sender_can_transfer_its_whole_balance() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter), anyRecipient, Units(1_000));

        // Assert
        Assert.True(ledger.BalanceOf(anyMinter).IsZero);
        Assert.Equal(Units(1_000), ledger.BalanceOf(anyRecipient));
        Assert.False(ledger.Accounts.Single(a => a.Address.Equals(anyMinter)).IsHolder);
        Assert.True(ledger.Accounts.Single(a => a.Address.Equals(anyRecipient)).IsHolder);
    }

    [Fact]
    public void A_transfer_above_the_sender_balance_is_refused_and_changes_nothing() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientBalance,
                                       () => ledger.Transfer(Tx(ledger, anyMinter), anyRecipient, Units(1_001)),
                                       anyMinter, anyRecipient);
    }

    [Fact]
    public void A_stranger_has_nothing_to_transfer() {
        // Arrange
        Address anyStranger = Any.Address().Generate();
        Ledger  ledger      = FeelessLedger(Any.Address().Generate(), Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientBalance,
                                       () => ledger.Transfer(Tx(ledger, anyStranger), Any.Address().Generate(), Units(1)),
                                       anyStranger);
    }

    [Fact]
    public void A_refused_transfer_does_not_create_an_account_for_a_stranger() {
        // Arrange
        Address anyStranger = Any.Address().Generate();
        Ledger  ledger      = FeelessLedger(Any.Address().Generate(), Any.Address().Generate(), 1_000).Generate();

        // Act
        Assert.Throws<LedgerRefusedException>(() => ledger.Transfer(Tx(ledger, anyStranger), Any.Address().Generate(), Units(1)));

        // Assert
        Assert.DoesNotContain(ledger.Accounts, a => a.Address.Equals(anyStranger));
    }

    [Fact]
    public void A_transfer_to_the_zero_address_is_refused_as_a_zero_address() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.ZeroAddress,
                                       () => ledger.Transfer(Tx(ledger, anyMinter), Address.Zero, Units(1)),
                                       anyMinter);
    }

    [Fact]
    public void A_successful_transfer_consumes_one_nonce_of_the_sender_only() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter), anyRecipient, Units(1));

        // Assert
        Assert.Equal(UInt128.One, ledger.NextNonceOf(anyMinter));
        Assert.Equal(UInt128.Zero, ledger.NextNonceOf(anyRecipient));
    }

    [Fact]
    public void Transfer_refuses_null_arguments() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        TransactionEnvelope anyEnvelope = Tx(ledger, anyMinter);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ledger.Transfer(null!, anyMinter, Units(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.Transfer(anyEnvelope, null!, Units(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.Transfer(anyEnvelope, anyMinter, null!));
    }

}
