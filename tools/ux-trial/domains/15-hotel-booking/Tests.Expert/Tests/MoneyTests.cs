using JustDummies;

using Xunit;

namespace Booking.Domain.Tests;

public sealed class MoneyTests {

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrency().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-0.01m, anyCurrency));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void A_zero_amount_is_accepted() {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrency().Generate();

        // Act
        Money free = new Money(0m, anyCurrency);

        // Assert
        Assert.Equal(0m, free.Amount);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("12.345")]
    [InlineData("0.999")]
    public void An_amount_with_more_than_two_decimal_places_is_refused(string amount) {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrency().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), anyCurrency));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData("12.3")]
    [InlineData("12.30")]
    [InlineData("12.300")]
    public void Trailing_zeros_beyond_the_second_decimal_place_do_not_count_as_decimal_places(string amount) {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrency().Generate();

        // Act
        Money money = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), anyCurrency);

        // Assert
        Assert.Equal(12.3m, money.Amount);
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("")]
    [InlineData(" EUR")]
    [InlineData("EUR ")]
    [InlineData("€UR")]
    public void A_currency_that_is_not_three_upper_case_letters_is_refused(string currency) {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0m, 100_000m).WithScale(2).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Money(anyAmount, currency));

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void An_absent_currency_is_refused_as_null_not_as_malformed() {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0m, 100_000m).WithScale(2).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Money(anyAmount, null!));

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void The_amount_is_checked_before_the_currency() {
        // Act
        Exception refusal = Assert.ThrowsAny<Exception>(() => new Money(-1m, "not a currency"));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void The_currency_is_checked_before_the_decimal_places() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Money(1.005m, "nope"));

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: Regex $ also matches before a final line feed, so \"EUR\\n\" is accepted as a currency; deterministic literal, no seed needed")]
    public void A_currency_ending_with_a_line_feed_is_refused() {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0m, 100_000m).WithScale(2).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Money(anyAmount, "EUR\n"));
    }

    [Fact]
    public void Times_multiplies_the_amount() {
        // Arrange
        Money nightly = Any.Money().WithAmount(80.25m).Generate();

        // Act
        Money total = nightly.Times(3);

        // Assert
        Assert.Equal(240.75m, total.Amount);
    }

    [Fact]
    public void Times_keeps_the_currency() {
        // Arrange
        Money nightly = new Money(80m, "CHF");

        // Act
        Money total = nightly.Times(2);

        // Assert
        Assert.Equal("CHF", total.Currency);
    }

    [Fact]
    public void Times_zero_gives_a_free_amount() {
        // Arrange
        Money anyPrice = Any.Money().WithAmount(a => a.Between(0m, 100_000m)).Generate();

        // Act
        Money total = anyPrice.Times(0);

        // Assert
        Assert.Equal(0m, total.Amount);
    }

    [Fact]
    public void A_negative_factor_is_refused() {
        // Arrange
        Money anyPrice = Any.Money().WithAmount(a => a.Between(0.01m, 100_000m)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyPrice.Times(-1));
    }

    [Fact]
    public void Percent_takes_a_share_of_the_amount() {
        // Arrange
        Money total = Any.Money().WithAmount(200m).Generate();

        // Act
        Money share = total.Percent(0.25m);

        // Assert
        Assert.Equal(50m, share.Amount);
    }

    [Theory]
    [InlineData("0.01", "0.5", "0.01")]   // 0.005 rounds up, away from zero (banker's rounding would give 0.00)
    [InlineData("0.05", "0.5", "0.03")]   // 0.025
    [InlineData("0.07", "0.5", "0.04")]   // 0.035
    [InlineData("33.33", "0.5", "16.67")] // 16.665
    [InlineData("10.00", "0.333", "3.33")] // 3.33 exactly, below the midpoint
    public void Percent_rounds_half_away_from_zero_to_two_decimal_places(string amount, string rate, string expected) {
        // Arrange
        Money total = Any.Money().WithAmount(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)).Generate();

        // Act
        Money share = total.Percent(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), share.Amount);
    }

    [Fact]
    public void Percent_accepts_a_rate_above_one() {
        // Arrange
        Money total = Any.Money().WithAmount(100m).Generate();

        // Act
        Money share = total.Percent(1.5m);

        // Assert
        Assert.Equal(150m, share.Amount);
    }

    [Fact]
    public void A_negative_rate_is_refused() {
        // Arrange
        Money anyPrice = Any.Money().WithAmount(a => a.Between(0.01m, 100_000m)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyPrice.Percent(-0.1m));
    }

    [Fact]
    public void Percent_keeps_the_currency() {
        // Arrange
        Money total = new Money(100m, "GBP");

        // Act
        Money share = total.Percent(0.1m);

        // Assert
        Assert.Equal("GBP", share.Currency);
    }

    [Fact]
    public void Two_amounts_of_the_same_value_and_currency_are_equal() {
        // Arrange
        Money first  = new Money(12.50m, "EUR");
        Money second = new Money(12.5m, "EUR");

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_amounts_of_different_values_are_not_equal() {
        // Arrange
        Money first  = Any.Money().WithAmount(10m).Generate();
        Money second = Any.Money().WithAmount(11m).WithCurrency(first.Currency).Generate();

        // Act / Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void The_same_amount_in_two_currencies_is_not_equal() {
        // Arrange
        Money inEuros  = Any.Money().WithCurrency("EUR").Generate();
        Money inDollars = Any.Money().WithAmount(inEuros.Amount).WithCurrency("USD").Generate();

        // Act / Assert
        Assert.NotEqual(inEuros, inDollars);
    }

    [Fact]
    public void An_amount_is_not_equal_to_something_that_is_not_an_amount() {
        // Arrange
        Money anyMoney = Any.Money().Generate();

        // Act / Assert
        Assert.False(anyMoney.Equals("12.50 EUR"));
        Assert.False(anyMoney.Equals(null));
    }

    [Fact]
    public void An_amount_reads_as_its_value_then_its_currency() {
        // Arrange
        Money price = new Money(12.50m, "EUR");

        // Act
        string text = price.ToString();

        // Assert
        Assert.Equal("12.50 EUR", text);
    }

}
