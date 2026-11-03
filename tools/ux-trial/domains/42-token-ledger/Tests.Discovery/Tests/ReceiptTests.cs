using System.Numerics;

using JustDummies;

using TokenLedger.Domain;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class ReceiptTests {

    [Fact]
    public void A_receipt_keeps_what_it_was_given_and_computes_its_fee() {
        // Arrange
        Address sender = Any.Address().NonZero().Generate();

        // Act
        Receipt receipt = new Receipt(TransactionKind.Mint, sender, 4, 77, 40_000, new TokenAmount(3));

        // Assert
        Assert.Equal(TransactionKind.Mint, receipt.Kind);
        Assert.Same(sender, receipt.Sender);
        Assert.Equal(new UInt128(0, 4), receipt.Nonce);
        Assert.Equal(77UL, receipt.BlockNumber);
        Assert.Equal(40_000UL, receipt.GasUsed);
        Assert.Equal(new TokenAmount(3), receipt.GasPrice);
        Assert.Equal(new TokenAmount(120_000), receipt.Fee);
    }

    [Fact]
    public void A_drawn_receipt_always_has_a_fee_that_fits_in_256_bits() {
        for (int i = 0; i < 100; i++) {
            Receipt anyReceipt = Any.Receipt().Generate();

            Assert.True(anyReceipt.Fee.BaseUnits <= TokenAmount.MaxBaseUnits);
            Assert.Equal(anyReceipt.GasPrice.BaseUnits * anyReceipt.GasUsed, anyReceipt.Fee.BaseUnits);
        }
    }

    [Fact]
    public void A_free_receipt_has_a_zero_fee() {
        Receipt receipt = Any.Receipt().WithGasPrice(TokenAmount.Zero).Generate();

        Assert.True(receipt.Fee.IsZero);
    }

    [Fact]
    public void Every_defined_kind_is_accepted() {
        foreach (TransactionKind kind in Enum.GetValues<TransactionKind>()) {
            Receipt receipt = Any.Receipt().WithKind(kind).Generate();

            Assert.Equal(kind, receipt.Kind);
        }
    }

    [Fact]
    public void An_undefined_kind_is_refused() {
        Address sender = Any.Address().NonZero().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Receipt((TransactionKind)99, sender, 0, 0, 21_000, TokenAmount.Zero));

        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void A_receipt_cannot_come_from_the_zero_address() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Receipt(TransactionKind.Transfer, Address.Zero, 0, 0, 21_000, TokenAmount.Zero));

        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("sender", refusal.ParamName);
    }

    [Fact]
    public void Gas_used_outside_the_intrinsic_gas_and_a_block_is_refused() {
        Address sender     = Any.Address().NonZero().Generate();
        ulong   anyTooLow  = Any.UInt64().LessThan(GasSchedule.IntrinsicGas).Generate();
        ulong   anyTooHigh = Any.UInt64().GreaterThan(GasSchedule.BlockGasLimit).Generate();

        Assert.Equal("gasUsed", Assert.Throws<ArgumentOutOfRangeException>(() => new Receipt(TransactionKind.Burn, sender, 0, 0, anyTooLow, TokenAmount.Zero)).ParamName);
        Assert.Equal("gasUsed", Assert.Throws<ArgumentOutOfRangeException>(() => new Receipt(TransactionKind.Burn, sender, 0, 0, anyTooHigh, TokenAmount.Zero)).ParamName);
    }

    [Fact]
    public void The_edges_of_the_gas_used_range_are_accepted() {
        Address sender = Any.Address().NonZero().Generate();

        Assert.Equal(21_000UL, new Receipt(TransactionKind.Burn, sender, 0, 0, 21_000, TokenAmount.Zero).GasUsed);
        Assert.Equal(30_000_000UL, new Receipt(TransactionKind.Burn, sender, 0, 0, 30_000_000, TokenAmount.Zero).GasUsed);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Receipt(TransactionKind.Burn, sender, 0, 0, 20_999, TokenAmount.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Receipt(TransactionKind.Burn, sender, 0, 0, 30_000_001, TokenAmount.Zero));
    }

    [Fact]
    public void A_price_whose_fee_passes_two_to_the_256_minus_one_is_refused() {
        Address     sender  = Any.Address().NonZero().Generate();
        ulong       anyUsed = Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit).Generate();
        TokenAmount tooHigh = new TokenAmount(TokenAmount.MaxBaseUnits / anyUsed + 1);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Receipt(TransactionKind.Transfer, sender, 0, 0, anyUsed, tooHigh));

        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("gasPrice", refusal.ParamName);
    }

    [Fact]
    public void The_highest_price_that_still_fits_gives_a_fee_at_most_the_maximum() {
        Address     sender  = Any.Address().NonZero().Generate();
        ulong       anyUsed = Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit).Generate();
        TokenAmount highest = new TokenAmount(TokenAmount.MaxBaseUnits / anyUsed);

        Receipt receipt = new Receipt(TransactionKind.Transfer, sender, 0, 0, anyUsed, highest);

        Assert.True(receipt.Fee.BaseUnits <= TokenAmount.MaxBaseUnits);
        Assert.True(TokenAmount.MaxBaseUnits - receipt.Fee.BaseUnits < anyUsed);
    }

    [Fact]
    public void A_null_sender_or_price_is_refused() {
        Address sender = Any.Address().NonZero().Generate();

        Assert.Throws<ArgumentNullException>(() => new Receipt(TransactionKind.Transfer, null!, 0, 0, 21_000, TokenAmount.Zero));
        Assert.Throws<ArgumentNullException>(() => new Receipt(TransactionKind.Transfer, sender, 0, 0, 21_000, null!));
    }

    [Fact]
    public void Two_receipts_with_the_same_fields_are_equal_and_hash_alike() {
        Address sender = Any.Address().NonZero().Generate();

        Receipt first  = new Receipt(TransactionKind.Approve, sender, 1, 2, 30_000, new TokenAmount(5));
        Receipt second = new Receipt(TransactionKind.Approve, new Address(sender.Value), 1, 2, 30_000, new TokenAmount(5));

        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_receipt_differs_when_any_one_field_differs() {
        Address other  = Any.Address().NonZero().Generate();
        Address sender = Any.Address().NonZero().DifferentFromAddress(other).Generate();

        Receipt baseline = new Receipt(TransactionKind.Approve, sender, 1, 2, 30_000, new TokenAmount(5));

        Assert.NotEqual(baseline, new Receipt(TransactionKind.Burn, sender, 1, 2, 30_000, new TokenAmount(5)));
        Assert.NotEqual(baseline, new Receipt(TransactionKind.Approve, other, 1, 2, 30_000, new TokenAmount(5)));
        Assert.NotEqual(baseline, new Receipt(TransactionKind.Approve, sender, 2, 2, 30_000, new TokenAmount(5)));
        Assert.NotEqual(baseline, new Receipt(TransactionKind.Approve, sender, 1, 3, 30_000, new TokenAmount(5)));
        Assert.NotEqual(baseline, new Receipt(TransactionKind.Approve, sender, 1, 2, 30_001, new TokenAmount(5)));
        Assert.NotEqual(baseline, new Receipt(TransactionKind.Approve, sender, 1, 2, 30_000, new TokenAmount(6)));
        Assert.False(baseline.Equals(null));
    }

    [Fact]
    public void A_receipt_describes_itself() {
        Receipt receipt = new Receipt(TransactionKind.Mint, new Address("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed"), 4, 77, 40_000, new TokenAmount(3));

        Assert.Equal("Mint by 0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed #4 in block 77: 40000 gas, fee 120000", receipt.ToString());
    }

}
