using System.Globalization;

using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Regular;

public sealed class ExchangeRateTests {

    private static decimal D(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    private static readonly CurrencyPair EurUsd = new CurrencyPair(Currency.Eur, Currency.Usd);
    private static readonly CurrencyPair UsdJpy = new CurrencyPair(Currency.Usd, Currency.Jpy);

    [Fact]
    public void A_rate_within_the_range_and_the_pairs_places_is_accepted() {
        // Act
        ExchangeRate five = new ExchangeRate(EurUsd, 1.08425m);
        ExchangeRate three = new ExchangeRate(UsdJpy, 151.237m);

        // Assert
        Assert.Equal(1.08425m, five.Value);
        Assert.Equal(151.237m, three.Value);
        Assert.Equal(EurUsd, five.Pair);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-0.00001")]
    public void A_rate_that_is_not_positive_is_refused(string value) {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRate(anyPair, D(value)));
    }

    [Fact]
    public void A_rate_above_one_million_is_refused_and_one_million_is_accepted() {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act
        ExchangeRate largest = new ExchangeRate(anyPair, 1_000_000m);

        // Assert
        Assert.Equal(ExchangeRate.MaxValue, largest.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRate(anyPair, 1_000_000.001m));
    }

    [Fact]
    public void The_smallest_rate_of_a_pair_is_one_in_its_last_place() {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act
        ExchangeRate smallest = new ExchangeRate(anyPair, 1m / (decimal)Math.Pow(10, anyPair.RateDecimalPlaces));

        // Assert
        Assert.True(smallest.Value > 0m);
    }

    [Theory]
    [InlineData("151.2371")]
    [InlineData("151.2370001")]
    public void A_rate_with_more_places_than_a_whole_unit_quote_allows_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ExchangeRate(UsdJpy, D(value)));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_rate_with_six_places_is_refused_when_the_quote_has_minor_units() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ExchangeRate(EurUsd, 1.084251m));
    }

    [Fact]
    public void Trailing_zeros_beyond_the_places_do_not_count() {
        // Act
        ExchangeRate rate = new ExchangeRate(UsdJpy, 151.2370m);

        // Assert
        Assert.Equal(151.237m, rate.Value);
    }

    [Fact]
    public void A_null_pair_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ExchangeRate(null!, 1m));
    }

    [Fact]
    public void Invert_gives_the_rate_of_the_inverse_pair() {
        // Act
        ExchangeRate inverse = new ExchangeRate(EurUsd, 1.08425m).Invert(MidpointRounding.ToEven);

        // Assert
        Assert.Equal(EurUsd.Inverse(), inverse.Pair);
        Assert.Equal(0.92230m, inverse.Value);
    }

    [Fact]
    public void Invert_rounds_to_the_places_of_the_inverse_pair() {
        // Act
        ExchangeRate toDollars = new ExchangeRate(UsdJpy, 151.237m).Invert(MidpointRounding.ToEven);
        ExchangeRate toYen = new ExchangeRate(new CurrencyPair(Currency.Jpy, Currency.Usd), 0.00661m).Invert(MidpointRounding.ToEven);

        // Assert
        Assert.Equal(0.00661m, toDollars.Value);
        Assert.Equal(151.286m, toYen.Value);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "0.01562")]
    [InlineData(MidpointRounding.AwayFromZero, "0.01563")]
    public void Invert_names_how_a_midpoint_rounds(MidpointRounding mode, string expected) {
        // Arrange: 1 / 64 = 0.015625, exactly on a midpoint of five places.
        ExchangeRate rate = new ExchangeRate(EurUsd, 64m);

        // Act
        ExchangeRate inverse = rate.Invert(mode);

        // Assert
        Assert.Equal(D(expected), inverse.Value);
    }

    [Fact]
    public void Invert_refuses_an_undefined_mode() {
        // Arrange
        ExchangeRate anyRate = Any.ExchangeRate().WithValue(v => v.Between(1m, 100m)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyRate.Invert((MidpointRounding)42));
    }

    [Fact]
    public void Invert_refuses_a_rate_whose_inverse_rounds_to_zero() {
        // Arrange: 1 / 1 000 000 = 0.000001, below the five places of USD/EUR.
        ExchangeRate rate = new ExchangeRate(EurUsd, ExchangeRate.MaxValue);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => rate.Invert(MidpointRounding.ToEven));
    }

    [Fact]
    public void Inverting_twice_a_rate_that_is_its_own_inverse_gives_the_rate_back() {
        // Arrange
        Currency anyBase  = Any.Currency().WithMinorUnitsAtLeast(1).Generate();
        Currency anyQuote = new AnyCurrency().WithMinorUnitsAtLeast(1).Except(anyBase).Generate();
        ExchangeRate parity = new ExchangeRate(new CurrencyPair(anyBase, anyQuote), 1m);

        // Act
        ExchangeRate back = parity.Invert(MidpointRounding.ToEven).Invert(MidpointRounding.ToEven);

        // Assert
        Assert.Equal(parity, back);
    }

    [Fact]
    public void Convert_multiplies_a_base_amount_into_the_quote() {
        // Act
        Money quote = new ExchangeRate(EurUsd, 1.08425m).Convert(new Money(1_000m, Currency.Eur), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(new Money(1_084.25m, Currency.Usd), quote);
    }

    [Fact]
    public void Convert_divides_a_quote_amount_into_the_base() {
        // Act
        Money @base = new ExchangeRate(EurUsd, 1.08425m).Convert(new Money(1_084.25m, Currency.Usd), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(new Money(1_000m, Currency.Eur), @base);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "105.10")]
    [InlineData(MidpointRounding.AwayFromZero, "105.11")]
    public void Convert_names_how_a_midpoint_rounds(MidpointRounding mode, string expected) {
        // Arrange: 100.10 x 1.05 = 105.105, exactly on a midpoint of two places.
        ExchangeRate rate = new ExchangeRate(EurUsd, 1.05m);

        // Act
        Money converted = rate.Convert(new Money(100.10m, Currency.Eur), mode);

        // Assert
        Assert.Equal(new Money(D(expected), Currency.Usd), converted);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "-105.10")]
    [InlineData(MidpointRounding.AwayFromZero, "-105.11")]
    public void Convert_rounds_a_negative_midpoint_symmetrically(MidpointRounding mode, string expected) {
        // Arrange
        ExchangeRate rate = new ExchangeRate(EurUsd, 1.05m);

        // Act
        Money converted = rate.Convert(new Money(-100.10m, Currency.Eur), mode);

        // Assert
        Assert.Equal(new Money(D(expected), Currency.Usd), converted);
    }

    [Fact]
    public void Convert_into_a_currency_without_minor_units_rounds_to_whole_units() {
        // Arrange: 2.00 x 150.25 = 300.5 yen.
        ExchangeRate rate = new ExchangeRate(UsdJpy, 150.25m);

        // Act / Assert
        Assert.Equal(new Money(300m, Currency.Jpy), rate.Convert(new Money(2m, Currency.Usd), MidpointRounding.ToEven));
        Assert.Equal(new Money(301m, Currency.Jpy), rate.Convert(new Money(2m, Currency.Usd), MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void Convert_refuses_an_amount_in_neither_currency_of_the_pair() {
        // Arrange
        ExchangeRate anyRate  = Any.ExchangeRate().WithValue(v => v.Between(1m, 100m)).Generate();
        Currency     anyOther = new AnyCurrency().Except(anyRate.Pair.Base, anyRate.Pair.Quote).Generate();
        Money        anyForeign = Any.Money().WithCurrency(anyOther).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => anyRate.Convert(anyForeign, MidpointRounding.ToEven));
    }

    [Fact]
    public void Convert_refuses_a_null_amount() {
        // Arrange
        ExchangeRate anyRate = Any.ExchangeRate().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyRate.Convert(null!, MidpointRounding.ToEven));
    }

    [Fact]
    public void Convert_of_zero_is_zero() {
        // Arrange
        ExchangeRate anyRate = Any.ExchangeRate().Generate();

        // Act
        Money converted = anyRate.Convert(Money.Zero(anyRate.Pair.Base), MidpointRounding.ToEven);

        // Assert
        Assert.True(converted.IsZero);
        Assert.Equal(anyRate.Pair.Quote, converted.Currency);
    }

    [Fact]
    public void Rates_are_equal_by_pair_and_value() {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act
        ExchangeRate first  = new ExchangeRate(anyPair, 2m);
        ExchangeRate second = new ExchangeRate(anyPair, 2.000m);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new ExchangeRate(anyPair, 3m));
        Assert.NotEqual(first, new ExchangeRate(anyPair.Inverse(), 2m));
    }

    [Fact]
    public void A_rate_prints_its_pair_and_value_to_the_pairs_places() {
        // Act / Assert
        Assert.Equal("EUR/USD 1.08425", new ExchangeRate(EurUsd, 1.08425m).ToString());
        Assert.Equal("EUR/USD 1.20000", new ExchangeRate(EurUsd, 1.2m).ToString());
        Assert.Equal("USD/JPY 151.237", new ExchangeRate(UsdJpy, 151.237m).ToString());
    }

}
