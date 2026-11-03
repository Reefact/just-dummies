using System.Numerics;

using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class LedgerTransferTests {

    // ---- what a transfer does -------------------------------------------------------------------------------------

    [Fact]
    public void A_transfer_moves_the_amount_and_the_sender_pays_the_fee_to_the_collector() {
        // Arrange: Bob already holds tokens, so no new holder is created.
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act
        chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Assert: 35_000 gas at 10 per gas is a fee of 350_000.
        Assert.Equal(Chain.Funds - 500 - 350_000, chain.BalanceOf(chain.Alice));
        Assert.Equal(Chain.Funds + 500, chain.BalanceOf(chain.Bob));
        Assert.Equal(350_000, chain.BalanceOf(chain.Collector));
    }

    [Fact]
    public void A_transfer_neither_creates_nor_destroys_tokens_so_the_supply_stays_the_same() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();
        TokenAmount supplyBefore = chain.Ledger.TotalSupply;

        // Act
        chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        Assert.Equal(supplyBefore, chain.Ledger.TotalSupply);
    }

    [Fact]
    public void A_transfer_returns_a_receipt_of_what_it_cost() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();
        TransactionEnvelope envelope = chain.Envelope(chain.Alice).Generate();

        // Act
        Receipt receipt = chain.Ledger.Transfer(envelope, chain.Bob, Amount.Of(500));

        // Assert
        Assert.Equal(TransactionKind.Transfer, receipt.Kind);
        Assert.Equal(chain.Alice, receipt.Sender);
        Assert.Equal(envelope.Nonce, receipt.Nonce);
        Assert.Equal(chain.Ledger.CurrentBlock, receipt.BlockNumber);
        Assert.Equal(35_000UL, receipt.GasUsed);
        Assert.Equal(Amount.Of(Chain.Price), receipt.GasPrice);
        Assert.Equal(Amount.Of(350_000), receipt.Fee);
        Assert.Same(receipt, chain.LastReceipt);
    }

    [Fact]
    public void A_receipt_is_stamped_with_the_block_the_ledger_is_at() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();
        chain.Ledger.AdvanceBlocks(41);

        // Act
        Receipt receipt = chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        Assert.Equal(chain.Ledger.CurrentBlock, receipt.BlockNumber);
    }

    [Fact]
    public void A_transfer_is_logged_and_then_so_is_its_fee() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();
        int   mark  = chain.Mark();

        // Act
        chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        IReadOnlyList<TransferRecord> records = chain.TransfersSince(mark);
        Assert.Equal(2, records.Count);
        Assert.Equal(new TransferRecord(chain.Alice, chain.Bob, Amount.Of(500), chain.Ledger.CurrentBlock), records[0]);
        Assert.Equal(new TransferRecord(chain.Alice, chain.Collector, Amount.Of(350_000), chain.Ledger.CurrentBlock), records[1]);
    }

    [Fact]
    public void Each_accepted_transaction_moves_the_sender_to_its_next_nonce() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act
        chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithNonce(0).Generate(), chain.Bob, Amount.Of(1));
        chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithNonce(1).Generate(), chain.Bob, Amount.Of(1));

        // Assert
        Assert.Equal((UInt128)2, chain.Ledger.NextNonceOf(chain.Alice));
        Assert.Equal((UInt128)0, chain.Ledger.NextNonceOf(chain.Bob));
    }

    [Fact]
    public void The_receipts_are_kept_in_the_order_the_transactions_were_accepted() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();
        int   before = chain.Ledger.Receipts.Count;

        // Act
        Receipt first  = chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1));
        Receipt second = chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1));

        // Assert
        Assert.Equal(before + 2, chain.Ledger.Receipts.Count);
        Assert.Same(first, chain.Ledger.Receipts[before]);
        Assert.Same(second, chain.Ledger.Receipts[before + 1]);
    }

    [Fact]
    public void A_sender_who_never_held_anything_may_transact_when_the_gas_is_free() {
        // Arrange: the minimum price is zero, so no fee needs funds.
        Chain chain = Chain.Start();

        // Act
        chain.Ledger.Approve(chain.Envelope(chain.Alice).WithGasPrice(TokenAmount.Zero).Generate(), chain.Bob, Amount.Of(1));

        // Assert
        Assert.Equal((UInt128)1, chain.Ledger.NextNonceOf(chain.Alice));
    }

    // ---- a new holder costs a surcharge ---------------------------------------------------------------------------

    [Fact]
    public void A_transfer_to_an_address_holding_nothing_costs_the_new_holder_surcharge() {
        // Arrange: Carol holds nothing.
        Chain chain = Chain.Start().FundAlice();

        // Act
        Receipt receipt = chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Carol, Amount.Of(500));

        // Assert: (35_000 + 20_000) gas at 10.
        Assert.Equal(55_000UL, receipt.GasUsed);
        Assert.Equal(Amount.Of(550_000), receipt.Fee);
        Assert.Equal(Chain.Funds - 500 - 550_000, chain.BalanceOf(chain.Alice));
        Assert.Equal(500, chain.BalanceOf(chain.Carol));
    }

    [Fact]
    public void Moving_nothing_to_an_address_holding_nothing_creates_no_holder_and_costs_no_surcharge() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act
        Receipt receipt = chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Carol, TokenAmount.Zero);

        // Assert
        Assert.Equal(35_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Paying_the_fee_collector_for_the_first_time_never_costs_a_surcharge() {
        // Arrange: the collector holds nothing yet.
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act
        Receipt receipt = chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1));

        // Assert
        Assert.Equal(35_000UL, receipt.GasUsed);
        Assert.Equal(350_000, chain.BalanceOf(chain.Collector));
    }

    // ---- the fee is the gas used, not the gas limit ---------------------------------------------------------------

    [Fact]
    public void The_fee_follows_the_gas_used_and_not_the_gas_limit() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act
        Receipt tightLimit = chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(35_000).Generate(), chain.Bob, Amount.Of(1));
        Receipt wideLimit  = chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(100_000).Generate(), chain.Bob, Amount.Of(1));

        // Assert
        Assert.Equal(tightLimit.Fee, wideLimit.Fee);
        Assert.Equal(Amount.Of(350_000), wideLimit.Fee);
    }

    [Fact]
    public void At_a_gas_price_of_zero_there_is_no_fee_and_no_fee_record() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();
        int   mark  = chain.Mark();

        // Act
        Receipt receipt = chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasPrice(TokenAmount.Zero).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        Assert.Equal(TokenAmount.Zero, receipt.Fee);
        Assert.Single(chain.TransfersSince(mark));
        Assert.Equal(Chain.Funds - 500, chain.BalanceOf(chain.Alice));
        Assert.Equal(0, chain.BalanceOf(chain.Collector));
    }

    [Fact]
    public void A_sender_who_collects_the_fees_pays_nothing_for_them() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Collector, Chain.Funds).FundBob();

        // Act
        chain.Ledger.Transfer(chain.Envelope(chain.Collector).Generate(), chain.Bob, Amount.Of(500));

        // Assert: only the 500 left the collector; the fee went out and came back.
        Assert.Equal(Chain.Funds - 500, chain.BalanceOf(chain.Collector));
    }

    [Fact]
    public void A_transfer_to_oneself_costs_the_fee_and_nothing_else() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act
        chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Alice, Amount.Of(500));

        // Assert
        Assert.Equal(Chain.Funds - 350_000, chain.BalanceOf(chain.Alice));
    }

    // ---- refusals -------------------------------------------------------------------------------------------------

    [Fact]
    public void A_transfer_of_more_than_the_balance_is_refused_by_one_base_unit() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(1_000_000).FundBob();

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientBalance,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1_000_001)));
    }

    [Fact]
    public void A_transfer_needs_the_balance_to_cover_the_amount_and_then_the_maximum_fee() {
        // Arrange: 100_000 gas at 10 is a maximum fee of 1_000_000, whatever the transfer ends up using.
        Chain chain = Chain.Start().FundAlice(1_000_500).FundBob();

        // Act
        chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(100_000).Generate(), chain.Bob, Amount.Of(500));

        // Assert: it was accepted, and only the 350_000 actually used was charged.
        Assert.Equal(1_000_500 - 500 - 350_000, chain.BalanceOf(chain.Alice));
    }

    [Fact]
    public void A_transfer_leaving_one_base_unit_less_than_the_maximum_fee_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(1_000_499).FundBob();

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientFundsForFee,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(100_000).Generate(), chain.Bob, Amount.Of(500)));
    }

    [Fact]
    public void A_transfer_to_oneself_still_counts_its_amount_as_leaving_the_balance_when_checking_the_fee() {
        // Arrange: the credit coming back does not count.
        Chain chain = Chain.Start().FundAlice(1_000_499);

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientFundsForFee,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(100_000).Generate(), chain.Alice, Amount.Of(500)));
    }

    [Fact]
    public void A_transfer_to_the_zero_address_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.ZeroAddress,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), Address.Zero, Amount.Of(1)));
    }

    [Fact]
    public void A_transfer_whose_gas_limit_does_not_cover_a_plain_transfer_is_refused() {
        // Arrange: Bob already holds tokens, so a plain transfer costs 35_000.
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act / Assert
        chain.Refuses(RefusalReason.GasLimitTooLow,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(34_999).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void A_transfer_whose_gas_limit_just_covers_a_plain_transfer_is_accepted() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act
        Receipt receipt = chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(35_000).Generate(), chain.Bob, Amount.Of(1));

        // Assert
        Assert.Equal(35_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_transfer_to_a_new_holder_needs_a_gas_limit_that_covers_the_surcharge_too() {
        // Arrange: Carol holds nothing, so the transfer costs 55_000.
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.GasLimitTooLow,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(54_999).Generate(), chain.Carol, Amount.Of(1)));
        Assert.Equal(55_000UL, chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(55_000).Generate(), chain.Carol, Amount.Of(1)).GasUsed);
    }

    [Fact]
    public void A_transfer_below_the_ledgers_minimum_gas_price_is_refused() {
        // Arrange
        Chain chain = Chain.Start(minimumGasPrice: 5).FundAlice().FundBob();

        // Act / Assert
        chain.Refuses(RefusalReason.GasPriceTooLow,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasPrice(Amount.Of(4)).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void A_transfer_at_the_ledgers_minimum_gas_price_is_accepted() {
        // Arrange
        Chain chain = Chain.Start(minimumGasPrice: 5).FundAlice().FundBob();

        // Act
        Receipt receipt = chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasPrice(Amount.Of(5)).Generate(), chain.Bob, Amount.Of(1));

        // Assert
        Assert.Equal(Amount.Of(175_000), receipt.Fee);
    }

    // ---- nonces ---------------------------------------------------------------------------------------------------

    [Fact]
    public void A_nonce_already_used_is_refused_as_too_low() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();
        chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1));

        // Act / Assert
        chain.Refuses(RefusalReason.NonceTooLow,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithNonce(0).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void A_nonce_that_skips_ahead_is_refused_as_too_high() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act / Assert
        chain.Refuses(RefusalReason.NonceTooHigh,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithNonce(1).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void The_largest_nonce_on_an_account_that_has_barely_started_is_still_just_too_high() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act / Assert
        chain.Refuses(RefusalReason.NonceTooHigh,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithNonce(UInt128.MaxValue).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void A_refused_transaction_does_not_use_up_its_nonce() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(1_500_000).FundBob();
        Assert.Throws<LedgerRefusedException>(() => chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1_500_001)));

        // Act: the very same nonce, with an amount that fits.
        Receipt receipt = chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        Assert.Equal(UInt128.Zero, receipt.Nonce);
    }

    // ---- deadline -------------------------------------------------------------------------------------------------

    [Fact]
    public void A_transaction_is_still_accepted_in_its_deadline_block() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act
        Receipt receipt = chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithDeadlineBlock(chain.Ledger.CurrentBlock).Generate(), chain.Bob, Amount.Of(1));

        // Assert
        Assert.Equal(TransactionKind.Transfer, receipt.Kind);
    }

    [Fact]
    public void A_transaction_is_refused_the_block_after_its_deadline() {
        // Arrange
        Chain chain    = Chain.Start().FundAlice().FundBob();
        ulong deadline = chain.Ledger.CurrentBlock;
        chain.Ledger.AdvanceBlocks(1);

        // Act / Assert
        chain.Refuses(RefusalReason.DeadlinePassed,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithDeadlineBlock(deadline).Generate(), chain.Bob, Amount.Of(1)));
    }

    // ---- in which order the checks are made -----------------------------------------------------------------------

    [Fact]
    public void A_wrong_nonce_is_reported_before_a_passed_deadline() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();
        chain.Ledger.AdvanceBlocks(1);

        // Act / Assert
        chain.Refuses(RefusalReason.NonceTooHigh,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithNonce(9).WithDeadlineBlock(chain.Ledger.CurrentBlock - 1).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void A_passed_deadline_is_reported_before_a_gas_price_that_is_too_low() {
        // Arrange
        Chain chain = Chain.Start(minimumGasPrice: 5).FundAlice().FundBob();
        chain.Ledger.AdvanceBlocks(1);

        // Act / Assert
        chain.Refuses(RefusalReason.DeadlinePassed,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasPrice(Amount.Of(1)).WithDeadlineBlock(chain.Ledger.CurrentBlock - 1).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void A_gas_price_that_is_too_low_is_reported_before_a_zero_recipient() {
        // Arrange
        Chain chain = Chain.Start(minimumGasPrice: 5).FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.GasPriceTooLow,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasPrice(Amount.Of(1)).Generate(), Address.Zero, Amount.Of(1)));
    }

    [Fact]
    public void A_zero_recipient_is_reported_before_a_gas_limit_that_is_too_low() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.ZeroAddress,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(21_000).Generate(), Address.Zero, Amount.Of(1)));
    }

    [Fact]
    public void A_gas_limit_that_is_too_low_is_reported_before_an_insufficient_balance() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(100).FundBob();

        // Act / Assert
        chain.Refuses(RefusalReason.GasLimitTooLow,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithGasLimit(21_000).Generate(), chain.Bob, Amount.Of(101)));
    }

    [Fact]
    public void An_insufficient_balance_is_reported_before_funds_that_cannot_cover_the_fee() {
        // Arrange: Alice holds 100, cannot move 101, and could not pay the fee either.
        Chain chain = Chain.Start().FundAlice(100).FundBob();

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientBalance,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(101)));
    }

    // ---- missing arguments ----------------------------------------------------------------------------------------

    [Fact]
    public void A_transfer_with_a_missing_argument_is_refused_as_such() {
        // Arrange
        Chain               chain    = Chain.Start().FundAlice().FundBob();
        TransactionEnvelope envelope = chain.Envelope(chain.Alice).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Transfer(null!, chain.Bob, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Transfer(envelope, null!, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Transfer(envelope, chain.Bob, null!));
    }

}
