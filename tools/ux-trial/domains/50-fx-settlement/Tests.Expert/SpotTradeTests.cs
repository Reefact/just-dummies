using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Expert;

public sealed class SpotTradeTests {

    private static readonly CurrencyPair EurUsd = new CurrencyPair(Currency.Eur, Currency.Usd);
    private static readonly CurrencyPair UsdJpy = new CurrencyPair(Currency.Usd, Currency.Jpy);
    private static readonly CurrencyPair UsdCad = new CurrencyPair(Currency.Usd, Currency.Cad);

    private static ExchangeRate Rate(CurrencyPair pair, string value) {
        return new ExchangeRate(pair, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static Money Amount(string amount, Currency currency) {
        return new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), currency);
    }

    private static SpotTrade TradeOf(TradeSide side, Money dealt, ExchangeRate rate, DateOnly tradeDate) {
        return new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), side, dealt, rate, tradeDate);
    }

    [Fact]
    public void Dealing_the_base_converts_into_the_quote_at_the_dealt_rate() {
        // Act
        SpotTrade trade = TradeOf(TradeSide.BuyBase, Amount("1000000", Currency.Eur), Rate(EurUsd, "1.08425"), Week.Monday);

        // Assert
        Assert.Equal(Amount("1000000", Currency.Eur), trade.BaseAmount);
        Assert.Equal(Amount("1084250", Currency.Usd), trade.QuoteAmount);
        Assert.Equal(Amount("1000000", Currency.Eur), trade.DealtAmount);
    }

    [Fact]
    public void Dealing_the_quote_converts_into_the_base_at_the_dealt_rate() {
        // Act
        SpotTrade trade = TradeOf(TradeSide.BuyBase, Amount("108425", Currency.Usd), Rate(EurUsd, "1.08425"), Week.Monday);

        // Assert
        Assert.Equal(Amount("100000", Currency.Eur), trade.BaseAmount);
        Assert.Equal(Amount("108425", Currency.Usd), trade.QuoteAmount);
    }

    [Fact]
    public void The_other_leg_is_rounded_away_from_zero_to_its_minor_units() {
        // Arrange: 1.00 EUR at 1.00500 is 1.005 USD; banker's rounding would give 1.00
        Money dealt = Amount("1", Currency.Eur);

        // Act
        SpotTrade trade = TradeOf(TradeSide.SellBase, dealt, Rate(EurUsd, "1.00500"), Week.Monday);

        // Assert
        Assert.Equal(Amount("1.01", Currency.Usd), trade.QuoteAmount);
    }

    [Fact]
    public void Client_conversion_rounds_a_quote_dealt_away_from_zero_too() {
        // Arrange: 1.00 USD at 8.00000 is 0.125 EUR; banker's rounding would give 0.12
        Money dealt = Amount("1", Currency.Usd);

        // Act
        SpotTrade trade = TradeOf(TradeSide.BuyBase, dealt, Rate(EurUsd, "8"), Week.Monday);

        // Assert
        Assert.Equal(Amount("1", Currency.Usd), trade.QuoteAmount);
        Assert.Equal(Amount("0.13", Currency.Eur), trade.BaseAmount);
    }

    [Fact]
    public void Client_rounding_is_the_published_constant() {
        // Assert
        Assert.Equal(MidpointRounding.AwayFromZero, SpotTrade.ClientRounding);
    }

    [Fact]
    public void A_half_minor_unit_is_worth_one_minor_unit_but_less_than_that_is_worth_nothing() {
        // Arrange
        ExchangeRate half     = Rate(EurUsd, "0.50000");
        ExchangeRate lessThan = Rate(EurUsd, "0.40000");

        // Act
        SpotTrade worthAMinorUnit = TradeOf(TradeSide.BuyBase, Amount("0.01", Currency.Eur), half, Week.Monday);

        // Assert
        Assert.Equal(Amount("0.01", Currency.Usd), worthAMinorUnit.QuoteAmount);
        Assert.Throws<ArgumentException>(() => TradeOf(TradeSide.BuyBase, Amount("0.01", Currency.Eur), lessThan, Week.Monday));
    }

    [Fact]
    public void A_trade_worth_less_than_one_yen_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => TradeOf(TradeSide.BuyBase, Amount("1", Currency.Usd), Rate(UsdJpy, "0.499"), Week.Monday));
        Assert.Equal(Amount("1", Currency.Jpy), TradeOf(TradeSide.BuyBase, Amount("1", Currency.Usd), Rate(UsdJpy, "0.500"), Week.Monday).QuoteAmount);
    }

    [Fact]
    public void Dealing_a_single_yen_against_a_dollar_needs_a_rate_that_leaves_a_cent() {
        // Act
        SpotTrade cent = TradeOf(TradeSide.SellBase, Amount("1", Currency.Jpy), Rate(UsdJpy, "151.237"), Week.Monday);

        // Assert
        Assert.Equal(Amount("0.01", Currency.Usd), cent.BaseAmount);
        Assert.Throws<ArgumentException>(() => TradeOf(TradeSide.SellBase, Amount("1", Currency.Jpy), Rate(UsdJpy, "1000"), Week.Monday));
    }

    [Fact]
    public void The_dealt_currency_must_be_one_of_the_pair() {
        // Arrange
        Currency anyForeignCurrency = Any.Currency().Except(Currency.Eur, Currency.Usd).Generate();
        Money    anyForeignAmount   = new AnyMoney().Positive().InCurrency(anyForeignCurrency).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TradeOf(TradeSide.BuyBase, anyForeignAmount, Rate(EurUsd, "1.08425"), Week.Monday));

        // Assert
        Assert.Equal("dealtAmount", refusal.ParamName);
    }

    [Fact]
    public void A_dealt_amount_is_strictly_positive() {
        // Arrange
        Currency anyDealtCurrency = Any.Currency().Generate();
        Currency anyOtherCurrency = Any.Currency().Except(anyDealtCurrency).Generate();
        ExchangeRate anyRate      = new AnyExchangeRate().WithPair(new CurrencyPair(anyDealtCurrency, anyOtherCurrency)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeOf(TradeSide.BuyBase, Money.Zero(anyDealtCurrency), anyRate, Week.Monday));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeOf(TradeSide.BuyBase, Amount("-1", anyDealtCurrency), anyRate, Week.Monday));
    }

    [Fact]
    public void A_dealt_amount_stops_at_one_trillion() {
        // Act
        SpotTrade largest = TradeOf(TradeSide.BuyBase, Amount("1000000000000", Currency.Eur), Rate(EurUsd, "1.08425"), Week.Monday);

        // Assert
        Assert.Equal(SpotTrade.MaxDealtAmount, largest.DealtAmount.Amount);
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeOf(TradeSide.BuyBase, Amount("1000000000000.01", Currency.Eur), Rate(EurUsd, "1.08425"), Week.Monday));
    }

    [Fact]
    public void The_other_leg_may_not_exceed_the_largest_money() {
        // Arrange
        Money trillionEuros = Amount("1000000000000", Currency.Eur);

        // Act
        SpotTrade justUnder = TradeOf(TradeSide.BuyBase, trillionEuros, Rate(EurUsd, "999.99999"), Week.Monday);

        // Assert
        Assert.Equal(Amount("999999990000000", Currency.Usd), justUnder.QuoteAmount);
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeOf(TradeSide.BuyBase, trillionEuros, Rate(EurUsd, "1000"), Week.Monday));
    }

    [Fact]
    public void Dealing_the_quote_can_blow_the_base_leg_too() {
        // Arrange: 1 trillion dollars at 0.00001 would be 1e17 euros
        Money trillionDollars = Amount("1000000000000", Currency.Usd);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeOf(TradeSide.BuyBase, trillionDollars, Rate(EurUsd, "0.00001"), Week.Monday));
    }

    [Fact]
    public void A_trade_is_dealt_on_a_business_day() {
        // Arrange
        Money    anyDealt   = new AnyMoney().InCurrency(Currency.Eur).Between(1_000m, 1_000_000m).Generate();
        DateOnly anyWeekend = Any.WeekendDay().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => TradeOf(TradeSide.BuyBase, anyDealt, Rate(EurUsd, "1.08425"), anyWeekend));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void A_side_the_desk_does_not_know_is_refused(int unknown) {
        // Arrange
        Money anyDealt = new AnyMoney().InCurrency(Currency.Eur).Between(1_000m, 1_000_000m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeOf((TradeSide)unknown, anyDealt, Rate(EurUsd, "1.08425"), Week.Monday));
    }

    [Fact]
    public void A_trade_needs_all_of_its_references() {
        // Arrange
        TradeId      anyId           = Any.TradeId().Generate();
        Lei          anyCounterparty = Any.Lei().Generate();
        Money        anyDealt        = new AnyMoney().InCurrency(Currency.Eur).Between(1_000m, 1_000_000m).Generate();
        ExchangeRate anyRate         = new AnyExchangeRate().WithPair(EurUsd).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(null!, anyCounterparty, TradeSide.BuyBase, anyDealt, anyRate, Week.Monday));
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(anyId, null!, TradeSide.BuyBase, anyDealt, anyRate, Week.Monday));
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(anyId, anyCounterparty, TradeSide.BuyBase, null!, anyRate, Week.Monday));
        Assert.Throws<ArgumentNullException>(() => new SpotTrade(anyId, anyCounterparty, TradeSide.BuyBase, anyDealt, null!, Week.Monday));
    }

    [Fact]
    public void A_trade_settles_on_the_spot_value_date_of_its_pair() {
        // Act
        SpotTrade euros   = TradeOf(TradeSide.BuyBase, Amount("1000", Currency.Eur), Rate(EurUsd, "1.08425"), Week.Thursday);
        SpotTrade canadas = TradeOf(TradeSide.BuyBase, Amount("1000", Currency.Usd), Rate(UsdCad, "1.35000"), Week.Friday);

        // Assert
        Assert.Equal(Week.NextMonday, euros.ValueDate);
        Assert.Equal(Week.Thursday, euros.TradeDate);
        Assert.Equal(Week.NextMonday, canadas.ValueDate);
        Assert.Equal(Week.Friday, canadas.TradeDate);
    }

    [Fact]
    public void Buying_the_base_receives_the_base_and_pays_the_quote() {
        // Act
        SpotTrade trade = TradeOf(TradeSide.BuyBase, Amount("1000000", Currency.Eur), Rate(EurUsd, "1.08425"), Week.Monday);

        // Assert
        Assert.Equal(Amount("1000000", Currency.Eur), trade.BankReceives);
        Assert.Equal(Amount("1084250", Currency.Usd), trade.BankPays);
    }

    [Fact]
    public void Selling_the_base_receives_the_quote_and_pays_the_base() {
        // Act
        SpotTrade trade = TradeOf(TradeSide.SellBase, Amount("1000000", Currency.Eur), Rate(EurUsd, "1.08425"), Week.Monday);

        // Assert
        Assert.Equal(Amount("1084250", Currency.Usd), trade.BankReceives);
        Assert.Equal(Amount("1000000", Currency.Eur), trade.BankPays);
    }

    [Fact]
    public void A_trade_exposes_what_it_was_dealt() {
        // Arrange
        TradeId id           = new TradeId("FX0000048213");
        Lei     counterparty = new Lei("5493001KJTIIGC8Y1R12");

        // Act
        SpotTrade trade = new SpotTrade(id, counterparty, TradeSide.SellBase, Amount("1000000", Currency.Eur), Rate(EurUsd, "1.08425"), Week.Monday);

        // Assert
        Assert.Equal(id, trade.Id);
        Assert.Equal(counterparty, trade.Counterparty);
        Assert.Equal(TradeSide.SellBase, trade.Side);
        Assert.Equal(Rate(EurUsd, "1.08425"), trade.Rate);
        Assert.Equal(EurUsd, trade.Pair);
    }

    [Fact]
    public void A_trade_reads_as_its_two_legs() {
        // Arrange
        SpotTrade trade = new SpotTrade(new TradeId("FX0000048213"), new Lei("5493001KJTIIGC8Y1R12"), TradeSide.BuyBase, Amount("1000000", Currency.Eur), Rate(EurUsd, "1.08425"), Week.Monday);

        // Act
        string text = trade.ToString();

        // Assert
        Assert.Equal("FX0000048213 BuyBase 1000000.00 EUR vs 1084250.00 USD @ EUR/USD 1.08425 value 2025-03-05", text);
    }

}
