using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Expert;

public sealed class ExchangeRateTests {

    private static readonly CurrencyPair EurUsd = new CurrencyPair(Currency.Eur, Currency.Usd);
    private static readonly CurrencyPair UsdJpy = new CurrencyPair(Currency.Usd, Currency.Jpy);

    [Fact]
    public void A_rate_cannot_be_zero_or_negative() {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRate(anyPair, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRate(anyPair, -1.5m));
    }

    [Fact]
    public void A_rate_cannot_exceed_one_million() {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRate(anyPair, 1_000_000.001m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRate(anyPair, 1_000_000.00001m));
    }

    [Fact]
    public void One_million_and_the_smallest_quotable_rate_are_the_inclusive_limits() {
        // Act
        ExchangeRate largest  = new ExchangeRate(EurUsd, 1_000_000m);
        ExchangeRate smallest = new ExchangeRate(EurUsd, 0.00001m);

        // Assert
        Assert.Equal(1_000_000m, largest.Value);
        Assert.Equal(0.00001m, smallest.Value);
    }

    [Fact]
    public void A_pair_quoted_in_whole_units_allows_three_decimal_places_and_no_more() {
        // Arrange
        CurrencyPair anyWholeUnitQuote = Any.CurrencyPair().WithQuote(quote => quote.WithMinorUnits(0)).Generate();

        // Act
        ExchangeRate accepted = new ExchangeRate(anyWholeUnitQuote, 151.237m);
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ExchangeRate(anyWholeUnitQuote, 151.2371m));

        // Assert
        Assert.Equal(151.237m, accepted.Value);
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_pair_quoted_in_a_currency_with_minor_units_allows_five_decimal_places_and_no_more() {
        // Arrange
        CurrencyPair anyPairQuotedInCents = Any.CurrencyPair().WithQuote(quote => quote.WithMinorUnitsAtLeast(1)).Generate();

        // Act
        ExchangeRate accepted = new ExchangeRate(anyPairQuotedInCents, 1.08425m);
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ExchangeRate(anyPairQuotedInCents, 1.084251m));

        // Assert
        Assert.Equal(1.08425m, accepted.Value);
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void Trailing_zeros_are_not_decimal_places() {
        // Act
        ExchangeRate rate = new ExchangeRate(UsdJpy, 151.2370000m);

        // Assert
        Assert.Equal(151.237m, rate.Value);
    }

