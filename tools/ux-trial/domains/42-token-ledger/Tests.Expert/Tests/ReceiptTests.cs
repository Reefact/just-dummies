using System.Numerics;

using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class ReceiptTests {

    // ---- construction ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_receipt_keeps_what_it_was_given_and_derives_the_fee() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        Receipt receipt = new Receipt(TransactionKind.Transfer, anySender, 7, 1_200, 35_000, Amount.Of(10));

        // Assert
        Assert.Equal(TransactionKind.Transfer, receipt.Kind);
        Assert.Equal(anySender, receipt.Sender);
        Assert.Equal((UInt128)7, receipt.Nonce);
        Assert.Equal(1_200UL, receipt.BlockNumber);
        Assert.Equal(35_000UL, receipt.GasUsed);
        Assert.Equal(Amount.Of(10), receipt.GasPrice);
        Assert.Equal(Amount.Of(350_000), receipt.Fee);
    }

    [Fact]
    public void Every_defined_kind_of_transaction_is_accepted() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act / Assert
        foreach (TransactionKind kind in Enum.GetValues<TransactionKind>()) {
            Assert.Equal(kind, new Receipt(kind, anySender, 0, 0, 21_000, Amount.Of(1)).Kind);
        }
    }

    [Fact]
    public void A_kind_that_is_not_defined_is_refused() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Receipt((TransactionKind)99, anySender, 0, 0, 21_000, Amount.Of(1)));
    }

    [Fact]
    public void No_transaction_is_sent_from_the_zero_address() {
        Assert.Throws<ArgumentException>(() => new Receipt(TransactionKind.Transfer, Address.Zero, 0, 0, 21_000, Amount.Of(1)));
    }

    [Fact]
    public void A_missing_sender_or_gas_price_is_refused_as_a_missing_argument() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Receipt(TransactionKind.Transfer, null!, 0, 0, 21_000, Amount.Of(1)));
        Assert.Throws<ArgumentNullException>(() => new Receipt(TransactionKind.Transfer, anySender, 0, 0, 21_000, null!));
    }

    [Fact]
    public void The_extremes_of_the_nonce_and_of_the_block_number_are_accepted() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        Receipt receipt = new Receipt(TransactionKind.Burn, anySender, UInt128.MaxValue, ulong.MaxValue, 21_000, Amount.Of(1));

        // Assert
        Assert.Equal(UInt128.MaxValue, receipt.Nonce);
        Assert.Equal(ulong.MaxValue, receipt.BlockNumber);
    }

    // ---- gas used -------------------------------------------------------------------------------------------------

    [Fact]
    public void Gas_used_may_be_the_intrinsic_gas_or_a_whole_block() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Assert
        Assert.Equal(21_000UL, new Receipt(TransactionKind.Transfer, anySender, 0, 0, 21_000, Amount.Of(1)).GasUsed);
        Assert.Equal(30_000_000UL, new Receipt(TransactionKind.Transfer, anySender, 0, 0, 30_000_000, Amount.Of(1)).GasUsed);
    }

    [Fact]
    public void Gas_used_below_the_intrinsic_gas_is_refused() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Receipt(TransactionKind.Transfer, anySender, 0, 0, 20_999, Amount.Of(1)));
    }

    [Fact]
    public void Gas_used_above_a_block_is_refused() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Receipt(TransactionKind.Transfer, anySender, 0, 0, 30_000_001, Amount.Of(1)));
    }

    // ---- fee ------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_receipt_paying_nothing_per_gas_has_a_zero_fee() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        Receipt receipt = new Receipt(TransactionKind.Transfer, anySender, 0, 0, 35_000, TokenAmount.Zero);

        // Assert
        Assert.Equal(TokenAmount.Zero, receipt.Fee);
    }

    [Fact]
    public void A_fee_may_reach_the_largest_amount_without_passing_it() {
        // Arrange
        Address    anySender = Any.Address().Generate();
        BigInteger bestPrice = TokenAmount.MaxBaseUnits / 35_000;

        // Act
        Receipt receipt = new Receipt(TransactionKind.Transfer, anySender, 0, 0, 35_000, Amount.Of(bestPrice));

        // Assert
        Assert.Equal(Amount.Of(bestPrice * 35_000), receipt.Fee);
    }

    [Fact]
    public void A_gas_price_one_unit_too_high_for_the_gas_used_is_refused() {
        // Arrange
        Address    anySender = Any.Address().Generate();
        BigInteger tooHigh   = TokenAmount.MaxBaseUnits / 35_000 + 1;

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Receipt(TransactionKind.Transfer, anySender, 0, 0, 35_000, Amount.Of(tooHigh)));
    }

    // ---- equality -------------------------------------------------------------------------------------------------

    [Fact]
    public void Two_receipts_with_the_same_six_facts_are_equal_and_share_their_hash_code() {
        // Arrange
        Address anySender = Any.Address().Generate();

        // Act
        Receipt first  = new Receipt(TransactionKind.Mint, anySender, 2, 50, 40_000, Amount.Of(5));
        Receipt second = new Receipt(TransactionKind.Mint, anySender, 2, 50, 40_000, Amount.Of(5));

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_receipt_differing_by_any_of_the_six_facts_is_not_equal() {
        // Arrange
        Address anySender      = Any.Address().Generate();
        Address anyOtherSender = Any.Address().DifferentFrom(anySender).Generate();
        Receipt reference      = new Receipt(TransactionKind.Mint, anySender, 2, 50, 40_000, Amount.Of(5));

        // Assert
        Assert.False(reference.Equals(new Receipt(TransactionKind.Burn, anySender, 2, 50, 40_000, Amount.Of(5))));
        Assert.False(reference.Equals(new Receipt(TransactionKind.Mint, anyOtherSender, 2, 50, 40_000, Amount.Of(5))));
        Assert.False(reference.Equals(new Receipt(TransactionKind.Mint, anySender, 3, 50, 40_000, Amount.Of(5))));
        Assert.False(reference.Equals(new Receipt(TransactionKind.Mint, anySender, 2, 51, 40_000, Amount.Of(5))));
        Assert.False(reference.Equals(new Receipt(TransactionKind.Mint, anySender, 2, 50, 40_001, Amount.Of(5))));
        Assert.False(reference.Equals(new Receipt(TransactionKind.Mint, anySender, 2, 50, 40_000, Amount.Of(6))));
        Assert.False(reference.Equals((Receipt?)null));
    }

    [Fact]
    public void A_receipt_prints_its_kind_sender_block_gas_and_fee() {
        // Arrange
        Address sender = new Address("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed");

        // Act
        Receipt receipt = new Receipt(TransactionKind.Transfer, sender, 7, 1_200, 35_000, Amount.Of(10));

        // Assert
        Assert.Equal("Transfer by 0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed #7 in block 1200: 35000 gas, fee 350000", receipt.ToString());
    }

}
