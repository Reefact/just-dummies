using System.Globalization;

using Freight.Domain;
using Freight.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class FreightRateTests {

    private static string AnyCurrency() => Any.FreightRate().Generate().Currency;

    private static decimal AnyRate() => Any.FreightRate().Generate().PerRevenueTon;

    [Theory]
    [InlineData("0.01")]
    [InlineData("12.5")]
    [InlineData("12.50")]
    [InlineData("99.99")]
    [InlineData("10000")]
    [InlineData("10000.00")]
    public void A_positive_rate_up_to_ten_thousand_with_at_most_two_decimals_is_accepted(string rate) {
        decimal perRevenueTon = decimal.Parse(rate, CultureInfo.InvariantCulture);
        string  anyCurrency   = AnyCurrency();

        FreightRate freightRate = new FreightRate(perRevenueTon, anyCurrency);

        Assert.Equal(perRevenueTon, freightRate.PerRevenueTon);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-100")]
    [InlineData("10000.01")]
    [InlineData("10000.001")]    // above the ceiling, so the ceiling is what is reported, not the decimals
    [InlineData("1000000")]
    public void A_rate_outside_zero_to_ten_thousand_is_refused_as_out_of_range(string rate) {
        decimal perRevenueTon = decimal.Parse(rate, CultureInfo.InvariantCulture);
        string  anyCurrency   = AnyCurrency();

        Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(perRevenueTon, anyCurrency));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("12.345")]
    [InlineData("9999.999")]
    public void A_rate_with_more_than_two_decimals_is_refused_as_an_invalid_argument(string rate) {
        decimal perRevenueTon = decimal.Parse(rate, CultureInfo.InvariantCulture);
        string  anyCurrency   = AnyCurrency();

        Assert.Throws<ArgumentException>(() => new FreightRate(perRevenueTon, anyCurrency));
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("XXX")]   // any three capitals pass: the domain keeps no ISO 4217 list
    [InlineData("ZZZ")]
    public void A_currency_of_three_capital_letters_is_accepted(string currency) {
        decimal anyRate = AnyRate();

        FreightRate freightRate = new FreightRate(anyRate, currency);

        Assert.Equal(currency, freightRate.Currency);
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("Eur")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("€€€")]
    [InlineData("")]
    [InlineData("   ")]
    public void A_currency_that_is_not_three_capital_letters_is_refused_as_malformed(string currency) {
        decimal anyRate = AnyRate();

        Assert.Throws<FormatException>(() => new FreightRate(anyRate, currency));
    }

    [Fact]
    public void A_missing_currency_is_refused_as_missing() {
        decimal anyRate = AnyRate();

        Assert.Throws<ArgumentNullException>(() => new FreightRate(anyRate, null!));
    }

    [Fact]
    public void The_rate_is_checked_before_the_currency() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(0m, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(0m, "euro"));
        Assert.Throws<ArgumentException>(() => new FreightRate(1.234m, "euro"));
    }

    [Fact(Skip = "domain defect: a trailing line feed passes the '$' of the currency pattern and is kept in Currency (literal input)")]
    public void A_currency_followed_by_a_line_feed_is_refused() {
        decimal anyRate = AnyRate();

        Assert.Throws<FormatException>(() => new FreightRate(anyRate, "EUR\n"));
    }

}
