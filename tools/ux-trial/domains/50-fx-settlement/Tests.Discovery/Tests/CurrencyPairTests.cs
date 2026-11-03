using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests;

public sealed class CurrencyPairTests {

    [Fact]
    public void A_pair_needs_two_different_currencies() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        Action construct = () => new CurrencyPair(anyCurrency, anyCurrency);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_pair_refuses_the_same_code_even_with_other_minor_units() {
        // Arrange
        string anyCode = Any.StringMatching("[A-WYZ][A-Z]{2}").Except(Fixtures.DeskIsoTable.Select(entry => entry.Code).ToArray()).Generate();

        // Act
        Action construct = () => new CurrencyPair(new Currency(anyCode, 2), new Currency(anyCode, 3));

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_pair_refuses_a_missing_currency() {
        Assert.Throws<ArgumentNullException>(() => new CurrencyPair(null!, Currency.Usd));
        Assert.Throws<ArgumentNullException>(() => new CurrencyPair(Currency.Usd, null!));
    }

    [Fact]
    public void A_pair_is_written_base_slash_quote() {
        Assert.Equal("EUR/USD", Fixtures.Pair(Currency.Eur, Currency.Usd).ToString());
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("KRW")]
    public void A_rate_quoted_in_a_currency_without_minor_units_carries_three_decimal_places(string quoteCode) {
        // Arrange
        Currency anyBase = Any.Currency().WithCode(c => c.Except("JPY", "KRW")).Generate();

        // Act
        CurrencyPair pair = new CurrencyPair(anyBase, Currency.Iso(quoteCode));

        // Assert
        Assert.Equal(3, pair.RateDecimalPlaces);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("KWD")]
    [InlineData("CLF")]
    public void A_rate_quoted_in_a_currency_with_minor_units_carries_five_decimal_places(string quoteCode) {
        // Arrange
        Currency anyBase = Any.Currency().WithCode(c => c.Except(quoteCode)).Generate();

        // Act
        CurrencyPair pair = new CurrencyPair(anyBase, Currency.Iso(quoteCode));

        // Assert
        Assert.Equal(5, pair.RateDecimalPlaces);
    }

    [Fact]
    public void Only_the_quote_currency_decides_the_decimal_places() {
        Assert.Equal(5, Fixtures.Pair(Currency.Jpy, Currency.Usd).RateDecimalPlaces);
        Assert.Equal(3, Fixtures.Pair(Currency.Usd, Currency.Jpy).RateDecimalPlaces);
    }

    [Fact]
    public void Spot_settles_in_one_business_day_for_USD_CAD_and_CAD_USD() {
        Assert.Equal(1, Fixtures.Pair(Currency.Usd, Currency.Cad).SpotLagDays);
        Assert.Equal(1, Fixtures.Pair(Currency.Cad, Currency.Usd).SpotLagDays);
    }

    [Fact]
    public void Spot_settles_in_two_business_days_for_every_other_pair() {
        Assert.Equal(2, Fixtures.Pair(Currency.Eur, Currency.Usd).SpotLagDays);
        Assert.Equal(2, Fixtures.Pair(Currency.Usd, Currency.Jpy).SpotLagDays);
        Assert.Equal(2, Fixtures.Pair(Currency.Cad, Currency.Jpy).SpotLagDays);
        Assert.Equal(2, Fixtures.Pair(Currency.Usd, Currency.Chf).SpotLagDays);
    }

    [Fact]
    public void A_pair_of_any_other_currencies_settles_in_two_business_days() {
        // Arrange: neither USD nor CAD is involved, whatever the two currencies are.
        CurrencyPair anyPair = Any.CurrencyPair()
                                  .WithBase(c => c.WithCode(code => code.Except("USD", "CAD")))
                                  .WithQuote(c => c.WithCode(code => code.Except("USD", "CAD")))
                                  .Generate();

        // Assert
        Assert.Equal(2, anyPair.SpotLagDays);
    }

    [Theory]
    [InlineData("2025-03-10", "2025-03-12")] // Monday     -> Wednesday
    [InlineData("2025-03-11", "2025-03-13")] // Tuesday    -> Thursday
    [InlineData("2025-03-12", "2025-03-14")] // Wednesday  -> Friday
    [InlineData("2025-03-13", "2025-03-17")] // Thursday   -> next Monday
    [InlineData("2025-03-14", "2025-03-18")] // Friday     -> next Tuesday
    public void The_spot_value_date_is_two_business_days_after_the_trade_date(string tradeDate, string expected) {
        CurrencyPair pair = Fixtures.Pair(Currency.Eur, Currency.Usd);

        Assert.Equal(DateOnly.Parse(expected), pair.SpotValueDate(DateOnly.Parse(tradeDate)));
    }

    [Theory]
    [InlineData("2025-03-10", "2025-03-11")] // Monday  -> Tuesday
    [InlineData("2025-03-13", "2025-03-14")] // Thursday-> Friday
    [InlineData("2025-03-14", "2025-03-17")] // Friday  -> next Monday
    public void The_spot_value_date_of_USD_CAD_is_one_business_day_after_the_trade_date(string tradeDate, string expected) {
        Assert.Equal(DateOnly.Parse(expected), Fixtures.Pair(Currency.Usd, Currency.Cad).SpotValueDate(DateOnly.Parse(tradeDate)));
        Assert.Equal(DateOnly.Parse(expected), Fixtures.Pair(Currency.Cad, Currency.Usd).SpotValueDate(DateOnly.Parse(tradeDate)));
    }

    [Fact]
    public void The_spot_value_date_ignores_no_holiday_because_there_is_no_calendar() {
        // 2025-12-24 Wednesday -> Friday the 26th, though the 25th is Christmas: only weekends are skipped.
        Assert.Equal(new DateOnly(2025, 12, 26), Fixtures.Pair(Currency.Eur, Currency.Usd).SpotValueDate(new DateOnly(2025, 12, 24)));
    }

    [Theory]
    [InlineData("2025-03-15")] // Saturday
    [InlineData("2025-03-16")] // Sunday
    public void A_weekend_trade_date_has_no_spot_value_date(string tradeDate) {
        // Arrange
        CurrencyPair anyPair = Any.CurrencyPair().Generate();

        // Act
        Action compute = () => anyPair.SpotValueDate(DateOnly.Parse(tradeDate));

        // Assert
        Assert.Throws<ArgumentException>(compute);
    }

    [Fact]
    public void The_value_date_of_a_business_day_trade_is_itself_a_business_day() {
        // Arrange: whatever the pair and the weekday the trade is dealt on.
        CurrencyPair anyPair      = Any.CurrencyPair().Generate();
        DateOnly     anyTradeDate = BusinessDays.Any().Generate();

        // Act
        DateOnly valueDate = anyPair.SpotValueDate(anyTradeDate);

        // Assert
        Assert.False(CurrencyPair.IsWeekend(valueDate));
        Assert.True(valueDate > anyTradeDate);
    }

    [Fact]
    public void The_inverse_swaps_base_and_quote() {
        CurrencyPair inverse = Fixtures.Pair(Currency.Eur, Currency.Jpy).Inverse();

        Assert.Equal(Currency.Jpy, inverse.Base);
        Assert.Equal(Currency.Eur, inverse.Quote);
    }

    [Fact]
    public void A_pair_involves_its_two_currencies_and_no_other() {
        // Arrange
        CurrencyPair anyPair  = Any.CurrencyPair().Generate();
        Currency     anyOther = Any.Currency().WithCode(c => c.Except(anyPair.Base.Code, anyPair.Quote.Code)).Generate();

        // Assert
        Assert.True(anyPair.Involves(anyPair.Base));
        Assert.True(anyPair.Involves(anyPair.Quote));
        Assert.False(anyPair.Involves(anyOther));
    }

    [Fact]
    public void Two_pairs_are_equal_when_both_currencies_match_in_order() {
        Assert.Equal(Fixtures.Pair(Currency.Eur, Currency.Usd), Fixtures.Pair(Currency.Eur, Currency.Usd));
        Assert.Equal(Fixtures.Pair(Currency.Eur, Currency.Usd).GetHashCode(), Fixtures.Pair(Currency.Eur, Currency.Usd).GetHashCode());
        Assert.NotEqual(Fixtures.Pair(Currency.Eur, Currency.Usd), Fixtures.Pair(Currency.Usd, Currency.Eur));
    }

    [Theory]
    [InlineData("2025-03-15", true)]
    [InlineData("2025-03-16", true)]
    [InlineData("2025-03-14", false)]
    [InlineData("2025-03-17", false)]
    public void Only_Saturday_and_Sunday_are_weekend_days(string date, bool expected) {
        Assert.Equal(expected, CurrencyPair.IsWeekend(DateOnly.Parse(date)));
    }

}
