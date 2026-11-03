using JustDummies;

using TaxRules.Domain;

namespace TaxRules.Tests.Discovery;

public sealed class MoneyTests {

    [Fact]
    public void An_amount_keeps_the_value_it_was_built_with() {
        // Act
        Money money = new Money(12.34m);

        // Assert
        Assert.Equal(12.34m, money.Amount);
        Assert.False(money.IsZero);
    }

    [Fact]
    public void Zero_and_the_maximum_are_valid_amounts() {
        Assert.True(new Money(0m).IsZero);
        Assert.Equal(999_999_999_999.99m, new Money(Money.MaxAmount).Amount);
        Assert.Equal(Money.Zero, new Money(0.00m));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-100")]
    [InlineData("1000000000000.00")]
    [InlineData("999999999999.995")]
    public void An_amount_outside_the_range_is_refused(string amount) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("12.345")]
    [InlineData("0.0000001")]
    public void A_sub_cent_amount_is_refused_rather_than_rounded(string amount) {
        Assert.Throws<ArgumentException>(() => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("1.004", "1.00")]
    [InlineData("1.005", "1.01")]
    [InlineData("1.006", "1.01")]
    [InlineData("2.345", "2.35")]
    [InlineData("0.004", "0.00")]
    [InlineData("0.005", "0.01")]
    public void Rounding_goes_to_the_cent_with_halves_away_from_zero(string computed, string expected) {
        // Act
        Money rounded = Money.RoundedFrom(decimal.Parse(computed, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), rounded.Amount);
    }

    [Fact]
    public void Rounding_a_negative_amount_is_still_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.RoundedFrom(-0.01m));
    }

    [Fact]
    public void Plus_adds_two_amounts() {
        Assert.Equal(new Money(10.30m), new Money(10.10m).Plus(new Money(0.20m)));
    }

    [Fact]
    public void Plus_beyond_the_maximum_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(Money.MaxAmount).Plus(new Money(0.01m)));
    }

    [Fact]
    public void Minus_subtracts_and_may_reach_zero() {
        Assert.Equal(new Money(6.50m), new Money(10m).Minus(new Money(3.50m)));
        Assert.True(new Money(3.50m).Minus(new Money(3.50m)).IsZero);
    }

    [Fact]
    public void Minus_refuses_to_go_below_zero() {
        Assert.Throws<InvalidOperationException>(() => new Money(3.50m).Minus(new Money(3.51m)));
    }

    [Fact]
    public void MinusOrZero_stops_at_zero() {
        Assert.Equal(new Money(0.01m), new Money(3.51m).MinusOrZero(new Money(3.50m)));
        Assert.True(new Money(3.50m).MinusOrZero(new Money(3.51m)).IsZero);
        Assert.True(new Money(3.50m).MinusOrZero(new Money(3.50m)).IsZero);
    }

    [Theory]
    [InlineData("100.05", "0.0125", "1.25")]
    [InlineData("0.01", "0.5", "0.01")]
    [InlineData("0.01", "0.4999", "0.00")]
    [InlineData("1000", "0", "0.00")]
    [InlineData("1000", "1", "1000.00")]
    public void Times_a_rate_rounds_to_the_cent(string amount, string rate, string expected) {
        // Arrange
        Money money = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));
        Rate  r     = new Rate(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        Money result = money.Times(r);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), result.Amount);
    }

    [Fact]
    public void Times_a_whole_factor_multiplies() {
        Assert.Equal(new Money(7.50m), new Money(2.50m).Times(3));
        Assert.True(new Money(2.50m).Times(0).IsZero);
    }

    [Fact]
    public void Times_a_negative_factor_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(2.50m).Times(-1));
    }

    [Fact]
    public void Times_beyond_the_maximum_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(Money.MaxAmount).Times(2));
    }

    [Fact]
    public void Min_and_Max_pick_the_smaller_and_the_larger() {
        Money small = new Money(1.00m);
        Money large = new Money(2.00m);

        Assert.Same(small, Money.Min(small, large));
        Assert.Same(small, Money.Min(large, small));
        Assert.Same(large, Money.Max(small, large));
        Assert.Same(large, Money.Max(large, small));
    }

    [Fact]
    public void Amounts_compare_and_equal_by_value() {
        Assert.True(new Money(1.00m).CompareTo(new Money(2.00m)) < 0);
        Assert.True(new Money(2.00m).CompareTo(new Money(1.00m)) > 0);
        Assert.Equal(0, new Money(1.5m).CompareTo(new Money(1.50m)));
        Assert.Equal(1, new Money(1.5m).CompareTo(null));
        Assert.Equal(new Money(1.5m), new Money(1.50m));
        Assert.Equal(new Money(1.5m).GetHashCode(), new Money(1.50m).GetHashCode());
        Assert.NotEqual(new Money(1.5m), new Money(1.51m));
    }

    [Fact]
    public void An_amount_is_written_to_the_cent() {
        Assert.Equal("12.50", new Money(12.5m).ToString());
        Assert.Equal("0.00", Money.Zero.ToString());
    }

    [Fact]
    public void Operations_refuse_a_missing_operand() {
        // Arrange
        Money anyMoney = Any.Money().Generate();

        // Act and assert
        Assert.Throws<ArgumentNullException>(() => anyMoney.Plus(null!));
        Assert.Throws<ArgumentNullException>(() => anyMoney.Minus(null!));
        Assert.Throws<ArgumentNullException>(() => anyMoney.MinusOrZero(null!));
        Assert.Throws<ArgumentNullException>(() => anyMoney.Times(null!));
    }

}
