using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class LedgerConstructionTests {

    // ---- the initial supply ---------------------------------------------------------------------------------------

    [Fact]
    public void The_initial_supply_is_credited_to_the_minter_and_is_the_whole_supply() {
        // Arrange
        Address anyMinter = Any.Address().Generate();

        // Act
        Ledger ledger = Any.Ledger().WithMinter(anyMinter).WithInitialSupply(Amount.Of(1_000)).Generate();

        // Assert
        Assert.Equal(Amount.Of(1_000), ledger.BalanceOf(anyMinter));
        Assert.Equal(Amount.Of(1_000), ledger.TotalSupply);
    }

    [Fact]
    public void The_initial_supply_is_logged_as_a_mint_at_the_genesis_block() {
        // Arrange
        Address anyMinter = Any.Address().Generate();

        // Act
        Ledger ledger = Any.Ledger().WithMinter(anyMinter).WithInitialSupply(Amount.Of(1_000)).WithGenesisBlock(77).Generate();

        // Assert
        TransferRecord record = Assert.Single(ledger.Transfers);
        Assert.True(record.IsMint);
        Assert.Equal(anyMinter, record.To);
        Assert.Equal(Amount.Of(1_000), record.Amount);
        Assert.Equal(77UL, record.BlockNumber);
    }

    [Fact]
    public void The_ledger_starts_at_the_genesis_block_without_any_receipt() {
        // Act
        Ledger ledger = Any.Ledger().WithGenesisBlock(77).Generate();

        // Assert
        Assert.Equal(77UL, ledger.CurrentBlock);
        Assert.Empty(ledger.Receipts);
    }

    [Fact]
    public void Without_an_initial_supply_nothing_exists_yet_and_nothing_is_logged() {
        // Act
        Ledger ledger = Any.Ledger().WithInitialSupply(TokenAmount.Zero).Generate();

        // Assert
        Assert.Equal(TokenAmount.Zero, ledger.TotalSupply);
        Assert.Empty(ledger.Transfers);
    }

    [Fact]
    public void The_minter_and_the_collector_have_an_account_from_the_start_even_with_nothing_in_it() {
        // Arrange
        Address anyMinter    = Any.Address().Generate();
        Address anyCollector = Any.Address().DifferentFrom(anyMinter).Generate();

        // Act
        Ledger ledger = Any.Ledger().WithMinter(anyMinter).WithFeeCollector(anyCollector).WithInitialSupply(TokenAmount.Zero).Generate();

        // Assert
        Assert.Equal(2, ledger.Accounts.Count);
        Assert.Contains(ledger.Accounts, account => account.Address.Equals(anyMinter));
        Assert.Contains(ledger.Accounts, account => account.Address.Equals(anyCollector));
    }

    [Fact]
    public void The_minter_and_the_collector_may_be_the_same_address() {
        // Arrange
        Address anyAddress = Any.Address().Generate();

        // Act
        Ledger ledger = Any.Ledger().WithMinter(anyAddress).WithFeeCollector(anyAddress).WithInitialSupply(Amount.Of(1_000)).Generate();

        // Assert
        Account account = Assert.Single(ledger.Accounts);
        Assert.Equal(Amount.Of(1_000), account.Balance);
    }

    [Fact]
    public void The_ledger_keeps_the_token_the_roles_the_schedule_and_the_minimum_price_it_was_given() {
        // Arrange
        TokenDefinition anyToken        = Any.TokenDefinition().Generate();
        Address         anyMinter       = Any.Address().Generate();
        Address         anyCollector    = Any.Address().Generate();
        GasSchedule     anySchedule     = Any.GasSchedule().Generate();
        TokenAmount     anyMinimumPrice = Amount.Of(0);

        // Act
        Ledger ledger = new Ledger(anyToken, anyMinter, anyCollector, TokenAmount.Zero, anySchedule, anyMinimumPrice, 5);

        // Assert
        Assert.Same(anyToken, ledger.Token);
        Assert.Equal(anyMinter, ledger.Minter);
        Assert.Equal(anyCollector, ledger.FeeCollector);
        Assert.Same(anySchedule, ledger.GasSchedule);
        Assert.Equal(anyMinimumPrice, ledger.MinimumGasPrice);
    }

    // ---- what is mintable -----------------------------------------------------------------------------------------

    [Fact]
    public void What_remains_to_mint_is_the_cap_less_the_supply() {
        // Arrange
        Address anyMinter = Any.Address().Generate();

        // Act
        Ledger ledger = Any.Ledger()
                           .WithToken(t => t.WithSupplyCap(Amount.Of(Chain.Cap)).WithDecimals(d => d.Between(0, 12)))
                           .WithMinter(anyMinter)
                           .WithInitialSupply(Amount.Of(400))
                           .Generate();

        // Assert
        Assert.Equal(Amount.Of(Chain.Cap - 400), ledger.RemainingMintable);
    }

    [Fact]
    public void An_initial_supply_may_be_exactly_the_cap_and_leaves_nothing_to_mint() {
        // Act
        Ledger ledger = Any.Ledger()
                           .WithToken(t => t.WithSupplyCap(Amount.Of(Chain.Cap)).WithDecimals(d => d.Between(0, 12)))
                           .WithInitialSupply(Amount.Of(Chain.Cap))
                           .Generate();

        // Assert
        Assert.Equal(TokenAmount.Zero, ledger.RemainingMintable);
    }

    [Fact]
    public void An_initial_supply_one_base_unit_above_the_cap_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Ledger()
                     .WithToken(t => t.WithSupplyCap(Amount.Of(Chain.Cap)).WithDecimals(d => d.Between(0, 12)))
                     .WithInitialSupply(Amount.Of(Chain.Cap + 1))
                     .Generate());
    }

    // ---- the roles ------------------------------------------------------------------------------------------------

    [Fact]
    public void The_zero_address_cannot_be_the_minter() {
        Assert.Throws<ArgumentException>(() => Any.Ledger().WithMinter(Address.Zero).Generate());
    }

    [Fact]
    public void The_zero_address_cannot_collect_fees_since_that_would_burn_them() {
        Assert.Throws<ArgumentException>(() => Any.Ledger().WithFeeCollector(Address.Zero).Generate());
    }

    // ---- the minimum gas price must leave a transfer payable -------------------------------------------------------

    [Fact]
    public void A_minimum_price_at_which_one_transfer_costs_exactly_the_cap_is_accepted() {
        // Arrange: a cap of 21_000_000 and a transfer of 21_000 gas: 1_000 per gas is the last price that fits.
        AnyLedger ledger = Any.Ledger()
                              .WithToken(t => t.WithDecimals(0).WithSupplyCap(Amount.Of(21_000_000)))
                              .WithGasSchedule(g => g.WithTransfer(21_000));

        // Act / Assert
        Assert.Null(Record.Exception(() => ledger.WithMinimumGasPrice(Amount.Of(1_000)).Generate()));
    }

    [Fact]
    public void A_minimum_price_one_base_unit_above_what_one_transfer_can_cost_is_refused() {
        // Arrange
        AnyLedger ledger = Any.Ledger()
                              .WithToken(t => t.WithDecimals(0).WithSupplyCap(Amount.Of(21_000_000)))
                              .WithGasSchedule(g => g.WithTransfer(21_000));

        // Act / Assert
        Assert.Throws<ArgumentException>(() => ledger.WithMinimumGasPrice(Amount.Of(1_001)).Generate());
    }

    // ---- missing arguments ----------------------------------------------------------------------------------------

    [Fact]
    public void Every_missing_argument_is_refused_as_a_missing_argument() {
        // Arrange
        Ledger          anyLedger = Any.Ledger().Generate();
        TokenDefinition token     = anyLedger.Token;
        Address         minter    = anyLedger.Minter;
        Address         collector = anyLedger.FeeCollector;
        GasSchedule     schedule  = anyLedger.GasSchedule;
        TokenAmount     price     = anyLedger.MinimumGasPrice;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Ledger(null!, minter, collector, TokenAmount.Zero, schedule, price, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, null!, collector, TokenAmount.Zero, schedule, price, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, minter, null!, TokenAmount.Zero, schedule, price, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, minter, collector, null!, schedule, price, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, minter, collector, TokenAmount.Zero, null!, price, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, minter, collector, TokenAmount.Zero, schedule, null!, 0));
    }

}
