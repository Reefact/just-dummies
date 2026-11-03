namespace TokenLedger.Domain.Tests.Regular;

public sealed class ReceiptTests {

    [Fact]
    public void A_receipt_exposes_what_it_was_built_with() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        Receipt receipt = new Receipt(TransactionKind.Burn, anySender, 9, 120, 32_000, new TokenAmount(4));

        // Assert
        Assert.Equal(TransactionKind.Burn, receipt.Kind);
        Assert.Equal(anySender, receipt.Sender);
        Assert.Equal((UInt128)9, receipt.Nonce);
        Assert.Equal(120UL, receipt.BlockNumber);
        Assert.Equal(32_000UL, receipt.GasUsed);
        Assert.Equal(new TokenAmount(4), receipt.GasPrice);
    }

    [Fact]
    public void The_fee_is_the_gas_used_times_the_gas_price() {
        // Act
        Receipt receipt = Any.Receipt().WithGasUsed(35_000).WithGasPrice(new TokenAmount(3)).Generate();

        // Assert
        Assert.Equal(new TokenAmount(105_000), receipt.Fee);
    }

    [Fact]
    public void A_free_transaction_pays_no_fee() {
        // Act
        Receipt receipt = Any.Receipt().WithGasPrice(TokenAmount.Zero).Generate();

        // Assert
        Assert.True(receipt.Fee.IsZero);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(99)]
    [InlineData(-1)]
    public void An_undefined_kind_is_refused(int kind) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Receipt().WithKind((TransactionKind)kind).Generate());
    }

    [Fact]
    public void Every_defined_kind_is_accepted() {
        foreach (TransactionKind kind in Enum.GetValues<TransactionKind>()) {
            // Act
            Receipt receipt = Any.Receipt().WithKind(kind).Generate();

            // Assert
            Assert.Equal(kind, receipt.Kind);
        }
    }

    [Fact]
    public void No_transaction_is_sent_from_the_zero_address() {
        Assert.Throws<ArgumentException>(() => Any.Receipt().WithSender(Address.Zero).Generate());
    }

    [Theory]
    [InlineData(20_999UL)]
    [InlineData(30_000_001UL)]
    [InlineData(0UL)]
    public void Gas_used_outside_the_intrinsic_gas_and_the_block_limit_is_refused(ulong gasUsed) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Receipt().WithGasUsed(gasUsed).Generate());
    }

    [Theory]
    [InlineData(21_000UL)]
    [InlineData(30_000_000UL)]
    public void Gas_used_at_either_end_of_the_range_is_accepted(ulong gasUsed) {
        Assert.Equal(gasUsed, Any.Receipt().WithGasUsed(gasUsed).Generate().GasUsed);
    }

    [Fact]
    public void A_fee_beyond_the_largest_amount_is_refused() {
        // Arrange
        ulong      gasUsed      = 1_000_000;
        BigInteger largestPrice = TokenAmount.MaxBaseUnits / gasUsed;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.Receipt().WithGasUsed(gasUsed).WithGasPrice(new TokenAmount(largestPrice + 1)).Generate());
    }

    [Fact]
    public void The_largest_fee_that_fits_is_accepted() {
        // Arrange
        ulong      gasUsed      = 1_000_000;
        BigInteger largestPrice = TokenAmount.MaxBaseUnits / gasUsed;

        // Act
        Receipt receipt = Any.Receipt().WithGasUsed(gasUsed).WithGasPrice(new TokenAmount(largestPrice)).Generate();

        // Assert
        Assert.Equal(largestPrice * gasUsed, receipt.Fee.BaseUnits);
    }

    [Fact]
    public void A_receipt_refuses_null_arguments() {
        // Arrange
        Address     anySender   = Any.Address().Generate();
        TokenAmount anyGasPrice = TokenAmount.Zero;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Receipt(TransactionKind.Transfer, null!, 0, 0, 21_000, anyGasPrice));
        Assert.Throws<ArgumentNullException>(() => new Receipt(TransactionKind.Transfer, anySender, 0, 0, 21_000, null!));
    }

    [Fact]
    public void Receipts_with_the_same_content_are_equal() {
        // Arrange
        Receipt anyReceipt = Any.Receipt().Generate();

        // Act
        Receipt copy = new Receipt(anyReceipt.Kind, anyReceipt.Sender, anyReceipt.Nonce, anyReceipt.BlockNumber, anyReceipt.GasUsed, anyReceipt.GasPrice);

        // Assert
        Assert.Equal(anyReceipt, copy);
        Assert.Equal(anyReceipt.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Receipts_differing_in_one_part_are_not_equal() {
        // Arrange
        Address anySender = Any.Address().Generate();
        Address anyOther  = Any.Address().Generate();
        Receipt reference = new Receipt(TransactionKind.Transfer, anySender, 1, 10, 35_000, new TokenAmount(2));

        // Assert
        Assert.NotEqual(reference, new Receipt(TransactionKind.Burn, anySender, 1, 10, 35_000, new TokenAmount(2)));
        Assert.NotEqual(reference, new Receipt(TransactionKind.Transfer, anyOther, 1, 10, 35_000, new TokenAmount(2)));
        Assert.NotEqual(reference, new Receipt(TransactionKind.Transfer, anySender, 2, 10, 35_000, new TokenAmount(2)));
        Assert.NotEqual(reference, new Receipt(TransactionKind.Transfer, anySender, 1, 11, 35_000, new TokenAmount(2)));
        Assert.NotEqual(reference, new Receipt(TransactionKind.Transfer, anySender, 1, 10, 35_001, new TokenAmount(2)));
        Assert.NotEqual(reference, new Receipt(TransactionKind.Transfer, anySender, 1, 10, 35_000, new TokenAmount(3)));
    }

    [Fact]
    public void A_receipt_is_described_by_kind_sender_nonce_block_gas_and_fee() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        Receipt receipt = new Receipt(TransactionKind.Mint, anySender, 3, 77, 40_000, new TokenAmount(2));

        // Assert
        Assert.Equal($"Mint by {anySender} #3 in block 77: 40000 gas, fee 80000", receipt.ToString());
    }

}
