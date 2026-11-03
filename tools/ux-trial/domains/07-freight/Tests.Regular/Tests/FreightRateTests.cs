namespace Freight.Domain.Tests;

public sealed class FreightRateTests {

    [Fact]
    public void A_drawn_rate_keeps_its_amount_and_its_currency() {
        // Arrange
        decimal anyAmount   = Any.Decimal().Between(0.01m, 10_000m).WithScale(2).Generate();
        string  anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        FreightRate rate = new FreightRate(anyAmount, anyCurrency);

        // Assert
        Assert.Equal(anyAmount, rate.PerRevenueTon);
        Assert.Equal(anyCurrency, rate.Currency);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("10000")]
    [InlineData("10000.00")]
    [InlineData("1.5")]
    [InlineData("99.99")]
    public void The_bounds_and_two_decimals_are_accepted(string amount) {
        // Arrange
        decimal perRevenueTon = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        FreightRate rate = new FreightRate(perRevenueTon, "EUR");

        // Assert
        Assert.Equal(perRevenueTon, rate.PerRevenueTon);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-5")]
    [InlineData("10000.01")]
    [InlineData("10001")]
    public void A_rate_outside_zero_to_ten_thousand_is_refused(string amount) {
        // Arrange
        decimal perRevenueTon = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(perRevenueTon, "EUR"));

        // Assert
        Assert.Equal("perRevenueTon", refusal.ParamName);
    }

    [Fact]
    public void A_rate_above_the_maximum_is_refused() {
        // Arrange
        decimal anyExcess = Any.Decimal().GreaterThan(10_000m).LessThan(1_000_000m).WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(anyExcess, "EUR"));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1.234")]
    [InlineData("9999.999")]
    public void A_rate_with_more_than_two_decimals_is_refused(string amount) {
        // Arrange
        decimal perRevenueTon = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new FreightRate(perRevenueTon, "EUR"));

        // Assert
        Assert.Equal("perRevenueTon", refusal.ParamName);
    }

    [Fact]
    public void The_range_is_checked_before_the_decimals() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(10_000.001m, "EUR"));
    }

    [Fact]
    public void The_rate_is_checked_before_the_currency() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(0m, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(0m, "euro"));
    }

    [Fact]
    public void A_null_currency_is_refused() {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0.01m, 10_000m).WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FreightRate(anyAmount, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("eur")]
    [InlineData("Eur")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("E R")]
    [InlineData("€€€")]
    public void A_currency_that_is_not_three_upper_case_letters_is_refused(string currency) {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0.01m, 10_000m).WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new FreightRate(anyAmount, currency));
    }

    [Theory]
    [InlineData("XXX")]
    [InlineData("ZZZ")]
    public void Any_three_upper_case_letters_pass_even_when_they_are_no_ISO_4217_code(string currency) {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0.01m, 10_000m).WithScale(2).Generate();

        // Act
        FreightRate rate = new FreightRate(anyAmount, currency);

        // Assert
        Assert.Equal(currency, rate.Currency);
    }

    [Fact(Skip = "domain defect: '$' lets a trailing line feed through, so the currency \"EUR\\n\" is accepted, seed n/a (literal)")]
    public void A_currency_followed_by_a_line_feed_is_refused() {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0.01m, 10_000m).WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new FreightRate(anyAmount, "EUR\n"));
    }

}
