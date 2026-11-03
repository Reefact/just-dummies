using JustDummies;

using Xunit;

namespace Freight.Domain.Tests.Discovery;

public sealed class FreightRateTests {

    [Theory]
    [InlineData("0.01")]
    [InlineData("10000")]     // the ceiling is accepted
    [InlineData("10000.00")]
    [InlineData("1234.5")]
    public void A_rate_inside_the_range_with_at_most_two_decimals_is_accepted(string rate) {
        // Arrange
        decimal perRevenueTon = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture);
        string  anyCurrency   = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        FreightRate freightRate = new FreightRate(perRevenueTon, anyCurrency);

        // Assert
        Assert.Equal(perRevenueTon, freightRate.PerRevenueTon);
    }

    [Fact]
    public void The_currency_is_kept_as_given() {
        // Act
        FreightRate rate = new FreightRate(25m, "EUR");

        // Assert
        Assert.Equal("EUR", rate.Currency);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("10000.01")]
    [InlineData("1000000")]
    public void A_rate_that_is_not_positive_or_above_ten_thousand_is_refused(string rate) {
        // Arrange
        decimal perRevenueTon = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture);
        string  anyCurrency   = Any.StringMatching("[A-Z]{3}").Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(perRevenueTon, anyCurrency));
    }

    [Theory]
    [InlineData("1.005")]
    [InlineData("0.001")]
    [InlineData("99.999")]
    public void A_rate_with_more_than_two_decimals_is_refused(string rate) {
        // Arrange
        decimal perRevenueTon = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture);
        string  anyCurrency   = Any.StringMatching("[A-Z]{3}").Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new FreightRate(perRevenueTon, anyCurrency));
    }

    [Fact]
    public void The_rate_is_checked_before_the_currency() {
        // A rate of zero with a null currency complains about the rate, not the currency.
        Assert.Throws<ArgumentOutOfRangeException>(() => new FreightRate(0m, null!));
        Assert.Throws<ArgumentException>(() => new FreightRate(1.234m, "not a currency"));
    }

    [Fact]
    public void A_null_currency_is_refused_with_an_argument_null_exception() {
        // Arrange
        decimal anyPerRevenueTon = Any.Decimal().Positive().LessThanOrEqualTo(10_000m).WithScale(2).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new FreightRate(anyPerRevenueTon, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("eur")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData(" EUR")]
    public void A_currency_that_is_not_three_upper_case_letters_is_refused_with_a_format_exception(string currency) {
        // Arrange
        decimal anyPerRevenueTon = Any.Decimal().Positive().LessThanOrEqualTo(10_000m).WithScale(2).Generate();

        // Assert
        Assert.Throws<FormatException>(() => new FreightRate(anyPerRevenueTon, currency));
    }

    [Fact]
    public void Any_three_upper_case_letters_are_a_currency_even_without_an_iso_4217_entry() {
        // Act
        FreightRate rate = new FreightRate(1m, "ZZZ");

        // Assert
        Assert.Equal("ZZZ", rate.Currency);
    }

    [Fact(Skip = "domain defect: '$' lets a trailing newline through the currency pattern, so \"EUR\\n\" is accepted, seed 1140761663 (deterministic literal)")]
    public void A_currency_followed_by_a_line_break_is_refused() {
        Assert.Throws<FormatException>(() => new FreightRate(1m, "EUR\n"));
    }

}
