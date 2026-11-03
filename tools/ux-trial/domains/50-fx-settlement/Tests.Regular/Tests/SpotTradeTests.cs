using System.Globalization;

using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Regular;

public sealed class SpotTradeTests {

    private static decimal D(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    private static readonly CurrencyPair EurUsd = new CurrencyPair(Currency.Eur, Currency.Usd);
    private static readonly CurrencyPair UsdJpy = new CurrencyPair(Currency.Usd, Currency.Jpy);

    // 2026-03-16 is a Monday.
    private static readonly DateOnly Monday = new DateOnly(2026, 3, 16);

    private static SpotTrade Trade(Money dealt, ExchangeRate rate, DateOnly? tradeDate = null, TradeSide side = TradeSide.BuyBase) {
        return new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), side, dealt, rate, tradeDate ?? Monday);
    }

    [Fact]
    public void Dealing_the_base_converts_the_quote_leg_at_the_rate() {
        // Act
        SpotTrade trade = Trade(new Money(1_000_000m, Currency.Eur), new ExchangeRate(EurUsd, 1.08425m));

        // Assert
        Assert.Equal(new Money(1_000_000m, Currency.Eur), trade.BaseAmount);
        Assert.Equal(new Money(1_084_250m, Currency.Usd), trade.QuoteAmount);
        Assert.Equal(new Money(1_000_000m, Currency.Eur), trade.DealtAmount);
    }

    [Fact]
    public void Dealing_the_quote_converts_the_base_leg_at_the_rate() {
        // Act
        SpotTrade trade = Trade(new Money(1_084_250m, Currency.Usd), new ExchangeRate(EurUsd, 1.08425m));

        // Assert
        Assert.Equal(new Money(1_000_000m, Currency.Eur), trade.BaseAmount);
        Assert.Equal(new Money(1_084_250m, Currency.Usd), trade.QuoteAmount);
    }

    [Fact]
    public void The_other_leg_rounds_midpoints_away_from_zero() {
        // Arrange: 100.10 x 1.05 = 105.105.
        // Act
        SpotTrade trade = Trade(new Money(100.10m, Currency.Eur), new ExchangeRate(EurUsd, 1.05m));

        // Assert
        Assert.Equal(new Money(105.11m, Currency.Usd), trade.QuoteAmount);
        Assert.Equal(MidpointRounding.AwayFromZero, SpotTrade.ClientRounding);
    }

    [Fact]
    public void A_leg_in_a_currency_without_minor_units_rounds_to_whole_units() {
        // Arrange: 2.00 x 150.25 = 300.5.
        // Act
        SpotTrade trade = Trade(new Money(2m, Currency.Usd), new ExchangeRate(UsdJpy, 150.25m));

        // Assert
        Assert.Equal(new Money(301m, Currency.Jpy), trade.QuoteAmount);
    }

    [Fact]
    public void A_buyer_of_the_base_receives_the_base_and_pays_the_quote() {
        // Act
        SpotTrade trade = Trade(new Money(1_000m, Currency.Eur), new ExchangeRate(EurUsd, 1.08m), side: TradeSide.BuyBase);

        // Assert
        Assert.Equal(new Money(1_000m, Currency.Eur), trade.BankReceives);
        Assert.Equal(new Money(1_080m, Currency.Usd), trade.BankPays);
    }

    [Fact]
    public void A_seller_of_the_base_receives_the_quote_and_pays_the_base() {
        // Act
        SpotTrade trade = Trade(new Money(1_000m, Currency.Eur), new ExchangeRate(EurUsd, 1.08m), side: TradeSide.SellBase);

        // Assert
        Assert.Equal(new Money(1_080m, Currency.Usd), trade.BankReceives);
        Assert.Equal(new Money(1_000m, Currency.Eur), trade.BankPays);
    }

    [Fact]
    public void The_trade_exposes_its_pair_and_what_it_was_given() {
        // Arrange
        TradeId      anyId           = Any.TradeId().Generate();
        Lei          anyCounterparty = Any.Lei().Generate();
        ExchangeRate rate            = new ExchangeRate(EurUsd, 1.08m);

        // Act
        SpotTrade trade = new SpotTrade(anyId, anyCounterparty, TradeSide.SellBase, new Money(10m, Currency.Eur), rate, Monday);

        // Assert
        Assert.Same(anyId, trade.Id);
        Assert.Same(anyCounterparty, trade.Counterparty);
        Assert.Equal(TradeSide.SellBase, trade.Side);
        Assert.Same(rate, trade.Rate);
        Assert.Equal(EurUsd, trade.Pair);
        Assert.Equal(Monday, trade.TradeDate);
    }

    [Fact]
    public void The_value_date_is_the_pairs_spot_value_date() {
        // Act
        SpotTrade twoDays = Trade(new Money(10m, Currency.Eur), new ExchangeRate(EurUsd, 1.08m), new DateOnly(2026, 3, 19));
        SpotTrade oneDay  = Trade(new Money(10m, Currency.Usd), new ExchangeRate(new CurrencyPair(Currency.Usd, Currency.Cad), 1.35m), new DateOnly(2026, 3, 20));

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 23), twoDays.ValueDate);
        Assert.Equal(new DateOnly(2026, 3, 23), oneDay.ValueDate);
    }

    [Fact]
    public void A_dealt_currency_outside_the_pair_is_refused() {
        // Arrange
        Money anyForeignAmount = Any.Money().WithCurrency(c => c.Except(Currency.Eur, Currency.Usd)).WithAmount(a => a.Between(1m, 1_000m)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Trade(anyForeignAmount, new ExchangeRate(EurUsd, 1.08m)));

        // Assert
        Assert.Equal("dealtAmount", refusal.ParamName);
    }

    [Fact]
    public void A_zero_or_negative_dealt_amount_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Trade(Money.Zero(Currency.Eur), new ExchangeRate(EurUsd, 1.08m)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trade(new Money(-1m, Currency.Eur), new ExchangeRate(EurUsd, 1.08m)));
    }

    [Fact]
    public void The_largest_dealt_amount_is_a_trillion() {
        // Act
        SpotTrade largest = Trade(new Money(SpotTrade.MaxDealtAmount, Currency.Eur), new ExchangeRate(EurUsd, 1m));

        // Assert
        Assert.Equal(new Money(1_000_000_000_000m, Currency.Usd), largest.QuoteAmount);
        Assert.Throws<ArgumentOutOfRangeException>(() => Trade(new Money(1_000_000_000_000.01m, Currency.Eur), new ExchangeRate(EurUsd, 1m)));
    }

    [Fact]
    public void A_trade_whose_other_leg_exceeds_the_money_maximum_is_refused() {
        // Arrange: a trillion euros at 1 000 USD per euro would be 10^15 dollars.
        ExchangeRate rate = new ExchangeRate(EurUsd, 1_000m);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Trade(new Money(1_000_000_000_000m, Currency.Eur), rate));
    }

    [Fact]
    public void A_trade_whose_other_leg_is_worth_less_than_one_minor_unit_is_refused() {
        // Arrange: 1.00 EUR at 0.001 is 0.001 USD, which rounds to nothing.
        ExchangeRate rate = new ExchangeRate(EurUsd, 0.001m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Trade(new Money(1m, Currency.Eur), rate));

        // Assert
        Assert.Equal("dealtAmount", refusal.ParamName);
    }

    [Fact]
    public void A_leg_of_exactly_half_a_minor_unit_rounds_up_and_is_accepted() {
        // Arrange: 1.00 EUR at 0.005 is 0.005 USD, which rounds away from zero to 0.01.
        ExchangeRate rate = new ExchangeRate(EurUsd, 0.005m);

        // Act
        SpotTrade trade = Trade(new Money(1m, Currency.Eur), rate);

        // Assert
        Assert.Equal(new Money(0.01m, Currency.Usd), trade.QuoteAmount);
    }

    [Fact]
    public void Dealing_the_quote_can_also_round_the_base_leg_to_nothing() {
        // Arrange: 1 JPY dealt at 1 000 JPY per dollar is 0.001 USD, which rounds to nothing.
        ExchangeRate rate = new ExchangeRate(UsdJpy, 1_000m);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Trade(new Money(1m, Currency.Jpy), rate));
    }

    [Theory]
    [InlineData("2026-03-14")]
    [InlineData("2026-03-15")]
    public void A_weekend_trade_date_is_refused(string tradeDate) {
        // Arrange
        ExchangeRate anyRate = Any.ExchangeRate().WithValue(v => v.Between(1m, 100m)).Generate();
        Money        anyDealt = Any.Money().WithCurrency(anyRate.Pair.Base).WithAmount(a => a.Between(1_000m, 1_000_000m)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Trade(anyDealt, anyRate, DateOnly.Parse(tradeDate)));
    }

    [Fact]
    public void An_undefined_side_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), (TradeSide)5,
                                                                       new Money(10m, Currency.Eur), new ExchangeRate(EurUsd, 1.08m), Monday));
    }

    [Fact]
    public void A_trade_refuses_missing_parts() {
        // Arrange
        TradeId      anyId    = Any.TradeId().Generate();
        Lei          anyLei   = Any.Lei().Generate();
        ExchangeRate rate     = new ExchangeRate(EurUsd, 1.08m);
        Money        dealt    = new Money(10m, Currency.Eur);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(null!, anyLei, TradeSide.BuyBase, dealt, rate, Monday));
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(anyId, null!, TradeSide.BuyBase, dealt, rate, Monday));
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(anyId, anyLei, TradeSide.BuyBase, null!, rate, Monday));
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(anyId, anyLei, TradeSide.BuyBase, dealt, null!, Monday));
    }

    [Fact]
    public void An_arbitrary_trade_is_always_a_trade_the_desk_would_accept() {
        // Arrange
        SpotTrade anyTrade = Any.SpotTrade().Generate();

        // Act / Assert
        Assert.True(anyTrade.BaseAmount.IsPositive);
        Assert.True(anyTrade.QuoteAmount.IsPositive);
        Assert.False(CurrencyPair.IsWeekend(anyTrade.ValueDate));
    }

    [Fact]
    public void A_trade_prints_its_legs_rate_and_value_date() {
        // Arrange
        SpotTrade trade = new SpotTrade(new TradeId("FX0000048213"), Any.Lei().Generate(), TradeSide.BuyBase, new Money(1_000m, Currency.Eur),
                                        new ExchangeRate(EurUsd, 1.08m), Monday);

        // Act / Assert
        Assert.Equal("FX0000048213 BuyBase 1000.00 EUR vs 1080.00 USD @ EUR/USD 1.08000 value 2026-03-18", trade.ToString());
    }

}
