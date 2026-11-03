using JustDummies;

using TokenLedger.Domain;
using TokenLedger.Tests.Discovery.Support;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class LedgerMintAndBurnTests {

    private const ulong Supply = 10_000_000_000;

    // A ledger whose cap is 10^12 and whose minter holds 10^10.
    [Fact]
    public void The_minter_creates_tokens_for_a_recipient() {
        // Arrange
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        // Act
        Receipt receipt = ledger.Mint(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(5_000));

        // Assert
        Assert.Equal(new TokenAmount(5_000), ledger.BalanceOf(alice));
        Assert.Equal(new TokenAmount(Supply + 5_000), ledger.TotalSupply);
        Assert.Equal(TransactionKind.Mint, receipt.Kind);
    }

    [Fact]
    public void A_mint_to_a_new_holder_uses_the_mint_gas_and_the_surcharge() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        Receipt receipt = ledger.Mint(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1));

        Assert.Equal(60_000UL, receipt.GasUsed);
        Assert.Equal(new TokenAmount(120_000), receipt.Fee);
    }

    [Fact]
    public void A_mint_to_an_existing_holder_uses_the_mint_gas_alone() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 10);

        Receipt receipt = ledger.Mint(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1));

        Assert.Equal(40_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_mint_of_nothing_to_a_new_holder_pays_no_surcharge() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        Receipt receipt = ledger.Mint(Chain.Next(ledger, ledger.Minter), alice, TokenAmount.Zero);

        Assert.Equal(40_000UL, receipt.GasUsed);
        Assert.Equal(new TokenAmount(Supply), ledger.TotalSupply);
    }

    [Fact]
    public void A_mint_is_logged_as_a_movement_from_the_zero_address() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        int     before = ledger.Transfers.Count;

        ledger.Mint(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(5_000));

        TransferRecord record = ledger.Transfers[before];
        Assert.True(record.IsMint);
        Assert.Equal(alice, record.To);
        Assert.Equal(new TokenAmount(5_000), record.Amount);
        Assert.Equal(ledger.CurrentBlock, record.BlockNumber);
        Assert.Equal(ledger.FeeCollector, ledger.Transfers[before + 1].To);
    }

    [Fact]
    public void A_mint_pays_its_fee_from_the_minter_and_creates_nothing_for_the_collector() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        ledger.Mint(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(5_000));

        Assert.Equal(new TokenAmount(Supply - 120_000), ledger.BalanceOf(ledger.Minter));
        Assert.Equal(new TokenAmount(120_000), ledger.BalanceOf(ledger.FeeCollector));
        Assert.Equal(new System.Numerics.BigInteger(Supply + 5_000), Chain.SumOfBalances(ledger));
    }

    [Fact]
    public void The_minter_may_mint_to_itself() {
        Ledger ledger = Chain.NewLedger(Supply);

        ledger.Mint(Chain.Next(ledger, ledger.Minter), ledger.Minter, new TokenAmount(5_000));

        Assert.Equal(new TokenAmount(Supply + 5_000 - 80_000), ledger.BalanceOf(ledger.Minter));
    }

    [Fact]
    public void Minting_up_to_exactly_the_cap_is_allowed_and_leaves_nothing_mintable() {
        // Arrange
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1);
        TokenAmount remaining = ledger.RemainingMintable;

        // Act
        ledger.Mint(Chain.Next(ledger, ledger.Minter), alice, remaining);

        // Assert
        Assert.Equal(TokenAmount.Zero, ledger.RemainingMintable);
        Assert.Equal(ledger.Token.SupplyCap, ledger.TotalSupply);
    }

    [Fact]
    public void Minting_one_unit_past_the_cap_is_refused() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        TokenAmount tooMuch = ledger.RemainingMintable.Add(new TokenAmount(1));

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Mint(Chain.Next(ledger, ledger.Minter), alice, tooMuch), alice);

        Assert.Equal(RefusalReason.CapExceeded, refusal.Reason);
    }

    [Fact]
    public void Burnt_tokens_do_not_come_back_as_mintable_room_beyond_the_cap_arithmetic() {
        // Arrange: burning lowers the supply, so exactly that much more can then be minted.
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        ledger.Burn(Chain.Next(ledger, ledger.Minter), new TokenAmount(1_000));
        TokenAmount remaining = ledger.RemainingMintable;

        // Act
        ledger.Mint(Chain.Next(ledger, ledger.Minter), alice, remaining);

        // Assert
        Assert.Equal(ledger.Token.SupplyCap, ledger.TotalSupply);
    }

    [Fact]
    public void Nobody_but_the_minter_may_mint() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1_000_000);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Mint(Chain.Next(ledger, alice), bob, new TokenAmount(1)), alice, bob);

        Assert.Equal(RefusalReason.NotMinter, refusal.Reason);
    }

    [Fact]
    public void The_fee_collector_is_not_the_minter() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address bob    = Chain.NewAddress();
        Chain.Fund(ledger, ledger.FeeCollector, 1_000_000);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Mint(Chain.Next(ledger, ledger.FeeCollector), bob, new TokenAmount(1)), bob);

        Assert.Equal(RefusalReason.NotMinter, refusal.Reason);
    }

    [Fact]
    public void Minting_to_the_zero_address_is_refused() {
        Ledger ledger = Chain.NewLedger(Supply);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Mint(Chain.Next(ledger, ledger.Minter), Address.Zero, new TokenAmount(1)));

        Assert.Equal(RefusalReason.ZeroAddress, refusal.Reason);
    }

    [Fact]
    public void A_mint_needs_the_mint_gas_and_the_surcharge_for_a_new_holder() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Mint(Chain.NextWithGasLimit(ledger, ledger.Minter, 59_999), alice, new TokenAmount(1)), alice);
        Receipt                receipt = ledger.Mint(Chain.NextWithGasLimit(ledger, ledger.Minter, 60_000), alice, new TokenAmount(1));

        Assert.Equal(RefusalReason.GasLimitTooLow, refusal.Reason);
        Assert.Equal(60_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_minter_who_holds_nothing_cannot_pay_the_fee_even_for_a_mint_to_itself() {
        // Arrange: no initial supply, so the minter's own balance is empty, and the mint would credit it.
        Ledger ledger = Chain.NewLedger(initialSupply: 0);

        // Act
        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Mint(Chain.Next(ledger, ledger.Minter), ledger.Minter, new TokenAmount(1_000_000)));

        // Assert: credits to the sender do not count towards the fee.
        Assert.Equal(RefusalReason.InsufficientFundsForFee, refusal.Reason);
    }

    [Fact]
    public void A_minter_with_a_free_price_can_bootstrap_a_supply() {
        Ledger  ledger = Chain.NewLedger(initialSupply: 0, minimumGasPrice: 0);
        Address alice  = Chain.NewAddress();

        ledger.Mint(Chain.Next(ledger, ledger.Minter, gasPrice: 0), alice, new TokenAmount(1_000));

        Assert.Equal(new TokenAmount(1_000), ledger.TotalSupply);
        Assert.Equal(new TokenAmount(1_000), ledger.BalanceOf(alice));
    }

    [Fact]
    public void Burning_destroys_the_amount_of_the_sender_balance_and_of_the_supply() {
        Ledger ledger = Chain.NewLedger(Supply);

        Receipt receipt = ledger.Burn(Chain.Next(ledger, ledger.Minter), new TokenAmount(1_000_000));

        Assert.Equal(new TokenAmount(Supply - 1_000_000 - 64_000), ledger.BalanceOf(ledger.Minter));
        Assert.Equal(new TokenAmount(Supply - 1_000_000), ledger.TotalSupply);
        Assert.Equal(TransactionKind.Burn, receipt.Kind);
        Assert.Equal(32_000UL, receipt.GasUsed);
        Assert.Equal(new TokenAmount(64_000), ledger.BalanceOf(ledger.FeeCollector));
    }

    [Fact]
    public void A_burn_is_logged_as_a_movement_to_the_zero_address() {
        Ledger ledger = Chain.NewLedger(Supply);
        int    before = ledger.Transfers.Count;

        ledger.Burn(Chain.Next(ledger, ledger.Minter), new TokenAmount(1_000_000));

        TransferRecord record = ledger.Transfers[before];
        Assert.True(record.IsBurn);
        Assert.Equal(ledger.Minter, record.From);
        Assert.Equal(new TokenAmount(1_000_000), record.Amount);
        Assert.Equal(ledger.FeeCollector, ledger.Transfers[before + 1].To);
    }

    [Fact]
    public void A_burn_by_anyone_burns_their_own_tokens() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 2_000_000);

        ledger.Burn(Chain.Next(ledger, alice), new TokenAmount(500_000));

        Assert.Equal(new TokenAmount(2_000_000 - 500_000 - 64_000), ledger.BalanceOf(alice));
        Assert.Equal(new TokenAmount(Supply - 500_000), ledger.TotalSupply);
    }

    [Fact]
    public void A_burn_of_nothing_is_allowed_and_still_pays_the_fee() {
        Ledger ledger = Chain.NewLedger(Supply);

        Receipt receipt = ledger.Burn(Chain.Next(ledger, ledger.Minter), TokenAmount.Zero);

        Assert.Equal(new TokenAmount(64_000), receipt.Fee);
        Assert.Equal(new TokenAmount(Supply), ledger.TotalSupply);
    }

    [Fact]
    public void A_burn_above_the_balance_is_refused() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 2_000_000);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Burn(Chain.Next(ledger, alice), new TokenAmount(2_000_001)), alice);

        Assert.Equal(RefusalReason.InsufficientBalance, refusal.Reason);
    }

    [Fact]
    public void Burning_the_whole_balance_leaves_nothing_for_the_fee() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 2_000_000);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Burn(Chain.Next(ledger, alice), new TokenAmount(2_000_000)), alice);

        Assert.Equal(RefusalReason.InsufficientFundsForFee, refusal.Reason);
    }

    [Fact]
    public void Burning_all_but_the_maximum_fee_is_allowed() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 2_000_000);

        ledger.Burn(Chain.NextWithGasLimit(ledger, alice, 100_000), new TokenAmount(2_000_000 - 200_000));

        Assert.Equal(new TokenAmount(200_000 - 64_000), ledger.BalanceOf(alice));
    }

    [Fact]
    public void Burning_needs_only_the_burn_gas() {
        Ledger ledger = Chain.NewLedger(Supply);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Burn(Chain.NextWithGasLimit(ledger, ledger.Minter, 31_999), new TokenAmount(1)));
        Receipt                receipt = ledger.Burn(Chain.NextWithGasLimit(ledger, ledger.Minter, 32_000), new TokenAmount(1));

        Assert.Equal(RefusalReason.GasLimitTooLow, refusal.Reason);
        Assert.Equal(32_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Minting_and_burning_refuse_null_arguments() {
        Ledger              ledger   = Chain.NewLedger(Supply);
        Address             alice    = Chain.NewAddress();
        TransactionEnvelope envelope = Chain.Next(ledger, ledger.Minter);

        Assert.Throws<ArgumentNullException>(() => ledger.Mint(null!, alice, TokenAmount.Zero));
        Assert.Throws<ArgumentNullException>(() => ledger.Mint(envelope, null!, TokenAmount.Zero));
        Assert.Throws<ArgumentNullException>(() => ledger.Mint(envelope, alice, null!));
        Assert.Throws<ArgumentNullException>(() => ledger.Burn(null!, TokenAmount.Zero));
        Assert.Throws<ArgumentNullException>(() => ledger.Burn(envelope, null!));
    }

}
