namespace TokenLedger.Domain.Tests.Regular;

public sealed class TransactionEnvelopeTests {

    [Fact]
    public void An_envelope_exposes_what_it_was_built_with() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        TransactionEnvelope envelope = new TransactionEnvelope(anySender, 7, 50_000, new TokenAmount(3), 1_000);

        // Assert
        Assert.Equal(anySender, envelope.Sender);
        Assert.Equal((UInt128)7, envelope.Nonce);
        Assert.Equal(50_000UL, envelope.GasLimit);
        Assert.Equal(new TokenAmount(3), envelope.GasPrice);
        Assert.Equal(1_000UL, envelope.DeadlineBlock);
    }

    [Fact]
    public void Nobody_signs_for_the_zero_address() {
        Assert.Throws<ArgumentException>(() => Any.TransactionEnvelope().WithSender(Address.Zero).Generate());
    }

    [Theory]
    [InlineData(20_999UL)]
    [InlineData(30_000_001UL)]
    [InlineData(0UL)]
    [InlineData(ulong.MaxValue)]
    public void A_gas_limit_outside_the_intrinsic_gas_and_the_block_limit_is_refused(ulong gasLimit) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.TransactionEnvelope().WithGasLimit(gasLimit).Generate());
    }

    [Theory]
    [InlineData(21_000UL)]
    [InlineData(30_000_000UL)]
    public void A_gas_limit_at_either_end_of_the_range_is_accepted(ulong gasLimit) {
        // Act
        TransactionEnvelope envelope = Any.TransactionEnvelope().WithGasLimit(gasLimit).Generate();

        // Assert
        Assert.Equal(gasLimit, envelope.GasLimit);
    }

    [Fact]
    public void A_free_gas_price_is_allowed() {
        // Act
        TransactionEnvelope envelope = Any.TransactionEnvelope().WithGasPrice(TokenAmount.Zero).Generate();

        // Assert
        Assert.True(envelope.GasPrice.IsZero);
        Assert.True(envelope.MaxFee.IsZero);
    }

    [Fact]
    public void The_maximum_fee_is_the_gas_limit_times_the_gas_price() {
        // Act
        TransactionEnvelope envelope = Any.TransactionEnvelope().WithGasLimit(60_000).WithGasPrice(new TokenAmount(25)).Generate();

        // Assert
        Assert.Equal(new TokenAmount(1_500_000), envelope.MaxFee);
    }

    [Fact]
    public void A_maximum_fee_beyond_the_largest_amount_is_refused() {
        // Arrange: the largest price a limit allows is the integer quotient of 2^256 - 1 by that limit.
        ulong      limit        = 1_000_000;
        BigInteger largestPrice = TokenAmount.MaxBaseUnits / limit;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.TransactionEnvelope().WithGasLimit(limit).WithGasPrice(new TokenAmount(largestPrice + 1)).Generate());
    }

    [Fact]
    public void A_maximum_fee_of_exactly_the_largest_amount_is_accepted() {
        // Arrange: the largest price a limit allows is the integer quotient of 2^256 - 1 by that limit.
        ulong      limit        = 1_000_000;
        BigInteger largestPrice = TokenAmount.MaxBaseUnits / limit;

        // Act
        TransactionEnvelope envelope = Any.TransactionEnvelope().WithGasLimit(limit).WithGasPrice(new TokenAmount(largestPrice)).Generate();

        // Assert
        Assert.Equal(new BigInteger(limit) * largestPrice, envelope.MaxFee.BaseUnits);
    }

    [Fact]
    public void An_envelope_refuses_null_arguments() {
        // Arrange
        Address     anySender   = Any.Address().Generate();
        TokenAmount anyGasPrice = Any.TokenAmount().WithBaseUnits(b => b.Between(0, 1_000)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TransactionEnvelope(null!, 0, 21_000, anyGasPrice, 10));
        Assert.Throws<ArgumentNullException>(() => new TransactionEnvelope(anySender, 0, 21_000, null!, 10));
    }

    [Fact]
    public void Envelopes_with_the_same_content_are_equal() {
        // Arrange
        TransactionEnvelope anyEnvelope = Any.TransactionEnvelope().Generate();

        // Act
        TransactionEnvelope copy = new TransactionEnvelope(anyEnvelope.Sender, anyEnvelope.Nonce, anyEnvelope.GasLimit, anyEnvelope.GasPrice, anyEnvelope.DeadlineBlock);

        // Assert
        Assert.Equal(anyEnvelope, copy);
        Assert.Equal(anyEnvelope.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Envelopes_differing_in_one_part_are_not_equal() {
        // Arrange
        Address     anySender   = Any.Address().Generate();
        Address     anyOther    = Any.Address().Generate();
        TransactionEnvelope reference = new TransactionEnvelope(anySender, 1, 30_000, new TokenAmount(5), 100);

        // Assert
        Assert.NotEqual(reference, new TransactionEnvelope(anyOther, 1, 30_000, new TokenAmount(5), 100));
        Assert.NotEqual(reference, new TransactionEnvelope(anySender, 2, 30_000, new TokenAmount(5), 100));
        Assert.NotEqual(reference, new TransactionEnvelope(anySender, 1, 30_001, new TokenAmount(5), 100));
        Assert.NotEqual(reference, new TransactionEnvelope(anySender, 1, 30_000, new TokenAmount(6), 100));
        Assert.NotEqual(reference, new TransactionEnvelope(anySender, 1, 30_000, new TokenAmount(5), 101));
    }

    [Fact]
    public void An_envelope_is_described_by_sender_nonce_gas_and_deadline() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        TransactionEnvelope envelope = new TransactionEnvelope(anySender, 4, 50_000, new TokenAmount(2), 900);

        // Assert
        Assert.Equal($"{anySender} #4 (gas 50000 @ 2, until block 900)", envelope.ToString());
    }

}
