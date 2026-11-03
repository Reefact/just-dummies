using System.Numerics;

using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class TokenAmountTests {

    private static readonly BigInteger MaxUnits = (BigInteger.One << 256) - 1;

    // ---- construction ---------------------------------------------------------------------------------------------

    [Fact]
    public void An_amount_can_be_zero() {
        Assert.True(Amount.Of(0).IsZero);
    }

    [Fact]
    public void An_amount_can_be_two_to_the_256_minus_one_and_no_more() {
        // Act
        TokenAmount largest = Amount.Of(MaxUnits);

        // Assert
        Assert.Equal(MaxUnits, largest.BaseUnits);
        Assert.Equal(largest, TokenAmount.Max);
        Assert.Equal(MaxUnits, TokenAmount.MaxBaseUnits);
    }

    [Fact]
    public void One_base_unit_below_zero_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Amount.Of(-1));
    }

    [Fact]
    public void One_base_unit_above_two_to_the_256_minus_one_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Amount.Of(MaxUnits + 1));
    }

    [Fact]
    public void Any_negative_number_of_base_units_is_refused() {
        // Arrange
        BigInteger anyNegative = new AnyBigInteger().Between(-BigInteger.Pow(10, 80), -1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Amount.Of(anyNegative));
    }

    [Fact]
    public void Any_number_of_base_units_past_the_256_bit_word_is_refused() {
        // Arrange
        BigInteger anyTooLarge = new AnyBigInteger().Between(MaxUnits + 1, MaxUnits * 1000).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Amount.Of(anyTooLarge));
    }

    [Fact]
    public void Any_number_of_base_units_within_the_word_is_accepted() {
        // Arrange
        BigInteger anyUnits = new AnyBigInteger().Between(0, MaxUnits).Generate();

        // Act / Assert
        Assert.Null(Record.Exception(() => Amount.Of(anyUnits)));
    }

    // ---- addition --------------------------------------------------------------------------------------------------

    [Fact]
    public void Adding_two_amounts_sums_their_base_units() {
        Assert.Equal(Amount.Of(5), Amount.Of(2).Add(Amount.Of(3)));
    }

    [Fact]
    public void Adding_may_land_exactly_on_the_largest_amount() {
        Assert.Equal(TokenAmount.Max, Amount.Of(MaxUnits - 1).Add(Amount.Of(1)));
    }

    [Fact]
    public void Adding_past_the_largest_amount_overflows_instead_of_wrapping() {
        Assert.Throws<OverflowException>(() => TokenAmount.Max.Add(Amount.Of(1)));
    }

    [Fact]
    public void Adding_nothing_is_refused_as_a_missing_argument() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyAmount.Add(null!));
    }

    [Fact]
    public void Adding_leaves_both_operands_as_they_were() {
        // Arrange
        TokenAmount five  = Amount.Of(5);
        TokenAmount seven = Amount.Of(7);

        // Act
        TokenAmount twelve = five.Add(seven);

        // Assert
        Assert.Equal(Amount.Of(12), twelve);
        Assert.Equal(Amount.Of(5), five);
        Assert.Equal(Amount.Of(7), seven);
    }

    // ---- subtraction -----------------------------------------------------------------------------------------------

    [Fact]
    public void Subtracting_takes_base_units_away() {
        Assert.Equal(Amount.Of(2), Amount.Of(5).Subtract(Amount.Of(3)));
    }

    [Fact]
    public void Subtracting_an_amount_from_itself_gives_zero() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Act
        TokenAmount remainder = anyAmount.Subtract(anyAmount);

        // Assert
        Assert.Equal(TokenAmount.Zero, remainder);
    }

    [Fact]
    public void Subtracting_one_base_unit_more_than_held_overflows_below_zero() {
        Assert.Throws<OverflowException>(() => Amount.Of(5).Subtract(Amount.Of(6)));
    }

    [Fact]
    public void Subtracting_nothing_is_refused_as_a_missing_argument() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyAmount.Subtract(null!));
    }

    // ---- multiplication --------------------------------------------------------------------------------------------

    [Fact]
    public void Multiplying_scales_the_base_units_by_an_unsigned_factor() {
        Assert.Equal(Amount.Of(42), Amount.Of(7).MultiplyBy(6));
    }

    [Fact]
    public void Multiplying_by_zero_gives_zero() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Act
        TokenAmount product = anyAmount.MultiplyBy(0);

        // Assert
        Assert.Equal(TokenAmount.Zero, product);
    }

    [Fact]
    public void Multiplying_by_one_keeps_the_largest_amount() {
        Assert.Equal(TokenAmount.Max, TokenAmount.Max.MultiplyBy(1));
    }

    [Fact]
    public void Multiplying_the_largest_amount_by_two_overflows() {
        Assert.Throws<OverflowException>(() => TokenAmount.Max.MultiplyBy(2));
    }

    [Fact]
    public void Multiplying_may_land_exactly_on_the_largest_amount() {
        // 2^256 - 1 = (2^64 - 1) x (2^192 + 2^128 + 2^64 + 1)
        BigInteger cofactor = (BigInteger.One << 192) + (BigInteger.One << 128) + (BigInteger.One << 64) + 1;

        Assert.Equal(TokenAmount.Max, Amount.Of(cofactor).MultiplyBy(ulong.MaxValue));
    }

    [Fact]
    public void Multiplying_by_the_largest_factor_overflows_once_the_amount_is_one_unit_too_large_for_it() {
        // Arrange
        BigInteger cofactor = (BigInteger.One << 192) + (BigInteger.One << 128) + (BigInteger.One << 64) + 1;

        // Act / Assert
        Assert.Throws<OverflowException>(() => Amount.Of(cofactor + 1).MultiplyBy(ulong.MaxValue));
    }

    // ---- CanAdd ----------------------------------------------------------------------------------------------------

    [Fact]
    public void An_amount_can_add_another_when_the_sum_stays_within_the_word() {
        Assert.True(Amount.Of(MaxUnits - 1).CanAdd(Amount.Of(1)));
    }

    [Fact]
    public void An_amount_cannot_add_another_when_the_sum_passes_the_word() {
        Assert.False(TokenAmount.Max.CanAdd(Amount.Of(1)));
    }

    [Fact]
    public void Asking_whether_nothing_can_be_added_is_refused_as_a_missing_argument() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyAmount.CanAdd(null!));
    }

    // ---- comparison, equality, text --------------------------------------------------------------------------------

    [Fact]
    public void Amounts_order_by_their_base_units() {
        Assert.True(Amount.Of(1).CompareTo(Amount.Of(2)) < 0);
        Assert.True(Amount.Of(2).CompareTo(Amount.Of(1)) > 0);
        Assert.Equal(0, Amount.Of(2).CompareTo(Amount.Of(2)));
    }

    [Fact]
    public void Every_amount_is_greater_than_nothing() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Assert
        Assert.True(anyAmount.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_amounts_of_the_same_base_units_are_equal_and_share_their_hash_code() {
        // Arrange
        BigInteger anyUnits = new AnyBigInteger().Between(0, MaxUnits).Generate();

        // Act
        TokenAmount first  = Amount.Of(anyUnits);
        TokenAmount second = Amount.Of(anyUnits);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void An_amount_is_not_equal_to_one_with_other_base_units() {
        Assert.False(Amount.Of(1).Equals(Amount.Of(2)));
    }

    [Fact]
    public void An_amount_is_not_equal_to_nothing() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Assert
        Assert.False(anyAmount.Equals((TokenAmount?)null));
        Assert.False(anyAmount.Equals((object?)null));
    }

    [Fact]
    public void An_amount_prints_its_base_units_as_plain_invariant_digits() {
        Assert.Equal("1234567890123456789012345678901234567890", Amount.Of(BigInteger.Parse("1234567890123456789012345678901234567890")).ToString());
        Assert.Equal("0", TokenAmount.Zero.ToString());
    }

}
