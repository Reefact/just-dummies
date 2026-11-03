using JustDummies;

using TokenLedger.Domain;
using TokenLedger.Tests.Discovery.Support;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class LedgerConstructionTests {

    private static TokenDefinition WholeTokenDefinition(ulong cap) {
        return Any.TokenDefinition().WithDecimals(0).WithSupplyCap(new TokenAmount(cap)).Generate();
    }

    [Fact]
    public void A_new_ledger_credits_the_initial_supply_to_the_minter() {
        // Arrange
        TokenDefinition token = WholeTokenDefinition(1_000_000);

        // Act
        Ledger ledger = Any.Ledger().WithToken(token).WithInitialSupply(new TokenAmount(250_000)).Generate();

        // Assert
        Assert.Equal(new TokenAmount(250_000), ledger.BalanceOf(ledger.Minter));
        Assert.Equal(new TokenAmount(250_000), ledger.TotalSupply);
        Assert.Equal(new TokenAmount(750_000), ledger.RemainingMintable);
        Assert.Same(token, ledger.Token);
    }

    [Fact]
    public void The_initial_supply_is_logged_as_a_mint_at_the_genesis_block() {
        Ledger ledger = Any.Ledger().WithInitialSupply(new TokenAmount(5)).WithGenesisBlock(42).WithToken(WholeTokenDefinition(10)).Generate();

        TransferRecord record = Assert.Single(ledger.Transfers);
        Assert.True(record.IsMint);
        Assert.Equal(ledger.Minter, record.To);
        Assert.Equal(new TokenAmount(5), record.Amount);
        Assert.Equal(42UL, record.BlockNumber);
    }

    [Fact]
    public void The_ledger_starts_at_its_genesis_block_with_no_receipts() {
        ulong anyGenesis = Any.UInt64().Generate();

        Ledger ledger = Any.Ledger().WithGenesisBlock(anyGenesis).Generate();

        Assert.Equal(anyGenesis, ledger.CurrentBlock);
        Assert.Empty(ledger.Receipts);
    }

    [Fact]
    public void An_initial_supply_of_nothing_logs_nothing_and_leaves_the_whole_cap_mintable() {
        TokenDefinition token = WholeTokenDefinition(1_000);

        Ledger ledger = Any.Ledger().WithToken(token).WithInitialSupply(TokenAmount.Zero).Generate();

        Assert.Empty(ledger.Transfers);
        Assert.Equal(TokenAmount.Zero, ledger.TotalSupply);
        Assert.Equal(token.SupplyCap, ledger.RemainingMintable);
    }

    [Fact]
    public void The_minter_and_the_fee_collector_have_accounts_from_the_start() {
        Ledger ledger = Any.Ledger().Generate();

        Assert.Equal(2, ledger.Accounts.Count);
        Assert.Contains(ledger.Accounts, account => account.Address.Equals(ledger.Minter));
        Assert.Contains(ledger.Accounts, account => account.Address.Equals(ledger.FeeCollector));
    }

    [Fact]
    public void The_minter_and_the_fee_collector_may_be_the_same_address() {
        Address anyAddress = Any.Address().NonZero().Generate();

        Ledger ledger = Any.Ledger().WithMinter(anyAddress).WithFeeCollector(anyAddress).Generate();

        Account account = Assert.Single(ledger.Accounts);
        Assert.Equal(anyAddress, account.Address);
    }

    [Fact]
    public void The_fee_collector_is_not_a_holder_before_it_is_paid_anything() {
        Ledger ledger = Any.Ledger().WithInitialSupply(new TokenAmount(1)).WithToken(WholeTokenDefinition(10)).Generate();

        Assert.Equal(TokenAmount.Zero, ledger.BalanceOf(ledger.FeeCollector));
        Assert.Contains(ledger.Accounts, account => account.Address.Equals(ledger.Minter) && account.IsHolder);
        Assert.Contains(ledger.Accounts, account => account.Address.Equals(ledger.FeeCollector) && !account.IsHolder);
    }

    [Fact]
    public void A_ledger_keeps_its_configuration() {
        // Arrange
        GasSchedule schedule = new GasSchedule(30_000, 31_000, 32_000, 33_000, 34_000, 1_000);

        // Act
        Ledger ledger = Any.Ledger().WithGasSchedule(schedule).WithMinimumGasPrice(new TokenAmount(3)).Generate();

        // Assert
        Assert.Same(schedule, ledger.GasSchedule);
        Assert.Equal(new TokenAmount(3), ledger.MinimumGasPrice);
    }

    [Fact]
    public void The_zero_address_cannot_be_the_minter() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Ledger().WithMinter(Address.Zero).Generate());

        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("minter", refusal.ParamName);
    }

    [Fact]
    public void The_zero_address_cannot_collect_fees() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Ledger().WithFeeCollector(Address.Zero).Generate());

        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("feeCollector", refusal.ParamName);
    }

    [Fact]
    public void An_initial_supply_above_the_cap_is_refused_but_the_cap_itself_is_accepted() {
        TokenDefinition token = WholeTokenDefinition(1_000);

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Ledger().WithToken(token).WithInitialSupply(new TokenAmount(1_001)).Generate());
        Ledger                      full    = Any.Ledger().WithToken(token).WithInitialSupply(new TokenAmount(1_000)).Generate();

        Assert.Equal("initialSupply", refusal.ParamName);
        Assert.Equal(TokenAmount.Zero, full.RemainingMintable);
    }

    [Fact]
    public void A_minimum_gas_price_that_makes_one_transfer_dearer_than_the_cap_is_refused() {
        // Arrange: a cap of 1 000 000 and a transfer of 25 000 gas, so the price tops out at 40.
        TokenDefinition token    = WholeTokenDefinition(1_000_000);
        GasSchedule     schedule = new GasSchedule(25_000, 30_000, 45_000, 40_000, 32_000, 20_000);

        // Act
        Ledger                  atTheLimit = Any.Ledger().WithToken(token).WithGasSchedule(schedule).WithMinimumGasPrice(new TokenAmount(40)).WithInitialSupply(TokenAmount.Zero).Generate();
        ArgumentException       refusal    = Assert.Throws<ArgumentException>(() => Any.Ledger().WithToken(token).WithGasSchedule(schedule).WithMinimumGasPrice(new TokenAmount(41)).WithInitialSupply(TokenAmount.Zero).Generate());

        // Assert
        Assert.Equal(new TokenAmount(40), atTheLimit.MinimumGasPrice);
        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("minimumGasPrice", refusal.ParamName);
    }

    [Fact]
    public void A_null_argument_is_refused() {
        TokenDefinition token    = Any.TokenDefinition().Generate();
        Address         minter   = Any.Address().NonZero().Generate();
        Address         collector = Any.Address().NonZero().Generate();
        GasSchedule     schedule = GasSchedule.Standard;

        Assert.Throws<ArgumentNullException>(() => new Ledger(null!, minter, collector, TokenAmount.Zero, schedule, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, null!, collector, TokenAmount.Zero, schedule, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, minter, null!, TokenAmount.Zero, schedule, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, minter, collector, null!, schedule, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, minter, collector, TokenAmount.Zero, null!, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new Ledger(token, minter, collector, TokenAmount.Zero, schedule, null!, 0));
    }

    [Fact]
    public void A_drawn_ledger_holds_the_invariant_that_balances_add_up_to_the_supply() {
        for (int i = 0; i < 50; i++) {
            Ledger anyLedger = Any.Ledger().Generate();

            Assert.Equal(anyLedger.TotalSupply.BaseUnits, Chain.SumOfBalances(anyLedger));
            Assert.True(anyLedger.TotalSupply.CompareTo(anyLedger.Token.SupplyCap) <= 0);
        }
    }

}
