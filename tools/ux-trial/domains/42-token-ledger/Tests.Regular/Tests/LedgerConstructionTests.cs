namespace TokenLedger.Domain.Tests.Regular;

public sealed class LedgerConstructionTests : LedgerTestBase {

    [Fact]
    public void The_initial_supply_is_credited_to_the_minter_and_logged_as_a_mint_at_the_genesis_block() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();

        // Act
        Ledger ledger = FeelessLedger(anyMinter, anyCollector, 5_000).WithGenesisBlock(77).Generate();

        // Assert
        Assert.Equal(Units(5_000), ledger.BalanceOf(anyMinter));
        Assert.Equal(Units(5_000), ledger.TotalSupply);
        Assert.Equal(77UL, ledger.CurrentBlock);
        TransferRecord genesis = Assert.Single(ledger.Transfers);
        Assert.True(genesis.IsMint);
        Assert.Equal(anyMinter, genesis.To);
        Assert.Equal(Units(5_000), genesis.Amount);
        Assert.Equal(77UL, genesis.BlockNumber);
        Assert.Empty(ledger.Receipts);
    }

    [Fact]
    public void An_empty_initial_supply_logs_nothing() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();

        // Act
        Ledger ledger = FeelessLedger(anyMinter, anyCollector, 0).Generate();

        // Assert
        Assert.True(ledger.TotalSupply.IsZero);
        Assert.Empty(ledger.Transfers);
        Assert.False(ledger.Accounts.Single(a => a.Address.Equals(anyMinter)).IsHolder);
    }

    [Fact]
    public void The_minter_and_the_fee_collector_have_accounts_from_the_start() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();

        // Act
        Ledger ledger = FeelessLedger(anyMinter, anyCollector, 10).Generate();

        // Assert
        Assert.Equal(2, ledger.Accounts.Count);
        Assert.Contains(ledger.Accounts, a => a.Address.Equals(anyMinter));
        Assert.Contains(ledger.Accounts, a => a.Address.Equals(anyCollector));
    }

    [Fact]
    public void The_minter_and_the_fee_collector_may_be_the_same_address() {
        // Arrange
        Address anyAddress = Any.Address().Generate();

        // Act
        Ledger ledger = FeelessLedger(anyAddress, anyAddress, 10).Generate();

        // Assert
        Assert.Single(ledger.Accounts);
        Assert.Equal(ledger.Minter, ledger.FeeCollector);
    }

    [Fact]
    public void The_ledger_exposes_what_it_was_built_with() {
        // Arrange
        Address         anyMinter    = Any.Address().Generate();
        Address         anyCollector = Any.Address().Generate();
        TokenDefinition anyToken     = Any.TokenDefinition().Generate();
        GasSchedule     anySchedule  = Any.GasSchedule().Generate();

        // Act
        Ledger ledger = Any.Ledger()
                           .WithToken(anyToken)
                           .WithMinter(anyMinter)
                           .WithFeeCollector(anyCollector)
                           .WithGasSchedule(anySchedule)
                           .WithMinimumGasPrice(Units(0))
                           .Generate();

        // Assert
        Assert.Same(anyToken, ledger.Token);
        Assert.Equal(anyMinter, ledger.Minter);
        Assert.Equal(anyCollector, ledger.FeeCollector);
        Assert.Same(anySchedule, ledger.GasSchedule);
        Assert.Equal(TokenAmount.Zero, ledger.MinimumGasPrice);
    }

    [Fact]
    public void The_remaining_mintable_amount_is_the_cap_minus_the_supply() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().Generate();

        // Act
        Ledger ledger = Any.Ledger()
                           .WithToken(TokenCappedAt(1_000))
                           .WithMinter(anyMinter)
                           .WithFeeCollector(anyCollector)
                           .WithInitialSupply(Units(400))
                           .WithMinimumGasPrice(TokenAmount.Zero)
                           .Generate();

        // Assert
        Assert.Equal(Units(600), ledger.RemainingMintable);
    }

    [Fact]
    public void An_initial_supply_equal_to_the_cap_is_accepted() {
        // Act
        Ledger ledger = Any.Ledger().WithToken(TokenCappedAt(1_000)).WithInitialSupply(Units(1_000)).WithMinimumGasPrice(TokenAmount.Zero).Generate();

        // Assert
        Assert.True(ledger.RemainingMintable.IsZero);
    }

    [Fact]
    public void An_initial_supply_above_the_cap_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Ledger().WithToken(TokenCappedAt(1_000)).WithInitialSupply(Units(1_001)).WithMinimumGasPrice(TokenAmount.Zero).Generate());
    }

    [Fact]
    public void The_minter_cannot_be_the_zero_address() {
        Assert.Throws<ArgumentException>(() => Any.Ledger().WithMinter(Address.Zero).Generate());
    }

    [Fact]
    public void The_fee_collector_cannot_be_the_zero_address() {
        Assert.Throws<ArgumentException>(() => Any.Ledger().WithFeeCollector(Address.Zero).Generate());
    }

    [Fact]
    public void A_minimum_gas_price_that_makes_one_transfer_cost_more_than_the_cap_is_refused() {
        // Arrange: a transfer costs 35 000 gas; 29 x 35 000 = 1 015 000 exceeds a cap of 1 000 000.
        AnyLedger ledger = Any.Ledger()
                              .WithToken(TokenCappedAt(1_000_000))
                              .WithInitialSupply(TokenAmount.Zero)
                              .WithGasSchedule(GasSchedule.Standard);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ledger.WithMinimumGasPrice(Units(29)).Generate());
    }

    [Fact]
    public void A_minimum_gas_price_that_makes_one_transfer_cost_exactly_the_cap_is_accepted() {
        // Arrange: 35 000 gas x 20 = 700 000, within a cap of 700 000.
        Ledger ledger = Any.Ledger()
                           .WithToken(TokenCappedAt(700_000))
                           .WithInitialSupply(TokenAmount.Zero)
                           .WithGasSchedule(GasSchedule.Standard)
                           .WithMinimumGasPrice(Units(20))
                           .Generate();

        // Assert
        Assert.Equal(Units(20), ledger.MinimumGasPrice);
    }

    [Fact]
    public void The_constructor_refuses_null_arguments() {
        // Arrange
        TokenDefinition anyToken     = Any.TokenDefinition().Generate();
        Address         anyMinter    = Any.Address().Generate();
        Address         anyCollector = Any.Address().Generate();
        GasSchedule     anySchedule  = Any.GasSchedule().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Ledger(null!, anyMinter, anyCollector, TokenAmount.Zero, anySchedule, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(anyToken, null!, anyCollector, TokenAmount.Zero, anySchedule, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(anyToken, anyMinter, null!, TokenAmount.Zero, anySchedule, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(anyToken, anyMinter, anyCollector, null!, anySchedule, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(anyToken, anyMinter, anyCollector, TokenAmount.Zero, null!, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(anyToken, anyMinter, anyCollector, TokenAmount.Zero, anySchedule, null!, 0));
    }

}
