using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class LedgerAllowanceTests {

    // ---- Approve --------------------------------------------------------------------------------------------------

    [Fact]
    public void Approving_sets_what_the_spender_may_spend() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        Assert.Equal(500, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void Approving_again_replaces_the_previous_allowance_instead_of_adding_to_it() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Act
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(200));

        // Assert
        Assert.Equal(200, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void Approving_nothing_withdraws_the_allowance() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Act
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, TokenAmount.Zero);

        // Assert
        Assert.Equal(0, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void An_allowance_does_not_have_to_be_backed_by_a_balance() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(1_000_000);

        // Act
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(5_000_000));

        // Assert
        Assert.Equal(5_000_000, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void The_largest_amount_is_a_valid_allowance_and_means_unlimited() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, TokenAmount.Max);

        // Assert
        Assert.Equal(TokenAmount.Max, chain.Ledger.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void An_approval_costs_the_approve_gas_and_never_the_new_holder_surcharge() {
        // Arrange: Carol holds nothing.
        Chain chain = Chain.Start().FundAlice();

        // Act
        Receipt receipt = chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Carol, Amount.Of(500));

        // Assert: 30_000 gas at 10.
        Assert.Equal(TransactionKind.Approve, receipt.Kind);
        Assert.Equal(30_000UL, receipt.GasUsed);
        Assert.Equal(Chain.Funds - 300_000, chain.BalanceOf(chain.Alice));
    }

    [Fact]
    public void An_approval_moves_no_token_and_logs_only_its_fee() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        int   mark  = chain.Mark();

        // Act
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        TransferRecord record = Assert.Single(chain.TransfersSince(mark));
        Assert.Equal(new TransferRecord(chain.Alice, chain.Collector, Amount.Of(300_000), chain.Ledger.CurrentBlock), record);
    }

    [Fact]
    public void An_approval_needs_only_the_maximum_fee_in_the_balance() {
        // Arrange: 100_000 gas at 10.
        Chain chain = Chain.Start().FundAlice(1_000_000);

        // Act
        Receipt receipt = chain.Ledger.Approve(chain.Envelope(chain.Alice).WithGasLimit(100_000).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        Assert.Equal(TransactionKind.Approve, receipt.Kind);
    }

    [Fact]
    public void An_approval_one_base_unit_short_of_the_maximum_fee_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(999_999);

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientFundsForFee,
                      () => chain.Ledger.Approve(chain.Envelope(chain.Alice).WithGasLimit(100_000).Generate(), chain.Bob, Amount.Of(500)));
    }

    [Fact]
    public void Approving_the_zero_address_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.ZeroAddress,
                      () => chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), Address.Zero, Amount.Of(500)));
    }

    [Fact]
    public void An_approval_whose_gas_limit_does_not_cover_the_approve_gas_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.GasLimitTooLow,
                      () => chain.Ledger.Approve(chain.Envelope(chain.Alice).WithGasLimit(29_999).Generate(), chain.Bob, Amount.Of(500)));
    }

    // ---- IncreaseAllowance ----------------------------------------------------------------------------------------

    [Fact]
    public void Increasing_adds_to_what_the_spender_may_already_spend() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Act
        Receipt receipt = chain.Ledger.IncreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(300));

        // Assert
        Assert.Equal(800, chain.AllowanceOf(chain.Alice, chain.Bob));
        Assert.Equal(TransactionKind.IncreaseAllowance, receipt.Kind);
        Assert.Equal(30_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Increasing_from_nothing_sets_the_allowance() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act
        chain.Ledger.IncreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(300));

        // Assert
        Assert.Equal(300, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void Increasing_up_to_exactly_the_largest_amount_is_accepted() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(TokenAmount.MaxBaseUnits - 5));

        // Act
        chain.Ledger.IncreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(5));

        // Assert
        Assert.Equal(TokenAmount.Max, chain.Ledger.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void Increasing_past_the_largest_amount_is_refused_as_an_overflow() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(TokenAmount.MaxBaseUnits - 5));

        // Act / Assert
        chain.Refuses(RefusalReason.AllowanceOverflow,
                      () => chain.Ledger.IncreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(6)));
    }

    [Fact]
    public void Increasing_an_unlimited_allowance_by_anything_is_refused_as_an_overflow() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, TokenAmount.Max);

        // Act / Assert
        chain.Refuses(RefusalReason.AllowanceOverflow,
                      () => chain.Ledger.IncreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void Increasing_by_nothing_is_accepted_and_still_costs_the_approve_gas() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act
        Receipt receipt = chain.Ledger.IncreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, TokenAmount.Zero);

        // Assert
        Assert.Equal(30_000UL, receipt.GasUsed);
        Assert.Equal(0, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void Increasing_the_allowance_of_the_zero_address_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.ZeroAddress,
                      () => chain.Ledger.IncreaseAllowance(chain.Envelope(chain.Alice).Generate(), Address.Zero, Amount.Of(1)));
    }

    // ---- DecreaseAllowance ----------------------------------------------------------------------------------------

    [Fact]
    public void Decreasing_takes_from_what_the_spender_may_spend() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Act
        Receipt receipt = chain.Ledger.DecreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(200));

        // Assert
        Assert.Equal(300, chain.AllowanceOf(chain.Alice, chain.Bob));
        Assert.Equal(TransactionKind.DecreaseAllowance, receipt.Kind);
        Assert.Equal(30_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Decreasing_by_exactly_the_allowance_leaves_none() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Act
        chain.Ledger.DecreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Assert
        Assert.Equal(0, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void Decreasing_by_one_base_unit_more_than_the_allowance_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(500));

        // Act / Assert
        chain.Refuses(RefusalReason.AllowanceExceeded,
                      () => chain.Ledger.DecreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(501)));
    }

    [Fact]
    public void Decreasing_an_allowance_nobody_set_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.AllowanceExceeded,
                      () => chain.Ledger.DecreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void Decreasing_an_unlimited_allowance_makes_it_limited_like_any_other() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, TokenAmount.Max);

        // Act
        chain.Ledger.DecreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1));

        // Assert
        Assert.Equal(TokenAmount.MaxBaseUnits - 1, chain.AllowanceOf(chain.Alice, chain.Bob));
    }

    [Fact]
    public void Decreasing_the_allowance_of_the_zero_address_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.ZeroAddress,
                      () => chain.Ledger.DecreaseAllowance(chain.Envelope(chain.Alice).Generate(), Address.Zero, TokenAmount.Zero));
    }

    // ---- the allowance operations all refuse the same way for the envelope ----------------------------------------

    [Fact]
    public void An_allowance_operation_with_a_missing_argument_is_refused_as_such() {
        // Arrange
        Chain               chain    = Chain.Start().FundAlice();
        TransactionEnvelope envelope = chain.Envelope(chain.Alice).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Approve(null!, chain.Bob, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Approve(envelope, null!, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Approve(envelope, chain.Bob, null!));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.IncreaseAllowance(null!, chain.Bob, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.IncreaseAllowance(envelope, null!, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.IncreaseAllowance(envelope, chain.Bob, null!));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.DecreaseAllowance(null!, chain.Bob, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.DecreaseAllowance(envelope, null!, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.DecreaseAllowance(envelope, chain.Bob, null!));
    }

}
