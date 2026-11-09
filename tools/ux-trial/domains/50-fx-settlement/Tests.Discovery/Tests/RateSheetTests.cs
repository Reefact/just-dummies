using System.Globalization;

using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests;

public sealed class RateSheetTests {

    // USD is the pivot: EUR and AUD are quoted as USD per unit, the yen as yen per dollar.
    private static RateSheet StandardSheet() {
        return new RateSheet(Currency.Usd, Fixtures.Wednesday, [
            Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m),
            Fixtures.Rate(Currency.Usd, Currency.Jpy, 151.237m),
            Fixtures.Rate(Fixtures.Aud, Currency.Usd, 0.76543m),
        ]);
    }

    [Theory]
    [InlineData("2025-03-15")]
    [InlineData("2025-03-16")]
    public void A_sheet_is_not_fixed_on_a_weekend(string asOf) {
        // Arrange
        ExchangeRate anyQuotedRate = Any.ExchangeRate().WithPair(Fixtures.Pair(Currency.Eur, Currency.Usd)).Generate();

        // Act
        Action construct = () => new RateSheet(Currency.Usd, DateOnly.Parse(asOf), [anyQuotedRate]);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_sheet_quotes_at_least_one_currency() {
        // Arrange
        Currency pivot = Currency.Usd;

        // Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(pivot, Fixtures.Wednesday, []));
    }

    [Fact]
    public void A_sheet_refuses_a_rate_that_does_not_involve_the_pivot() {
        // Arrange
        ExchangeRate crossRate = Fixtures.Rate(Currency.Eur, Currency.Gbp, 0.85m);
        ExchangeRate anyQuotedRate = Any.ExchangeRate().WithPair(Fixtures.Pair(Currency.Eur, Currency.Usd)).Generate();

        // Act
        Action construct = () => new RateSheet(Currency.Usd, Fixtures.Wednesday, [anyQuotedRate, crossRate]);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_sheet_refuses_a_currency_quoted_twice_even_the_other_way_round() {
        // Arrange
        ExchangeRate eurUsd = Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m);
        ExchangeRate usdEur = Fixtures.Rate(Currency.Usd, Currency.Eur, 0.92230m);

        // Act
        Action constructInverse   = () => new RateSheet(Currency.Usd, Fixtures.Wednesday, [eurUsd, usdEur]);
        Action constructSameTwice = () => new RateSheet(Currency.Usd, Fixtures.Wednesday, [eurUsd, eurUsd]);

        // Assert
        Assert.Throws<ArgumentException>(constructInverse);
        Assert.Throws<ArgumentException>(constructSameTwice);
    }

    [Fact]
    public void A_sheet_refuses_a_null_rate() {
        // Arrange
        ExchangeRate anyQuotedRate = Any.ExchangeRate().WithPair(Fixtures.Pair(Currency.Eur, Currency.Usd)).Generate();

        // Act
        Action construct = () => new RateSheet(Currency.Usd, Fixtures.Wednesday, [anyQuotedRate, null!]);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_sheet_refuses_a_missing_pivot_or_missing_rates() {
        // Arrange
        ExchangeRate anyQuotedRate = Any.ExchangeRate().WithPair(Fixtures.Pair(Currency.Eur, Currency.Usd)).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new RateSheet(null!, Fixtures.Wednesday, [anyQuotedRate]));
        Assert.Throws<ArgumentNullException>(() => new RateSheet(Currency.Usd, Fixtures.Wednesday, null!));
    }

    [Fact]
    public void A_sheet_exposes_its_pivot_date_and_one_rate_per_quoted_currency() {
        RateSheet sheet = StandardSheet();

        Assert.Equal(Currency.Usd, sheet.Pivot);
        Assert.Equal(Fixtures.Wednesday, sheet.AsOf);
        Assert.Equal(3, sheet.Rates.Count);
    }

    [Fact]
    public void A_sheet_covers_its_pivot_and_every_quoted_currency_in_either_orientation() {
        RateSheet sheet = StandardSheet();

        Assert.True(sheet.Covers(Currency.Usd));
        Assert.True(sheet.Covers(Currency.Eur));
        Assert.True(sheet.Covers(Currency.Jpy));
        Assert.True(sheet.Covers(Fixtures.Aud));
    }

    [Fact]
    public void A_sheet_does_not_cover_a_currency_it_does_not_quote() {
        // Arrange
        Currency anyUnquoted = Any.Currency().WithCode(c => c.Except("USD", "EUR", "JPY", "AUD")).Generate();

        // Assert
        Assert.False(StandardSheet().Covers(anyUnquoted));
    }

    [Fact]
    public void A_sheet_covers_a_pair_when_it_covers_both_currencies() {
        // Arrange
        RateSheet sheet      = StandardSheet();
        Currency  anyUnquoted = Any.Currency().WithCode(c => c.Except("USD", "EUR", "JPY", "AUD")).Generate();

        // Assert
        Assert.True(sheet.Covers(Fixtures.Pair(Currency.Eur, Currency.Usd)));
        Assert.True(sheet.Covers(Fixtures.Pair(Currency.Usd, Currency.Eur)));
        Assert.True(sheet.Covers(Fixtures.Pair(Currency.Eur, Currency.Jpy)));
        Assert.True(sheet.Covers(Fixtures.Pair(Currency.Jpy, Fixtures.Aud)));
        Assert.False(sheet.Covers(Fixtures.Pair(Currency.Eur, anyUnquoted)));
        Assert.False(sheet.Covers(Fixtures.Pair(anyUnquoted, Currency.Usd)));
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven)]
    [InlineData(MidpointRounding.AwayFromZero)]
    public void A_quoted_pair_is_given_as_quoted_whatever_the_mode(MidpointRounding mode) {
        ExchangeRate rate = StandardSheet().RateFor(Fixtures.Pair(Currency.Eur, Currency.Usd), mode);

        Assert.Equal(Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m), rate);
    }

    [Fact]
    public void The_inverse_of_a_quoted_pair_is_the_quoted_rate_inverted() {
        // 1 / 1.08425 = 0.9222965...
        ExchangeRate rate = StandardSheet().RateFor(Fixtures.Pair(Currency.Usd, Currency.Eur), MidpointRounding.ToEven);

        Assert.Equal(Fixtures.Rate(Currency.Usd, Currency.Eur, 0.92230m), rate);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "0.01562")]
    [InlineData(MidpointRounding.AwayFromZero, "0.01563")]
    public void The_inverse_of_a_quoted_pair_is_rounded_with_the_mode_given(MidpointRounding mode, string expected) {
        // 1 / 64 = 0.015625: a midpoint at five places.
        RateSheet sheet = new RateSheet(Currency.Usd, Fixtures.Wednesday, [Fixtures.Rate(Currency.Eur, Currency.Usd, 64m)]);

        ExchangeRate rate = sheet.RateFor(Fixtures.Pair(Currency.Usd, Currency.Eur), mode);

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), rate.Value);
    }

    [Fact]
    public void A_cross_rate_goes_through_the_pivot() {
        // EUR/JPY = 1.08425 USD per EUR * 151.237 JPY per USD = 163.97871725, three places for a yen quote.
        ExchangeRate rate = StandardSheet().RateFor(Fixtures.Pair(Currency.Eur, Currency.Jpy), MidpointRounding.ToEven);

        Assert.Equal(Fixtures.Rate(Currency.Eur, Currency.Jpy, 163.979m), rate);
    }

    [Fact]
    public void A_cross_rate_is_rounded_once_at_the_end() {
        // EUR/AUD = 1.08425 / 0.76543 = 1.416524...; rounding AUD's inverse to five places first would give 1.41653.
        ExchangeRate rate = StandardSheet().RateFor(Fixtures.Pair(Currency.Eur, Fixtures.Aud), MidpointRounding.ToEven);

        Assert.Equal(1.41652m, rate.Value);
    }

    [Fact]
    public void A_cross_rate_can_be_read_the_other_way_round() {
        // JPY/EUR = (1 / 151.237) / 1.08425 = 0.0060983..., five places for a euro quote.
        ExchangeRate rate = StandardSheet().RateFor(Fixtures.Pair(Currency.Jpy, Currency.Eur), MidpointRounding.ToEven);

        Assert.Equal(Fixtures.Rate(Currency.Jpy, Currency.Eur, 0.00610m), rate);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "0.50000")]
    [InlineData(MidpointRounding.AwayFromZero, "0.50001")]
    public void A_cross_rate_is_rounded_with_the_mode_given(MidpointRounding mode, string expected) {
        // 1.00001 / 2.00000 = 0.500005: a midpoint at five places.
        RateSheet sheet = new RateSheet(Currency.Usd, Fixtures.Wednesday, [
            Fixtures.Rate(Currency.Eur, Currency.Usd, 1.00001m),
            Fixtures.Rate(Currency.Gbp, Currency.Usd, 2.00000m),
        ]);

        ExchangeRate rate = sheet.RateFor(Fixtures.Pair(Currency.Eur, Currency.Gbp), mode);

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), rate.Value);
    }

    [Fact]
    public void A_cross_rate_that_rounds_to_zero_is_refused() {
        // JPY is worth 1/150 USD and GBP 1 000 000 USD: JPY/GBP is 0.0000000067.
        RateSheet sheet = new RateSheet(Currency.Usd, Fixtures.Wednesday, [
            Fixtures.Rate(Currency.Usd, Currency.Jpy, 150m),
            Fixtures.Rate(Currency.Gbp, Currency.Usd, 1_000_000m),
        ]);

        Action price = () => sheet.RateFor(Fixtures.Pair(Currency.Jpy, Currency.Gbp), MidpointRounding.ToEven);

        Assert.Throws<InvalidOperationException>(price);
    }

    [Fact]
    public void A_pair_the_sheet_does_not_cover_has_no_rate() {
        // Arrange
        Currency anyUnquoted = Any.Currency().WithCode(c => c.Except("USD", "EUR", "JPY", "AUD")).Generate();
        CurrencyPair unpriced = Fixtures.Pair(Currency.Eur, anyUnquoted);

        // Act
        Action price = () => StandardSheet().RateFor(unpriced, MidpointRounding.ToEven);

        // Assert
        Assert.Throws<InvalidOperationException>(price);
    }

    [Fact]
    public void An_undefined_mode_is_refused_where_it_would_be_used() {
        Action priceCross = () => StandardSheet().RateFor(Fixtures.Pair(Currency.Eur, Currency.Jpy), (MidpointRounding)99);

        Assert.Throws<ArgumentOutOfRangeException>(priceCross);
    }

}