    [Fact]
    public void A_rate_refuses_a_missing_pair() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ExchangeRate(null!, 1m));
    }

    public static TheoryData<string, string, string, MidpointRounding, string, string> Inversions => new TheoryData<string, string, string, MidpointRounding, string, string> {
        { "EUR", "USD", "1.08425", MidpointRounding.ToEven, "USD/EUR", "0.92230" },
        { "EUR", "USD", "1.08425", MidpointRounding.AwayFromZero, "USD/EUR", "0.92230" },
        { "EUR", "USD", "64", MidpointRounding.ToEven, "USD/EUR", "0.01562" },
        { "EUR", "USD", "64", MidpointRounding.AwayFromZero, "USD/EUR", "0.01563" },
        { "JPY", "USD", "400", MidpointRounding.ToEven, "USD/JPY", "0.002" },
        { "JPY", "USD", "400", MidpointRounding.AwayFromZero, "USD/JPY", "0.003" },
        { "USD", "JPY", "151.237", MidpointRounding.ToEven, "JPY/USD", "0.00661" }
    };

    [Theory]
    [MemberData(nameof(Inversions))]
    public void Invert_rounds_one_over_the_value_to_the_places_of_the_inverse_pair(string baseCode, string quoteCode, string value, MidpointRounding mode, string expectedPair, string expectedValue) {
        // Arrange
        ExchangeRate rate = new ExchangeRate(new CurrencyPair(Currency.Iso(baseCode), Currency.Iso(quoteCode)), decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        ExchangeRate inverse = rate.Invert(mode);

        // Assert
        Assert.Equal(expectedPair, inverse.Pair.ToString());
        Assert.Equal(decimal.Parse(expectedValue, System.Globalization.CultureInfo.InvariantCulture), inverse.Value);
    }

    [Fact]
    public void Invert_refuses_a_rate_whose_inverse_rounds_to_zero_with_the_mode_asked_and_accepts_it_with_another() {
        // Arrange: 1 / 200 000 = 0.000005, exactly half of the smallest quotable rate
        ExchangeRate veryHigh = new ExchangeRate(EurUsd, 200_000m);

        // Act
        ExchangeRate awayFromZero = veryHigh.Invert(MidpointRounding.AwayFromZero);

        // Assert
        Assert.Equal(0.00001m, awayFromZero.Value);
        Assert.Throws<InvalidOperationException>(() => veryHigh.Invert(MidpointRounding.ToEven));
    }

    [Fact]
    public void Invert_refuses_a_mode_that_is_not_a_midpoint_mode() {
        // Arrange
        ExchangeRate anyRate = Any.ExchangeRate().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyRate.Invert((MidpointRounding)99));
    }

    [Fact]
    public void Converting_from_the_base_multiplies() {
        // Arrange
        ExchangeRate rate = new ExchangeRate(EurUsd, 1.08425m);

        // Act
        Money dollars = rate.Convert(new Money(1_000m, Currency.Eur), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(new Money(1_084.25m, Currency.Usd), dollars);
    }

    [Fact]
    public void Converting_from_the_quote_divides() {
        // Arrange
        ExchangeRate rate = new ExchangeRate(EurUsd, 1.08425m);

        // Act
        Money euros = rate.Convert(new Money(100m, Currency.Usd), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(new Money(92.23m, Currency.Eur), euros);
    }

    [Fact]
    public void Converting_into_a_currency_without_minor_units_lands_on_whole_units() {
        // Arrange
        ExchangeRate rate = new ExchangeRate(UsdJpy, 151.237m);

        // Act
        Money yen = rate.Convert(new Money(100m, Currency.Usd), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(new Money(15_124m, Currency.Jpy), yen);
    }

    public static TheoryData<string, string, string, string, MidpointRounding, string, string> Midpoints => new TheoryData<string, string, string, string, MidpointRounding, string, string> {
        { "EUR", "USD", "1.00500", "1.00", MidpointRounding.ToEven, "1.00", "USD" },
        { "EUR", "USD", "1.00500", "1.00", MidpointRounding.AwayFromZero, "1.01", "USD" },
        { "EUR", "USD", "1.02500", "1.00", MidpointRounding.ToEven, "1.02", "USD" },
        { "EUR", "USD", "1.02500", "1.00", MidpointRounding.AwayFromZero, "1.03", "USD" },
        { "EUR", "USD", "1.00500", "-1.00", MidpointRounding.ToEven, "-1.00", "USD" },
        { "EUR", "USD", "1.00500", "-1.00", MidpointRounding.AwayFromZero, "-1.01", "USD" },
        { "USD", "JPY", "150.500", "1.00", MidpointRounding.ToEven, "150", "JPY" },
        { "USD", "JPY", "150.500", "1.00", MidpointRounding.AwayFromZero, "151", "JPY" },
        { "USD", "KWD", "0.30650", "1.00", MidpointRounding.ToEven, "0.306", "KWD" },
        { "USD", "KWD", "0.30650", "1.00", MidpointRounding.AwayFromZero, "0.307", "KWD" }
    };

    [Theory]
    [MemberData(nameof(Midpoints))]
    public void Converting_rounds_to_the_target_minor_units_with_the_mode_it_is_given(string baseCode, string quoteCode, string rate, string amount, MidpointRounding mode, string expectedAmount, string expectedCode) {
        // Arrange
        ExchangeRate exchangeRate = new ExchangeRate(new CurrencyPair(Currency.Iso(baseCode), Currency.Iso(quoteCode)), decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture));
        Money        dealt        = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), Currency.Iso(baseCode));

        // Act
        Money converted = exchangeRate.Convert(dealt, mode);

        // Assert
        Assert.Equal(new Money(decimal.Parse(expectedAmount, System.Globalization.CultureInfo.InvariantCulture), Currency.Iso(expectedCode)), converted);
    }

    [Fact]
    public void Converting_a_third_currency_is_refused() {
        // Arrange
        Currency     anyThirdCurrency = Any.Currency().Except(Currency.Eur, Currency.Usd).Generate();
        ExchangeRate rate             = new ExchangeRate(EurUsd, 1.08425m);
        Money        anyForeignAmount = new AnyMoney().InCurrency(anyThirdCurrency).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => rate.Convert(anyForeignAmount, MidpointRounding.ToEven));
    }

    [Fact]
    public void Converting_refuses_a_mode_that_is_not_a_midpoint_mode() {
        // Arrange
        ExchangeRate rate = new ExchangeRate(EurUsd, 1.08425m);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => rate.Convert(new Money(1m, Currency.Eur), (MidpointRounding)99));
    }

    [Fact]
    public void A_rate_prints_its_pair_and_every_decimal_place_of_the_pair() {
        // Assert
        Assert.Equal("EUR/USD 1.08000", new ExchangeRate(EurUsd, 1.08m).ToString());
        Assert.Equal("USD/JPY 151.237", new ExchangeRate(UsdJpy, 151.237m).ToString());
    }

    [Fact]
    public void Rates_are_equal_when_pair_and_value_are() {
        // Assert
        Assert.Equal(new ExchangeRate(EurUsd, 1.08m), new ExchangeRate(EurUsd, 1.0800m));
        Assert.NotEqual(new ExchangeRate(EurUsd, 1.08m), new ExchangeRate(EurUsd, 1.09m));
        Assert.NotEqual(new ExchangeRate(EurUsd, 1.08m), new ExchangeRate(new CurrencyPair(Currency.Eur, Currency.Gbp), 1.08m));
    }

}
