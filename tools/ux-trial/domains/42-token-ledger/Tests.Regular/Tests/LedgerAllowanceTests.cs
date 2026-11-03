namespace TokenLedger.Domain.Tests.Regular;

public sealed class LedgerAllowanceTests : LedgerTestBase {

    private static readonly TokenAmount Unlimited = TokenAmount.Max;

    [Fact]
    public void Approving_sets_the_allowance_of_the_spender_over_the_sender() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(250));

        // Assert
        Assert.Equal(Units(250), ledger.AllowanceOf(anyMinter, anySpender));
        Assert.True(ledger.AllowanceOf(anySpender, anyMinter).IsZero);
    }

    [Fact]
    public void Approving_again_replaces_the_previous_allowance() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(250));

        // Act
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(40));

        // Assert
        Assert.Equal(Units(40), ledger.AllowanceOf(anyMinter, anySpender));
    }

    [Fact]
    public void Approving_zero_withdraws_the_allowance() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(250));

        // Act
        ledger.Approve(Tx(ledger, anyMinter), anySpender, TokenAmount.Zero);

        // Assert
        Assert.True(ledger.AllowanceOf(anyMinter, anySpender).IsZero);
    }

    [Fact]
    public void An_allowance_may_exceed_the_balance_of_its_owner() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(1_000_000));

        // Assert
        Assert.Equal(Units(1_000_000), ledger.AllowanceOf(anyMinter, anySpender));
    }

    [Fact]
    public void An_allowance_costs_the_approval_gas_whatever_the_kind() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = StandardLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        Receipt approve  = ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(10));
        Receipt increase = ledger.IncreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(10));
        Receipt decrease = ledger.DecreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(10));

        // Assert: the spender is a new account but no balance is credited, so there is no surcharge.
        Assert.Equal(new[] { 30_000UL, 30_000UL, 30_000UL }, new[] { approve.GasUsed, increase.GasUsed, decrease.GasUsed });
        Assert.Equal(new[] { TransactionKind.Approve, TransactionKind.IncreaseAllowance, TransactionKind.DecreaseAllowance },
                     new[] { approve.Kind, increase.Kind, decrease.Kind });
    }

    [Fact]
    public void An_allowance_operation_moves_no_token_and_logs_no_movement() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(10));
        ledger.IncreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(10));
        ledger.DecreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(5));

        // Assert
        Assert.Single(ledger.Transfers);
        Assert.Equal(Units(1_000), ledger.BalanceOf(anyMinter));
    }

    [Fact]
    public void Approving_the_zero_address_is_refused() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.ZeroAddress,
                                       () => ledger.Approve(Tx(ledger, anyMinter), Address.Zero, Units(1)),
                                       anyMinter);
    }

    [Fact]
    public void A_sender_who_cannot_pay_the_maximum_fee_cannot_approve() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(10));

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientFundsForFee,
                                       () => ledger.Approve(Tx(ledger, anyAlice, gasPrice: 1, gasLimit: 30_000), anyMinter, Units(1)),
                                       anyAlice, anyMinter);
    }

    [Fact]
    public void Increasing_adds_to_the_current_allowance() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(100));

        // Act
        ledger.IncreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(50));

        // Assert
        Assert.Equal(Units(150), ledger.AllowanceOf(anyMinter, anySpender));
    }

    [Fact]
    public void Increasing_an_unset_allowance_starts_from_zero() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.IncreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(50));

        // Assert
        Assert.Equal(Units(50), ledger.AllowanceOf(anyMinter, anySpender));
    }

    [Fact]
    public void Increasing_up_to_exactly_the_largest_amount_is_accepted_and_makes_it_unlimited() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyMinter), anySpender, new TokenAmount(TokenAmount.MaxBaseUnits - 5));

        // Act
        ledger.IncreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(5));

        // Assert
        Assert.Equal(Unlimited, ledger.AllowanceOf(anyMinter, anySpender));
    }

    [Fact]
    public void Increasing_past_the_largest_amount_is_refused_and_changes_nothing() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyMinter), anySpender, new TokenAmount(TokenAmount.MaxBaseUnits - 5));

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.AllowanceOverflow,
                                       () => ledger.IncreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(6)),
                                       anyMinter, anySpender);
        Assert.Equal(new TokenAmount(TokenAmount.MaxBaseUnits - 5), ledger.AllowanceOf(anyMinter, anySpender));
    }

    [Fact]
    public void Decreasing_subtracts_from_the_current_allowance() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(100));

        // Act
        ledger.DecreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(30));

        // Assert
        Assert.Equal(Units(70), ledger.AllowanceOf(anyMinter, anySpender));
    }

    [Fact]
    public void Decreasing_by_exactly_the_allowance_clears_it() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(100));

        // Act
        ledger.DecreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(100));

        // Assert
        Assert.True(ledger.AllowanceOf(anyMinter, anySpender).IsZero);
    }

    [Fact]
    public void Decreasing_below_zero_is_refused_and_changes_nothing() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyMinter), anySpender, Units(100));

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.AllowanceExceeded,
                                       () => ledger.DecreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(101)),
                                       anyMinter, anySpender);
        Assert.Equal(Units(100), ledger.AllowanceOf(anyMinter, anySpender));
    }

    [Fact]
    public void Decreasing_an_unset_allowance_is_refused() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.AllowanceExceeded, () => ledger.DecreaseAllowance(Tx(ledger, anyMinter), anySpender, Units(1)));
    }

    [Fact]
    public void Decreasing_an_unset_allowance_by_nothing_is_accepted() {
        // Arrange
        Address anyMinter  = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        Receipt receipt = ledger.DecreaseAllowance(Tx(ledger, anyMinter), anySpender, TokenAmount.Zero);

        // Assert
        Assert.Equal(TransactionKind.DecreaseAllowance, receipt.Kind);
    }

    [Fact]
    public void Adjusting_the_zero_address_is_refused() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.ZeroAddress, () => ledger.IncreaseAllowance(Tx(ledger, anyMinter), Address.Zero, Units(1)));
        AssertRefused(RefusalReason.ZeroAddress, () => ledger.DecreaseAllowance(Tx(ledger, anyMinter), Address.Zero, Units(1)));
    }

    [Fact]
    public void A_spender_moves_tokens_on_the_allowance_of_the_owner() {
        // Arrange
        Address anyOwner     = Any.Address().Generate();
        Address anySpender   = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Units(300));

        // Act
        ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, anyRecipient, Units(120));

        // Assert
        Assert.Equal(Units(880), ledger.BalanceOf(anyOwner));
        Assert.Equal(Units(120), ledger.BalanceOf(anyRecipient));
        Assert.True(ledger.BalanceOf(anySpender).IsZero);
        Assert.Equal(Units(180), ledger.AllowanceOf(anyOwner, anySpender));
    }

    [Fact]
    public void A_delegated_transfer_is_logged_from_the_owner_to_the_recipient() {
        // Arrange
        Address anyOwner     = Any.Address().Generate();
        Address anySpender   = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Units(300));

        // Act
        Receipt receipt = ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, anyRecipient, Units(120));

        // Assert
        TransferRecord movement = ledger.Transfers.Last();
        Assert.Equal(anyOwner, movement.From);
        Assert.Equal(anyRecipient, movement.To);
        Assert.Equal(Units(120), movement.Amount);
        Assert.Equal(TransactionKind.TransferFrom, receipt.Kind);
        Assert.Equal(anySpender, receipt.Sender);
        Assert.Equal(UInt128.One, ledger.NextNonceOf(anySpender) );
        Assert.Equal(UInt128.One, ledger.NextNonceOf(anyOwner));
    }

    [Fact]
    public void A_delegated_transfer_to_a_new_holder_costs_the_delegated_gas_plus_the_surcharge() {
        // Arrange
        Address anyOwner     = Any.Address().Generate();
        Address anySpender   = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Units(300));

        // Act
        Receipt toNewHolder      = ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, anyRecipient, Units(10));
        Receipt toExistingHolder = ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, anyRecipient, Units(10));

        // Assert
        Assert.Equal(65_000UL, toNewHolder.GasUsed);
        Assert.Equal(45_000UL, toExistingHolder.GasUsed);
    }

    [Fact]
    public void An_unlimited_allowance_is_not_decreased_by_spending() {
        // Arrange
        Address anyOwner     = Any.Address().Generate();
        Address anySpender   = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Unlimited);

        // Act
        ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, anyRecipient, Units(400));

        // Assert
        Assert.Equal(Unlimited, ledger.AllowanceOf(anyOwner, anySpender));
        Assert.Equal(Units(600), ledger.BalanceOf(anyOwner));
    }

    [Fact]
    public void Spending_the_whole_allowance_clears_it() {
        // Arrange
        Address anyOwner   = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Units(300));

        // Act
        ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, Any.Address().Generate(), Units(300));

        // Assert
        Assert.True(ledger.AllowanceOf(anyOwner, anySpender).IsZero);
    }

    [Fact]
    public void Spending_more_than_the_allowance_is_refused() {
        // Arrange
        Address anyOwner     = Any.Address().Generate();
        Address anySpender   = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Units(300));

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.AllowanceExceeded,
                                       () => ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, anyRecipient, Units(301)),
                                       anyOwner, anySpender, anyRecipient);
    }

    [Fact]
    public void Spending_without_any_allowance_is_refused() {
        // Arrange
        Address anyOwner   = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.AllowanceExceeded, () => ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, Any.Address().Generate(), Units(1)));
    }

    [Fact]
    public void An_allowance_is_one_way() {
        // Arrange
        Address anyOwner   = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Units(300));
        ledger.Transfer(Tx(ledger, anyOwner), anySpender, Units(500));

        // Act & Assert: the owner has no allowance over the spender.
        AssertRefused(RefusalReason.AllowanceExceeded, () => ledger.TransferFrom(Tx(ledger, anyOwner), anySpender, anyOwner, Units(1)));
    }

    [Fact]
    public void A_sufficient_allowance_does_not_cover_a_balance_the_owner_lacks() {
        // Arrange
        Address anyOwner   = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Unlimited);

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientBalance,
                                       () => ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, Any.Address().Generate(), Units(1_001)),
                                       anyOwner, anySpender);
        Assert.Equal(Unlimited, ledger.AllowanceOf(anyOwner, anySpender));
    }

    [Fact]
    public void The_allowance_is_checked_before_the_balance() {
        // Arrange
        Address anyOwner   = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Units(10));

        // Act & Assert: 5 000 exceeds both the allowance and the balance.
        AssertRefused(RefusalReason.AllowanceExceeded, () => ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, Any.Address().Generate(), Units(5_000)));
    }

    [Fact]
    public void A_delegated_transfer_from_the_zero_address_or_to_it_is_refused() {
        // Arrange
        Address anyOwner   = Any.Address().Generate();
        Address anySpender = Any.Address().Generate();
        Ledger  ledger     = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Units(10));

        // Act & Assert
        AssertRefused(RefusalReason.ZeroAddress, () => ledger.TransferFrom(Tx(ledger, anySpender), Address.Zero, anyOwner, Units(1)));
        AssertRefused(RefusalReason.ZeroAddress, () => ledger.TransferFrom(Tx(ledger, anySpender), anyOwner, Address.Zero, Units(1)));
    }

    [Fact]
    public void The_spender_pays_the_fee_not_the_owner() {
        // Arrange
        Address anyOwner     = Any.Address().Generate();
        Address anySpender   = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyOwner, anyCollector, 10_000_000).Generate();
        ledger.Approve(Tx(ledger, anyOwner), anySpender, Units(300));
        ledger.Transfer(Tx(ledger, anyOwner), anySpender, Units(1_000_000));

        // Act: delegated transfer to the (already funded) spender itself keeps the arithmetic simple.
        ledger.TransferFrom(Tx(ledger, anySpender, gasPrice: 2, gasLimit: 100_000), anyOwner, anyOwner, Units(100));

        // Assert: 45 000 gas x 2 paid by the spender.
        Assert.Equal(Units(1_000_000 - 90_000), ledger.BalanceOf(anySpender));
        Assert.Equal(Units(90_000), ledger.BalanceOf(anyCollector));
    }

    [Fact]
    public void A_spender_delegating_its_own_tokens_has_them_counted_against_the_fee_funds() {
        // Arrange: the spender holds 100 000 and spends it from itself on an allowance it granted itself.
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(100_000));
        ledger.Approve(Tx(ledger, anyAlice), anyAlice, Units(60_000));

        // Act & Assert: 60 000 leaves 40 000 against a maximum fee of 50 000.
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientFundsForFee,
                                       () => ledger.TransferFrom(Tx(ledger, anyAlice, gasPrice: 1, gasLimit: 50_000), anyAlice, anyMinter, Units(60_000)),
                                       anyAlice, anyMinter);
    }

    [Fact]
    public void Credits_to_the_sender_do_not_count_towards_the_fee_funds() {
        // Arrange: Bob has nothing, Alice lets him move 500 of hers to himself, but he cannot pay the maximum fee.
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Address anyBob    = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 10_000_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(1_000_000));
        ledger.Approve(Tx(ledger, anyAlice), anyBob, Units(500));

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientFundsForFee,
                                       () => ledger.TransferFrom(Tx(ledger, anyBob, gasPrice: 1, gasLimit: 100_000), anyAlice, anyBob, Units(500)),
                                       anyAlice, anyBob);
    }

    [Fact]
    public void Transfer_from_refuses_null_arguments() {
        // Arrange
        Address anyOwner = Any.Address().Generate();
        Ledger  ledger   = FeelessLedger(anyOwner, Any.Address().Generate(), 1_000).Generate();
        TransactionEnvelope anyEnvelope = Tx(ledger, anyOwner);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ledger.TransferFrom(null!, anyOwner, anyOwner, Units(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.TransferFrom(anyEnvelope, null!, anyOwner, Units(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.TransferFrom(anyEnvelope, anyOwner, null!, Units(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.TransferFrom(anyEnvelope, anyOwner, anyOwner, null!));
        Assert.Throws<ArgumentNullException>(() => ledger.Approve(anyEnvelope, null!, Units(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.IncreaseAllowance(anyEnvelope, anyOwner, null!));
        Assert.Throws<ArgumentNullException>(() => ledger.DecreaseAllowance(null!, anyOwner, Units(1)));
    }

}
