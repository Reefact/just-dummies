namespace TokenLedger.Domain.Tests.Regular;

public sealed class LedgerNonceAndDeadlineTests : LedgerTestBase {

    [Fact]
    public void Nonces_are_consumed_one_after_the_other() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        Receipt first  = ledger.Transfer(TxWithNonce(anyMinter, 0), Any.Address().Generate(), Units(1));
        Receipt second = ledger.Transfer(TxWithNonce(anyMinter, 1), Any.Address().Generate(), Units(1));
        Receipt third  = ledger.Burn(TxWithNonce(anyMinter, 2), Units(1));

        // Assert
        Assert.Equal(new UInt128[] { 0, 1, 2 }, new[] { first.Nonce, second.Nonce, third.Nonce });
        Assert.Equal((UInt128)3, ledger.NextNonceOf(anyMinter));
    }

    [Fact]
    public void Each_sender_has_its_own_nonce() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(100));

        // Act
        ledger.Transfer(Tx(ledger, anyAlice), anyMinter, Units(1));

        // Assert
        Assert.Equal((UInt128)1, ledger.NextNonceOf(anyMinter));
        Assert.Equal((UInt128)1, ledger.NextNonceOf(anyAlice));
    }

    [Fact]
    public void A_nonce_already_used_is_too_low() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), Any.Address().Generate(), Units(1));

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.NonceTooLow,
                                       () => ledger.Transfer(TxWithNonce(anyMinter, 0), Any.Address().Generate(), Units(1)),
                                       anyMinter);
    }

    [Fact]
    public void A_nonce_that_skips_ahead_is_too_high() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.NonceTooHigh,
                                       () => ledger.Transfer(TxWithNonce(anyMinter, 1), Any.Address().Generate(), Units(1)),
                                       anyMinter);
    }

    [Fact]
    public void A_stranger_must_start_at_nonce_zero() {
        // Arrange
        Address anyStranger = Any.Address().Generate();
        Ledger  ledger      = FeelessLedger(Any.Address().Generate(), Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.NonceTooHigh, () => ledger.Transfer(TxWithNonce(anyStranger, 1), Any.Address().Generate(), Units(1)));
    }

    [Fact]
    public void A_refused_transaction_does_not_consume_the_nonce() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        AssertRefused(RefusalReason.InsufficientBalance, () => ledger.Transfer(Tx(ledger, anyMinter), Any.Address().Generate(), Units(5_000)));

        // Act: the same nonce is still the right one.
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter), Any.Address().Generate(), Units(5));

        // Assert
        Assert.Equal(UInt128.Zero, receipt.Nonce);
    }

    [Fact]
    public void The_deadline_block_is_inclusive() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).WithGenesisBlock(100).Generate();

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter, deadlineBlock: 100), Any.Address().Generate(), Units(1));

        // Assert
        Assert.Equal(100UL, receipt.BlockNumber);
    }

    [Fact]
    public void A_transaction_after_its_deadline_is_refused() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).WithGenesisBlock(100).Generate();
        TransactionEnvelope envelope = Tx(ledger, anyMinter, deadlineBlock: 100);
        ledger.AdvanceBlocks(1);

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.DeadlinePassed,
                                       () => ledger.Transfer(envelope, Any.Address().Generate(), Units(1)),
                                       anyMinter);
    }

    [Fact]
    public void A_transaction_with_a_deadline_in_the_future_is_accepted() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).WithGenesisBlock(100).Generate();

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter, deadlineBlock: 101), Any.Address().Generate(), Units(1));

        // Assert
        Assert.Equal(100UL, receipt.BlockNumber);
    }

    [Fact]
    public void A_transaction_without_deadline_is_accepted_at_the_last_block() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).WithGenesisBlock(ulong.MaxValue).Generate();

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter, deadlineBlock: ulong.MaxValue), Any.Address().Generate(), Units(1));

        // Assert
        Assert.Equal(ulong.MaxValue, receipt.BlockNumber);
    }

    [Fact]
    public void An_expired_transaction_is_refused_for_its_nonce_first_when_the_nonce_is_wrong_too() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).WithGenesisBlock(100).Generate();
        ledger.AdvanceBlocks(10);

        // Act & Assert: nonce 5 is too high and the deadline has passed; the nonce is checked first.
        AssertRefused(RefusalReason.NonceTooHigh,
                      () => ledger.Transfer(TxWithNonce(anyMinter, 5, deadlineBlock: 100), Any.Address().Generate(), Units(1)));
    }

    [Fact]
    public void An_expired_transaction_is_refused_for_its_deadline_before_its_gas_price() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000_000, minimumGasPrice: 5).WithGenesisBlock(100).Generate();
        ledger.AdvanceBlocks(10);

        // Act & Assert
        AssertRefused(RefusalReason.DeadlinePassed,
                      () => ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 1, gasLimit: 100_000, deadlineBlock: 100), Any.Address().Generate(), Units(1)));
    }

    [Fact]
    public void A_gas_price_below_the_minimum_is_refused_before_a_zero_recipient() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000_000, minimumGasPrice: 5).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.GasPriceTooLow,
                      () => ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 1, gasLimit: 100_000), Address.Zero, Units(1)));
    }

    [Fact]
    public void A_zero_recipient_is_refused_before_a_gas_limit_that_is_too_low() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000_000).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.ZeroAddress,
                      () => ledger.Transfer(Tx(ledger, anyMinter, gasLimit: 21_000), Address.Zero, Units(1)));
    }

    [Fact]
    public void A_gas_limit_that_is_too_low_is_refused_before_an_insufficient_balance() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.GasLimitTooLow,
                      () => ledger.Transfer(Tx(ledger, anyMinter, gasLimit: 21_000), Any.Address().Generate(), Units(5_000)));
    }

    [Fact]
    public void An_insufficient_balance_is_refused_before_the_fee_funds() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert: the amount exceeds the balance, and the maximum fee (60 000) does too.
        AssertRefused(RefusalReason.InsufficientBalance,
                      () => ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 1, gasLimit: 60_000), Any.Address().Generate(), Units(5_000)));
    }

    [Fact]
    public void Several_refused_transactions_in_a_row_leave_the_ledger_untouched() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        string  before       = Snapshot(ledger, anyMinter, anyRecipient);

        // Act
        Assert.Throws<LedgerRefusedException>(() => ledger.Transfer(TxWithNonce(anyMinter, 3), anyRecipient, Units(1)));
        Assert.Throws<LedgerRefusedException>(() => ledger.Transfer(Tx(ledger, anyMinter), Address.Zero, Units(1)));
        Assert.Throws<LedgerRefusedException>(() => ledger.Transfer(Tx(ledger, anyMinter), anyRecipient, Units(1_001)));
        Assert.Throws<LedgerRefusedException>(() => ledger.Burn(Tx(ledger, anyMinter), Units(1_001)));
        Assert.Throws<LedgerRefusedException>(() => ledger.Mint(Tx(ledger, anyRecipient), anyRecipient, Units(1)));

        // Assert
        Assert.Equal(before, Snapshot(ledger, anyMinter, anyRecipient));
    }

}
