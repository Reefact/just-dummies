using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Expert;

public sealed class CurrencyPairTests {

    private static readonly CurrencyPair UsdCad = new CurrencyPair(Currency.Usd, Currency.Cad);
    private static readonly CurrencyPair CadUsd = new CurrencyPair(Currency.Cad, Currency.Usd);

    private static IAny<CurrencyPair> AnyPairOutsideTheCanadianDollarLag() {
        return Any.CurrencyPair().DifferentFrom(UsdCad).DifferentFrom(CadUsd);
    }

    [Fact]
    public void A_pair_needs_two_different_currencies() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CurrencyPair(anyCurrency, anyCurrency));
    }

    [Fact]
    public void A_pair_refuses_a_missing_currency() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new CurrencyPair(null!, anyCurrency));
        Assert.Throws<ArgumentNullException>(() => new CurrencyPair(anyCurrency, null!));
    }

    [Fact]
    public void A_pair_remembers_which_currency_is_the_base_and_which_the_quote() {
        // Act
        CurrencyPair pair = new CurrencyPair(Currency.Eur, Currency.Usd);

        // Assert
        Assert.Equal(Currency.Eur, pair.Base);
        Assert.Equal(Currency.Usd, pair.Quote);
        Assert.Equal("EUR/USD", pair.ToString());
    }

    [Fact]
    public void A_rate_quoted_in_a_currency_without_minor_units_has_three_decimal_places() {
        // Arrange
        CurrencyPair anyPairQuotedInWholeUnits = Any.CurrencyPair().WithQuote(quote => quote.WithMinorUnits(0)).Generate();

        // Act
        int places = anyPairQuotedInWholeUnits.RateDecimalPlaces;

        // Assert
        Assert.Equal(3, places);
    }

    [Fact]
    public void A_rate_quoted_in_a_currency_with_minor_units_has_five_decimal_places() {
        // Arrange: the base does not matter, a currency without minor units is as good as any
        CurrencyPair anyPairQuotedInCents = Any.CurrencyPair().WithQuote(quote => quote.WithMinorUnitsAtLeast(1)).Generate();

        // Act
        int places = anyPairQuotedInCents.RateDecimalPlaces;

        // Assert
        Assert.Equal(5, places);
    }

    [Fact]
    public void Only_the_quote_decides_the_decimal_places() {
        // Assert
        Assert.Equal(3, new CurrencyPair(Currency.Usd, Currency.Jpy).RateDecimalPlaces);
        Assert.Equal(5, new CurrencyPair(Currency.Jpy, Currency.Usd).RateDecimalPlaces);
    }

    [Fact]
    public void The_Canadian_dollar_against_the_US_dollar_settles_after_one_business_day_either_way_round() {
        // Assert
        Assert.Equal(1, UsdCad.SpotLagDays);
        Assert.Equal(1, CadUsd.SpotLagDays);
    }

    [Fact]
    public void Any_other_pair_settles_after_two_business_days() {
        // Arrange
        CurrencyPair anyOtherPair = AnyPairOutsideTheCanadianDollarLag().Generate();

        // Act
        int lag = anyOtherPair.SpotLagDays;

        // Assert
        Assert.Equal(2, lag);
    }

    [Theory]
    [InlineData("USD", "EUR")]
    [InlineData("CAD", "EUR")]
    [InlineData("USD", "GBP")]
    [InlineData("JPY", "CAD")]
    public void Sharing_one_currency_with_USD_CAD_does_not_make_a_pair_settle_early(string baseCode, string quoteCode) {
        // Arrange
        CurrencyPair pair = new CurrencyPair(Currency.Iso(baseCode), Currency.Iso(quoteCode));

        // Assert
        Assert.Equal(2, pair.SpotLagDays);
    }

    public static TheoryData<DateOnly, DateOnly> TwoBusinessDaysAfter => new TheoryData<DateOnly, DateOnly> {
        { Week.Monday, Week.Wednesday },
        { Week.Tuesday, Week.Thursday },
        { Week.Wednesday, Week.Friday },
        { Week.Thursday, Week.NextMonday },
        { Week.Friday, Week.NextTuesday }
    };

    [Theory]
    [MemberData(nameof(TwoBusinessDaysAfter))]
    public void Spot_value_date_is_two_business_days_after_the_trade_date(DateOnly tradeDate, DateOnly expectedValueDate) {
        // Arrange
        CurrencyPair anyOtherPair = AnyPairOutsideTheCanadianDollarLag().Generate();

        // Act
        DateOnly valueDate = anyOtherPair.SpotValueDate(tradeDate);

        // Assert
        Assert.Equal(expectedValueDate, valueDate);
    }

    public static TheoryData<DateOnly, DateOnly> OneBusinessDayAfter => new TheoryData<DateOnly, DateOnly> {
        { Week.Monday, Week.Tuesday },
        { Week.Thursday, Week.Friday },
        { Week.Friday, Week.NextMonday }
    };

    [Theory]
    [MemberData(nameof(OneBusinessDayAfter))]
    public void Spot_value_date_of_USD_CAD_is_one_business_day_after_the_trade_date(DateOnly tradeDate, DateOnly expectedValueDate) {
        // Assert
        Assert.Equal(expectedValueDate, UsdCad.SpotValueDate(tradeDate));
        Assert.Equal(expectedValueDate, CadUsd.SpotValueDate(tradeDate));
    }

    [Fact]
    public void A_weekend_trade_date_has_no_spot_value_date() {
        // Arrange
        CurrencyPair anyPair    = Any.CurrencyPair().Generate();
        DateOnly     anyWeekend = Any.WeekendDay().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => anyPair.SpotValueDate(anyWeekend));
    }

    [Fact]
    public void The_inverse_swaps_base_and_quote() {
        // Act
        CurrencyPair inverse = new CurrencyPair(Currency.Eur, Currency.Usd).Inverse();

        // Assert
        Assert.Equal(new CurrencyPair(Currency.Usd, Currency.Eur), inverse);
    }

    [Fact]
    public void A_pair_involves_both_its_currencies_and_no_other() {
        // Arrange
        CurrencyPair pair             = new CurrencyPair(Currency.Eur, Currency.Usd);
        Currency     anyThirdCurrency = Any.Currency().Except(Currency.Eur, Currency.Usd).Generate();

        // Assert
        Assert.True(pair.Involves(Currency.Eur));
        Assert.True(pair.Involves(Currency.Usd));
        Assert.False(pair.Involves(anyThirdCurrency));
    }

    [Fact]
    public void Direction_matters_to_the_equality_of_pairs() {
        // Assert
        Assert.Equal(new CurrencyPair(Currency.Eur, Currency.Usd), new CurrencyPair(Currency.Eur, Currency.Usd));
        Assert.NotEqual(new CurrencyPair(Currency.Eur, Currency.Usd), new CurrencyPair(Currency.Usd, Currency.Eur));
    }

}
