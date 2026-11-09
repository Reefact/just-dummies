using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Expert;

public sealed class MoneyTests {

    public static TheoryData<string, decimal> AmountsEachCurrencyAccepts => new TheoryData<string, decimal> {
        { "JPY", 100m },
        { "JPY", 100.0m },
        { "USD", 10.50m },
        { "USD", 10.500m },
        { "USD", -0.01m },
        { "KWD", 0.001m },
        { "CLF", 0.0001m },
        { "USD", 0m }
    };

    [Theory]
    [MemberData(nameof(AmountsEachCurrencyAccepts))]
    public void An_amount_may_use_every_minor_unit_of_its_currency_and_trailing_zeros_do_not_count(string code, decimal amount) {
        // Act
        Money money = new Money(amount, Currency.Iso(code));

        // Assert
        Assert.Equal(amount, money.Amount);
    }

    public static TheoryData<string, decimal> AmountsEachCurrencyRefuses => new TheoryData<string, decimal> {
        { "JPY", 0.5m },
        { "JPY", 100.1m },
        { "USD", 1.005m },
        { "USD", -1.005m },
        { "KWD", 0.0001m },
        { "CLF", 0.00001m }
    };

    [Theory]
    [MemberData(nameof(AmountsEachCurrencyRefuses))]
    public void An_amount_with_more_fractional_digits_than_the_currency_has_is_refused(string code, decimal amount) {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Money(amount, Currency.Iso(code)));
    }

    [Fact]
    public void The_largest_magnitude_is_fifteen_nines_on_either_side() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        Money largest  = new Money(999_999_999_999_999m, anyCurrency);
        Money smallest = new Money(-999_999_999_999_999m, anyCurrency);

        // Assert
        Assert.Equal(999_999_999_999_999m, largest.Amount);
        Assert.Equal(-999_999_999_999_999m, smallest.Amount);
        Assert.Equal(Money.MaxMagnitude, largest.Amount);
    }

    [Theory]
    [InlineData("1000000000000000")]
    [InlineData("-1000000000000000")]
    [InlineData("999999999999999.01")]
    public void An_amount_beyond_fifteen_nines_is_refused_whatever_its_currency(string amount) {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), anyCurrency));
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Money(1m, null!));
    }

    [Fact]
    public void Zero_is_zero_in_any_currency() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        Money zero = Money.Zero(anyCurrency);

        // Assert
        Assert.True(zero.IsZero);
        Assert.False(zero.IsPositive);
    }

    [Theory]
    [InlineData(0.01, true, false)]
    [InlineData(-0.01, false, false)]
    [InlineData(0, false, true)]
    public void Positive_and_zero_are_told_apart_by_the_sign_of_the_amount(double amount, bool positive, bool zero) {
        // Arrange
        Money money = new Money((decimal)amount, Currency.Usd);

        // Assert
        Assert.Equal(positive, money.IsPositive);
        Assert.Equal(zero, money.IsZero);
    }

    [Fact]
    public void Plus_adds_amounts_of_the_same_currency() {
        // Act
        Money sum = new Money(12.25m, Currency.Eur).Plus(new Money(30.50m, Currency.Eur));

        // Assert
        Assert.Equal(new Money(42.75m, Currency.Eur), sum);
    }

    [Fact]
    public void Minus_subtracts_and_may_go_below_zero() {
        // Act
        Money difference = new Money(12m, Currency.Jpy).Minus(new Money(30m, Currency.Jpy));

        // Assert
        Assert.Equal(new Money(-18m, Currency.Jpy), difference);
    }

    [Fact]
    public void Plus_and_Minus_refuse_another_currency() {
        // Arrange
        Currency anyCurrency      = Any.Currency().Generate();
        Currency anyOtherCurrency = Any.Currency().Except(anyCurrency).Generate();

        Money anyAmount      = new Money(1m, anyCurrency);
        Money anyOtherAmount = new Money(1m, anyOtherCurrency);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyAmount.Plus(anyOtherAmount));
        Assert.Throws<InvalidOperationException>(() => anyAmount.Minus(anyOtherAmount));
    }

    [Fact]
    public void A_sum_beyond_the_largest_magnitude_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        Money largest = new Money(Money.MaxMagnitude, anyCurrency);
        Money one     = new Money(1m, anyCurrency);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => largest.Plus(one));
    }

    [Fact]
    public void Negated_flips_the_sign_and_Abs_drops_it() {
        // Arrange
        Money debt = new Money(-12.50m, Currency.Gbp);

        // Assert
        Assert.Equal(new Money(12.50m, Currency.Gbp), debt.Negated());
        Assert.Equal(new Money(12.50m, Currency.Gbp), debt.Abs());
        Assert.Equal(debt, debt.Negated().Negated());
        Assert.Equal(new Money(12.50m, Currency.Gbp), new Money(12.50m, Currency.Gbp).Abs());
    }

    [Fact]
    public void Money_is_equal_to_money_of_the_same_value_whatever_its_trailing_zeros() {
        // Assert
        Assert.Equal(new Money(1.0m, Currency.Usd), new Money(1.00m, Currency.Usd));
        Assert.Equal(new Money(1.0m, Currency.Usd).GetHashCode(), new Money(1.00m, Currency.Usd).GetHashCode());
    }

    [Fact]
    public void The_same_amount_in_two_currencies_is_not_the_same_money() {
        // Assert
        Assert.NotEqual(new Money(1m, Currency.Eur), new Money(1m, Currency.Gbp));
    }

    [Theory]
    [InlineData("USD", 1.5, "1.50 USD")]
    [InlineData("JPY", 100, "100 JPY")]
    [InlineData("KWD", -0.25, "-0.250 KWD")]
    public void Money_prints_every_minor_digit_then_its_code(string code, double amount, string expected) {
        // Act
        string text = new Money((decimal)amount, Currency.Iso(code)).ToString();

        // Assert
        Assert.Equal(expected, text);
    }

}
