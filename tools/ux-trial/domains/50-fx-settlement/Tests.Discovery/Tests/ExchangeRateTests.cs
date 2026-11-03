using System.Globalization;

using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests;

public sealed class ExchangeRateTests {

    private static decimal Dec(string value) {
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-0.00001")]
    [InlineData("1000000.00001")]
    [InlineData("1000001")]
    public void A_rate_outside_zero_to_one_million_is_refused(string value) {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().WithQuote(Currency.Usd).Generate();

        // Act
        Action construct = () => new ExchangeRate(anyPair, Dec(value));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Theory]
    [InlineData("1000000")]
    [InlineData("0.00001")]
    public void The_bounds_of_the_range_are_themselves_quotable(string value) {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().WithQuote(Currency.Usd).Generate();

        // Act
        ExchangeRate rate = new ExchangeRate(anyPair, Dec(value));

        // Assert
        Assert.Equal(Dec(value), rate.Value);
    }

    [Theory]
    [InlineData("EUR", "USD", "1.084251")]
    [InlineData("EUR", "USD", "1.0000001")]
    [InlineData("USD", "JPY", "151.2375")]
    [InlineData("EUR", "KRW", "1450.0001")]
    [InlineData("JPY", "USD", "0.006667")]
    public void A_rate_with_more_decimal_places_than_its_pair_is_refused(string @base, string quote, string value) {
        CurrencyPair pair = Fixtures.Pair(Currency.Iso(@base), Currency.Iso(quote));

        Action construct = () => new ExchangeRate(pair, Dec(value));

        Assert.Throws<ArgumentException>(construct);
    }

    [Theory]
    [InlineData("EUR", "USD", "1.08425")]
    [InlineData("EUR", "USD", "1.0842")]
    [InlineData("EUR", "USD", "1")]
    [InlineData("USD", "JPY", "151.237")]
    [InlineData("USD", "JPY", "151.2")]
    [InlineData("EUR", "KRW", "1450.125")]
    [InlineData("JPY", "USD", "0.00667")]
    public void A_rate_within_its_pairs_decimal_places_is_accepted(string @base, string quote, string value) {
        CurrencyPair pair = Fixtures.Pair(Currency.Iso(@base), Currency.Iso(quote));

        ExchangeRate rate = new ExchangeRate(pair, Dec(value));

        Assert.Equal(Dec(value), rate.Value);
        Assert.Equal(pair, rate.Pair);
    }

    [Fact]
    public void A_rate_without_pair_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ExchangeRate(null!, 1m));
    }

    [Fact]
    public void Invert_gives_the_rate_of_the_inverse_pair_rounded_to_its_places() {
        // 1 / 1.08425 = 0.9222965...
        ExchangeRate inverse = Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m).Invert(MidpointRounding.ToEven);

        Assert.Equal(Fixtures.Pair(Currency.Usd, Currency.Eur), inverse.Pair);
        Assert.Equal(0.92230m, inverse.Value);
    }

    [Fact]
    public void Invert_rounds_to_the_places_of_the_inverse_pair_not_of_the_original() {
        // USD/JPY has three places; JPY/USD has five. 1 / 150 = 0.0066666...
        ExchangeRate inverse = Fixtures.Rate(Currency.Usd, Currency.Jpy, 150m).Invert(MidpointRounding.ToEven);

        Assert.Equal(0.00667m, inverse.Value);

        // And back: JPY/USD 0.00667 is USD/JPY 149.925 (1 / 0.00667 = 149.9250374...).
        ExchangeRate back = inverse.Invert(MidpointRounding.ToEven);

        Assert.Equal(149.925m, back.Value);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "0.01562")]
    [InlineData(MidpointRounding.AwayFromZero, "0.01563")]
    public void Invert_applies_the_midpoint_mode_it_is_given(MidpointRounding mode, string expected) {
        // 1 / 64 = 0.015625 exactly: a midpoint at five places.
        ExchangeRate inverse = Fixtures.Rate(Currency.Eur, Currency.Usd, 64m).Invert(mode);

        Assert.Equal(Dec(expected), inverse.Value);
    }

    [Fact]
    public void Invert_refuses_a_rate_whose_inverse_rounds_to_zero() {
        // 1 / 1 000 000 = 0.000001, which is 0.00000 at five places.
        ExchangeRate huge = Fixtures.Rate(Currency.Eur, Currency.Usd, 1_000_000m);

        Action invert = () => huge.Invert(MidpointRounding.ToEven);

        Assert.Throws<InvalidOperationException>(invert);
    }

    [Fact]
    public void Invert_refuses_an_undefined_midpoint_mode() {
        // Arrange
        ExchangeRate anyRate = Any.ExchangeRate().Generate();

        // Act
        Action invert = () => anyRate.Invert((MidpointRounding)99);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(invert);
    }

    [Fact]
    public void Convert_multiplies_an_amount_in_the_base_currency() {
        // 100.00 EUR * 1.08425 = 108.425 USD, a midpoint at two places.
        ExchangeRate rate = Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m);

        Assert.Equal(Fixtures.Usd(108.43m), rate.Convert(Fixtures.Eur(100.00m), MidpointRounding.AwayFromZero));
        Assert.Equal(Fixtures.Usd(108.42m), rate.Convert(Fixtures.Eur(100.00m), MidpointRounding.ToEven));
    }

    [Fact]
    public void Convert_divides_an_amount_in_the_quote_currency() {
        // 108.43 USD / 1.08425 = 100.0046... EUR
        ExchangeRate rate = Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m);

        Assert.Equal(Fixtures.Eur(100.00m), rate.Convert(Fixtures.Usd(108.43m), MidpointRounding.ToEven));
    }

    [Fact]
    public void Convert_rounds_to_the_minor_units_of_the_target_currency() {
        // 100.00 USD * 151.237 = 15 123.7 JPY, which has no minor units.
        ExchangeRate rate = Fixtures.Rate(Currency.Usd, Currency.Jpy, 151.237m);

        Money converted = rate.Convert(Fixtures.Usd(100.00m), MidpointRounding.ToEven);

        Assert.Equal(Fixtures.Jpy(15124m), converted);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "150")]
    [InlineData(MidpointRounding.AwayFromZero, "151")]
    public void Convert_applies_the_midpoint_mode_at_the_target_minor_units(MidpointRounding mode, string expected) {
        // 1.00 USD * 150.5 = 150.5 JPY, a midpoint at zero places.
        ExchangeRate rate = Fixtures.Rate(Currency.Usd, Currency.Jpy, 150.5m);

        Assert.Equal(Fixtures.Jpy(Dec(expected)), rate.Convert(Fixtures.Usd(1.00m), mode));
    }

    [Fact]
    public void Convert_keeps_the_sign_of_the_amount() {
        ExchangeRate rate = Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m);

        Assert.Equal(Fixtures.Usd(-108.43m), rate.Convert(Fixtures.Eur(-100.00m), MidpointRounding.AwayFromZero));
        Assert.Equal(Fixtures.Usd(-108.42m), rate.Convert(Fixtures.Eur(-100.00m), MidpointRounding.ToEven));
    }

    [Fact]
    public void Convert_refuses_an_amount_in_neither_currency_of_the_pair() {
        // Arrange
        ExchangeRate anyRate  = Any.ExchangeRate().Generate();
        Money        anyOther = Any.Money()
                                   .WithCurrency(c => c.WithCode(code => code.Except(anyRate.Pair.Base.Code, anyRate.Pair.Quote.Code)))
                                   .Generate();

        // Act
        Action convert = () => anyRate.Convert(anyOther, MidpointRounding.ToEven);

        // Assert
        Assert.Throws<ArgumentException>(convert);
    }

    [Fact]
    public void Two_rates_are_equal_when_pair_and_value_are_equal() {
        Assert.Equal(Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m), Fixtures.Rate(Currency.Eur, Currency.Usd, 1.084250m));
        Assert.NotEqual(Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m), Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08426m));
        Assert.NotEqual(Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m), Fixtures.Rate(Currency.Eur, Currency.Gbp, 1.08425m));
    }

    [Fact]
    public void A_rate_is_written_with_the_places_of_its_pair() {
        Assert.Equal("EUR/USD 1.08400", Fixtures.Rate(Currency.Eur, Currency.Usd, 1.084m).ToString());
        Assert.Equal("USD/JPY 151.200", Fixtures.Rate(Currency.Usd, Currency.Jpy, 151.2m).ToString());
    }

}
