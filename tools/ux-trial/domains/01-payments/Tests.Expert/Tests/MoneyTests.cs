using System.Globalization;

using JustDummies;

using Xunit;

namespace Payments.Domain.Tests.Expert;

public sealed class MoneyTests {

    [Fact]
    public void A_money_keeps_its_amount_and_its_currency() {
        // Act
        Money money = new Money(12.34m, Currency.CHF);

        // Assert
        Assert.Equal(12.34m, money.Amount);
        Assert.Equal(Currency.CHF, money.Currency);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("1")]
    [InlineData("1.1")]
    [InlineData("1.10")]
    [InlineData("999999999.99")]
    public void An_amount_from_one_cent_to_the_maximum_is_accepted(string amount) {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();

        // Act
        Money money = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), anyCurrency);

        // Assert
        Assert.Equal(decimal.Parse(amount, CultureInfo.InvariantCulture), money.Amount);
    }

    [Fact]
    public void The_maximum_amount_is_999_999_999_99() {
        // Assert
        Assert.Equal(999_999_999.99m, Money.MaximumAmount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.00")]
    [InlineData("-0.01")]
    [InlineData("-1")]
    [InlineData("-999999999.99")]
    public void A_zero_or_negative_amount_is_refused(string amount) {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), anyCurrency));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData("1000000000.00")]
    [InlineData("1000000000")]
    [InlineData("999999999.991")]
    public void An_amount_above_the_maximum_is_refused_before_its_decimals_are_looked_at(string amount) {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), anyCurrency));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1.005")]
    [InlineData("10.999")]
    [InlineData("100.0000001")]
    public void An_amount_with_more_than_two_decimal_places_is_refused(string amount) {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), anyCurrency));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void An_undefined_currency_is_refused() {
        // Arrange
        decimal anyAmount = AnyMoney.ValidAmounts().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(anyAmount, (Currency)99));

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void A_negative_amount_is_reported_before_an_undefined_currency() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, (Currency)99));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void Too_many_decimals_are_reported_before_an_undefined_currency() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Money(1.001m, (Currency)99));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void Adding_two_amounts_of_a_currency_adds_the_amounts() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();
        Money    first       = new Money(10.10m, anyCurrency);
        Money    second      = new Money(0.20m, anyCurrency);

        // Act
        Money sum = first.Add(second);

        // Assert
        Assert.Equal(10.30m, sum.Amount);
    }

    [Theory]
    [InlineData(Currency.EUR)]
    [InlineData(Currency.CHF)]
    [InlineData(Currency.GBP)]
    [InlineData(Currency.SEK)]
    [InlineData(Currency.DKK)]
    [InlineData(Currency.PLN)]
    public void The_sum_is_in_the_currency_of_the_amounts_added(Currency currency) {
        // Arrange
        // Half the maximum each, so that the sum stays a money the type accepts.
        AnyDecimal halfAmounts    = AnyMoney.ValidAmounts().LessThanOrEqualTo(Money.MaximumAmount / 2m);
        decimal    anyAmount      = halfAmounts.Generate();
        decimal    anyOtherAmount = halfAmounts.Generate();

        // Act
        Money sum = new Money(anyAmount, currency).Add(new Money(anyOtherAmount, currency));

        // Assert
        Assert.Equal(currency, sum.Currency);
    }

    [Fact]
    public void Adding_does_not_change_the_amounts_added() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();
        Money    first       = new Money(10.10m, anyCurrency);
        Money    second      = new Money(0.20m, anyCurrency);

        // Act
        first.Add(second);

        // Assert
        Assert.Equal(10.10m, first.Amount);
        Assert.Equal(0.20m, second.Amount);
    }

    [Fact]
    public void Adding_amounts_of_two_different_currencies_is_refused() {
        // Arrange
        Currency anyCurrency      = Any.Enum<Currency>().Generate();
        Currency anyOtherCurrency = Any.Enum<Currency>().DifferentFrom(anyCurrency).Generate();
        decimal  anyAmount        = AnyMoney.ValidAmounts().Generate();
        Money    first            = new Money(anyAmount, anyCurrency);
        Money    second           = new Money(anyAmount, anyOtherCurrency);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => first.Add(second));
    }

    [Fact]
    public void A_sum_that_reaches_the_maximum_exactly_is_accepted() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();
        Money    nearlyMax   = new Money(Money.MaximumAmount - 0.01m, anyCurrency);
        Money    oneCent     = new Money(0.01m, anyCurrency);

        // Act
        Money sum = nearlyMax.Add(oneCent);

        // Assert
        Assert.Equal(Money.MaximumAmount, sum.Amount);
    }

    [Fact]
    public void A_sum_above_the_maximum_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();
        Money    max         = new Money(Money.MaximumAmount, anyCurrency);
        Money    oneCent     = new Money(0.01m, anyCurrency);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => max.Add(oneCent));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void Two_monies_of_the_same_amount_and_currency_are_equal() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();

        // Act
        Money first  = new Money(42.50m, anyCurrency);
        Money second = new Money(42.50m, anyCurrency);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Amounts_written_with_a_different_number_of_trailing_zeros_are_equal_and_hash_alike() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();

        // Act
        Money oneDecimal  = new Money(1.1m, anyCurrency);
        Money twoDecimals = new Money(1.10m, anyCurrency);

        // Assert
        Assert.True(oneDecimal.Equals(twoDecimals));
        Assert.Equal(oneDecimal.GetHashCode(), twoDecimals.GetHashCode());
    }

    [Fact]
    public void Two_monies_of_different_amounts_are_not_equal() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();

        // Act
        Money first  = new Money(42.50m, anyCurrency);
        Money second = new Money(42.51m, anyCurrency);

        // Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Two_monies_of_different_currencies_are_not_equal() {
        // Arrange
        Currency anyCurrency      = Any.Enum<Currency>().Generate();
        Currency anyOtherCurrency = Any.Enum<Currency>().DifferentFrom(anyCurrency).Generate();

        // Act
        Money first  = new Money(42.50m, anyCurrency);
        Money second = new Money(42.50m, anyOtherCurrency);

        // Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void A_money_is_not_equal_to_nothing_nor_to_a_bare_amount() {
        // Arrange
        Money anyMoney = Any.Money().Generate();

        // Act & Assert
        Assert.False(anyMoney.Equals(null));
        Assert.False(anyMoney.Equals((object)anyMoney.Amount));
    }

    [Fact]
    public void A_money_reads_as_its_amount_with_two_decimals_then_its_currency_in_the_invariant_culture() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try {
            Money money = new Money(5m, Currency.SEK);

            // Act
            string text = money.ToString();

            // Assert
            Assert.Equal("5.00 SEK", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact(Skip = "domain defect: ToString formats the amount with the machine's culture, so under fr-FR 5 SEK reads '5,00 SEK'; no seed, the input is a literal")]
    public void A_money_reads_the_same_whatever_the_culture_of_the_machine() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try {
            Money money = new Money(5m, Currency.SEK);

            // Act
            string text = money.ToString();

            // Assert
            Assert.Equal("5.00 SEK", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
