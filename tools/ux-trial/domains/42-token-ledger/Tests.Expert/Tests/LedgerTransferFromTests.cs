using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class LedgerTransferFromTests {

    // Alice owns, Bob spends on her allowance, Carol receives. All three hold tokens unless a test says otherwise.
    private static Chain WithAnAllowance(long allowance) {
        Chain chain = Chain.Start().FundAlice().FundBob().FundCarol();

        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(allowance));

        return chain;
    }

    // ---- what a delegated transfer does --------------------------------------------------------------------------

    [Fact]
    public void A_delegated_transfer_moves_the_amount_from_the_owner_to_the_recipient() {
        // Arrange
        Chain chain        = WithAnAllowance(1_000);
        long  aliceBefore  = (long)chain.BalanceOf(chain.Alice);
        long  carolBefore  = (long)chain.BalanceOf(chain.Carol);

        // Act
        chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(400));

        // Assert
        Assert.Equal(aliceBefore - 400, chain.BalanceOf(chain.Alice));
        Assert.Equal(carolBefore + 400, chain.BalanceOf(chain.Carol));
    }

    [Fact]
    public void The_spender_pays_the_fee_and_the_owner_pays_nothing() {
        // Arrange
        Chain chain       = WithAnAllowance(1_000);
        long  aliceBefore = (long)chain.BalanceOf(chain.Alice);

        // Act
        chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(400));

        // Assert: 45_000 gas at 10 for Bob; Alice lost only the 400.
        Assert.Equal(Chain.Funds - 450_000, chain.BalanceOf(chain.Bob));
        Assert.Equal(aliceBefore - 400, chain.BalanceOf(chain.Alice));
    }

    [Fact]
    public void A_delegated_transfer_spends_the_allowance_it_used() {
        // Arrange
        Chain chain = WithAnAllowance(1_000);

        // Act
        chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(400));

        // Assert
        Assert.Equal(600, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void An_allowance_can_be_spent_to_the_last_base_unit() {
        // Arrange
        Chain chain = WithAnAllowance(1_000);

        // Act
        chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(1_000));

        // Assert
        Assert.Equal(0, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void An_unlimited_allowance_is_not_decreased_by_spending() {
        // Arrange
        Chain chain = WithAnAllowance(1);
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, TokenAmount.Max);

        // Act
        chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(400));

        // Assert
        Assert.Equal(TokenAmount.Max, chain.Ledger.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void A_delegated_transfer_returns_a_receipt_in_the_spenders_name() {
        // Arrange
        Chain               chain    = WithAnAllowance(1_000);
        TransactionEnvelope envelope = chain.Envelope(chain.Bob).Generate();

        // Act
        Receipt receipt = chain.Ledger.TransferFrom(envelope, chain.Alice, chain.Carol, Amount.Of(400));

        // Assert
        Assert.Equal(TransactionKind.TransferFrom, receipt.Kind);
        Assert.Equal(chain.Bob, receipt.Sender);
        Assert.Equal(envelope.Nonce, receipt.Nonce);
        Assert.Equal(45_000UL, receipt.GasUsed);
        Assert.Equal(Amount.Of(450_000), receipt.Fee);
    }

    [Fact]
    public void A_delegated_transfer_is_logged_as_the_movement_from_the_owner_and_then_the_spenders_fee() {
        // Arrange
        Chain chain = WithAnAllowance(1_000);
        int   mark  = chain.Mark();

        // Act
        chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(400));

        // Assert
        IReadOnlyList<TransferRecord> records = chain.TransfersSince(mark);
        Assert.Equal(2, records.Count);
        Assert.Equal(new TransferRecord(chain.Alice, chain.Carol, Amount.Of(400), chain.Ledger.CurrentBlock), records[0]);
        Assert.Equal(new TransferRecord(chain.Bob, chain.Collector, Amount.Of(450_000), chain.Ledger.CurrentBlock), records[1]);
    }

    [Fact]
    public void A_delegated_transfer_to_a_new_holder_costs_the_new_holder_surcharge() {
        // Arrange: Carol holds nothing here.
        Chain chain = Chain.Start().FundAlice().FundBob();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1_000));

        // Act
        Receipt receipt = chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(400));

        // Assert: (45_000 + 20_000) gas.
        Assert.Equal(65_000UL, receipt.GasUsed);
    }

    // ---- refusals -------------------------------------------------------------------------------------------------

    [Fact]
    public void Spending_one_base_unit_more_than_the_allowance_is_refused() {
        // Arrange
        Chain chain = WithAnAllowance(1_000);

        // Act / Assert
        chain.Refuses(RefusalReason.AllowanceExceeded,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(1_001)));
    }

    [Fact]
    public void Spending_without_any_allowance_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob().FundCarol();

        // Act / Assert
        chain.Refuses(RefusalReason.AllowanceExceeded,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(1)));
    }

    [Fact]
    public void The_allowance_runs_from_the_owner_to_the_spender_and_not_the_other_way() {
        // Arrange: Alice allowed Bob; Bob never allowed Alice.
        Chain chain = WithAnAllowance(1_000);

        // Act / Assert
        chain.Refuses(RefusalReason.AllowanceExceeded,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Alice).Generate(), chain.Bob, chain.Carol, Amount.Of(1)));
    }

    [Fact]
    public void An_allowance_larger_than_the_owners_balance_still_cannot_move_more_than_the_balance() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(1_000).FundBob().FundCarol();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).WithGasPrice(TokenAmount.Zero).Generate(), chain.Bob, Amount.Of(5_000));

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientBalance,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(1_001)));
    }

    [Fact]
    public void An_amount_over_both_the_allowance_and_the_balance_is_reported_as_over_the_allowance() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(1_000).FundBob().FundCarol();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).WithGasPrice(TokenAmount.Zero).Generate(), chain.Bob, Amount.Of(100));

        // Act / Assert
        chain.Refuses(RefusalReason.AllowanceExceeded,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(1_001)));
    }

    [Fact]
    public void Moving_from_the_zero_address_is_refused() {
        // Arrange
        Chain chain = WithAnAllowance(1_000);

        // Act / Assert
        chain.Refuses(RefusalReason.ZeroAddress,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), Address.Zero, chain.Carol, Amount.Of(1)));
    }

    [Fact]
    public void Moving_to_the_zero_address_is_refused() {
        // Arrange
        Chain chain = WithAnAllowance(1_000);

        // Act / Assert
        chain.Refuses(RefusalReason.ZeroAddress,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, Address.Zero, Amount.Of(1)));
    }

    [Fact]
    public void A_delegated_transfer_whose_gas_limit_does_not_cover_it_is_refused() {
        // Arrange: Carol holds tokens, so 45_000 is enough.
        Chain chain = WithAnAllowance(1_000);

        // Act / Assert
        chain.Refuses(RefusalReason.GasLimitTooLow,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).WithGasLimit(44_999).Generate(), chain.Alice, chain.Carol, Amount.Of(1)));
    }

    // ---- the spender must be able to pay the maximum fee ---------------------------------------------------------

    [Fact]
    public void A_spender_one_base_unit_short_of_the_maximum_fee_is_refused_whatever_the_owner_holds() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob(999_999).FundCarol();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1_000));

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientFundsForFee,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).WithGasLimit(100_000).Generate(), chain.Alice, chain.Carol, Amount.Of(400)));
    }

    [Fact]
    public void A_spender_receiving_the_tokens_himself_still_has_to_find_the_maximum_fee_in_what_he_already_holds() {
        // Arrange: the 400 he receives do not count towards the fee.
        Chain chain = Chain.Start().FundAlice().FundBob(999_999);
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1_000));

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientFundsForFee,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).WithGasLimit(100_000).Generate(), chain.Alice, chain.Bob, Amount.Of(400)));
    }

    [Fact]
    public void A_spender_moving_his_own_tokens_must_cover_both_the_amount_and_the_maximum_fee() {
        // Arrange: Bob allows himself, then spends 400 of his own: he needs 400 + 1_000_000.
        Chain chain = Chain.Start().FundAlice().FundBob(1_000_399).FundCarol();
        chain.Ledger.Approve(chain.Envelope(chain.Bob).WithGasPrice(TokenAmount.Zero).Generate(), chain.Bob, Amount.Of(1_000));

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientFundsForFee,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).WithGasLimit(100_000).Generate(), chain.Bob, chain.Carol, Amount.Of(400)));
    }

    [Fact]
    public void A_spender_moving_his_own_tokens_with_exactly_enough_for_the_amount_and_the_maximum_fee_is_accepted() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob(1_000_400).FundCarol();
        chain.Ledger.Approve(chain.Envelope(chain.Bob).WithGasPrice(TokenAmount.Zero).Generate(), chain.Bob, Amount.Of(1_000));

        // Act
        Receipt receipt = chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).WithGasLimit(100_000).Generate(), chain.Bob, chain.Carol, Amount.Of(400));

        // Assert
        Assert.Equal(TransactionKind.TransferFrom, receipt.Kind);
        Assert.Equal(1_000_400 - 400 - 450_000, chain.BalanceOf(chain.Bob));
    }

    // ---- missing arguments ----------------------------------------------------------------------------------------

    [Fact]
    public void A_delegated_transfer_with_a_missing_argument_is_refused_as_such() {
        // Arrange
        Chain               chain    = WithAnAllowance(1_000);
        TransactionEnvelope envelope = chain.Envelope(chain.Bob).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.TransferFrom(null!, chain.Alice, chain.Carol, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.TransferFrom(envelope, null!, chain.Carol, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.TransferFrom(envelope, chain.Alice, null!, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.TransferFrom(envelope, chain.Alice, chain.Carol, null!));
    }

}
