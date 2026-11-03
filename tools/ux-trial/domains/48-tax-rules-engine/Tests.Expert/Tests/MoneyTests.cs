using JustDummies;

using TaxRules.Domain;

using Xunit;

namespace TaxRules.Tests;

public sealed class MoneyTests {

    [Theory]
    [InlineData("0")]
    [InlineData("0.01")]
    [InlineData("999999999999.99")]
    public void An_amount_from_zero_to_the_ceiling_to_the_cent_is_accepted(string amount) {
        Money money = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), money.Amount);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-0.01m));
    }

    [Fact]
    public void An_amount_above_the_ceiling_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(Money.MaxAmount + 0.01m));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("10.005")]
    [InlineData("0.0000001")]
    public void An_amount_finer_than_a_cent_is_refused_rather_than_rounded(string amount) {
        Assert.Throws<ArgumentException>(() => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Zero_is_the_empty_amount() {
        Assert.True(Money.Zero.IsZero);
        Assert.Equal(new Money(0m), Money.Zero);
    }

    [Theory]
    [InlineData("0.125", "0.13")]
    [InlineData("0.135", "0.14")]
    [InlineData("2.675", "2.68")]
    [InlineData("2.674", "2.67")]
    [InlineData("0.004", "0.00")]
    [InlineData("0.005", "0.01")]
    public void A_computed_amount_is_rounded_to_the_cent_half_away_from_zero(string computed, string expected) {
        Money rounded = Money.RoundedFrom(decimal.Parse(computed, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), rounded.Amount);
    }

    [Fact]
    public void A_computed_amount_that_rounds_below_zero_is_still_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.RoundedFrom(-0.005m));
    }

    [Fact]
    public void Adding_two_amounts_gives_their_sum() {
        Money sum = new Money(10.25m).Plus(new Money(0.75m));

        Assert.Equal(new Money(11.00m), sum);
    }

    [Fact]
    public void Adding_beyond_the_ceiling_is_refused() {
        Money nearlyFull = new Money(Money.MaxAmount);

        Assert.Throws<ArgumentOutOfRangeException>(() => nearlyFull.Plus(new Money(0.01m)));
    }

    [Fact]
    public void Subtracting_a_smaller_amount_gives_the_difference() {
        Assert.Equal(new Money(9.50m), new Money(10.00m).Minus(new Money(0.50m)));
    }

    [Fact]
    public void Subtracting_an_equal_amount_gives_zero() {
        Assert.True(new Money(10.00m).Minus(new Money(10.00m)).IsZero);
    }

    [Fact]
    public void Subtracting_a_larger_amount_is_refused() {
        Assert.Throws<InvalidOperationException>(() => new Money(10.00m).Minus(new Money(10.01m)));
    }

    [Theory]
    [InlineData("10.00", "0.50", "9.50")]
    [InlineData("10.00", "10.00", "0.00")]
    [InlineData("10.00", "10.01", "0.00")]
    [InlineData("0.00", "999.99", "0.00")]
    public void Subtracting_or_zero_never_goes_below_zero(string from, string taken, string expected) {
        Money result = ParseMoney(from).MinusOrZero(ParseMoney(taken));

        Assert.Equal(ParseMoney(expected), result);
    }

    [Fact]
    public void Multiplying_by_a_rate_rounds_the_product_once_half_away_from_zero() {
        // 0.50 x 5 % = 0.025: a banker's rounding would give 0.02.
        Assert.Equal(new Money(0.03m), new Money(0.50m).Times(new Rate(0.05m)));
        Assert.Equal(new Money(12.51m), new Money(100.05m).Times(new Rate(0.1250m)));
    }

    [Fact]
    public void Multiplying_by_a_rate_of_one_changes_nothing_and_by_zero_gives_zero() {
        Assert.Equal(new Money(123.45m), new Money(123.45m).Times(new Rate(1m)));
        Assert.True(new Money(123.45m).Times(Rate.Zero).IsZero);
    }

    [Fact]
    public void Multiplying_by_a_whole_factor_repeats_the_amount() {
        Assert.Equal(new Money(7.50m), new Money(2.50m).Times(3));
        Assert.True(new Money(2.50m).Times(0).IsZero);
    }

    [Fact]
    public void Multiplying_by_a_negative_factor_is_refused() {
        Money anyAmount = Any.Money().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyAmount.Times(-1));
    }

    [Fact]
    public void The_smaller_and_the_larger_of_two_amounts_are_found() {
        Money small = new Money(1.00m);
        Money large = new Money(1.01m);

        Assert.Same(small, Money.Min(small, large));
        Assert.Same(small, Money.Min(large, small));
        Assert.Same(large, Money.Max(small, large));
        Assert.Same(large, Money.Max(large, small));
    }

    [Fact]
    public void Amounts_are_ordered_by_their_value() {
        Assert.True(new Money(1.00m).CompareTo(new Money(1.01m)) < 0);
        Assert.True(new Money(1.01m).CompareTo(new Money(1.00m)) > 0);
        Assert.Equal(0, new Money(1.50m).CompareTo(new Money(1.5m)));
    }

    [Fact]
    public void Any_amount_is_greater_than_nothing() {
        Money amount = Any.Money().Generate();

        Assert.True(amount.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_amounts_of_the_same_value_are_equal_whatever_the_scale_they_were_written_in() {
        Money cents = new Money(2.50m);
        Money tenths = new Money(2.5m);

        Assert.Equal(cents, tenths);
        Assert.Equal(cents.GetHashCode(), tenths.GetHashCode());
        Assert.True(cents.Equals((object)tenths));
    }

    [Fact]
    public void An_amount_does_not_equal_another_value_nor_another_type() {
        Money amount = new Money(10.00m);

        Assert.NotEqual(amount, new Money(10.01m));
        Assert.False(amount.Equals(null));
        Assert.False(amount.Equals((object)10.00m));
    }

    [Fact]
    public void An_amount_is_shown_with_two_decimals_whatever_the_culture() {
        Assert.Equal("1234.50", new Money(1234.5m).ToString());
        Assert.Equal("0.00", Money.Zero.ToString());
    }

    private static Money ParseMoney(string amount) {
        return new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));
    }

}
