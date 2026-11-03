using JustDummies;

using TokenLedger.Domain;
using TokenLedger.Tests.Discovery.Support;

using Xunit;

namespace TokenLedger.Tests.Discovery;

// The checks every transaction goes through, in their documented order, and the promise that a refusal changes nothing.
public sealed class LedgerRefusalTests {

    private const ulong Supply = 10_000_000_000;

    private static Receipt Submit(Ledger ledger, TransactionKind kind, TransactionEnvelope envelope, Address other) {
        return kind switch {
            TransactionKind.Transfer          => ledger.Transfer(envelope, other, new TokenAmount(1)),
            TransactionKind.Approve           => ledger.Approve(envelope, other, new TokenAmount(1)),
            TransactionKind.IncreaseAllowance => ledger.IncreaseAllowance(envelope, other, new TokenAmount(1)),
            TransactionKind.DecreaseAllowance => ledger.DecreaseAllowance(envelope, other, new TokenAmount(0)),
            TransactionKind.TransferFrom      => ledger.TransferFrom(envelope, other, other, new TokenAmount(0)),
            TransactionKind.Mint              => ledger.Mint(envelope, other, new TokenAmount(1)),
            TransactionKind.Burn              => ledger.Burn(envelope, new TokenAmount(1)),
            _                                 => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static TheoryData<TransactionKind> EveryKind() {
        TheoryData<TransactionKind> kinds = new TheoryData<TransactionKind>();
        foreach (TransactionKind kind in Enum.GetValues<TransactionKind>()) { kinds.Add(kind); }

        return kinds;
    }

    [Theory]
    [MemberData(nameof(EveryKind))]
    public void A_nonce_already_used_is_refused_as_too_low(TransactionKind kind) {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address other  = Chain.NewAddress();
        Chain.Fund(ledger, other, 1);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => Submit(ledger, kind, Chain.WithNonce(ledger, ledger.Minter, 0), other), other);

        Assert.Equal(RefusalReason.NonceTooLow, refusal.Reason);
    }

    [Theory]
    [MemberData(nameof(EveryKind))]
    public void A_nonce_that_skips_ahead_is_refused_as_too_high(TransactionKind kind) {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address other  = Chain.NewAddress();

        LedgerRefusedException refusal = Chain.Refused(ledger, () => Submit(ledger, kind, Chain.WithNonce(ledger, ledger.Minter, 1), other), other);

        Assert.Equal(RefusalReason.NonceTooHigh, refusal.Reason);
    }

    [Theory]
    [MemberData(nameof(EveryKind))]
    public void A_transaction_past_its_deadline_is_refused(TransactionKind kind) {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address other  = Chain.NewAddress();
        ledger.AdvanceBlocks(1);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => Submit(ledger, kind, Chain.WithDeadline(ledger, ledger.Minter, ledger.CurrentBlock - 1), other), other);

        Assert.Equal(RefusalReason.DeadlinePassed, refusal.Reason);
    }

    [Theory]
    [MemberData(nameof(EveryKind))]
    public void A_gas_price_under_the_minimum_is_refused(TransactionKind kind) {
        Ledger  ledger = Chain.NewLedger(Supply, minimumGasPrice: 5);
        Address other  = Chain.NewAddress();

        LedgerRefusedException refusal = Chain.Refused(ledger, () => Submit(ledger, kind, Chain.Next(ledger, ledger.Minter, gasPrice: 4), other), other);

        Assert.Equal(RefusalReason.GasPriceTooLow, refusal.Reason);
    }

