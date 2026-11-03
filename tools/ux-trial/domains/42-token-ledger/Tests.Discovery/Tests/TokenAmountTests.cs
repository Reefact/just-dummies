using System.Numerics;

using JustDummies;

using TokenLedger.Domain;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class TokenAmountTests {

    private static readonly BigInteger Max = (BigInteger.One << 256) - 1;

    [Fact]
    public void The_largest_amount_is_two_to_the_256_minus_one() {
        Assert.Equal(Max, TokenAmount.MaxBaseUnits);
        Assert.Equal(Max, TokenAmount.Max.BaseUnits);
        Assert.Equal(BigInteger.Zero, TokenAmount.Zero.BaseUnits);
        Assert.True(TokenAmount.Zero.IsZero);
    }

    [Fact]
    public void An_amount_inside_the_range_keeps_its_base_units() {
        // The value is what the test is about, so it stays a literal.
        BigInteger units = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935") - 12345;

        TokenAmount amount = new TokenAmount(units);

        Assert.Equal(units, amount.BaseUnits);
        Assert.False(amount.IsZero);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        BigInteger anyNegative = Any.BigInteger().Between(-Max, -1).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TokenAmount(anyNegative));

        // Assert
        Assert.Equal("baseUnits", refusal.ParamName);
    }

    [Fact]
    public void An_amount_beyond_two_to_the_256_minus_one_is_refused() {
        // Arrange
        BigInteger anyExcess = Any.BigInteger().Between(1, BigInteger.One << 200).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenAmount(Max + anyExcess));
    }

    [Fact]
    public void The_edges_of_the_range_are_accepted() {
        Assert.Equal(BigInteger.Zero, new TokenAmount(0).BaseUnits);
        Assert.Equal(Max, new TokenAmount(Max).BaseUnits);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenAmount(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenAmount(Max + 1));
    }

    [Fact]
    public void Adding_two_amounts_gives_their_sum() {
        TokenAmount sum = new TokenAmount(40).Add(new TokenAmount(2));

        Assert.Equal(new TokenAmount(42), sum);
    }

    [Fact]
    public void Adding_up_to_exactly_the_maximum_is_allowed() {
        TokenAmount sum = new TokenAmount(Max - 5).Add(new TokenAmount(5));

        Assert.Equal(TokenAmount.Max, sum);
    }

    [Fact]
    public void Adding_past_the_maximum_overflows_rather_than_wrapping() {
        // Arrange: any amount of at least one base unit.
        TokenAmount anyPositive = Any.TokenAmount().WithBaseUnits(units => units.AtLeast(1)).Generate();

        // Act / Assert
        Assert.Throws<OverflowException>(() => TokenAmount.Max.Add(anyPositive));
    }

    [Fact]
    public void Adding_zero_to_the_maximum_is_not_an_overflow() {
        Assert.Equal(TokenAmount.Max, TokenAmount.Max.Add(TokenAmount.Zero));
    }

    [Fact]
    public void Subtracting_gives_the_difference() {
        TokenAmount difference = new TokenAmount(100).Subtract(new TokenAmount(58));

        Assert.Equal(new TokenAmount(42), difference);
    }

    [Fact]
    public void Subtracting_below_zero_overflows_rather_than_going_negative() {
        // Arrange
        TokenAmount anySmall = Any.TokenAmount().WithBaseUnits(units => units.AtMost(Max - 1)).Generate();
        TokenAmount larger   = anySmall.Add(new TokenAmount(1));

        // Act / Assert
        Assert.Throws<OverflowException>(() => anySmall.Subtract(larger));
    }

    [Fact]
    public void Subtracting_to_exactly_zero_is_allowed() {
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        Assert.True(anyAmount.Subtract(anyAmount).IsZero);
    }

    [Fact]
    public void Multiplying_scales_the_amount() {
        Assert.Equal(new TokenAmount(1_000_000), new TokenAmount(1_000).MultiplyBy(1_000));
    }

    [Fact]
    public void Multiplying_by_zero_gives_zero_whatever_the_amount() {
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        Assert.True(anyAmount.MultiplyBy(0).IsZero);
    }

    [Fact]
    public void Multiplying_past_the_maximum_overflows() {
        // Arrange
        ulong anyFactor = Any.UInt64().GreaterThanOrEqualTo(2).Generate();

        // Act / Assert
        Assert.Throws<OverflowException>(() => TokenAmount.Max.MultiplyBy(anyFactor));
    }

    [Fact]
    public void Multiplying_by_a_factor_of_64_bits_stays_exact() {
        TokenAmount product = new TokenAmount(Max / ulong.MaxValue).MultiplyBy(ulong.MaxValue);

        Assert.True(product.BaseUnits <= Max);
        Assert.Equal(Max / ulong.MaxValue * ulong.MaxValue, product.BaseUnits);
    }

    [Fact]
    public void An_amount_can_tell_whether_adding_another_stays_in_range() {
        Assert.True(new TokenAmount(Max - 1).CanAdd(new TokenAmount(1)));
        Assert.False(new TokenAmount(Max - 1).CanAdd(new TokenAmount(2)));
        Assert.True(TokenAmount.Max.CanAdd(TokenAmount.Zero));
        Assert.False(TokenAmount.Max.CanAdd(new TokenAmount(1)));
    }

    [Fact]
    public void Amounts_are_ordered_by_their_base_units() {
        TokenAmount smaller = new TokenAmount(7);
        TokenAmount larger  = new TokenAmount(8);

        Assert.True(smaller.CompareTo(larger) < 0);
        Assert.True(larger.CompareTo(smaller) > 0);
        Assert.Equal(0, smaller.CompareTo(new TokenAmount(7)));
    }

    [Fact]
    public void Any_amount_is_greater_than_nothing() {
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        Assert.True(anyAmount.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_amounts_of_the_same_base_units_are_equal_and_hash_alike() {
        // Arrange
        BigInteger anyUnits = Any.BigInteger().Between(0, Max).Generate();

        // Act
        TokenAmount first  = new TokenAmount(anyUnits);
        TokenAmount second = new TokenAmount(anyUnits);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void An_amount_is_not_equal_to_null_or_to_another_amount() {
        TokenAmount anyAmount = Any.TokenAmount().WithBaseUnits(units => units.AtMost(Max - 1)).Generate();

        Assert.False(anyAmount.Equals(null));
        Assert.NotEqual(anyAmount, anyAmount.Add(new TokenAmount(1)));
    }

    [Fact]
    public void An_amount_prints_as_its_decimal_base_units() {
        Assert.Equal("1234567890123456789012345678901234567890", new TokenAmount(BigInteger.Parse("1234567890123456789012345678901234567890")).ToString());
        Assert.Equal("0", TokenAmount.Zero.ToString());
    }

    [Fact]
    public void Arithmetic_refuses_a_null_operand() {
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        Assert.Throws<ArgumentNullException>(() => anyAmount.Add(null!));
        Assert.Throws<ArgumentNullException>(() => anyAmount.Subtract(null!));
        Assert.Throws<ArgumentNullException>(() => anyAmount.CanAdd(null!));
    }

}
