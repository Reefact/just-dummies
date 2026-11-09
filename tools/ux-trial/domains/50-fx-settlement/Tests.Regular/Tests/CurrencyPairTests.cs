using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Regular;

public sealed class CurrencyPairTests {

    [Fact]
    public void A_pair_of_one_currency_twice_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CurrencyPair(anyCurrency, anyCurrency));

        // Assert
        Assert.Equal("quote", refusal.ParamName);
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
    public void A_pair_keeps_its_base_and_quote() {
        // Arrange
        Currency anyBase  = Any.Currency().Generate();
        Currency anyQuote = new AnyCurrency().Except(anyBase).Generate();

        // Act
        CurrencyPair pair = new CurrencyPair(anyBase, anyQuote);

        // Assert
        Assert.Equal(anyBase, pair.Base);
        Assert.Equal(anyQuote, pair.Quote);
        Assert.Equal($"{anyBase.Code}/{anyQuote.Code}", pair.ToString());
    }

    [Fact]
    public void A_rate_is_quoted_to_three_places_when_the_quote_has_no_minor_units() {
        // Arrange
        CurrencyPair anyPairQuotedInWholeUnits = Any.CurrencyPair().WithQuote(q => q.WithMinorUnits(0)).Generate();

        // Act / Assert
        Assert.Equal(3, anyPairQuotedInWholeUnits.RateDecimalPlaces);
    }

    [Fact]
    public void A_rate_is_quoted_to_five_places_when_the_quote_has_minor_units() {
        // Arrange
        CurrencyPair anyPairQuotedInDecimals = Any.CurrencyPair().WithQuote(q => q.WithMinorUnitsAtLeast(1)).Generate();

        // Act / Assert
        Assert.Equal(5, anyPairQuotedInDecimals.RateDecimalPlaces);
    }

    [Fact]
    public void The_places_follow_the_quote_not_the_base() {
        // Act / Assert
        Assert.Equal(3, new CurrencyPair(Currency.Usd, Currency.Jpy).RateDecimalPlaces);
        Assert.Equal(5, new CurrencyPair(Currency.Jpy, Currency.Usd).RateDecimalPlaces);
        Assert.Equal(3, new CurrencyPair(Currency.Eur, Currency.Krw).RateDecimalPlaces);
    }

    [Theory]
    [InlineData("USD", "CAD")]
    [InlineData("CAD", "USD")]
    public void Spot_settles_one_business_day_after_the_trade_for_dollar_canada(string @base, string quote) {
        // Arrange
        CurrencyPair pair = new CurrencyPair(Currency.Iso(@base), Currency.Iso(quote));

        // Act / Assert
        Assert.Equal(1, pair.SpotLagDays);
    }

    [Fact]
    public void Spot_settles_two_business_days_after_the_trade_for_any_other_pair() {
        // Arrange
        CurrencyPair anyOtherPair = Any.CurrencyPair()
                                       .WithBase(b => b.Except(Currency.Usd, Currency.Cad))
                                       .WithQuote(q => q.Except(Currency.Usd, Currency.Cad))
                                       .Generate();

        // Act / Assert
        Assert.Equal(2, anyOtherPair.SpotLagDays);
        Assert.Equal(2, new CurrencyPair(Currency.Usd, Currency.Eur).SpotLagDays);
        Assert.Equal(2, new CurrencyPair(Currency.Cad, Currency.Jpy).SpotLagDays);
    }

    // 2026-03-16 is a Monday.
    public static TheoryData<string, string, string> TwoDayValueDates => new TheoryData<string, string, string> {
        { "2026-03-16", "2026-03-18", "Monday trades value on Wednesday" },
        { "2026-03-18", "2026-03-20", "Wednesday trades value on Friday" },
        { "2026-03-19", "2026-03-23", "Thursday trades value on Monday" },
        { "2026-03-20", "2026-03-24", "Friday trades value on Tuesday" },
    };

    [Theory, MemberData(nameof(TwoDayValueDates))]
    public void The_value_date_skips_weekends(string tradeDate, string valueDate, string reading) {
        // Arrange
        CurrencyPair anyTwoDayPair = Any.CurrencyPair()
                                        .WithBase(b => b.Except(Currency.Usd, Currency.Cad))
                                        .Generate();

        // Act
        DateOnly actual = anyTwoDayPair.SpotValueDate(DateOnly.Parse(tradeDate));

        // Assert
        Assert.True(DateOnly.Parse(valueDate) == actual, reading);
    }

    [Theory]
    [InlineData("2026-03-16", "2026-03-17")]
    [InlineData("2026-03-20", "2026-03-23")]
    public void Dollar_canada_value_dates_skip_weekends_too(string tradeDate, string valueDate) {
        // Arrange
        CurrencyPair usdCad = new CurrencyPair(Currency.Usd, Currency.Cad);

        // Act / Assert
        Assert.Equal(DateOnly.Parse(valueDate), usdCad.SpotValueDate(DateOnly.Parse(tradeDate)));
    }

    [Theory]
    [InlineData("2026-03-14")]
    [InlineData("2026-03-15")]
    public void A_weekend_trade_date_is_refused(string tradeDate) {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => anyPair.SpotValueDate(DateOnly.Parse(tradeDate)));
    }

    [Fact]
    public void A_business_day_trade_date_is_followed_by_a_business_day_value_date() {
        // Arrange
        CurrencyPair anyPair  = Any.CurrencyPair().Generate();
        DateOnly     anyTradeDate = BusinessDays.Any().Generate();

        // Act
        DateOnly valueDate = anyPair.SpotValueDate(anyTradeDate);

        // Assert
        Assert.False(CurrencyPair.IsWeekend(valueDate));
        Assert.True(valueDate > anyTradeDate);
    }

    [Fact]
    public void Inverse_swaps_base_and_quote() {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act
        CurrencyPair inverse = anyPair.Inverse();

        // Assert
        Assert.Equal(anyPair.Quote, inverse.Base);
        Assert.Equal(anyPair.Base, inverse.Quote);
        Assert.Equal(anyPair, inverse.Inverse());
    }

    [Fact]
    public void A_pair_involves_both_its_currencies_and_no_other() {
        // Arrange
        CurrencyPair anyPair  = Any.CurrencyPair().Generate();
        Currency     anyOther = new AnyCurrency().Except(anyPair.Base, anyPair.Quote).Generate();

        // Act / Assert
        Assert.True(anyPair.Involves(anyPair.Base));
        Assert.True(anyPair.Involves(anyPair.Quote));
        Assert.False(anyPair.Involves(anyOther));
    }

    [Fact]
    public void Pairs_are_equal_by_base_and_quote_in_that_order() {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act
        CurrencyPair same = new CurrencyPair(anyPair.Base, anyPair.Quote);

        // Assert
        Assert.Equal(anyPair, same);
        Assert.Equal(anyPair.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(anyPair, anyPair.Inverse());
    }

    [Theory]
    [InlineData("2026-03-14", true)]
    [InlineData("2026-03-15", true)]
    [InlineData("2026-03-16", false)]
    [InlineData("2026-03-20", false)]
    public void Only_saturdays_and_sundays_are_weekends(string date, bool expected) {
        // Act / Assert
        Assert.Equal(expected, CurrencyPair.IsWeekend(DateOnly.Parse(date)));
    }

}
