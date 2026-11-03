namespace TokenLedger.Domain.Tests.Regular;

public sealed class LedgerFeeTests : LedgerTestBase {

    [Fact]
    public void The_fee_is_the_gas_used_times_the_gas_price_and_goes_to_the_collector() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyMinter, anyCollector, 10_000_000).Generate();

        // Act: a transfer to a new holder uses 35 000 + 20 000 gas, at 2 per gas.
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 2, gasLimit: 100_000), anyRecipient, Units(300));

        // Assert
        Assert.Equal(Units(110_000), receipt.Fee);
        Assert.Equal(Units(10_000_000 - 300 - 110_000), ledger.BalanceOf(anyMinter));
        Assert.Equal(Units(300), ledger.BalanceOf(anyRecipient));
        Assert.Equal(Units(110_000), ledger.BalanceOf(anyCollector));
    }

    [Fact]
    public void Fees_move_tokens_they_neither_create_nor_destroy_them() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 2, gasLimit: 100_000), Any.Address().Generate(), Units(300));

        // Assert
        Assert.Equal(Units(10_000_000), ledger.TotalSupply);
        Assert.Equal(ledger.TotalSupply, SumOfBalances(ledger));
    }

    [Fact]
    public void The_fee_is_logged_as_a_movement_to_the_collector_after_the_transfer_itself() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyMinter, anyCollector, 10_000_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 2, gasLimit: 100_000), anyRecipient, Units(300));

        // Assert: genesis mint, the movement, the fee.
        Assert.Equal(3, ledger.Transfers.Count);
        Assert.Equal(anyRecipient, ledger.Transfers[1].To);
        TransferRecord fee = ledger.Transfers[2];
        Assert.Equal(anyMinter, fee.From);
        Assert.Equal(anyCollector, fee.To);
        Assert.Equal(Units(110_000), fee.Amount);
    }

    [Fact]
    public void The_fee_charged_is_the_gas_used_not_the_gas_limit() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyMinter, anyCollector, 10_000_000).Generate();

        // Act: 5 000 000 gas allowed, 55 000 used.
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 1, gasLimit: 5_000_000), Any.Address().Generate(), Units(1));

        // Assert
        Assert.Equal(55_000UL, receipt.GasUsed);
        Assert.Equal(Units(55_000), ledger.BalanceOf(anyCollector));
    }

    [Fact]
    public void A_free_transaction_logs_no_fee_movement() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 0), Any.Address().Generate(), Units(300));

        // Assert: the genesis mint and the movement, nothing else.
        Assert.Equal(2, ledger.Transfers.Count);
    }

    [Fact]
    public void A_collector_who_pays_the_fee_gets_it_back() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, anyMinter, 10_000_000).Generate();

        // Act
        ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 2, gasLimit: 100_000), Any.Address().Generate(), Units(300));

        // Assert
        Assert.Equal(Units(10_000_000 - 300), ledger.BalanceOf(anyMinter));
        Assert.Equal(Units(10_000_000), SumOfBalances(ledger));
    }

    [Fact]
    public void The_sender_must_be_able_to_pay_the_maximum_fee_not_just_the_fee_actually_due() {
        // Arrange: Alice holds 150 000; the transfer will only use 70 000 gas (existing holder), but the limit is 100 000 at price 2.
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(150_000));

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientFundsForFee,
                                       () => ledger.Transfer(Tx(ledger, anyAlice, gasPrice: 2, gasLimit: 100_000), anyMinter, TokenAmount.Zero),
                                       anyAlice, anyMinter);
    }

    [Fact]
    public void A_sender_holding_exactly_the_maximum_fee_can_pay_it() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(200_000));

        // Act: maximum fee 100 000 x 2 = 200 000.
        ledger.Transfer(Tx(ledger, anyAlice, gasPrice: 2, gasLimit: 100_000), anyMinter, TokenAmount.Zero);

        // Assert: only the 70 000 actually due is paid.
        Assert.Equal(Units(200_000 - 70_000), ledger.BalanceOf(anyAlice));
    }

    [Fact]
    public void What_the_operation_debits_comes_off_the_balance_before_the_fee_is_checked() {
        // Arrange: Alice holds 100 000 and sends 60 000, leaving 40 000 against a maximum fee of 50 000.
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(100_000));

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientFundsForFee,
                                       () => ledger.Transfer(Tx(ledger, anyAlice, gasPrice: 1, gasLimit: 50_000), anyMinter, Units(60_000)),
                                       anyAlice, anyMinter);
    }

    [Fact]
    public void A_sender_keeping_exactly_the_maximum_fee_after_the_operation_is_accepted() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(100_000));

        // Act: 50 000 left after sending 50 000, against a maximum fee of 50 000.
        ledger.Transfer(Tx(ledger, anyAlice, gasPrice: 1, gasLimit: 50_000), anyMinter, Units(50_000));

        // Assert: the fee actually due is 35 000.
        Assert.Equal(Units(100_000 - 50_000 - 35_000), ledger.BalanceOf(anyAlice));
    }

    [Fact]
    public void A_refusal_for_fee_funds_is_checked_after_the_balance_itself() {
        // Arrange: Alice cannot cover the amount, and could not cover the fee either.
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(100));

        // Act & Assert
        AssertRefused(RefusalReason.InsufficientBalance,
                      () => ledger.Transfer(Tx(ledger, anyAlice, gasPrice: 1, gasLimit: 50_000), anyMinter, Units(101)));
    }

    [Fact]
    public void A_gas_price_below_the_minimum_is_refused() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000, minimumGasPrice: 5).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.GasPriceTooLow,
                                       () => ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 4, gasLimit: 100_000), Any.Address().Generate(), Units(1)),
                                       anyMinter);
    }

    [Fact]
    public void A_gas_price_equal_to_the_minimum_is_accepted() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000, minimumGasPrice: 5).Generate();

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter, gasPrice: 5, gasLimit: 100_000), Any.Address().Generate(), Units(1));

        // Assert
        Assert.Equal(Units(5), receipt.GasPrice);
    }

    [Fact]
    public void A_gas_limit_below_the_gas_the_operation_needs_is_refused() {
        // Arrange: a transfer to a new holder needs 55 000.
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.GasLimitTooLow,
                                       () => ledger.Transfer(Tx(ledger, anyMinter, gasLimit: 54_999), Any.Address().Generate(), Units(1)),
                                       anyMinter);
    }

    [Fact]
    public void A_gas_limit_equal_to_the_gas_the_operation_needs_is_accepted() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter, gasLimit: 55_000), Any.Address().Generate(), Units(1));

        // Assert
        Assert.Equal(55_000UL, receipt.GasUsed);
    }

    [Fact]
    public void The_surcharge_is_not_needed_when_nothing_is_credited() {
        // Arrange: 35 000 is enough for a zero transfer to a new account.
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();

        // Act
        Receipt receipt = ledger.Transfer(Tx(ledger, anyMinter, gasLimit: 35_000), Any.Address().Generate(), TokenAmount.Zero);

        // Assert
        Assert.Equal(35_000UL, receipt.GasUsed);
    }

}
