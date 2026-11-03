namespace TokenLedger.Domain.Tests.Regular;

public sealed class LedgerMintAndBurnTests : LedgerTestBase {

    private static AnyLedger CappedLedger(Address minter, Address collector, long cap, long initialSupply) {
        return Any.Ledger()
                  .WithToken(TokenCappedAt(cap))
                  .WithMinter(minter)
                  .WithFeeCollector(collector)
                  .WithInitialSupply(Units(initialSupply))
                  .WithMinimumGasPrice(TokenAmount.Zero);
    }

    [Fact]
    public void The_minter_creates_tokens_for_a_recipient() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = CappedLedger(anyMinter, Any.Address().Generate(), cap: 10_000, initialSupply: 1_000).Generate();

        // Act
        Receipt receipt = ledger.Mint(Tx(ledger, anyMinter), anyRecipient, Units(400));

        // Assert
        Assert.Equal(Units(400), ledger.BalanceOf(anyRecipient));
        Assert.Equal(Units(1_000), ledger.BalanceOf(anyMinter));
        Assert.Equal(Units(1_400), ledger.TotalSupply);
        Assert.Equal(Units(8_600), ledger.RemainingMintable);
        Assert.Equal(TransactionKind.Mint, receipt.Kind);
    }

    [Fact]
    public void A_mint_is_logged_as_a_movement_from_the_zero_address() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = CappedLedger(anyMinter, Any.Address().Generate(), cap: 10_000, initialSupply: 0).Generate();

        // Act
        ledger.Mint(Tx(ledger, anyMinter), anyRecipient, Units(400));

        // Assert
        TransferRecord record = Assert.Single(ledger.Transfers);
        Assert.True(record.IsMint);
        Assert.Equal(anyRecipient, record.To);
        Assert.Equal(Units(400), record.Amount);
    }

    [Fact]
    public void The_minter_can_mint_to_itself() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = CappedLedger(anyMinter, Any.Address().Generate(), cap: 10_000, initialSupply: 0).Generate();

        // Act
        ledger.Mint(Tx(ledger, anyMinter), anyMinter, Units(250));

        // Assert
        Assert.Equal(Units(250), ledger.BalanceOf(anyMinter));
    }

    [Fact]
    public void Minting_exactly_the_remaining_amount_is_accepted() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = CappedLedger(anyMinter, Any.Address().Generate(), cap: 10_000, initialSupply: 9_000).Generate();

        // Act
        ledger.Mint(Tx(ledger, anyMinter), Any.Address().Generate(), Units(1_000));

        // Assert
        Assert.Equal(Units(10_000), ledger.TotalSupply);
        Assert.True(ledger.RemainingMintable.IsZero);
    }

    [Fact]
    public void Minting_past_the_cap_is_refused_and_changes_nothing() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = CappedLedger(anyMinter, Any.Address().Generate(), cap: 10_000, initialSupply: 9_000).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.CapExceeded,
                                       () => ledger.Mint(Tx(ledger, anyMinter), anyRecipient, Units(1_001)),
                                       anyMinter, anyRecipient);
    }

    [Fact]
    public void Minting_nothing_at_the_cap_is_accepted() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = CappedLedger(anyMinter, Any.Address().Generate(), cap: 10_000, initialSupply: 10_000).Generate();

        // Act
        Receipt receipt = ledger.Mint(Tx(ledger, anyMinter), Any.Address().Generate(), TokenAmount.Zero);

        // Assert
        Assert.Equal(TransactionKind.Mint, receipt.Kind);
    }

    [Fact]
    public void Nobody_but_the_minter_may_mint() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = CappedLedger(anyMinter, Any.Address().Generate(), cap: 10_000, initialSupply: 1_000).Generate();
        ledger.Transfer(Tx(ledger, anyMinter), anyAlice, Units(500));

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.NotMinter,
                                       () => ledger.Mint(Tx(ledger, anyAlice), anyAlice, Units(1)),
                                       anyMinter, anyAlice);
    }

    [Fact]
    public void Even_the_fee_collector_may_not_mint() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();
        Ledger  ledger       = CappedLedger(anyMinter, anyCollector, cap: 10_000, initialSupply: 1_000).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.NotMinter, () => ledger.Mint(Tx(ledger, anyCollector), anyCollector, Units(1)));
    }

    [Fact]
    public void Minting_to_the_zero_address_is_refused_before_the_minter_is_checked() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Address anyAlice  = Any.Address().Generate();
        Ledger  ledger    = CappedLedger(anyMinter, Any.Address().Generate(), cap: 10_000, initialSupply: 1_000).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.ZeroAddress, () => ledger.Mint(Tx(ledger, anyMinter), Address.Zero, Units(1)));
        AssertRefused(RefusalReason.ZeroAddress, () => ledger.Mint(Tx(ledger, anyAlice), Address.Zero, Units(1)));
    }

    [Fact]
    public void A_stranger_minting_is_refused_as_not_the_minter_before_the_gas_is_looked_at() {
        // Arrange
        Address anyMinter   = Any.Address().Generate();
        Address anyStranger = Any.Address().Generate();
        Ledger  ledger      = StandardLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefused(RefusalReason.NotMinter, () => ledger.Mint(Tx(ledger, anyStranger, gasLimit: 21_000), anyStranger, Units(1)));
    }

    [Fact]
    public void Minting_to_a_new_holder_pays_the_mint_gas_plus_the_surcharge() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        Receipt toNewHolder      = ledger.Mint(Tx(ledger, anyMinter), anyRecipient, Units(5));
        Receipt toExistingHolder = ledger.Mint(Tx(ledger, anyMinter), anyRecipient, Units(5));

        // Assert
        Assert.Equal(60_000UL, toNewHolder.GasUsed);
        Assert.Equal(40_000UL, toExistingHolder.GasUsed);
    }

    [Fact]
    public void Minting_pays_its_fee_from_the_minter_to_the_collector() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();
        Address anyRecipient = Any.Address().Generate();
        Ledger  ledger       = StandardLedger(anyMinter, anyCollector, 1_000_000).Generate();

        // Act
        ledger.Mint(Tx(ledger, anyMinter, gasPrice: 1, gasLimit: 100_000), anyRecipient, Units(5));

        // Assert: 40 000 + 20 000 gas at 1.
        Assert.Equal(Units(1_000_000 - 60_000), ledger.BalanceOf(anyMinter));
        Assert.Equal(Units(60_000), ledger.BalanceOf(anyCollector));
        Assert.Equal(Units(5), ledger.BalanceOf(anyRecipient));
    }

    [Fact]
    public void Tokens_minted_to_the_minter_do_not_count_towards_the_fee_funds() {
        // Arrange: the minter holds nothing and cannot pay the maximum fee, whatever it mints to itself.
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 0).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientFundsForFee,
                                       () => ledger.Mint(Tx(ledger, anyMinter, gasPrice: 1, gasLimit: 100_000), anyMinter, Units(1_000_000)),
                                       anyMinter);
    }

    [Fact]
    public void Burning_destroys_tokens_of_the_sender() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = CappedLedger(anyMinter, Any.Address().Generate(), cap: 10_000, initialSupply: 1_000).Generate();

        // Act
        Receipt receipt = ledger.Burn(Tx(ledger, anyMinter), Units(300));

        // Assert
        Assert.Equal(Units(700), ledger.BalanceOf(anyMinter));
        Assert.Equal(Units(700), ledger.TotalSupply);
        Assert.Equal(Units(9_300), ledger.RemainingMintable);
        Assert.Equal(TransactionKind.Burn, receipt.Kind);
    }

    [Fact]
    public void A_burn_is_logged_as_a_movement_to_the_zero_address() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Burn(Tx(ledger, anyMinter), Units(300));

        // Assert
        TransferRecord record = ledger.Transfers.Last();
        Assert.True(record.IsBurn);
        Assert.Equal(anyMinter, record.From);
        Assert.Equal(Units(300), record.Amount);
    }

    [Fact]
    public void Burning_everything_is_accepted() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        ledger.Burn(Tx(ledger, anyMinter), Units(1_000));

        // Assert
        Assert.True(ledger.TotalSupply.IsZero);
        Assert.True(ledger.BalanceOf(anyMinter).IsZero);
    }

    [Fact]
    public void Burning_more_than_the_balance_is_refused_and_changes_nothing() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act & Assert
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientBalance,
                                       () => ledger.Burn(Tx(ledger, anyMinter), Units(1_001)),
                                       anyMinter);
    }

    [Fact]
    public void Burning_costs_the_burn_gas_without_any_surcharge() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();

        // Act
        Receipt receipt = ledger.Burn(Tx(ledger, anyMinter), Units(10));

        // Assert
        Assert.Equal(32_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_burner_must_keep_enough_for_the_maximum_fee_once_the_burn_is_done() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = StandardLedger(anyMinter, Any.Address().Generate(), 100_000).Generate();

        // Act & Assert: burning 80 000 leaves 20 000 against a maximum fee of 40 000.
        AssertRefusedAndNothingChanged(ledger, RefusalReason.InsufficientFundsForFee,
                                       () => ledger.Burn(Tx(ledger, anyMinter, gasPrice: 1, gasLimit: 40_000), Units(80_000)),
                                       anyMinter);
    }

    [Fact]
    public void Burning_frees_room_under_the_cap_for_a_later_mint() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = CappedLedger(anyMinter, Any.Address().Generate(), cap: 1_000, initialSupply: 1_000).Generate();
        AssertRefused(RefusalReason.CapExceeded, () => ledger.Mint(Tx(ledger, anyMinter), anyMinter, Units(1)));

        // Act
        ledger.Burn(Tx(ledger, anyMinter), Units(100));
        ledger.Mint(Tx(ledger, anyMinter), anyMinter, Units(100));

        // Assert
        Assert.Equal(Units(1_000), ledger.TotalSupply);
    }

    [Fact]
    public void Mint_and_burn_refuse_null_arguments() {
        // Arrange
        Address anyMinter = Any.Address().Generate();
        Ledger  ledger    = FeelessLedger(anyMinter, Any.Address().Generate(), 1_000).Generate();
        TransactionEnvelope anyEnvelope = Tx(ledger, anyMinter);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ledger.Mint(null!, anyMinter, Units(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.Mint(anyEnvelope, null!, Units(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.Mint(anyEnvelope, anyMinter, null!));
        Assert.Throws<ArgumentNullException>(() => ledger.Burn(null!, Units(1)));
        Assert.Throws<ArgumentNullException>(() => ledger.Burn(anyEnvelope, null!));
    }

}
