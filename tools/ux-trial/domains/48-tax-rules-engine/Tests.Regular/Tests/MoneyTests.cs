using JustDummies;

using Xunit;

namespace TaxRules.Domain.Tests;

public sealed class MoneyTests {

    private static AnyDecimal AnyAmount() {
        return Any.Decimal().Between(0m, 1_000_000m).WithScale(2);
    }

    [Fact]
    public void An_amount_in_range_expressed_to_the_cent_is_kept_as_it_is() {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0m, Money.MaxAmount).WithScale(2).Generate();

        // Act
        Money money = new Money(anyAmount);

        // Assert
        Assert.Equal(anyAmount, money.Amount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.01")]
    [InlineData("999999999999.99")]
    public void The_limits_of_the_range_are_accepted(string amount) {
        Money money = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), money.Amount);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        decimal anyNegative = Any.Decimal().Between(-1_000_000m, -0.01m).WithScale(2).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Money(anyNegative));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void An_amount_above_the_maximum_is_refused() {
        // Arrange
        decimal anyExcess = Any.Decimal().Between(Money.MaxAmount + 0.01m, Money.MaxAmount * 2).WithScale(2).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Money(anyExcess));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(thrown);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1000000000000.00")]
    [InlineData("999999999999.995")]
    public void Just_outside_the_range_is_refused(string amount) {
        Assert.ThrowsAny<ArgumentException>(() => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1.005")]
    [InlineData("12.345")]
    [InlineData("0.1234")]
    public void A_sub_cent_amount_is_refused_not_rounded(string amount) {
        Exception? thrown = Record.Exception(() => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Trailing_zeros_beyond_the_cent_are_not_a_sub_cent_amount() {
        Money money = new Money(12.50000m);

        Assert.Equal(12.5m, money.Amount);
    }

    [Theory]
    [InlineData("2.005", "2.01")]
    [InlineData("2.004", "2.00")]
    [InlineData("0.005", "0.01")]
    [InlineData("2.015", "2.02")]
    [InlineData("1.995", "2.00")]
    [InlineData("0.004", "0.00")]
    public void Rounding_goes_to_the_cent_half_away_from_zero(string raw, string expected) {
        Money rounded = Money.RoundedFrom(decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), rounded.Amount);
    }

    [Fact]
    public void Rounding_a_negative_amount_that_stays_negative_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.RoundedFrom(-0.005m));
    }

    [Fact]
    public void Rounding_an_amount_that_is_already_at_the_cent_changes_nothing() {
        // Arrange
        decimal anyAmount = AnyAmount().Generate();

        // Act
        Money rounded = Money.RoundedFrom(anyAmount);

        // Assert
        Assert.Equal(anyAmount, rounded.Amount);
    }

    [Fact]
    public void Zero_is_a_zero_amount() {
        Assert.True(Money.Zero.IsZero);
        Assert.Equal(0m, Money.Zero.Amount);
    }

    [Fact]
    public void An_amount_other_than_zero_is_not_zero() {
        Money anyPositive = new AnyMoney().WithAmount(a => a.Positive()).Generate();

        Assert.False(anyPositive.IsZero);
    }

    [Fact]
    public void Plus_adds_two_amounts() {
        // Arrange
        Money anyLeft  = new Money(AnyAmount().Generate());
        Money anyRight = new Money(AnyAmount().Generate());

        // Act
        Money sum = anyLeft.Plus(anyRight);

        // Assert
        Assert.Equal(anyLeft.Amount + anyRight.Amount, sum.Amount);
    }

    [Fact]
    public void Plus_refuses_to_go_past_the_maximum() {
        Money atMaximum = new Money(Money.MaxAmount);
        Money oneCent   = new Money(0.01m);

        Assert.Throws<ArgumentOutOfRangeException>(() => atMaximum.Plus(oneCent));
    }

    [Fact]
    public void Minus_subtracts_a_smaller_amount() {
        // Arrange
        (decimal anySmaller, decimal anyLarger) = Any.OrderedPair(AnyAmount(), AnyAmount()).Generate();

        // Act
        Money difference = new Money(anyLarger).Minus(new Money(anySmaller));

        // Assert
        Assert.Equal(anyLarger - anySmaller, difference.Amount);
    }

    [Fact]
    public void Minus_of_an_equal_amount_is_zero() {
        Money anyAmount = new Money(AnyAmount().Generate());

        Assert.True(anyAmount.Minus(anyAmount).IsZero);
    }

    [Fact]
    public void Minus_refuses_a_larger_amount() {
        // Arrange
        (decimal anySmaller, decimal anyLarger) = Any.OrderedPair(AnyAmount(), AnyAmount(), strict: true).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Money(anySmaller).Minus(new Money(anyLarger)));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Fact]
    public void MinusOrZero_subtracts_a_smaller_amount_and_floors_a_larger_one_at_zero() {
        // Arrange
        (decimal anySmaller, decimal anyLarger) = Any.OrderedPair(AnyAmount(), AnyAmount(), strict: true).Generate();
        Money smaller = new Money(anySmaller);
        Money larger  = new Money(anyLarger);

        // Act
        Money difference = larger.MinusOrZero(smaller);
        Money floored    = smaller.MinusOrZero(larger);

        // Assert
        Assert.Equal(anyLarger - anySmaller, difference.Amount);
        Assert.True(floored.IsZero);
    }

    [Fact]
    public void MinusOrZero_of_an_equal_amount_is_zero() {
        Money anyAmount = new Money(AnyAmount().Generate());

        Assert.True(anyAmount.MinusOrZero(anyAmount).IsZero);
    }

    [Theory]
    [InlineData("100.00", "0.1234", "12.34")]
    [InlineData("0.05", "0.5", "0.03")]
    [InlineData("0.05", "0.49", "0.02")]
    [InlineData("1000.00", "0", "0.00")]
    [InlineData("1000.00", "1", "1000.00")]
    [InlineData("33.33", "0.3333", "11.11")]
    public void Times_a_rate_rounds_to_the_cent_half_away_from_zero(string amount, string rate, string expected) {
        Money result = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture))
            .Times(new Rate(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), result.Amount);
    }

    [Fact]
    public void Times_an_integer_multiplies() {
        // Arrange
        Money anyAmount = new Money(AnyAmount().Generate());
        int   anyFactor = Any.Int32().Between(0, 100).Generate();

        // Act
        Money product = anyAmount.Times(anyFactor);

        // Assert
        Assert.Equal(anyAmount.Amount * anyFactor, product.Amount);
    }

    [Fact]
    public void Times_a_negative_integer_is_refused() {
        Money anyAmount   = new Money(AnyAmount().Generate());
        int   anyNegative = Any.Int32().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyAmount.Times(anyNegative));
    }

    [Fact]
    public void Times_an_integer_past_the_maximum_is_refused() {
        Money big = new Money(500_000_000_000m);

        Assert.Throws<ArgumentOutOfRangeException>(() => big.Times(3));
    }

    [Fact]
    public void Operations_refuse_null_operands() {
        Money anyAmount = new Money(AnyAmount().Generate());

        Assert.Throws<ArgumentNullException>(() => anyAmount.Plus(null!));
        Assert.Throws<ArgumentNullException>(() => anyAmount.Minus(null!));
        Assert.Throws<ArgumentNullException>(() => anyAmount.MinusOrZero(null!));
        Assert.Throws<ArgumentNullException>(() => anyAmount.Times((Rate)null!));
    }

    [Fact]
    public void Min_and_Max_pick_the_smaller_and_the_larger() {
        // Arrange
        (decimal anySmaller, decimal anyLarger) = Any.OrderedPair(AnyAmount(), AnyAmount(), strict: true).Generate();
        Money smaller = new Money(anySmaller);
        Money larger  = new Money(anyLarger);

        // Act / Assert
        Assert.Same(smaller, Money.Min(smaller, larger));
        Assert.Same(smaller, Money.Min(larger, smaller));
        Assert.Same(larger, Money.Max(smaller, larger));
        Assert.Same(larger, Money.Max(larger, smaller));
    }

    [Fact]
    public void Comparison_follows_the_amount() {
        // Arrange
        (decimal anySmaller, decimal anyLarger) = Any.OrderedPair(AnyAmount(), AnyAmount(), strict: true).Generate();
        Money smaller = new Money(anySmaller);
        Money larger  = new Money(anyLarger);

        // Act / Assert
        Assert.True(smaller.CompareTo(larger) < 0);
        Assert.True(larger.CompareTo(smaller) > 0);
        Assert.Equal(0, smaller.CompareTo(new Money(anySmaller)));
        Assert.True(smaller.CompareTo(null) > 0);
    }

    [Fact]
    public void Equality_follows_the_amount_whatever_the_instance() {
        // Arrange
        decimal anyAmount = AnyAmount().Generate();
        decimal anyOther  = AnyAmount().DifferentFrom(anyAmount).Generate();

        // Act
        Money first  = new Money(anyAmount);
        Money second = new Money(anyAmount);
        Money other  = new Money(anyOther);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.Equals(other));
        Assert.False(first.Equals(null));
        Assert.False(first.Equals("not money"));
    }

    [Fact]
    public void An_amount_with_a_different_scale_is_still_equal() {
        Assert.Equal(new Money(5m), new Money(5.00m));
        Assert.Equal(new Money(5m).GetHashCode(), new Money(5.00m).GetHashCode());
    }

    [Theory]
    [InlineData("5", "5.00")]
    [InlineData("0.5", "0.50")]
    [InlineData("1234.56", "1234.56")]
    public void ToString_always_shows_two_decimals_with_a_dot(string amount, string expected) {
        Assert.Equal(expected, new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)).ToString());
    }

    [Fact]
    public void ToString_ignores_the_current_culture() {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");

            Assert.Equal("1234.50", new Money(1234.5m).ToString());
        } finally {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

}
