namespace TokenLedger.Domain.Tests.Regular;

public sealed class TokenAmountTests {

    private static readonly BigInteger Max = (BigInteger.One << 256) - 1;

    [Fact]
    public void The_largest_amount_is_two_to_the_256_minus_one() {
        Assert.Equal(Max, TokenAmount.MaxBaseUnits);
        Assert.Equal(Max, TokenAmount.Max.BaseUnits);
    }

    [Fact]
    public void Zero_is_the_empty_amount() {
        Assert.True(TokenAmount.Zero.IsZero);
        Assert.Equal(BigInteger.Zero, TokenAmount.Zero.BaseUnits);
    }

    [Fact]
    public void Both_ends_of_the_range_are_valid_amounts() {
        Assert.Equal(BigInteger.Zero, new TokenAmount(BigInteger.Zero).BaseUnits);
        Assert.Equal(Max, new TokenAmount(Max).BaseUnits);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenAmount(BigInteger.MinusOne));
    }

    [Fact]
    public void An_amount_past_the_largest_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenAmount(Max + 1));
    }

    [Fact]
    public void Adding_two_amounts_gives_their_sum() {
        // Arrange
        TokenAmount hundred = new TokenAmount(100);

        // Act
        TokenAmount sum = hundred.Add(new TokenAmount(23));

        // Assert
        Assert.Equal(new BigInteger(123), sum.BaseUnits);
    }

    [Fact]
    public void Adding_up_to_exactly_the_largest_amount_is_accepted() {
        // Act
        TokenAmount sum = new TokenAmount(Max - 5).Add(new TokenAmount(5));

        // Assert
        Assert.Equal(TokenAmount.Max, sum);
    }

    [Fact]
    public void Adding_past_the_largest_amount_overflows_instead_of_wrapping() {
        // Arrange
        TokenAmount one = new TokenAmount(1);

        // Act & Assert
        Assert.Throws<OverflowException>(() => TokenAmount.Max.Add(one));
    }

    [Fact]
    public void Subtracting_gives_the_difference() {
        Assert.Equal(new BigInteger(77), new TokenAmount(100).Subtract(new TokenAmount(23)).BaseUnits);
    }

    [Fact]
    public void Subtracting_an_amount_from_itself_gives_zero() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Act
        TokenAmount difference = anyAmount.Subtract(anyAmount);

        // Assert
        Assert.True(difference.IsZero);
    }

    [Fact]
    public void Subtracting_below_zero_overflows_instead_of_wrapping() {
        Assert.Throws<OverflowException>(() => new TokenAmount(5).Subtract(new TokenAmount(6)));
    }

    [Fact]
    public void Multiplying_by_a_factor_gives_the_product() {
        Assert.Equal(new BigInteger(3_000_000), new TokenAmount(1_000).MultiplyBy(3_000).BaseUnits);
    }

    [Fact]
    public void Multiplying_by_zero_gives_zero() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Act
        TokenAmount product = anyAmount.MultiplyBy(0);

        // Assert
        Assert.True(product.IsZero);
    }

    [Fact]
    public void Multiplying_past_the_largest_amount_overflows() {
        Assert.Throws<OverflowException>(() => TokenAmount.Max.MultiplyBy(2));
    }

    [Fact]
    public void Multiplying_up_to_exactly_the_largest_amount_is_accepted() {
        // Arrange
        TokenAmount third = new TokenAmount(Max / 3);

        // Act
        TokenAmount product = third.MultiplyBy(3);

        // Assert
        Assert.Equal(Max, product.BaseUnits);
    }

    [Fact]
    public void An_amount_that_still_fits_can_be_added() {
        Assert.True(new TokenAmount(Max - 10).CanAdd(new TokenAmount(10)));
    }

    [Fact]
    public void An_amount_that_would_overflow_cannot_be_added() {
        Assert.False(new TokenAmount(Max - 10).CanAdd(new TokenAmount(11)));
    }

    [Fact]
    public void Amounts_compare_by_their_base_units() {
        // Arrange
        TokenAmount small = new TokenAmount(5);
        TokenAmount big   = new TokenAmount(500);

        // Assert
        Assert.True(small.CompareTo(big) < 0);
        Assert.True(big.CompareTo(small) > 0);
        Assert.Equal(0, small.CompareTo(new TokenAmount(5)));
        Assert.True(small.CompareTo(null) > 0);
    }

    [Fact]
    public void Amounts_with_the_same_base_units_are_equal() {
        // Arrange
        BigInteger anyBaseUnits = Any.TokenAmount().Generate().BaseUnits;

        // Act
        TokenAmount first  = new TokenAmount(anyBaseUnits);
        TokenAmount second = new TokenAmount(anyBaseUnits);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void An_amount_is_written_as_its_base_units() {
        Assert.Equal("1234567890123456789012345678901234567890", new TokenAmount(BigInteger.Parse("1234567890123456789012345678901234567890")).ToString());
    }

    [Fact]
    public void Arithmetic_refuses_a_null_operand() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyAmount.Add(null!));
        Assert.Throws<ArgumentNullException>(() => anyAmount.Subtract(null!));
        Assert.Throws<ArgumentNullException>(() => anyAmount.CanAdd(null!));
    }

}