    [Fact]
    public void A_gas_price_equal_to_the_minimum_is_accepted() {
        Ledger  ledger = Chain.NewLedger(Supply, minimumGasPrice: 5);
        Address alice  = Chain.NewAddress();

        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter, gasPrice: 5), alice, new TokenAmount(1));

        Assert.Equal(new TokenAmount(5), receipt.GasPrice);
    }

    [Fact]
    public void A_gas_price_above_the_minimum_is_paid_in_full() {
        Ledger  ledger = Chain.NewLedger(Supply, minimumGasPrice: 5);
        Address alice  = Chain.NewAddress();

        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter, gasPrice: 9), alice, new TokenAmount(1));

        Assert.Equal(new TokenAmount(55_000 * 9), receipt.Fee);
    }

    [Fact]
    public void The_deadline_is_inclusive() {
        // Arrange
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        ledger.AdvanceBlocks(3);

        // Act: the transaction is valid until the block the ledger is at.
        Receipt receipt = ledger.Transfer(Chain.WithDeadline(ledger, ledger.Minter, ledger.CurrentBlock), alice, new TokenAmount(1));

        // Assert
        Assert.Equal(ledger.CurrentBlock, receipt.BlockNumber);
    }

    [Fact]
    public void A_transaction_is_refused_once_the_chain_moves_past_its_deadline() {
        Ledger              ledger   = Chain.NewLedger(Supply);
        Address             alice    = Chain.NewAddress();
        TransactionEnvelope envelope = Chain.WithDeadline(ledger, ledger.Minter, ledger.CurrentBlock + 2);

        ledger.AdvanceBlocks(2);
        Receipt accepted = ledger.Transfer(envelope, alice, new TokenAmount(1));
        ledger.AdvanceBlocks(1);
        TransactionEnvelope next = Chain.WithDeadline(ledger, ledger.Minter, ledger.CurrentBlock - 1);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(next, alice, new TokenAmount(1)), alice);

        Assert.Equal(accepted.BlockNumber, envelope.DeadlineBlock);
        Assert.Equal(RefusalReason.DeadlinePassed, refusal.Reason);
    }

    [Fact]
    public void A_deadline_of_the_last_possible_block_never_expires() {
        Ledger  ledger = Any.Ledger().WithGenesisBlock(ulong.MaxValue - 5).WithInitialSupply(new TokenAmount(Supply)).WithMinimumGasPrice(new TokenAmount(1)).WithGasSchedule(GasSchedule.Standard).WithToken(t => t.WithSupplyCap(new TokenAmount(1_000_000_000_000)).WithDecimals(d => d.Between(0, 12))).Generate();
        Address alice  = Chain.NewAddress();
        ledger.AdvanceBlocks(5);

        Receipt receipt = ledger.Transfer(Chain.WithDeadline(ledger, ledger.Minter, ulong.MaxValue), alice, new TokenAmount(1));

        Assert.Equal(ulong.MaxValue, receipt.BlockNumber);
    }

    [Fact]
    public void A_replayed_transaction_is_refused() {
        Ledger              ledger   = Chain.NewLedger(Supply);
        Address             alice    = Chain.NewAddress();
        TransactionEnvelope envelope = Chain.Next(ledger, ledger.Minter);
        ledger.Transfer(envelope, alice, new TokenAmount(1));

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(envelope, alice, new TokenAmount(1)), alice);

        Assert.Equal(RefusalReason.NonceTooLow, refusal.Reason);
    }

    [Fact]
    public void A_refused_transaction_does_not_burn_its_nonce() {
        // Arrange
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Refused(ledger, () => ledger.Transfer(Chain.Next(ledger, ledger.Minter), Address.Zero, new TokenAmount(1)));

        // Act: the very same nonce is still the one to use.
        Receipt receipt = ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1));

        // Assert
        Assert.Equal(UInt128.Zero, receipt.Nonce);
        Assert.Equal(new UInt128(0, 1), ledger.NextNonceOf(ledger.Minter));
    }

    [Fact]
    public void Each_sender_has_a_nonce_of_their_own() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        Chain.Fund(ledger, alice, 2_000_000);
        Chain.Fund(ledger, bob, 2_000_000);
        ledger.Transfer(Chain.Next(ledger, alice), bob, new TokenAmount(1));
        ledger.Transfer(Chain.Next(ledger, alice), bob, new TokenAmount(1));

        Receipt bobsFirst = ledger.Transfer(Chain.Next(ledger, bob), alice, new TokenAmount(1));

        Assert.Equal(UInt128.Zero, bobsFirst.Nonce);
        Assert.Equal(new UInt128(0, 2), ledger.NextNonceOf(alice));
        Assert.Equal(new UInt128(0, 1), ledger.NextNonceOf(bob));
    }

    [Fact]
    public void The_nonce_is_checked_before_the_deadline() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        ledger.AdvanceBlocks(1);
        TransactionEnvelope expiredAndOutOfOrder = Any.TransactionEnvelope()
                                                      .WithSender(ledger.Minter)
                                                      .WithNonce(7)
                                                      .WithGasPrice(new TokenAmount(2))
                                                      .WithDeadlineBlock(ledger.CurrentBlock - 1)
                                                      .Generate();

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(expiredAndOutOfOrder, alice, new TokenAmount(1)), alice);

        Assert.Equal(RefusalReason.NonceTooHigh, refusal.Reason);
    }

    [Fact]
    public void The_deadline_is_checked_before_the_gas_price() {
        Ledger  ledger = Chain.NewLedger(Supply, minimumGasPrice: 5);
        Address alice  = Chain.NewAddress();
        ledger.AdvanceBlocks(1);
        TransactionEnvelope expiredAndCheap = Any.TransactionEnvelope()
                                                 .WithSender(ledger.Minter)
                                                 .WithNonce(0)
                                                 .WithGasPrice(new TokenAmount(1))
                                                 .WithDeadlineBlock(ledger.CurrentBlock - 1)
                                                 .Generate();

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(expiredAndCheap, alice, new TokenAmount(1)), alice);

        Assert.Equal(RefusalReason.DeadlinePassed, refusal.Reason);
    }

    [Fact]
    public void The_gas_price_is_checked_before_the_addresses() {
        Ledger ledger = Chain.NewLedger(Supply, minimumGasPrice: 5);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.Next(ledger, ledger.Minter, gasPrice: 1), Address.Zero, new TokenAmount(1)));

        Assert.Equal(RefusalReason.GasPriceTooLow, refusal.Reason);
    }

    [Fact]
    public void A_zero_address_is_refused_before_the_minter_is_checked() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1_000_000);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Mint(Chain.Next(ledger, alice), Address.Zero, new TokenAmount(1)), alice);

        Assert.Equal(RefusalReason.ZeroAddress, refusal.Reason);
    }

    [Fact]
    public void The_minter_is_checked_before_the_gas() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1_000_000);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Mint(Chain.NextWithGasLimit(ledger, alice, 21_000), bob, new TokenAmount(1)), alice, bob);

        Assert.Equal(RefusalReason.NotMinter, refusal.Reason);
    }

    [Fact]
    public void The_gas_is_checked_before_the_operation() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        Chain.Fund(ledger, alice, 1_000_000);

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.NextWithGasLimit(ledger, alice, 21_000), bob, new TokenAmount(9_999_999)), alice, bob);

        Assert.Equal(RefusalReason.GasLimitTooLow, refusal.Reason);
    }

    [Fact]
    public void The_cap_is_checked_before_the_fee() {
        Ledger  ledger = Chain.NewLedger(initialSupply: 0);
        Address bob    = Chain.NewAddress();

        // The minter holds nothing and could not pay: but asking for more than the cap is the first refusal.
        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Mint(Chain.Next(ledger, ledger.Minter), bob, ledger.RemainingMintable.Add(new TokenAmount(1))), bob);

        Assert.Equal(RefusalReason.CapExceeded, refusal.Reason);
    }

    [Fact]
    public void The_operation_is_checked_before_the_fee() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        Chain.Fund(ledger, alice, 100_000);

        // Alice holds 100 000: a transfer of 200 000 exceeds her balance, which is also short of the fee.
        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.Next(ledger, alice), bob, new TokenAmount(200_000)), alice, bob);

        Assert.Equal(RefusalReason.InsufficientBalance, refusal.Reason);
    }

    [Fact]
    public void A_refusal_names_a_reason_and_explains_it() {
        Ledger  ledger = Chain.NewLedger(Supply);
        Address alice  = Chain.NewAddress();

        LedgerRefusedException refusal = Chain.Refused(ledger, () => ledger.Transfer(Chain.Next(ledger, alice), ledger.Minter, new TokenAmount(1)), alice);

        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
        Assert.Equal(RefusalReason.InsufficientBalance, refusal.Reason);
        Assert.False(string.IsNullOrWhiteSpace(refusal.Message));
    }

    [Fact]
    public void A_refusal_cannot_name_an_undefined_reason() {
        string anyMessage = Any.String().Printable().NonEmpty().WithMaxLength(60).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new LedgerRefusedException((RefusalReason)0, anyMessage));

        Assert.Equal("reason", refusal.ParamName);
    }

    [Fact]
    public void A_refusal_carries_the_reason_it_was_given() {
        string anyMessage = Any.String().Printable().NonEmpty().WithMaxLength(60).Generate();

        LedgerRefusedException refusal = new LedgerRefusedException(RefusalReason.NotMinter, anyMessage);

        Assert.Equal(RefusalReason.NotMinter, refusal.Reason);
        Assert.Equal(anyMessage, refusal.Message);
    }

}
