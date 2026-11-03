using JustDummies;

using Payments.Domain;

using Xunit;

namespace Payments.Tests;

public sealed class MoneyTests {

    [Fact]
    public void An_amount_and_a_currency_are_kept() {
        // Act
        Money money = new Money(12.5m, Currency.CHF);

        // Assert
        Assert.Equal(12.5m, money.Amount);
        Assert.Equal(Currency.CHF, money.Currency);
    }

    [Fact]
    public void The_smallest_amount_is_one_cent() {
        Assert.Equal(0.01m, new Money(0.01m, Any.Enum<Currency>().Generate()).Amount);
    }

    [Fact]
    public void The_maximum_amount_is_accepted() {
        Assert.Equal(999_999_999.99m, new Money(Money.MaximumAmount, Any.Enum<Currency>().Generate()).Amount);
    }

    [Fact]
    public void An_amount_above_the_maximum_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(1_000_000_000m, anyCurrency));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void Zero_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(0m, Any.Enum<Currency>().Generate()));
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        decimal anyNegative = Any.Decimal().Negative().WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(anyNegative, Any.Enum<Currency>().Generate()));
    }

    [Theory]
    [InlineData("1.001")]
    [InlineData("0.005")]
    [InlineData("12.345")]
    public void More_than_two_decimal_places_is_refused(string amount) {
        Assert.Throws<ArgumentException>(() => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), Currency.EUR));
    }

    [Fact]
    public void Trailing_zeros_beyond_two_decimals_are_not_a_third_decimal_place() {
        Assert.Equal(1.5m, new Money(1.500m, Currency.EUR).Amount);
    }

    [Fact]
    public void An_undefined_currency_is_refused() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(10m, (Currency)99));

        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void The_amount_is_checked_before_the_decimals_and_the_decimals_before_the_currency() {
        // zero with a bad currency: the amount comes first
        Assert.Equal("amount", Assert.Throws<ArgumentOutOfRangeException>(() => new Money(0m, (Currency)99)).ParamName);
        // too large and too precise: the maximum comes first
        Assert.IsType<ArgumentOutOfRangeException>(Record.Exception(() => new Money(1_000_000_000.001m, Currency.EUR)));
        // too precise with a bad currency: the decimals come first
        Assert.IsType<ArgumentException>(Record.Exception(() => new Money(1.234m, (Currency)99)));
    }

    [Fact]
    public void Adding_two_amounts_of_the_same_currency_sums_them() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();
        Money    first       = new Money(10.50m, anyCurrency);
        Money    second      = new Money(4.25m, anyCurrency);

        // Act
        Money total = first.Add(second);

        // Assert
        Assert.Equal(new Money(14.75m, anyCurrency), total);
    }

    [Fact]
    public void Adding_leaves_both_operands_untouched() {
        // Arrange
        Money first  = new Money(10m, Currency.EUR);
        Money second = new Money(5m, Currency.EUR);

        // Act
        first.Add(second);

        // Assert
        Assert.Equal(10m, first.Amount);
        Assert.Equal(5m, second.Amount);
    }

    [Fact]
    public void Adding_two_currencies_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();
        Currency anyOther    = Any.Enum<Currency>().DifferentFrom(anyCurrency).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new Money(1m, anyCurrency).Add(new Money(1m, anyOther)));
    }

    [Fact]
    public void A_sum_above_the_maximum_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Enum<Currency>().Generate();
        Money    atTheMaximum = new Money(Money.MaximumAmount, anyCurrency);
        Money    oneCent      = new Money(0.01m, anyCurrency);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => atTheMaximum.Add(oneCent));
    }

    [Fact]
    public void Two_amounts_are_equal_when_amount_and_currency_are_the_same() {
        Assert.Equal(new Money(5m, Currency.CHF), new Money(5.00m, Currency.CHF));
        Assert.Equal(new Money(5m, Currency.CHF).GetHashCode(), new Money(5.00m, Currency.CHF).GetHashCode());
    }

    [Fact]
    public void Amounts_differing_by_amount_or_by_currency_are_not_equal() {
        Assert.NotEqual(new Money(5m, Currency.CHF), new Money(5.01m, Currency.CHF));
        Assert.NotEqual(new Money(5m, Currency.CHF), new Money(5m, Currency.GBP));
    }

    [Fact]
    public void An_amount_reads_with_two_decimals_and_its_currency() {
        Assert.Equal("12.50 EUR", new Money(12.5m, Currency.EUR).ToString());
    }

}
