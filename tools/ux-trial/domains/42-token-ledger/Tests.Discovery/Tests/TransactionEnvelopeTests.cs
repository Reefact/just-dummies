using System.Numerics;

using JustDummies;

using TokenLedger.Domain;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class TransactionEnvelopeTests {

    [Fact]
    public void An_envelope_keeps_what_it_was_given() {
        // Arrange
        Address sender = Any.Address().NonZero().Generate();

        // Act
        TransactionEnvelope envelope = new TransactionEnvelope(sender, 7, 50_000, new TokenAmount(3), 1_000);

        // Assert
        Assert.Same(sender, envelope.Sender);
        Assert.Equal(new UInt128(0, 7), envelope.Nonce);
        Assert.Equal(50_000UL, envelope.GasLimit);
        Assert.Equal(new TokenAmount(3), envelope.GasPrice);
        Assert.Equal(1_000UL, envelope.DeadlineBlock);
    }

    [Fact]
    public void A_drawn_envelope_always_has_a_fee_that_fits_in_256_bits() {
        for (int i = 0; i < 100; i++) {
            TransactionEnvelope anyEnvelope = Any.TransactionEnvelope().Generate();

            Assert.True(anyEnvelope.MaxFee.BaseUnits <= TokenAmount.MaxBaseUnits);
            Assert.False(anyEnvelope.Sender.IsZero);
        }
    }

    [Fact]
    public void The_maximum_fee_is_the_gas_limit_times_the_gas_price() {
        // Arrange: the limit is drawn, the product is what is checked.
        ulong anyLimit = Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit).Generate();

        // Act
        TransactionEnvelope envelope = Any.TransactionEnvelope().WithGasLimit(anyLimit).WithGasPrice(new TokenAmount(1_000)).Generate();

        // Assert
        Assert.Equal(new TokenAmount(1_000 * (BigInteger)anyLimit), envelope.MaxFee);
    }

    [Fact]
    public void A_zero_gas_price_is_allowed_and_costs_nothing() {
        TransactionEnvelope envelope = Any.TransactionEnvelope().WithGasPrice(TokenAmount.Zero).Generate();

        Assert.True(envelope.MaxFee.IsZero);
    }

    [Fact]
    public void The_zero_address_cannot_sign() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TransactionEnvelope(Address.Zero, 0, 50_000, TokenAmount.Zero, 0));

        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("sender", refusal.ParamName);
    }

    [Fact]
    public void A_gas_limit_outside_the_intrinsic_gas_and_a_block_is_refused() {
        // Arrange
        Address sender        = Any.Address().NonZero().Generate();
        ulong   anyTooLow     = Any.UInt64().LessThan(GasSchedule.IntrinsicGas).Generate();
        ulong   anyTooHigh    = Any.UInt64().GreaterThan(GasSchedule.BlockGasLimit).Generate();

        // Act
        ArgumentOutOfRangeException low  = Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionEnvelope(sender, 0, anyTooLow, TokenAmount.Zero, 0));
        ArgumentOutOfRangeException high = Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionEnvelope(sender, 0, anyTooHigh, TokenAmount.Zero, 0));

        // Assert
        Assert.Equal("gasLimit", low.ParamName);
        Assert.Equal("gasLimit", high.ParamName);
    }

    [Fact]
    public void The_gas_limit_edges_are_accepted() {
        Address sender = Any.Address().NonZero().Generate();

        Assert.Equal(21_000UL, new TransactionEnvelope(sender, 0, 21_000, TokenAmount.Zero, 0).GasLimit);
        Assert.Equal(30_000_000UL, new TransactionEnvelope(sender, 0, 30_000_000, TokenAmount.Zero, 0).GasLimit);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionEnvelope(sender, 0, 20_999, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionEnvelope(sender, 0, 30_000_001, TokenAmount.Zero, 0));
    }

    [Fact]
    public void A_price_whose_maximum_fee_passes_two_to_the_256_minus_one_is_refused() {
        Address sender = Any.Address().NonZero().Generate();
        ulong   anyLimit = Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit).Generate();
        TokenAmount tooHigh = new TokenAmount(TokenAmount.MaxBaseUnits / anyLimit + 1);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TransactionEnvelope(sender, 0, anyLimit, tooHigh, 0));

        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("gasPrice", refusal.ParamName);
    }

    [Fact]
    public void The_highest_price_that_still_fits_is_accepted() {
        Address     sender   = Any.Address().NonZero().Generate();
        ulong       anyLimit = Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit).Generate();
        TokenAmount highest  = new TokenAmount(TokenAmount.MaxBaseUnits / anyLimit);

        TransactionEnvelope envelope = new TransactionEnvelope(sender, 0, anyLimit, highest, 0);

        Assert.True(envelope.MaxFee.BaseUnits <= TokenAmount.MaxBaseUnits);
    }

    [Fact]
    public void The_nonce_may_be_any_unsigned_128_bit_value_and_the_deadline_any_block() {
        Address sender = Any.Address().NonZero().Generate();

        TransactionEnvelope envelope = new TransactionEnvelope(sender, UInt128.MaxValue, 21_000, TokenAmount.Zero, ulong.MaxValue);

        Assert.Equal(UInt128.MaxValue, envelope.Nonce);
        Assert.Equal(ulong.MaxValue, envelope.DeadlineBlock);
    }

    [Fact]
    public void A_null_sender_or_price_is_refused() {
        Address sender = Any.Address().NonZero().Generate();

        Assert.Throws<ArgumentNullException>(() => new TransactionEnvelope(null!, 0, 21_000, TokenAmount.Zero, 0));
        Assert.Throws<ArgumentNullException>(() => new TransactionEnvelope(sender, 0, 21_000, null!, 0));
    }

    [Fact]
    public void Two_envelopes_with_the_same_fields_are_equal_and_hash_alike() {
        Address sender = Any.Address().NonZero().Generate();

        TransactionEnvelope first  = new TransactionEnvelope(sender, 1, 50_000, new TokenAmount(2), 9);
        TransactionEnvelope second = new TransactionEnvelope(new Address(sender.Value), 1, 50_000, new TokenAmount(2), 9);

        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void An_envelope_differs_when_any_one_field_differs() {
        Address other  = Any.Address().NonZero().Generate();
        Address sender = Any.Address().NonZero().DifferentFromAddress(other).Generate();

        TransactionEnvelope baseline = new TransactionEnvelope(sender, 1, 50_000, new TokenAmount(2), 9);

        Assert.NotEqual(baseline, new TransactionEnvelope(other, 1, 50_000, new TokenAmount(2), 9));
        Assert.NotEqual(baseline, new TransactionEnvelope(sender, 2, 50_000, new TokenAmount(2), 9));
        Assert.NotEqual(baseline, new TransactionEnvelope(sender, 1, 50_001, new TokenAmount(2), 9));
        Assert.NotEqual(baseline, new TransactionEnvelope(sender, 1, 50_000, new TokenAmount(3), 9));
        Assert.NotEqual(baseline, new TransactionEnvelope(sender, 1, 50_000, new TokenAmount(2), 10));
        Assert.False(baseline.Equals(null));
    }

    [Fact]
    public void An_envelope_describes_itself() {
        TransactionEnvelope envelope = new TransactionEnvelope(new Address("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed"), 3, 50_000, new TokenAmount(2), 9);

        Assert.Equal("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed #3 (gas 50000 @ 2, until block 9)", envelope.ToString());
    }

}
