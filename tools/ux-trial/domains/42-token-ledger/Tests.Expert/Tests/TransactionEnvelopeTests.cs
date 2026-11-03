using System.Numerics;

using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class TransactionEnvelopeTests {

    // ---- construction ---------------------------------------------------------------------------------------------

    [Fact]
    public void An_envelope_keeps_what_it_was_given() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        TransactionEnvelope envelope = new TransactionEnvelope(anySender, 7, 50_000, Amount.Of(20), 1_000);

        // Assert
        Assert.Equal(anySender, envelope.Sender);
        Assert.Equal((UInt128)7, envelope.Nonce);
        Assert.Equal(50_000UL, envelope.GasLimit);
        Assert.Equal(Amount.Of(20), envelope.GasPrice);
        Assert.Equal(1_000UL, envelope.DeadlineBlock);
    }

    [Fact]
    public void The_extremes_of_the_nonce_and_of_the_deadline_are_accepted() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        TransactionEnvelope first = new TransactionEnvelope(anySender, UInt128.Zero, 50_000, Amount.Of(1), 0);
        TransactionEnvelope last  = new TransactionEnvelope(anySender, UInt128.MaxValue, 50_000, Amount.Of(1), ulong.MaxValue);

        // Assert
        Assert.Equal(UInt128.Zero, first.Nonce);
        Assert.Equal(0UL, first.DeadlineBlock);
        Assert.Equal(UInt128.MaxValue, last.Nonce);
        Assert.Equal(ulong.MaxValue, last.DeadlineBlock);
    }

    [Fact]
    public void Nobody_signs_for_the_zero_address() {
        Assert.Throws<ArgumentException>(() => new TransactionEnvelope(Address.Zero, 0, 50_000, Amount.Of(1), 10));
    }

    [Fact]
    public void A_missing_sender_or_gas_price_is_refused_as_a_missing_argument() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new TransactionEnvelope(null!, 0, 50_000, Amount.Of(1), 10));
        Assert.Throws<ArgumentNullException>(() => new TransactionEnvelope(anySender, 0, 50_000, null!, 10));
    }

    // ---- gas limit ------------------------------------------------------------------------------------------------

    [Fact]
    public void A_gas_limit_may_be_the_intrinsic_gas_or_a_whole_block() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Assert
        Assert.Equal(21_000UL, new TransactionEnvelope(anySender, 0, 21_000, Amount.Of(1), 10).GasLimit);
        Assert.Equal(30_000_000UL, new TransactionEnvelope(anySender, 0, 30_000_000, Amount.Of(1), 10).GasLimit);
    }

    [Fact]
    public void A_gas_limit_below_the_intrinsic_gas_is_refused() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionEnvelope(anySender, 0, 20_999, Amount.Of(1), 10));
    }

    [Fact]
    public void A_gas_limit_above_a_block_is_refused() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionEnvelope(anySender, 0, 30_000_001, Amount.Of(1), 10));
    }

    // ---- gas price and maximum fee --------------------------------------------------------------------------------

    [Fact]
    public void A_gas_price_of_zero_is_allowed_and_makes_the_maximum_fee_zero() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        TransactionEnvelope envelope = new TransactionEnvelope(anySender, 0, 50_000, TokenAmount.Zero, 10);

        // Assert
        Assert.Equal(TokenAmount.Zero, envelope.MaxFee);
    }

    [Fact]
    public void The_maximum_fee_is_the_whole_gas_limit_paid_at_the_gas_price() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        TransactionEnvelope envelope = new TransactionEnvelope(anySender, 0, 50_000, Amount.Of(20), 10);

        // Assert
        Assert.Equal(Amount.Of(1_000_000), envelope.MaxFee);
    }

    [Fact]
    public void The_maximum_fee_may_reach_exactly_the_largest_amount() {
        // Arrange: the largest price that fits under the limit.
        Address    anySender  = Any.Address().Generate();
        BigInteger bestPrice  = TokenAmount.MaxBaseUnits / 50_000;

        // Act
        TransactionEnvelope envelope = new TransactionEnvelope(anySender, 0, 50_000, Amount.Of(bestPrice), 10);

        // Assert
        Assert.True(envelope.MaxFee.BaseUnits <= TokenAmount.MaxBaseUnits);
        Assert.Equal(Amount.Of(bestPrice * 50_000), envelope.MaxFee);
    }

    [Fact]
    public void A_gas_price_one_unit_too_high_for_the_gas_limit_is_refused() {
        // Arrange
        Address    anySender = Any.Address().Generate();
        BigInteger tooHigh   = TokenAmount.MaxBaseUnits / 50_000 + 1;

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new TransactionEnvelope(anySender, 0, 50_000, Amount.Of(tooHigh), 10));
    }

    [Fact]
    public void The_largest_gas_price_is_refused_whatever_the_gas_limit() {
        // Arrange
        Address anySender   = Any.Address().Generate();
        ulong   anyGasLimit = Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new TransactionEnvelope(anySender, 0, anyGasLimit, TokenAmount.Max, 10));
    }

    // ---- equality -------------------------------------------------------------------------------------------------

    [Fact]
    public void Two_envelopes_with_the_same_five_facts_are_equal_and_share_their_hash_code() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        TransactionEnvelope first  = new TransactionEnvelope(anySender, 3, 50_000, Amount.Of(20), 100);
        TransactionEnvelope second = new TransactionEnvelope(anySender, 3, 50_000, Amount.Of(20), 100);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void An_envelope_differing_by_any_of_the_five_facts_is_not_equal() {
        // Arrange
        Address             anySender      = Any.Address().Generate();
        Address             anyOtherSender = Any.Address().DifferentFrom(anySender).Generate();
        TransactionEnvelope reference      = new TransactionEnvelope(anySender, 3, 50_000, Amount.Of(20), 100);

        // Assert
        Assert.False(reference.Equals(new TransactionEnvelope(anyOtherSender, 3, 50_000, Amount.Of(20), 100)));
        Assert.False(reference.Equals(new TransactionEnvelope(anySender, 4, 50_000, Amount.Of(20), 100)));
        Assert.False(reference.Equals(new TransactionEnvelope(anySender, 3, 50_001, Amount.Of(20), 100)));
        Assert.False(reference.Equals(new TransactionEnvelope(anySender, 3, 50_000, Amount.Of(21), 100)));
        Assert.False(reference.Equals(new TransactionEnvelope(anySender, 3, 50_000, Amount.Of(20), 101)));
        Assert.False(reference.Equals((TransactionEnvelope?)null));
    }

    [Fact]
    public void An_envelope_prints_who_signed_it_and_under_which_terms() {
        // Arrange
        Address sender = new Address("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed");

        // Act
        TransactionEnvelope envelope = new TransactionEnvelope(sender, 3, 50_000, Amount.Of(20), 100);

        // Assert
        Assert.Equal("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed #3 (gas 50000 @ 20, until block 100)", envelope.ToString());
    }

}
