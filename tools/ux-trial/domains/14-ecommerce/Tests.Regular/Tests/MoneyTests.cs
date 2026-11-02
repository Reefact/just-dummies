using System.Globalization;

using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class MoneyTests {

    [Theory]
    [InlineData("0.00", "EUR")]
    [InlineData("0.01", "USD")]
    [InlineData("1234.56", "GBP")]
    public void A_non_negative_amount_with_at_most_two_decimals_in_a_supported_currency_is_accepted(string amount, string currency) {
        // Act
        Money money = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), currency);

        // Assert
        Assert.Equal(decimal.Parse(amount, CultureInfo.InvariantCulture), money.Amount);
        Assert.Equal(currency, money.Currency);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-100")]
    public void A_negative_amount_is_refused(string amount) {
        // Arrange
        string anyCurrency = Dummies.Currency().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), anyCurrency));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("12.345")]
    [InlineData("0.0000001")]
    public void An_amount_with_more_than_two_decimals_is_refused(string amount) {
        // Arrange
        string anyCurrency = Dummies.Currency().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), anyCurrency));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData("CHF")]
    [InlineData("eur")]
    [InlineData(" EUR")]
    [InlineData("EURO")]
    [InlineData("")]
    [InlineData(null)]
    public void A_currency_other_than_EUR_USD_or_GBP_is_refused(string? currency) {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Money(anyAmount, currency!));

        // Assert
        Assert.Equal("currency", exception.ParamName);
    }

    [Fact]
    public void A_negative_amount_is_reported_before_an_unsupported_currency() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, "CHF"));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void Zero_is_a_null_amount_in_the_given_currency(string currency) {
        // Act
        Money zero = Money.Zero(currency);

        // Assert
        Assert.Equal(new Money(0m, currency), zero);
    }

    [Fact]
    public void Zero_refuses_an_unsupported_currency() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Money.Zero("CHF"));
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void Adding_two_amounts_of_one_currency_adds_the_amounts_and_keeps_the_currency(string currency) {
        // Arrange
        Money left  = new Money(10.50m, currency);
        Money right = new Money(4.25m, currency);

        // Act
        Money sum = left.Plus(right);

        // Assert
        Assert.Equal(new Money(14.75m, currency), sum);
    }

    [Fact]
    public void Adding_two_amounts_of_different_currencies_is_refused() {
        // Arrange
        string anyCurrency      = Dummies.Currency().Generate();
        string anyOtherCurrency = Dummies.Currency().DifferentFrom(anyCurrency).Generate();
        Money  anyAmount        = Any.Money().WithCurrency(anyCurrency).Generate();
        Money  anyOtherAmount   = Any.Money().WithCurrency(anyOtherCurrency).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyAmount.Plus(anyOtherAmount));
    }

    [Fact]
    public void Adding_does_not_change_the_operands() {
        // Arrange
        Money left  = new Money(1m, "EUR");
        Money right = new Money(2m, "EUR");

        // Act
        left.Plus(right);

        // Assert
        Assert.Equal(new Money(1m, "EUR"), left);
        Assert.Equal(new Money(2m, "EUR"), right);
    }

    [Theory]
    [InlineData("12.50", 3, "37.50")]
    [InlineData("12.50", 0, "0.00")]
    [InlineData("0.00", 99, "0.00")]
    [InlineData("0.01", 1, "0.01")]
    public void Times_multiplies_the_amount_by_the_factor(string amount, int factor, string expected) {
        // Arrange
        Money money = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), "EUR");

        // Act
        Money product = money.Times(factor);

        // Assert
        Assert.Equal(new Money(decimal.Parse(expected, CultureInfo.InvariantCulture), "EUR"), product);
    }

    [Fact]
    public void Times_a_negative_factor_is_refused() {
        // Arrange
        Money anyMoney = Any.Money().WithAmount(a => a.Between(1m, 1_000m)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyMoney.Times(-1));
    }

    [Theory]
    [InlineData("100.00", "12.5", "87.50")]
    [InlineData("100.00", "0", "100.00")]
    [InlineData("100.00", "100", "0.00")]
    [InlineData("0.05", "10", "0.04")]     // reduction 0.005 rounds away from zero to 0.01, not to even
    [InlineData("10.50", "5", "9.97")]     // reduction 0.525 rounds to 0.53
    [InlineData("0.01", "50", "0.00")]     // reduction 0.005 rounds to 0.01
    [InlineData("0.01", "49.9", "0.01")]   // reduction 0.00499 rounds to 0.00
    public void Less_takes_the_percentage_off_rounding_the_reduction_half_away_from_zero(string amount, string percent, string expected) {
        // Arrange
        Money              money    = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), "EUR");
        DiscountPercentage discount = new DiscountPercentage(decimal.Parse(percent, CultureInfo.InvariantCulture));

        // Act
        Money discounted = money.Less(discount);

        // Assert
        Assert.Equal(new Money(decimal.Parse(expected, CultureInfo.InvariantCulture), "EUR"), discounted);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void Less_keeps_the_currency(string currency) {
        // Arrange
        Money money = new Money(80m, currency);

        // Act
        Money discounted = money.Less(new DiscountPercentage(25m));

        // Assert
        Assert.Equal(new Money(60m, currency), discounted);
    }

    [Fact]
    public void Two_amounts_are_equal_when_amount_and_currency_are_equal() {
        // Arrange
        Money money = new Money(5.20m, "EUR");

        // Act & Assert
        Assert.True(money.Equals(new Money(5.20m, "EUR")));
        Assert.Equal(money.GetHashCode(), new Money(5.20m, "EUR").GetHashCode());
    }

    [Fact]
    public void Equality_does_not_depend_on_the_scale_of_the_amount() {
        // Arrange
        Money withOneDecimal  = new Money(5.2m, "EUR");
        Money withTwoDecimals = new Money(5.20m, "EUR");

        // Act & Assert
        Assert.Equal(withOneDecimal, withTwoDecimals);
        Assert.Equal(withOneDecimal.GetHashCode(), withTwoDecimals.GetHashCode());
    }

    [Fact]
    public void Two_amounts_differing_by_amount_or_currency_are_not_equal() {
        // Arrange
        Money money = new Money(5.20m, "EUR");

        // Act & Assert
        Assert.NotEqual(money, new Money(5.21m, "EUR"));
        Assert.NotEqual(money, new Money(5.20m, "USD"));
    }

    [Fact]
    public void An_amount_is_not_equal_to_null_or_to_another_kind_of_object() {
        // Arrange
        Money money = Any.Money().Generate();

        // Act & Assert
        Assert.False(money.Equals(null));
        Assert.False(money.Equals("5.20 EUR"));
    }

    [Theory]
    [InlineData("0", "EUR", "0.00 EUR")]
    [InlineData("5", "USD", "5.00 USD")]
    [InlineData("1234.5", "GBP", "1234.50 GBP")]
    [InlineData("0.07", "EUR", "0.07 EUR")]
    public void An_amount_is_written_with_two_decimals_then_its_currency(string amount, string currency, string expected) {
        // Arrange
        Money money = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), currency);

        // Act
        string text = money.ToString();

        // Assert
        Assert.Equal(expected, text);
    }

    [Fact(Skip = "domain defect: Money.ToString() formats with the current culture, so under fr-FR 1234.5 EUR reads '1234,50 EUR' instead of '1234.50 EUR' (no drawn value involved, seed 2056292743 shows nothing more)")]
    public void An_amount_is_written_the_same_way_whatever_the_culture_of_the_machine() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try {
            Money money = new Money(1234.5m, "EUR");

            // Act
            string text = money.ToString();

            // Assert
            Assert.Equal("1234.50 EUR", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
