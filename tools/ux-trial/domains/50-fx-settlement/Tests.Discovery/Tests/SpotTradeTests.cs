using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests;

public sealed class SpotTradeTests {

    // A trade whose id, counterparty and side the assertion is not about; the amount, rate and date are given.
    private static SpotTrade Trade(Money dealt, ExchangeRate rate, DateOnly tradeDate, TradeSide? side = null) {
        TradeId anyId           = Any.TradeId().Generate();
        Lei     anyCounterparty = Any.Lei().Generate();
        TradeSide anySide       = side ?? Any.Enum<TradeSide>().Generate();

        return new SpotTrade(anyId, anyCounterparty, anySide, dealt, rate, tradeDate);
    }

    private static readonly ExchangeRate EurUsd = Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m);

    [Fact]
    public void A_trade_dealt_in_the_base_currency_converts_to_the_quote_with_midpoints_away_from_zero() {
        // 100.00 EUR * 1.08425 = 108.425 USD, rounded up to 108.43.
        SpotTrade trade = Trade(Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday);

        Assert.Equal(Fixtures.Eur(100.00m), trade.BaseAmount);
        Assert.Equal(Fixtures.Usd(108.43m), trade.QuoteAmount);
        Assert.Equal(Fixtures.Eur(100.00m), trade.DealtAmount);
    }

    [Fact]
    public void A_trade_dealt_in_the_quote_currency_converts_to_the_base() {
        // 108.43 USD / 1.08425 = 100.0046... EUR
        SpotTrade trade = Trade(Fixtures.Usd(108.43m), EurUsd, Fixtures.Monday);

        Assert.Equal(Fixtures.Eur(100.00m), trade.BaseAmount);
        Assert.Equal(Fixtures.Usd(108.43m), trade.QuoteAmount);
        Assert.Equal(Fixtures.Usd(108.43m), trade.DealtAmount);
    }

    [Fact]
    public void The_other_leg_is_rounded_to_the_minor_units_of_its_currency() {
        // 1 000.00 USD * 151.237 = 151 237 JPY exactly; 1 000.50 USD * 151.237 = 151 312.6185 -> 151 313 JPY.
        ExchangeRate usdJpy = Fixtures.Rate(Currency.Usd, Currency.Jpy, 151.237m);

        SpotTrade trade = Trade(Fixtures.Usd(1000.50m), usdJpy, Fixtures.Monday);

        Assert.Equal(Fixtures.Jpy(151313m), trade.QuoteAmount);
    }

    [Fact]
    public void Client_conversion_rounds_a_midpoint_away_from_zero_not_to_even() {
        // 1.00 USD * 150.5 = 150.5 JPY: away from zero gives 151, banker's rounding would give 150.
        ExchangeRate usdJpy = Fixtures.Rate(Currency.Usd, Currency.Jpy, 150.5m);

        SpotTrade trade = Trade(Fixtures.Usd(1.00m), usdJpy, Fixtures.Monday);

        Assert.Equal(Fixtures.Jpy(151m), trade.QuoteAmount);
    }

    [Fact]
    public void The_trade_exposes_the_rate_and_the_pair_it_was_dealt_at() {
        SpotTrade trade = Trade(Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday);

        Assert.Equal(EurUsd, trade.Rate);
        Assert.Equal(Fixtures.Pair(Currency.Eur, Currency.Usd), trade.Pair);
        Assert.Equal(Fixtures.Monday, trade.TradeDate);
    }

    [Fact]
    public void The_trade_settles_on_the_spot_value_date_of_its_pair() {
        Assert.Equal(Fixtures.Wednesday, Trade(Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday).ValueDate);
        Assert.Equal(Fixtures.NextMonday, Trade(Fixtures.Eur(100.00m), EurUsd, Fixtures.Thursday).ValueDate);
    }

    [Fact]
    public void A_USD_CAD_trade_settles_the_next_business_day() {
        ExchangeRate usdCad = Fixtures.Rate(Currency.Usd, Currency.Cad, 1.43210m);

        Assert.Equal(Fixtures.Tuesday, Trade(Fixtures.Usd(100.00m), usdCad, Fixtures.Monday).ValueDate);
        Assert.Equal(Fixtures.NextMonday, Trade(Fixtures.Usd(100.00m), usdCad, Fixtures.Friday).ValueDate);
    }

    [Theory]
    [InlineData(TradeSide.BuyBase, "EUR", "USD")]
    [InlineData(TradeSide.SellBase, "USD", "EUR")]
    public void The_bank_receives_the_base_when_it_buys_it_and_the_quote_when_it_sells_it(TradeSide side, string receivesCode, string paysCode) {
        SpotTrade trade = Trade(Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday, side);

        Assert.Equal(receivesCode, trade.BankReceives.Currency.Code);
        Assert.Equal(paysCode, trade.BankPays.Currency.Code);
    }

    [Fact]
    public void When_the_bank_buys_the_base_it_receives_the_base_amount_and_pays_the_quote_amount() {
        SpotTrade trade = Trade(Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday, TradeSide.BuyBase);

        Assert.Equal(Fixtures.Eur(100.00m), trade.BankReceives);
        Assert.Equal(Fixtures.Usd(108.43m), trade.BankPays);
    }

    [Fact]
    public void When_the_bank_sells_the_base_it_receives_the_quote_amount_and_pays_the_base_amount() {
        SpotTrade trade = Trade(Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday, TradeSide.SellBase);

        Assert.Equal(Fixtures.Usd(108.43m), trade.BankReceives);
        Assert.Equal(Fixtures.Eur(100.00m), trade.BankPays);
    }

    [Fact]
    public void The_dealt_currency_must_be_one_of_the_pair() {
        // Arrange
        Money anyOtherAmount = Any.Money()
                                  .WithCurrency(c => c.WithCode(code => code.Except("EUR", "USD")))
                                  .WithAmount(a => a.Between(1_000m, 1_000_000m))
                                  .Generate();

        // Act
        Action construct = () => Trade(anyOtherAmount, EurUsd, Fixtures.Monday);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-100")]
    public void A_dealt_amount_must_be_positive(string amount) {
        Money dealt = Fixtures.Eur(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        Action construct = () => Trade(dealt, EurUsd, Fixtures.Monday);

        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_dealt_amount_of_exactly_one_trillion_is_accepted() {
        SpotTrade trade = Trade(Fixtures.Eur(SpotTrade.MaxDealtAmount), EurUsd, Fixtures.Monday);

        Assert.Equal(SpotTrade.MaxDealtAmount, trade.DealtAmount.Amount);
    }

    [Fact]
    public void A_dealt_amount_above_one_trillion_is_refused() {
        // Arrange: the other leg is far from any limit, so only the dealt amount can be refused.
        Money tooMuch = Fixtures.Eur(SpotTrade.MaxDealtAmount + 0.01m);

        // Act
        Action construct = () => Trade(tooMuch, EurUsd, Fixtures.Monday);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_trade_whose_other_leg_exceeds_the_money_maximum_is_refused() {
        // 1 000 000 000 000.00 EUR * 1000 = 10^15 USD, one more than 999 999 999 999 999.
        ExchangeRate thousand = Fixtures.Rate(Currency.Eur, Currency.Usd, 1000m);

        Action construct = () => Trade(Fixtures.Eur(SpotTrade.MaxDealtAmount), thousand, Fixtures.Monday);

        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_trade_whose_other_leg_stays_just_under_the_money_maximum_is_accepted() {
        // 999 999 999 999.99 EUR * 1000 = 999 999 999 999 990.00 USD, under 999 999 999 999 999.
        ExchangeRate thousand = Fixtures.Rate(Currency.Eur, Currency.Usd, 1000m);

        SpotTrade trade = Trade(Fixtures.Eur(999_999_999_999.99m), thousand, Fixtures.Monday);

        Assert.Equal(Fixtures.Usd(999_999_999_999_990.00m), trade.QuoteAmount);
    }

    [Fact]
    public void A_trade_worth_less_than_one_minor_unit_of_the_other_leg_is_refused() {
        // 1 JPY / 300 = 0.0033 USD, which is 0.00 at two places.
        ExchangeRate usdJpy = Fixtures.Rate(Currency.Usd, Currency.Jpy, 300m);

        Action construct = () => Trade(Fixtures.Jpy(1m), usdJpy, Fixtures.Monday);

        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_trade_worth_exactly_half_a_minor_unit_is_rounded_up_and_accepted() {
        // 1 JPY / 200 = 0.005 USD, a midpoint rounded away from zero to 0.01 USD.
        ExchangeRate usdJpy = Fixtures.Rate(Currency.Usd, Currency.Jpy, 200m);

        SpotTrade trade = Trade(Fixtures.Jpy(1m), usdJpy, Fixtures.Monday);

        Assert.Equal(Fixtures.Usd(0.01m), trade.BaseAmount);
    }

    [Theory]
    [InlineData("2025-03-15")]
    [InlineData("2025-03-16")]
    public void A_trade_is_not_dealt_on_a_weekend(string tradeDate) {
        // Arrange
        Money anyDealt = Any.Money().WithCurrency(Currency.Eur).WithAmount(a => a.Between(1_000m, 1_000_000m)).Generate();

        // Act
        Action construct = () => Trade(anyDealt, EurUsd, DateOnly.Parse(tradeDate));

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void An_undefined_side_is_refused() {
        // Arrange
        TradeId anyId           = Any.TradeId().Generate();
        Lei     anyCounterparty = Any.Lei().Generate();

        // Act
        Action construct = () => new SpotTrade(anyId, anyCounterparty, (TradeSide)99, Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_trade_refuses_a_missing_part() {
        // Arrange
        TradeId   anyId           = Any.TradeId().Generate();
        Lei       anyCounterparty = Any.Lei().Generate();
        TradeSide anySide         = Any.Enum<TradeSide>().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(null!, anyCounterparty, anySide, Fixtures.Eur(100m), EurUsd, Fixtures.Monday));
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(anyId, null!, anySide, Fixtures.Eur(100m), EurUsd, Fixtures.Monday));
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(anyId, anyCounterparty, anySide, null!, EurUsd, Fixtures.Monday));
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(anyId, anyCounterparty, anySide, Fixtures.Eur(100m), null!, Fixtures.Monday));
    }

    [Fact]
    public void A_trade_reads_as_its_reference_side_legs_rate_and_value_date() {
        SpotTrade trade = new SpotTrade(new TradeId("FX0000048213"), Fixtures.LeiFirst, TradeSide.BuyBase, Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday);

        Assert.Equal("FX0000048213 BuyBase 100.00 EUR vs 108.43 USD @ EUR/USD 1.08425 value 2025-03-12", trade.ToString());
    }

}
