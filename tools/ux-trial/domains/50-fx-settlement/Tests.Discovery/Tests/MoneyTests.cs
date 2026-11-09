using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests;

public sealed class MoneyTests {

    [Theory]
    [InlineData("999999999999999")]
    [InlineData("-999999999999999")]
    [InlineData("0")]
    public void An_amount_up_to_fifteen_integer_digits_is_accepted(string amount) {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        Money money = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), anyCurrency);

        // Assert
        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), money.Amount);
    }

    [Theory]
    [InlineData("1000000000000000")]
    [InlineData("-1000000000000000")]
    [InlineData("999999999999999.01")]
    public void An_amount_beyond_fifteen_integer_digits_is_refused(string amount) {
        // Arrange: USD, so that 999999999999999.01 is refused for its size and not for its digits.
        Action construct = () => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), Currency.Usd);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Theory]
    [InlineData("USD", "1.005")]
    [InlineData("USD", "0.001")]
    [InlineData("JPY", "100.5")]
    [InlineData("JPY", "0.1")]
    [InlineData("KWD", "1.0001")]
    [InlineData("CLF", "1.00001")]
    public void An_amount_with_more_fractional_digits_than_the_currency_allows_is_refused(string code, string amount) {
        Currency currency = Currency.Iso(code);

        Action construct = () => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), currency);

        Assert.Throws<ArgumentException>(construct);
    }

    [Theory]
    [InlineData("USD", "1.50")]
    [InlineData("USD", "1.5")]
    [InlineData("USD", "-1.5")]
    [InlineData("JPY", "100")]
    [InlineData("JPY", "100.0")]
    [InlineData("KWD", "1.123")]
    [InlineData("CLF", "1.1234")]
    public void An_amount_with_no_more_digits_than_the_currency_allows_is_accepted(string code, string amount) {
        Currency currency = Currency.Iso(code);

        Money money = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), currency);

        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), money.Amount);
    }

    [Fact]
    public void A_money_without_currency_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Money(1m, null!));
    }

    [Fact]
    public void Plus_adds_two_amounts_of_the_same_currency() {
        Money sum = Fixtures.Usd(10.25m).Plus(Fixtures.Usd(0.80m));

        Assert.Equal(Fixtures.Usd(11.05m), sum);
    }

    [Fact]
    public void Minus_subtracts_and_may_go_negative() {
        Money difference = Fixtures.Eur(10.00m).Minus(Fixtures.Eur(12.50m));

        Assert.Equal(Fixtures.Eur(-2.50m), difference);
    }

    [Fact]
    public void Plus_and_Minus_leave_both_operands_unchanged() {
        // Arrange
        Money left  = Fixtures.Usd(10.00m);
        Money right = Fixtures.Usd(3.00m);

        // Act
        left.Plus(right);
        left.Minus(right);

        // Assert
        Assert.Equal(Fixtures.Usd(10.00m), left);
        Assert.Equal(Fixtures.Usd(3.00m), right);
    }

    [Fact]
    public void Plus_refuses_another_currency() {
        // Arrange: two amounts in two different currencies; neither amount nor which currencies matter.
        CurrencyPair anyPair   = Any.CurrencyPair().Generate();
        Money        anyFirst  = Any.Money().WithCurrency(anyPair.Base).Generate();
        Money        anySecond = Any.Money().WithCurrency(anyPair.Quote).Generate();

        // Act
        Action add = () => anyFirst.Plus(anySecond);

        // Assert
        Assert.Throws<InvalidOperationException>(add);
    }

    [Fact]
    public void Minus_refuses_another_currency() {
        // Arrange
        CurrencyPair anyPair   = Any.CurrencyPair().Generate();
        Money        anyFirst  = Any.Money().WithCurrency(anyPair.Base).Generate();
        Money        anySecond = Any.Money().WithCurrency(anyPair.Quote).Generate();

        // Act
        Action subtract = () => anyFirst.Minus(anySecond);

        // Assert
        Assert.Throws<InvalidOperationException>(subtract);
    }

    [Fact]
    public void Plus_refuses_a_sum_beyond_fifteen_integer_digits() {
        Money nearTheLimit = new Money(Money.MaxMagnitude, Currency.Usd);

        Action add = () => nearTheLimit.Plus(Fixtures.Usd(0.01m));

        Assert.Throws<ArgumentOutOfRangeException>(add);
    }

    [Fact]
    public void Negated_flips_the_sign() {
        Assert.Equal(Fixtures.Usd(-5.25m), Fixtures.Usd(5.25m).Negated());
        Assert.Equal(Fixtures.Usd(5.25m), Fixtures.Usd(-5.25m).Negated());
    }

    [Fact]
    public void Abs_drops_the_sign() {
        Assert.Equal(Fixtures.Jpy(300m), Fixtures.Jpy(-300m).Abs());
        Assert.Equal(Fixtures.Jpy(300m), Fixtures.Jpy(300m).Abs());
    }

    [Fact]
    public void Zero_is_zero_and_not_positive_in_the_currency_given() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        Money zero = Money.Zero(anyCurrency);

        // Assert
        Assert.True(zero.IsZero);
        Assert.False(zero.IsPositive);
        Assert.Equal(anyCurrency, zero.Currency);
    }

    [Fact]
    public void A_positive_amount_is_positive_and_not_zero() {
        Money money = Fixtures.Eur(0.01m);

        Assert.True(money.IsPositive);
        Assert.False(money.IsZero);
    }

    [Fact]
    public void A_negative_amount_is_neither_positive_nor_zero() {
        Money money = Fixtures.Eur(-0.01m);

        Assert.False(money.IsPositive);
        Assert.False(money.IsZero);
    }

    [Fact]
    public void Two_amounts_are_equal_when_amount_and_currency_are_equal_whatever_their_trailing_zeros() {
        Assert.Equal(Fixtures.Usd(5.00m), Fixtures.Usd(5m));
        Assert.Equal(Fixtures.Usd(5.00m).GetHashCode(), Fixtures.Usd(5m).GetHashCode());
        Assert.NotEqual(Fixtures.Usd(5m), Fixtures.Eur(5m));
        Assert.NotEqual(Fixtures.Usd(5m), Fixtures.Usd(5.01m));
    }

    [Fact]
    public void An_amount_is_written_with_the_minor_units_of_its_currency() {
        Assert.Equal("1.50 USD", Fixtures.Usd(1.5m).ToString());
        Assert.Equal("100 JPY", Fixtures.Jpy(100m).ToString());
        Assert.Equal("-0.123 KWD", new Money(-0.123m, Currency.Kwd).ToString());
    }

}
