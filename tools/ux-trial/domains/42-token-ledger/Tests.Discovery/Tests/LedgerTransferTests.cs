using JustDummies;

using TokenLedger.Domain;
using TokenLedger.Tests.Discovery.Support;

using Xunit;

namespace TokenLedger.Tests.Discovery;

// Standard schedule, gas price 2: a transfer to a new holder uses 35 000 + 20 000 = 55 000 gas and pays 110 000 base units.
public sealed class LedgerTransferTests {

    private const ulong Supply = 10_000_000_000;

    [Fact]
    public void A_transfer_moves_the_amount_and_pays_the_fee_to_the_collector() {
        // Arrange
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        // Act
        ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1_000_000));

        // Assert
        Assert.Equal(new TokenAmount(1_000_000), ledger.BalanceOf(alice));
        Assert.Equal(new TokenAmount(Supply - 1_000_000 - 110_000), ledger.BalanceOf(ledger.Minter));
        Assert.Equal(new TokenAmount(110_000), ledger.BalanceOf(ledger.FeeCollector));
    }

    [Fact]
    public void A_transfer_returns_a_receipt_stamped_with_the_current_block() {
        // Arrange
        Ledger ledger = Chain.NewLedger(Supply);
        ledger.AdvanceBlocks(7);
        Address             alice    = Chain.NewAddress();
        TransactionEnvelope envelope = Chain.Next(ledger, ledger.Minter);

        // Act
        Receipt receipt = ledger.Transfer(envelope, alice, new TokenAmount(1_000));

        // Assert
        Assert.Equal(TransactionKind.Transfer, receipt.Kind);
        Assert.Equal(ledger.Minter, receipt.Sender);
        Assert.Equal(envelope.Nonce, receipt.Nonce);
        Assert.Equal(ledger.CurrentBlock, receipt.BlockNumber);
        Assert.Equal(55_000UL, receipt.GasUsed);
        Assert.Equal(new TokenAmount(2), receipt.GasPrice);
        Assert.Equal(new TokenAmount(110_000), receipt.Fee);
        Assert.Same(receipt, Assert.Single(ledger.Receipts));
    }

    [Fact]
    public void The_fee_is_the_gas_used_not_the_gas_limit() {
        // Arrange: the limit is only an upper bound, so whatever it is, the fee is the same.
        Ledger  ledger   = Chain.NewLedger(Supply);
        Address alice    = Chain.NewAddress();
        ulong   anyLimit = Any.UInt64().Between(Chain.AmpleGasFrom, Chain.AmpleGasTo).Generate();

        // Act
        Receipt receipt = ledger.Transfer(Chain.NextWithGasLimit(ledger, ledger.Minter, anyLimit), alice, new TokenAmount(1_000));

        // Assert
        Assert.Equal(new TokenAmount(110_000), receipt.Fee);
        Assert.Equal(new TokenAmount(110_000), ledger.BalanceOf(ledger.FeeCollector));
    }

    [Fact]
    public void A_transfer_increases_the_sender_nonce_by_one() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1_000));
        ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1_000));

        Assert.Equal(new UInt128(0, 2), ledger.NextNonceOf(ledger.Minter));
        Assert.Equal(UInt128.Zero, ledger.NextNonceOf(alice));
    }

    [Fact]
    public void A_transfer_is_logged_before_the_fee_it_pays() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        int     before = ledger.Transfers.Count;

        ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1_000));

        Assert.Equal(before + 2, ledger.Transfers.Count);
        TransferRecord movement = ledger.Transfers[before];
        TransferRecord fee      = ledger.Transfers[before + 1];
        Assert.Equal(new TransferRecord(ledger.Minter, alice, new TokenAmount(1_000), ledger.CurrentBlock), movement);
        Assert.Equal(new TransferRecord(ledger.Minter, ledger.FeeCollector, new TokenAmount(110_000), ledger.CurrentBlock), fee);
    }

    [Fact]
    public void A_transfer_made_for_free_logs_no_fee_movement() {
        // Arrange: a ledger whose minimum gas price is zero, and a transaction paying zero.
        Ledger  ledger = Chain.NewLedger(Supply, minimumGasPrice: 0);
        Address alice  = Chain.NewAddress();

        // Act
        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter, gasPrice: 0), alice, new TokenAmount(1_000));

        // Assert
        Assert.True(receipt.Fee.IsZero);
        Assert.Equal(TokenAmount.Zero, ledger.BalanceOf(ledger.FeeCollector));
        Assert.Equal(2, ledger.Transfers.Count);
        Assert.False(ledger.Transfers[^1].To.Equals(ledger.FeeCollector));
    }

    [Fact]
    public void A_transfer_to_a_new_holder_pays_the_surcharge() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1));

        Assert.Equal(55_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_transfer_to_an_existing_holder_pays_no_surcharge() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1_000);

        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1));

        Assert.Equal(35_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_transfer_of_nothing_to_a_new_holder_pays_no_surcharge() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, TokenAmount.Zero);

        Assert.Equal(35_000UL, receipt.GasUsed);
        Assert.Equal(TokenAmount.Zero, ledger.BalanceOf(alice));
        Assert.Contains(ledger.Transfers, record => record.To.Equals(alice) && record.Amount.IsZero);
    }

    [Fact]
    public void A_holder_who_emptied_their_account_is_a_new_holder_again() {
        // Arrange: alice is funded, then sends everything on, for free, so her balance is back to zero.
        Ledger  ledger = Chain.NewLedger(Supply, minimumGasPrice: 0);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        ledger.Transfer(Chain.Next(ledger, ledger.Minter, gasPrice: 0), alice, new TokenAmount(500));
        ledger.Transfer(Chain.Next(ledger, alice, gasPrice: 0), bob, new TokenAmount(500));
        Assert.Equal(TokenAmount.Zero, ledger.BalanceOf(alice));

        // Act
        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter, gasPrice: 0), alice, new TokenAmount(5));

        // Assert
        Assert.Equal(55_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_transfer_to_the_fee_collector_while_it_holds_nothing_pays_the_surcharge() {
        Ledger ledger = Chain.NewLedger(Supply);

        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter), ledger.FeeCollector, new TokenAmount(1));

        Assert.Equal(55_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_transfer_to_oneself_costs_only_the_fee() {
        Ledger ledger = Chain.NewLedger(Supply);

        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter), ledger.Minter, new TokenAmount(1_000));

        Assert.Equal(35_000UL, receipt.GasUsed);
        Assert.Equal(new TokenAmount(Supply - 70_000), ledger.BalanceOf(ledger.Minter));
    }

    [Fact]
    public void A_sender_who_collects_the_fees_gets_the_fee_back() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        // The minter is also the collector in this ledger.
        Ledger single = Any.Ledger()
                           .WithMinter(ledger.Minter)
                           .WithFeeCollector(ledger.Minter)
                           .WithToken(ledger.Token)
                           .WithInitialSupply(new TokenAmount(Supply))
                           .WithMinimumGasPrice(new TokenAmount(1))
                           .WithGasSchedule(GasSchedule.Standard)
                           .Generate();

        single.Transfer(Chain.Next(single, single.Minter), alice, new TokenAmount(1_000));

        Assert.Equal(new TokenAmount(Supply - 1_000), single.BalanceOf(single.Minter));
    }

    [Fact]
    public void Transfers_and_fees_never_change_the_total_supply() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();

        ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(2_000_000));
        ledger.Transfer(Chain.Next(ledger, alice), bob, new TokenAmount(500_000));
        ledger.Transfer(Chain.Next(ledger, bob), ledger.Minter, new TokenAmount(1_000));

        Assert.Equal(new TokenAmount(Supply), ledger.TotalSupply);
        Assert.Equal(new System.Numerics.BigInteger(Supply), Chain.SumOfBalances(ledger));
    }

    [Fact]
    public void A_transfer_can_leave_the_sender_with_exactly_what_the_maximum_fee_needs() {
        // Arrange: alice holds 1 000 000 + the maximum fee of her transaction (limit 100 000 x price 2).
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1_000_000 + 200_000);
        Address bob = Chain.NewAddress();
        Chain.Fund(ledger, bob, 1);

        // Act
        Receipt receipt = ledger.Transfer(Chain.NextWithGasLimit(ledger, alice, 100_000), bob, new TokenAmount(1_000_000));

        // Assert: the actual fee is 70 000, not the 200 000 reserved.
        Assert.Equal(new TokenAmount(200_000 - 70_000), ledger.BalanceOf(alice));
        Assert.Equal(new TokenAmount(70_000), receipt.Fee);
    }

    [Fact]
    public void A_transfer_refused_for_a_short_balance_for_the_fee_changes_nothing() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1_000_000 + 199_999);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.NextWithGasLimit(ledger, alice, 100_000), bob, new TokenAmount(1_000_000)), alice, bob);

        Assert.Equal(RefusalReason.InsufficientFundsForFee, refusal.Reason);
    }

    [Fact]
    public void A_transfer_of_the_whole_balance_cannot_pay_its_fee() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1_000_000);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.Next(ledger, alice), bob, new TokenAmount(1_000_000)), alice, bob);

        Assert.Equal(RefusalReason.InsufficientFundsForFee, refusal.Reason);
    }

    [Fact]
    public void A_transfer_above_the_balance_is_refused_for_the_balance() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1_000_000);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.Next(ledger, alice), bob, new TokenAmount(1_000_001)), alice, bob);

        Assert.Equal(RefusalReason.InsufficientBalance, refusal.Reason);
    }

    [Fact]
    public void A_sender_without_an_account_cannot_transfer_anything() {
        Ledger  ledger   = Chain.NewLedger(Supply);
        Address stranger = Chain.NewAddress();
        Address bob      = Chain.NewAddress();

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.Next(ledger, stranger), bob, new TokenAmount(1)), stranger, bob);

        Assert.Equal(RefusalReason.InsufficientBalance, refusal.Reason);
    }

    [Fact]
    public void A_sender_without_an_account_can_send_nothing_if_the_fee_is_free() {
        Ledger  ledger   = Chain.NewLedger(Supply, minimumGasPrice: 0);
        Address stranger = Chain.NewAddress();
        Address bob      = Chain.NewAddress();

        Receipt receipt = ledger.Transfer(Chain.Next(ledger, stranger, gasPrice: 0), bob, TokenAmount.Zero);

        Assert.True(receipt.Fee.IsZero);
        Assert.Equal(new UInt128(0, 1), ledger.NextNonceOf(stranger));
    }

    [Fact]
    public void A_transfer_to_the_zero_address_is_refused() {
        Ledger ledger = Chain.NewLedger(Supply);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.Next(ledger, ledger.Minter), Address.Zero, new TokenAmount(1)));

        Assert.Equal(RefusalReason.ZeroAddress, refusal.Reason);
    }

    [Fact]
    public void A_transfer_needs_the_gas_of_the_operation_and_of_the_surcharge() {
        // Arrange
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        // Act: 55 000 gas are needed for a new holder, one less is refused, the exact figure is accepted.
        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.NextWithGasLimit(ledger, ledger.Minter, 54_999), alice, new TokenAmount(1)), alice);
        Receipt                receipt = ledger.Transfer(Chain.NextWithGasLimit(ledger, ledger.Minter, 55_000), alice, new TokenAmount(1));

        // Assert
        Assert.Equal(RefusalReason.GasLimitTooLow, refusal.Reason);
        Assert.Equal(55_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_transfer_to_an_existing_holder_needs_no_surcharge_gas() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 10);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.NextWithGasLimit(ledger, ledger.Minter, 34_999), alice, new TokenAmount(1)), alice);
        Receipt                receipt = ledger.Transfer(Chain.NextWithGasLimit(ledger, ledger.Minter, 35_000), alice, new TokenAmount(1));

        Assert.Equal(RefusalReason.GasLimitTooLow, refusal.Reason);
        Assert.Equal(35_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_ledger_with_another_schedule_charges_its_own_costs() {
        // Arrange
        GasSchedule schedule = new GasSchedule(25_000, 22_000, 26_000, 27_000, 28_000, 3_000);
        Ledger      ledger   = Chain.NewLedger(schedule, Supply);
        Address     alice    = Chain.NewAddress();

        // Act
        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1));

        // Assert
        Assert.Equal(28_000UL, receipt.GasUsed);
        Assert.Equal(new TokenAmount(56_000), receipt.Fee);
    }

    [Fact]
    public void A_transfer_refuses_null_arguments() {
        Ledger              ledger   = Chain.NewLedger(Supply);
        Address             alice    = Chain.NewAddress();
        TransactionEnvelope envelope = Chain.Next(ledger, ledger.Minter);

        Assert.Throws<ArgumentNullException>(() => ledger.Transfer(null!, alice, new TokenAmount(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.Transfer(envelope, null!, new TokenAmount(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.Transfer(envelope, alice, null!));
    }

}
