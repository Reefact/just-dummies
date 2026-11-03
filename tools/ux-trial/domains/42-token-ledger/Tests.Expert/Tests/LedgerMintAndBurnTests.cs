using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class LedgerMintAndBurnTests {

    // ---- Mint -----------------------------------------------------------------------------------------------------

    [Fact]
    public void The_minter_creates_tokens_for_an_address_and_the_supply_grows_by_as_much() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds).FundAlice(1_000);

        // Act
        chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Alice, Amount.Of(400));

        // Assert
        Assert.Equal(1_400, chain.BalanceOf(chain.Alice));
        Assert.Equal(Chain.Funds + 1_400, (long)chain.Ledger.TotalSupply.BaseUnits);
    }

    [Fact]
    public void A_mint_is_logged_as_coming_from_the_zero_address() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds).FundAlice(1_000);
        int mark = chain.Mark();

        // Act
        chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Alice, Amount.Of(400));

        // Assert: the mint, then the minter's fee (40_000 gas at 10).
        IReadOnlyList<TransferRecord> records = chain.TransfersSince(mark);
        Assert.Equal(2, records.Count);
        Assert.Equal(new TransferRecord(Address.Zero, chain.Alice, Amount.Of(400), chain.Ledger.CurrentBlock), records[0]);
        Assert.True(records[0].IsMint);
        Assert.Equal(new TransferRecord(chain.Minter, chain.Collector, Amount.Of(400_000), chain.Ledger.CurrentBlock), records[1]);
    }

    [Fact]
    public void The_minter_pays_the_fee_of_a_mint_and_the_recipient_pays_nothing() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds).FundAlice(1_000);

        // Act
        Receipt receipt = chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Alice, Amount.Of(400));

        // Assert
        Assert.Equal(TransactionKind.Mint, receipt.Kind);
        Assert.Equal(chain.Minter, receipt.Sender);
        Assert.Equal(40_000UL, receipt.GasUsed);
        Assert.Equal(Chain.Funds - 400_000, chain.BalanceOf(chain.Minter));
        Assert.Equal(1_400, chain.BalanceOf(chain.Alice));
    }

    [Fact]
    public void A_mint_to_a_new_holder_costs_the_new_holder_surcharge() {
        // Arrange: Carol holds nothing.
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds);

        // Act
        Receipt receipt = chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Carol, Amount.Of(400));

        // Assert: (40_000 + 20_000) gas.
        Assert.Equal(60_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_mint_of_nothing_to_a_new_holder_creates_no_holder_and_costs_no_surcharge() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds);

        // Act
        Receipt receipt = chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Carol, TokenAmount.Zero);

        // Assert
        Assert.Equal(40_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Anyone_but_the_minter_is_refused_a_mint() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.NotMinter,
                      () => chain.Ledger.Mint(chain.Envelope(chain.Alice).Generate(), chain.Alice, Amount.Of(1)));
    }

    [Fact]
    public void Minting_to_the_zero_address_is_refused() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds);

        // Act / Assert
        chain.Refuses(RefusalReason.ZeroAddress,
                      () => chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), Address.Zero, Amount.Of(1)));
    }

    [Fact]
    public void Minting_to_the_zero_address_is_reported_before_the_sender_is_found_not_to_be_the_minter() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.ZeroAddress,
                      () => chain.Ledger.Mint(chain.Envelope(chain.Alice).Generate(), Address.Zero, Amount.Of(1)));
    }

    [Fact]
    public void A_sender_who_is_not_the_minter_is_reported_before_a_gas_limit_that_is_too_low() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.NotMinter,
                      () => chain.Ledger.Mint(chain.Envelope(chain.Alice).WithGasLimit(21_000).Generate(), chain.Bob, Amount.Of(1)));
    }

    [Fact]
    public void A_mint_whose_gas_limit_does_not_cover_it_is_refused() {
        // Arrange: Carol is a new holder, so the mint costs 60_000.
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds);

        // Act / Assert
        chain.Refuses(RefusalReason.GasLimitTooLow,
                      () => chain.Ledger.Mint(chain.Envelope(chain.Minter).WithGasLimit(59_999).Generate(), chain.Carol, Amount.Of(1)));
    }

    [Fact]
    public void A_gas_limit_that_is_too_low_is_reported_before_a_mint_past_the_cap() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds);

        // Act / Assert
        chain.Refuses(RefusalReason.GasLimitTooLow,
                      () => chain.Ledger.Mint(chain.Envelope(chain.Minter).WithGasLimit(21_000).Generate(), chain.Alice, Amount.Of(Chain.Cap + 1)));
    }

    // ---- the cap --------------------------------------------------------------------------------------------------

    [Fact]
    public void A_mint_may_reach_the_cap_exactly_and_leaves_nothing_to_mint() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds);
        long remaining = Chain.Cap - Chain.Funds;

        // Act
        chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Alice, Amount.Of(remaining));

        // Assert
        Assert.Equal(Amount.Of(Chain.Cap), chain.Ledger.TotalSupply);
        Assert.Equal(TokenAmount.Zero, chain.Ledger.RemainingMintable);
    }

    [Fact]
    public void A_mint_one_base_unit_past_the_cap_is_refused() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds);
        long remaining = Chain.Cap - Chain.Funds;

        // Act / Assert
        chain.Refuses(RefusalReason.CapExceeded,
                      () => chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Alice, Amount.Of(remaining + 1)));
    }

    [Fact]
    public void The_largest_amount_is_far_past_the_cap() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds);

        // Act / Assert
        chain.Refuses(RefusalReason.CapExceeded,
                      () => chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Alice, TokenAmount.Max));
    }

    [Fact]
    public void Burning_makes_room_under_the_cap_again() {
        // Arrange: fill the cap, burn 1_000, then mint exactly that much.
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds).FundAlice(Chain.Cap - Chain.Funds);
        chain.Ledger.Burn(chain.Envelope(chain.Alice).Generate(), Amount.Of(1_000));

        // Act
        chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Bob, Amount.Of(1_000));

        // Assert
        Assert.Equal(TokenAmount.Zero, chain.Ledger.RemainingMintable);
    }

    // ---- the minter minting to himself ----------------------------------------------------------------------------

    [Fact]
    public void A_minter_minting_to_himself_still_has_to_find_the_maximum_fee_in_what_he_already_holds() {
        // Arrange: the credit he is about to give himself does not count.
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, 999_999);

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientFundsForFee,
                      () => chain.Ledger.Mint(chain.Envelope(chain.Minter).WithGasLimit(100_000).Generate(), chain.Minter, Amount.Of(500)));
    }

    [Fact]
    public void A_minter_with_exactly_the_maximum_fee_may_mint_to_himself() {
        // Arrange
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, 1_000_000);

        // Act
        chain.Ledger.Mint(chain.Envelope(chain.Minter).WithGasLimit(100_000).Generate(), chain.Minter, Amount.Of(500));

        // Assert: 40_000 gas at 10 was paid out of the million, and 500 came in.
        Assert.Equal(1_000_000 - 400_000 + 500, chain.BalanceOf(chain.Minter));
    }

    [Fact]
    public void A_minter_who_is_also_the_collector_receives_his_own_fee_back() {
        // Arrange
        Chain chain = Chain.StartWithTheMinterAsCollector();
        chain.Fund(chain.Minter, Chain.Funds);

        // Act
        chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Alice, Amount.Of(500));

        // Assert
        Assert.Equal(Chain.Funds, chain.BalanceOf(chain.Minter));
    }

    // ---- Burn -----------------------------------------------------------------------------------------------------

    [Fact]
    public void Burning_destroys_the_senders_tokens_and_shrinks_the_supply() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        long  supplyBefore = (long)chain.Ledger.TotalSupply.BaseUnits;

        // Act
        chain.Ledger.Burn(chain.Envelope(chain.Alice).Generate(), Amount.Of(400));

        // Assert: 32_000 gas at 10.
        Assert.Equal(Chain.Funds - 400 - 320_000, chain.BalanceOf(chain.Alice));
        Assert.Equal(supplyBefore - 400, (long)chain.Ledger.TotalSupply.BaseUnits);
    }

    [Fact]
    public void Burning_raises_what_remains_to_mint() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        TokenAmount remainingBefore = chain.Ledger.RemainingMintable;

        // Act
        chain.Ledger.Burn(chain.Envelope(chain.Alice).Generate(), Amount.Of(400));

        // Assert
        Assert.Equal(remainingBefore.Add(Amount.Of(400)), chain.Ledger.RemainingMintable);
    }

    [Fact]
    public void A_burn_is_logged_as_going_to_the_zero_address_and_then_its_fee_is_logged() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();
        int   mark  = chain.Mark();

        // Act
        chain.Ledger.Burn(chain.Envelope(chain.Alice).Generate(), Amount.Of(400));

        // Assert
        IReadOnlyList<TransferRecord> records = chain.TransfersSince(mark);
        Assert.Equal(2, records.Count);
        Assert.Equal(new TransferRecord(chain.Alice, Address.Zero, Amount.Of(400), chain.Ledger.CurrentBlock), records[0]);
        Assert.True(records[0].IsBurn);
        Assert.Equal(new TransferRecord(chain.Alice, chain.Collector, Amount.Of(320_000), chain.Ledger.CurrentBlock), records[1]);
    }

    [Fact]
    public void A_burn_returns_a_receipt_with_the_burn_gas_and_no_surcharge() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act
        Receipt receipt = chain.Ledger.Burn(chain.Envelope(chain.Alice).Generate(), Amount.Of(400));

        // Assert
        Assert.Equal(TransactionKind.Burn, receipt.Kind);
        Assert.Equal(32_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Burning_one_base_unit_more_than_the_balance_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(1_000);

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientBalance,
                      () => chain.Ledger.Burn(chain.Envelope(chain.Alice).Generate(), Amount.Of(1_001)));
    }

    [Fact]
    public void Burning_has_to_leave_the_maximum_fee_in_the_balance() {
        // Arrange: Alice holds 1_000_400; burning 400 leaves 1_000_000 for a maximum fee of 1_000_000.
        Chain chain = Chain.Start().FundAlice(1_000_400);

        // Act
        chain.Ledger.Burn(chain.Envelope(chain.Alice).WithGasLimit(100_000).Generate(), Amount.Of(400));

        // Assert
        Assert.Equal(1_000_400 - 400 - 320_000, chain.BalanceOf(chain.Alice));
    }

    [Fact]
    public void Burning_that_would_leave_one_base_unit_less_than_the_maximum_fee_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(1_000_399);

        // Act / Assert
        chain.Refuses(RefusalReason.InsufficientFundsForFee,
                      () => chain.Ledger.Burn(chain.Envelope(chain.Alice).WithGasLimit(100_000).Generate(), Amount.Of(400)));
    }

    [Fact]
    public void A_burn_whose_gas_limit_does_not_cover_it_is_refused() {
        // Arrange
        Chain chain = Chain.Start().FundAlice();

        // Act / Assert
        chain.Refuses(RefusalReason.GasLimitTooLow,
                      () => chain.Ledger.Burn(chain.Envelope(chain.Alice).WithGasLimit(31_999).Generate(), Amount.Of(1)));
    }

    [Fact]
    public void A_mint_or_a_burn_with_a_missing_argument_is_refused_as_such() {
        // Arrange
        Chain               chain    = Chain.Start().FundAlice();
        TransactionEnvelope envelope = chain.Envelope(chain.Alice).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Mint(null!, chain.Bob, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Mint(envelope, null!, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Mint(envelope, chain.Bob, null!));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Burn(null!, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => chain.Ledger.Burn(envelope, null!));
    }

}
