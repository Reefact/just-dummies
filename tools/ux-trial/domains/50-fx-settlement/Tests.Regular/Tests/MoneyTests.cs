using System.Globalization;

using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Regular;

public sealed class MoneyTests {

    private static decimal D(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    public static TheoryData<string, string> AmountsTheCurrencyAllows => new TheoryData<string, string> {
        { "100.0", "JPY" }, { "100", "JPY" }, { "1.005", "KWD" }, { "1.50", "USD" }, { "-1.5", "USD" }, { "0.0001", "CLF" },
    };

    [Theory, MemberData(nameof(AmountsTheCurrencyAllows))]
    public void An_amount_within_the_minor_units_is_accepted(string amount, string code) {
        // Act
        Money money = new Money(D(amount), Currency.Iso(code));

        // Assert
        Assert.Equal(D(amount), money.Amount);
        Assert.Equal(Currency.Iso(code), money.Currency);
    }

    public static TheoryData<string, string> AmountsTheCurrencyRefuses => new TheoryData<string, string> {
        { "1.005", "USD" }, { "0.5", "JPY" }, { "0.0001", "KWD" }, { "0.00001", "CLF" },
    };

    [Theory, MemberData(nameof(AmountsTheCurrencyRefuses))]
    public void An_amount_with_more_fractional_digits_than_the_minor_units_is_refused(string amount, string code) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Money(D(amount), Currency.Iso(code)));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void The_largest_magnitude_is_accepted_on_both_signs() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        Money largest  = new Money(999_999_999_999_999m, anyCurrency);
        Money smallest = new Money(-999_999_999_999_999m, anyCurrency);

        // Assert
        Assert.Equal(Money.MaxMagnitude, largest.Amount);
        Assert.Equal(-Money.MaxMagnitude, smallest.Amount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void One_unit_beyond_the_largest_magnitude_is_refused(int sign) {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(sign * 1_000_000_000_000_000m, anyCurrency));
    }

    [Fact]
    public void A_null_currency_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Money(1m, null!));
    }

    [Fact]
    public void Plus_adds_amounts_of_the_same_currency() {
        // Act
        Money sum = new Money(10.50m, Currency.Usd).Plus(new Money(2.25m, Currency.Usd));

        // Assert
        Assert.Equal(new Money(12.75m, Currency.Usd), sum);
    }

    [Fact]
    public void Minus_subtracts_and_may_cross_zero() {
        // Act
        Money difference = new Money(2.25m, Currency.Usd).Minus(new Money(10.50m, Currency.Usd));

        // Assert
        Assert.Equal(new Money(-8.25m, Currency.Usd), difference);
    }

    [Fact]
    public void Adding_two_currencies_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Currency anyOther    = new AnyCurrency().Except(anyCurrency).Generate();
        Money    anyMoney    = Any.Money().WithCurrency(anyCurrency).Generate();
        Money    anyOtherMoney = Any.Money().WithCurrency(anyOther).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyMoney.Plus(anyOtherMoney));
        Assert.Throws<InvalidOperationException>(() => anyMoney.Minus(anyOtherMoney));
    }

    [Fact]
    public void Adding_past_the_largest_magnitude_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(Money.MaxMagnitude, anyCurrency).Plus(new Money(1m, anyCurrency)));
    }

    [Fact]
    public void Plus_refuses_null() {
        // Arrange
        Money anyMoney = Any.Money().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyMoney.Plus(null!));
    }

    [Fact]
    public void Negated_flips_the_sign_and_keeps_the_currency() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        Money negated = new Money(3m, anyCurrency).Negated();

        // Assert
        Assert.Equal(new Money(-3m, anyCurrency), negated);
    }

    [Fact]
    public void Abs_of_a_negative_amount_is_its_positive_counterpart() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act / Assert
        Assert.Equal(new Money(3m, anyCurrency), new Money(-3m, anyCurrency).Abs());
        Assert.Equal(new Money(3m, anyCurrency), new Money(3m, anyCurrency).Abs());
    }

    [Fact]
    public void Zero_is_zero_and_not_positive_in_any_currency() {
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
        // Arrange
        Money anyPositive = Any.Money().WithAmount(a => a.Positive()).Generate();

        // Act / Assert
        Assert.True(anyPositive.IsPositive);
        Assert.False(anyPositive.IsZero);
    }

    [Fact]
    public void A_negative_amount_is_neither_positive_nor_zero() {
        // Arrange
        Money anyNegative = Any.Money().WithAmount(a => a.Negative()).Generate();

        // Act / Assert
        Assert.False(anyNegative.IsPositive);
        Assert.False(anyNegative.IsZero);
    }

    [Fact]
    public void Amounts_are_equal_by_value_whatever_their_trailing_zeros() {
        // Arrange
        Currency anyCurrency = Any.Currency().WithMinorUnitsAtLeast(2).Generate();

        // Act
        Money plain = new Money(5m, anyCurrency);
        Money padded = new Money(5.00m, anyCurrency);

        // Assert
        Assert.Equal(plain, padded);
        Assert.Equal(plain.GetHashCode(), padded.GetHashCode());
    }

    [Fact]
    public void The_same_amount_in_two_currencies_is_not_equal() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Currency anyOther    = new AnyCurrency().Except(anyCurrency).Generate();

        // Act / Assert
        Assert.NotEqual(new Money(5m, anyCurrency), new Money(5m, anyOther));
    }

    [Fact]
    public void Money_prints_with_the_minor_units_of_its_currency() {
        // Act / Assert
        Assert.Equal("12.50 USD", new Money(12.5m, Currency.Usd).ToString());
        Assert.Equal("100 JPY", new Money(100m, Currency.Jpy).ToString());
        Assert.Equal("-0.0500 CLF", new Money(-0.05m, Currency.Clf).ToString());
    }

    [Fact]
    public void An_arbitrary_money_always_respects_its_currency() {
        // Arrange
        Money anyMoney = Any.Money().Generate();

        // Act / Assert
        Assert.True(anyMoney.Currency.Allows(anyMoney.Amount));
    }

}
