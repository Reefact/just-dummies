using System.Numerics;

using JustDummies;

using TokenLedger.Domain;
using TokenLedger.Tests.Discovery.Support;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class LedgerAllowanceTests {

    private const ulong Supply = 10_000_000_000;

    // Alice owns 1 000 000 and has funds for fees; bob is her spender, also able to pay fees; carol is a recipient who holds nothing.
    private sealed class Scene {

        public Scene() {
            Ledger = Chain.NewLedger(Supply);
            List<Address> people = Chain.NewAddresses(3);
            Alice = people[0];
            Bob   = people[1];
            Carol = people[2];
            Chain.Fund(Ledger, Alice, 1_000_000);
            Chain.Fund(Ledger, Bob, 1_000_000);
        }

        public Ledger  Ledger { get; }
        public Address Alice  { get; }
        public Address Bob    { get; }
        public Address Carol  { get; }

    }

    [Fact]
    public void Approving_sets_the_allowance_of_a_spender_over_the_sender_balance() {
        Scene scene = new Scene();

        Receipt receipt = scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        Assert.Equal(new TokenAmount(300), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
        Assert.Equal(TransactionKind.Approve, receipt.Kind);
        Assert.Equal(30_000UL, receipt.GasUsed);
        Assert.Equal(new TokenAmount(60_000), receipt.Fee);
    }

    [Fact]
    public void Approving_again_replaces_the_previous_allowance() {
        Scene scene = new Scene();

        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(50));

        Assert.Equal(new TokenAmount(50), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void Approving_does_not_touch_the_other_direction_or_other_spenders() {
        Scene scene = new Scene();

        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        Assert.Equal(TokenAmount.Zero, scene.Ledger.AllowanceOf(scene.Bob, scene.Alice));
        Assert.Equal(TokenAmount.Zero, scene.Ledger.AllowanceOf(scene.Alice, scene.Carol));
    }

    [Fact]
    public void An_allowance_may_exceed_the_balance_of_its_owner() {
        Scene scene = new Scene();

        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, TokenAmount.Max);

        Assert.Equal(TokenAmount.Max, scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void Approving_zero_clears_the_allowance() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, TokenAmount.Zero);

        Assert.Equal(TokenAmount.Zero, scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void Approving_moves_no_tokens_except_the_fee() {
        Scene       scene    = new Scene();
        TokenAmount before   = scene.Ledger.BalanceOf(scene.Alice);

        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        Assert.Equal(before.Subtract(new TokenAmount(60_000)), scene.Ledger.BalanceOf(scene.Alice));
    }

    [Fact]
    public void Approving_a_new_holder_costs_no_surcharge() {
        Scene scene = new Scene();

        Receipt receipt = scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Carol, new TokenAmount(300));

        Assert.Equal(30_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Approving_the_zero_address_as_a_spender_is_refused() {
        Scene scene = new Scene();

        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), Address.Zero, new TokenAmount(1)), scene.Alice);

        Assert.Equal(RefusalReason.ZeroAddress, refusal.Reason);
    }

    [Fact]
    public void Approving_without_the_funds_for_the_maximum_fee_is_refused() {
        // Arrange: carol holds nothing, and the price is not zero.
        Scene scene = new Scene();

        // Act
        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Carol), scene.Bob, new TokenAmount(1)), scene.Carol);

        // Assert
        Assert.Equal(RefusalReason.InsufficientFundsForFee, refusal.Reason);
    }

    [Fact]
    public void Approving_needs_the_approve_gas_only() {
        Scene scene = new Scene();

        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.Approve(Chain.NextWithGasLimit(scene.Ledger, scene.Alice, 29_999), scene.Bob, new TokenAmount(1)), scene.Alice);
        Receipt                receipt = scene.Ledger.Approve(Chain.NextWithGasLimit(scene.Ledger, scene.Alice, 30_000), scene.Bob, new TokenAmount(1));

        Assert.Equal(RefusalReason.GasLimitTooLow, refusal.Reason);
        Assert.Equal(30_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Increasing_adds_to_the_current_allowance() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        Receipt receipt = scene.Ledger.IncreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(40));

        Assert.Equal(new TokenAmount(340), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
        Assert.Equal(TransactionKind.IncreaseAllowance, receipt.Kind);
        Assert.Equal(30_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Increasing_an_unset_allowance_sets_it() {
        Scene scene = new Scene();

        scene.Ledger.IncreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(40));

        Assert.Equal(new TokenAmount(40), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void Increasing_up_to_exactly_the_maximum_is_allowed() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(TokenAmount.MaxBaseUnits - 7));

        scene.Ledger.IncreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(7));

        Assert.Equal(TokenAmount.Max, scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void Increasing_past_the_maximum_is_refused_as_an_overflow() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(TokenAmount.MaxBaseUnits - 7));

        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.IncreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(8)), scene.Alice);

        Assert.Equal(RefusalReason.AllowanceOverflow, refusal.Reason);
        Assert.Equal(new TokenAmount(TokenAmount.MaxBaseUnits - 7), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void An_overflow_is_reported_before_a_lack_of_funds_for_the_fee() {
        // Arrange: a free ledger lets an owner who holds nothing set an allowance at the maximum; the next
        // transaction pays a price, which the owner cannot afford - but the overflow comes first.
        Ledger  freeLedger = Chain.NewLedger(Supply, minimumGasPrice: 0);
        Address owner      = Chain.NewAddress();
        Address spender    = Chain.NewAddress();
        freeLedger.Approve(Chain.Next(freeLedger, owner, gasPrice: 0), spender, TokenAmount.Max);

        // Act
        LedgerRefusedException refusal = Chain.Refused(freeLedger, () => freeLedger.IncreaseAllowance(Chain.Next(freeLedger, owner), spender, new TokenAmount(1)), owner);

        // Assert
        Assert.Equal(RefusalReason.AllowanceOverflow, refusal.Reason);
    }

    [Fact]
    public void Increasing_by_nothing_never_overflows() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, TokenAmount.Max);

        scene.Ledger.IncreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, TokenAmount.Zero);

        Assert.Equal(TokenAmount.Max, scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void Decreasing_subtracts_from_the_current_allowance() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        Receipt receipt = scene.Ledger.DecreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(120));

        Assert.Equal(new TokenAmount(180), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
        Assert.Equal(TransactionKind.DecreaseAllowance, receipt.Kind);
        Assert.Equal(30_000UL, receipt.GasUsed);
    }

    [Fact]
    public void Decreasing_to_exactly_zero_is_allowed() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        scene.Ledger.DecreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        Assert.Equal(TokenAmount.Zero, scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void Decreasing_below_zero_is_refused() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.DecreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(301)), scene.Alice);

        Assert.Equal(RefusalReason.AllowanceExceeded, refusal.Reason);
        Assert.Equal(new TokenAmount(300), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void Decreasing_an_unset_allowance_by_anything_is_refused() {
        Scene       scene      = new Scene();
        TokenAmount anyPositive = Any.TokenAmount().WithBaseUnits(units => units.Between(1, 1_000_000)).Generate();

        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.DecreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, anyPositive), scene.Alice);

        Assert.Equal(RefusalReason.AllowanceExceeded, refusal.Reason);
    }

    [Fact]
    public void Decreasing_the_unlimited_allowance_subtracts_like_any_other() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, TokenAmount.Max);

        scene.Ledger.DecreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(1));

        Assert.Equal(new TokenAmount(TokenAmount.MaxBaseUnits - 1), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void Changing_an_allowance_towards_the_zero_address_is_refused() {
        Scene scene = new Scene();

        Assert.Equal(RefusalReason.ZeroAddress, Chain.Refused(scene.Ledger, () => scene.Ledger.IncreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), Address.Zero, new TokenAmount(1)), scene.Alice).Reason);
        Assert.Equal(RefusalReason.ZeroAddress, Chain.Refused(scene.Ledger, () => scene.Ledger.DecreaseAllowance(Chain.Next(scene.Ledger, scene.Alice), Address.Zero, new TokenAmount(1)), scene.Alice).Reason);
    }

    [Fact]
    public void A_spender_moves_tokens_of_the_owner_within_the_allowance() {
        // Arrange
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));
        TokenAmount aliceBefore = scene.Ledger.BalanceOf(scene.Alice);
        TokenAmount bobBefore   = scene.Ledger.BalanceOf(scene.Bob);

        // Act
        Receipt receipt = scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, new TokenAmount(100));

        // Assert: carol is a new holder, so 45 000 + 20 000 gas at 2 are paid by the spender.
        Assert.Equal(aliceBefore.Subtract(new TokenAmount(100)), scene.Ledger.BalanceOf(scene.Alice));
        Assert.Equal(new TokenAmount(100), scene.Ledger.BalanceOf(scene.Carol));
        Assert.Equal(bobBefore.Subtract(new TokenAmount(130_000)), scene.Ledger.BalanceOf(scene.Bob));
        Assert.Equal(new TokenAmount(200), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
        Assert.Equal(TransactionKind.TransferFrom, receipt.Kind);
        Assert.Equal(65_000UL, receipt.GasUsed);
        Assert.Equal(scene.Bob, receipt.Sender);
    }

    [Fact]
    public void A_delegated_transfer_to_an_existing_holder_costs_no_surcharge() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        Receipt receipt = scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Bob, new TokenAmount(1));

        Assert.Equal(45_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_delegated_transfer_is_logged_as_a_movement_from_the_owner() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));
        int before = scene.Ledger.Transfers.Count;

        scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, new TokenAmount(100));

        Assert.Equal(new TransferRecord(scene.Alice, scene.Carol, new TokenAmount(100), scene.Ledger.CurrentBlock), scene.Ledger.Transfers[before]);
        Assert.Equal(new TransferRecord(scene.Bob, scene.Ledger.FeeCollector, new TokenAmount(130_000), scene.Ledger.CurrentBlock), scene.Ledger.Transfers[before + 1]);
    }

    [Fact]
    public void A_delegated_transfer_of_exactly_the_allowance_uses_it_up() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, new TokenAmount(300));

        Assert.Equal(TokenAmount.Zero, scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void A_delegated_transfer_above_the_allowance_is_refused() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, new TokenAmount(301)), scene.Alice, scene.Bob, scene.Carol);

        Assert.Equal(RefusalReason.AllowanceExceeded, refusal.Reason);
        Assert.Equal(new TokenAmount(300), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void A_delegated_transfer_without_any_allowance_is_refused() {
        Scene       scene       = new Scene();
        TokenAmount anyPositive = Any.TokenAmount().WithBaseUnits(units => units.Between(1, 1_000)).Generate();

        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, anyPositive), scene.Alice, scene.Bob, scene.Carol);

        Assert.Equal(RefusalReason.AllowanceExceeded, refusal.Reason);
    }

    [Fact]
    public void A_delegated_transfer_above_the_owner_balance_is_refused_for_the_balance() {
        // Arrange: the allowance is larger than what alice holds.
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(5_000_000));

        // Act
        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, new TokenAmount(4_000_000)), scene.Alice, scene.Bob, scene.Carol);

        // Assert
        Assert.Equal(RefusalReason.InsufficientBalance, refusal.Reason);
    }

    [Fact]
    public void The_allowance_is_checked_before_the_balance() {
        // Arrange: the amount is above both the allowance and the owner's balance.
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        // Act
        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, new TokenAmount(9_000_000)), scene.Alice, scene.Bob, scene.Carol);

        // Assert
        Assert.Equal(RefusalReason.AllowanceExceeded, refusal.Reason);
    }

    [Fact]
    public void An_unlimited_allowance_is_not_decreased_by_use() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, TokenAmount.Max);

        scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, new TokenAmount(250));
        scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, new TokenAmount(250));

        Assert.Equal(TokenAmount.Max, scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
        Assert.Equal(new TokenAmount(500), scene.Ledger.BalanceOf(scene.Carol));
    }

    [Fact]
    public void An_allowance_one_below_the_maximum_is_decreased_by_use() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(TokenAmount.MaxBaseUnits - 1));

        scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Carol, new TokenAmount(250));

        Assert.Equal(new TokenAmount(TokenAmount.MaxBaseUnits - 251), scene.Ledger.AllowanceOf(scene.Alice, scene.Bob));
    }

    [Fact]
    public void The_spender_pays_the_fee_and_the_owner_only_the_amount() {
        Scene       scene       = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));
        TokenAmount aliceBefore = scene.Ledger.BalanceOf(scene.Alice);

        scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, scene.Alice, new TokenAmount(100));

        Assert.Equal(aliceBefore, scene.Ledger.BalanceOf(scene.Alice));
    }

    [Fact]
    public void A_spender_who_cannot_pay_the_fee_is_refused_even_when_the_transfer_would_credit_them() {
        // Arrange: dave holds nothing; alice lets dave move 300 of hers to dave himself.
        Scene   scene = new Scene();
        Address dave  = Chain.NewAddress();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), dave, new TokenAmount(300));

        // Act
        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, dave), scene.Alice, dave, new TokenAmount(300)), scene.Alice, dave);

        // Assert: credits to the sender do not count towards the fee.
        Assert.Equal(RefusalReason.InsufficientFundsForFee, refusal.Reason);
    }

    [Fact]
    public void A_spender_spending_their_own_tokens_must_keep_the_fee_after_the_amount() {
        // Arrange: bob approves himself, then moves his own tokens: the amount leaves his balance, so it counts against the fee.
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Bob), scene.Bob, new TokenAmount(5_000_000));
        TokenAmount balance = scene.Ledger.BalanceOf(scene.Bob);

        // Act
        LedgerRefusedException refusal = Chain.Refused(scene.Ledger,
                                                       () => scene.Ledger.TransferFrom(Chain.NextWithGasLimit(scene.Ledger, scene.Bob, 100_000), scene.Bob, scene.Carol, balance.Subtract(new TokenAmount(199_999))),
                                                       scene.Bob,
                                                       scene.Carol);
        Receipt receipt = scene.Ledger.TransferFrom(Chain.NextWithGasLimit(scene.Ledger, scene.Bob, 100_000), scene.Bob, scene.Carol, balance.Subtract(new TokenAmount(200_000)));

        // Assert
        Assert.Equal(RefusalReason.InsufficientFundsForFee, refusal.Reason);
        Assert.Equal(65_000UL, receipt.GasUsed);
    }

    [Fact]
    public void A_delegated_transfer_from_or_to_the_zero_address_is_refused() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        LedgerRefusedException fromZero = Chain.Refused(scene.Ledger, () => scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), Address.Zero, scene.Carol, new TokenAmount(1)), scene.Bob);
        LedgerRefusedException toZero   = Chain.Refused(scene.Ledger, () => scene.Ledger.TransferFrom(Chain.Next(scene.Ledger, scene.Bob), scene.Alice, Address.Zero, new TokenAmount(1)), scene.Bob);

        Assert.Equal(RefusalReason.ZeroAddress, fromZero.Reason);
        Assert.Equal(RefusalReason.ZeroAddress, toZero.Reason);
    }

    [Fact]
    public void A_delegated_transfer_needs_the_gas_of_a_delegated_transfer_and_the_surcharge() {
        Scene scene = new Scene();
        scene.Ledger.Approve(Chain.Next(scene.Ledger, scene.Alice), scene.Bob, new TokenAmount(300));

        LedgerRefusedException refusal = Chain.Refused(scene.Ledger, () => scene.Ledger.TransferFrom(Chain.NextWithGasLimit(scene.Ledger, scene.Bob, 64_999), scene.Alice, scene.Carol, new TokenAmount(1)), scene.Bob, scene.Carol);

        Assert.Equal(RefusalReason.GasLimitTooLow, refusal.Reason);
    }

    [Fact]
    public void A_delegated_transfer_refuses_null_arguments() {
        Scene               scene    = new Scene();
        TransactionEnvelope envelope = Chain.Next(scene.Ledger, scene.Bob);

        Assert.Throws<ArgumentNullException>(() => scene.Ledger.TransferFrom(null!, scene.Alice, scene.Carol, TokenAmount.Zero));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.TransferFrom(envelope, null!, scene.Carol, TokenAmount.Zero));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.TransferFrom(envelope, scene.Alice, null!, TokenAmount.Zero));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.TransferFrom(envelope, scene.Alice, scene.Carol, null!));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.Approve(null!, scene.Bob, TokenAmount.Zero));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.IncreaseAllowance(envelope, null!, TokenAmount.Zero));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.DecreaseAllowance(envelope, scene.Bob, null!));
    }

}
