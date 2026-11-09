using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests;

public sealed class SettlementBatchTests {

    private static readonly ExchangeRate EurUsd = Fixtures.Rate(Currency.Eur, Currency.Usd, 1.08425m);

    // The sheet of Wednesday the 12th: EUR and AUD quoted against the dollar, the yen as yen per dollar.
    private static RateSheet StandardSheet() {
        return new RateSheet(Currency.Usd, Fixtures.Wednesday, [
            EurUsd,
            Fixtures.Rate(Currency.Usd, Currency.Jpy, 151.237m),
            Fixtures.Rate(Fixtures.Aud, Currency.Usd, 0.76543m),
        ]);
    }

    // A sheet where the euro is worth exactly one dollar, so that the tolerance is easy to read.
    private static RateSheet ParitySheet() {
        return new RateSheet(Currency.Usd, Fixtures.Wednesday, [Fixtures.Rate(Currency.Eur, Currency.Usd, 1.00000m)]);
    }

    private static SettlementBatch OpenBatch() {
        decimal anyTolerance = Any.Decimal().Between(0.005m, SettlementBatch.MaxOffMarketTolerance).Generate();

        return new SettlementBatch(Fixtures.Wednesday, StandardSheet(), anyTolerance);
    }

    // Trades settle on Wednesday the 12th: dealt on Monday the 10th for a pair with a two-day lag.
    private static SpotTrade EurUsdTrade(Lei counterparty, TradeSide side, decimal dealtEur, ExchangeRate? rate = null, DateOnly? tradeDate = null) {
        TradeId anyId = Any.TradeId().Generate();

        return new SpotTrade(anyId, counterparty, side, Fixtures.Eur(dealtEur), rate ?? EurUsd, tradeDate ?? Fixtures.Monday);
    }

    private static SettlementRefusedException Refusal(Action action) {
        return Assert.Throws<SettlementRefusedException>(action);
    }

    // ---- construction ---------------------------------------------------------------------------------------

    [Fact]
    public void A_batch_starts_open_and_empty_with_what_it_was_built_from() {
        // Arrange
        RateSheet sheet = StandardSheet();

        // Act
        SettlementBatch batch = new SettlementBatch(Fixtures.Wednesday, sheet, 0.02m);

        // Assert
        Assert.Equal(BatchStatus.Open, batch.Status);
        Assert.Equal(Fixtures.Wednesday, batch.ValueDate);
        Assert.Same(sheet, batch.ReferenceRates);
        Assert.Equal(0.02m, batch.OffMarketTolerance);
        Assert.Empty(batch.Trades);
        Assert.Empty(batch.NetPositions());
    }

    [Theory]
    [InlineData("2025-03-15")]
    [InlineData("2025-03-16")]
    public void Nothing_settles_on_a_weekend(string valueDate) {
        // Arrange
        RateSheet anySheet     = Any.RateSheet().WithAsOf(Fixtures.Friday).Generate();
        decimal   anyTolerance = Any.Decimal().Between(0.005m, SettlementBatch.MaxOffMarketTolerance).Generate();

        // Act
        Action construct = () => new SettlementBatch(DateOnly.Parse(valueDate), anySheet, anyTolerance);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void The_reference_rates_may_not_postdate_the_value_date() {
        // Arrange: a sheet fixed the day after the value date.
        RateSheet postdated   = Any.RateSheet().WithAsOf(Fixtures.Thursday).Generate();
        decimal   anyTolerance = Any.Decimal().Between(0.005m, SettlementBatch.MaxOffMarketTolerance).Generate();

        // Act
        Action construct = () => new SettlementBatch(Fixtures.Wednesday, postdated, anyTolerance);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void Reference_rates_of_the_value_date_itself_are_accepted() {
        // Arrange
        RateSheet sameDay      = Any.RateSheet().WithAsOf(Fixtures.Wednesday).Generate();
        decimal   anyTolerance = Any.Decimal().Between(0.005m, SettlementBatch.MaxOffMarketTolerance).Generate();

        // Act
        SettlementBatch batch = new SettlementBatch(Fixtures.Wednesday, sameDay, anyTolerance);

        // Assert
        Assert.Equal(Fixtures.Wednesday, batch.ReferenceRates.AsOf);
    }

    [Fact]
    public void Reference_rates_of_an_earlier_day_are_accepted() {
        // Arrange
        RateSheet earlier      = Any.RateSheet().WithAsOf(Fixtures.Monday).Generate();
        decimal   anyTolerance = Any.Decimal().Between(0.005m, SettlementBatch.MaxOffMarketTolerance).Generate();

        // Act
        SettlementBatch batch = new SettlementBatch(Fixtures.Wednesday, earlier, anyTolerance);

        // Assert
        Assert.Equal(Fixtures.Monday, batch.ReferenceRates.AsOf);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("0.0500001")]
    [InlineData("0.051")]
    [InlineData("1")]
    public void The_tolerance_lies_in_zero_exclusive_to_five_percent(string tolerance) {
        // Arrange
        RateSheet anySheet = Any.RateSheet().WithAsOf(Fixtures.Wednesday).Generate();

        // Act
        Action construct = () => new SettlementBatch(Fixtures.Wednesday, anySheet, decimal.Parse(tolerance, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Theory]
    [InlineData("0.05")]
    [InlineData("0.0000001")]
    public void The_bounds_of_the_tolerance_range_that_belong_to_it_are_accepted(string tolerance) {
        // Arrange
        RateSheet anySheet = Any.RateSheet().WithAsOf(Fixtures.Wednesday).Generate();

        // Act
        SettlementBatch batch = new SettlementBatch(Fixtures.Wednesday, anySheet, decimal.Parse(tolerance, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(decimal.Parse(tolerance, System.Globalization.CultureInfo.InvariantCulture), batch.OffMarketTolerance);
    }

    [Fact]
    public void A_batch_refuses_missing_reference_rates() {
        Assert.Throws<ArgumentNullException>(() => new SettlementBatch(Fixtures.Wednesday, null!, 0.01m));
    }

    // ---- booking --------------------------------------------------------------------------------------------

    [Fact]
    public void A_booked_trade_is_listed_among_the_trades_of_the_batch() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        Lei             anyCounterparty = Any.Lei().Generate();
        SpotTrade       trade = EurUsdTrade(anyCounterparty, TradeSide.BuyBase, 100.00m);

        // Act
        batch.Book(trade);

        // Assert
        Assert.Same(trade, Assert.Single(batch.Trades));
    }

    [Fact]
    public void Booking_a_buy_posts_the_base_received_and_the_quote_paid_per_counterparty_and_currency() {
        // Arrange: the bank buys 100.00 EUR for 108.43 USD (108.425 rounded away from zero).
        SettlementBatch batch = OpenBatch();

        // Act
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));

        // Assert
        Assert.Equal(
            [
                new NetPosition(Fixtures.LeiFirst, Fixtures.Eur(100.00m)),
                new NetPosition(Fixtures.LeiFirst, Fixtures.Usd(-108.43m)),
            ],
            batch.NetPositions());
    }

    [Fact]
    public void Booking_a_sell_posts_the_quote_received_and_the_base_paid() {
        // Arrange
        SettlementBatch batch = OpenBatch();

        // Act
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.SellBase, 100.00m));

        // Assert
        Assert.Equal(
            [
                new NetPosition(Fixtures.LeiFirst, Fixtures.Eur(-100.00m)),
                new NetPosition(Fixtures.LeiFirst, Fixtures.Usd(108.43m)),
            ],
            batch.NetPositions());
    }

    [Fact]
    public void Trades_with_one_counterparty_net_in_each_currency() {
        // Arrange: +100.00 EUR / -108.43 USD, then -40.00 EUR / +43.37 USD.
        SettlementBatch batch = OpenBatch();

        // Act
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.SellBase, 40.00m));

        // Assert
        Assert.Equal(
            [
                new NetPosition(Fixtures.LeiFirst, Fixtures.Eur(60.00m)),
                new NetPosition(Fixtures.LeiFirst, Fixtures.Usd(-65.06m)),
            ],
            batch.NetPositions());
    }

    [Fact]
    public void A_flat_position_is_not_listed() {
        // Arrange: buying and selling the same amount at the same rate leaves nothing in either currency.
        SettlementBatch batch = OpenBatch();

        // Act
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.SellBase, 100.00m));

        // Assert
        Assert.Empty(batch.NetPositions());
        Assert.Equal(2, batch.Trades.Count);
    }

    [Fact]
    public void Positions_are_ordered_by_counterparty_then_currency_code() {
        // Arrange: booked in the reverse order on purpose; LeiFirst sorts before LeiSecond.
        SettlementBatch batch = OpenBatch();
        ExchangeRate usdJpy = Fixtures.Rate(Currency.Usd, Currency.Jpy, 151.237m);

        // Act
        batch.Book(EurUsdTrade(Fixtures.LeiSecond, TradeSide.BuyBase, 100.00m));
        batch.Book(new SpotTrade(Any.TradeId().Generate(), Fixtures.LeiFirst, TradeSide.BuyBase, Fixtures.Usd(10.00m), usdJpy, Fixtures.Monday));
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.SellBase, 20.00m));

        // Assert
        Assert.Equal(
            [
                (Fixtures.LeiFirst, "EUR"),
                (Fixtures.LeiFirst, "JPY"),
                (Fixtures.LeiFirst, "USD"),
                (Fixtures.LeiSecond, "EUR"),
                (Fixtures.LeiSecond, "USD"),
            ],
            batch.NetPositions().Select(position => (position.Counterparty, position.Currency.Code)));
    }

    [Fact]
    public void The_bank_net_position_adds_up_every_counterparty() {
        // Arrange: the bank buys 100.00 EUR from one counterparty and sells 30.00 EUR to the other.
        SettlementBatch batch = OpenBatch();

        // Act
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));
        batch.Book(EurUsdTrade(Fixtures.LeiSecond, TradeSide.SellBase, 30.00m));

        // Assert: -108.43 + 32.53 (30 * 1.08425 = 32.5275, rounded away from zero).
        Assert.Equal(Fixtures.Eur(70.00m), batch.BankNetPosition(Currency.Eur));
        Assert.Equal(Fixtures.Usd(-75.90m), batch.BankNetPosition(Currency.Usd));
    }

    [Fact]
    public void Offsetting_trades_with_two_counterparties_leave_the_bank_flat_but_not_its_counterparties() {
        // Arrange
        SettlementBatch batch = OpenBatch();

        // Act
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));
        batch.Book(EurUsdTrade(Fixtures.LeiSecond, TradeSide.SellBase, 100.00m));

        // Assert
        Assert.True(batch.BankNetPosition(Currency.Eur).IsZero);
        Assert.True(batch.BankNetPosition(Currency.Usd).IsZero);
        Assert.Equal(4, batch.NetPositions().Count);
    }

    [Fact]
    public void The_bank_net_position_in_a_currency_never_traded_is_zero_in_that_currency() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        Currency        anyTraded = Any.Currency().Generate();

        // Act
        Money net = batch.BankNetPosition(anyTraded);

        // Assert
        Assert.True(net.IsZero);
        Assert.Equal(anyTraded, net.Currency);
    }

    [Fact]
    public void A_trade_settling_on_another_day_is_refused_with_its_reason() {
        // Arrange: dealt on Tuesday, it settles on Thursday.
        SettlementBatch batch = OpenBatch();
        SpotTrade       thursdayTrade = EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m, tradeDate: Fixtures.Tuesday);

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Book(thursdayTrade));

        // Assert
        Assert.Equal(SettlementRefusalReason.ValueDateMismatch, refusal.Reason);
        Assert.Empty(batch.Trades);
    }

    [Fact]
    public void A_trade_id_already_booked_is_refused_with_its_reason() {
        // Arrange
        SettlementBatch batch           = OpenBatch();
        TradeId         anyId           = Any.TradeId().Generate();
        Lei             anyCounterparty = Any.Lei().Generate();
        batch.Book(new SpotTrade(anyId, anyCounterparty, TradeSide.BuyBase, Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday));

        // Act: the same reference again, even for another counterparty and another amount.
        SettlementRefusedException refusal = Refusal(() => batch.Book(new SpotTrade(anyId, Any.Lei().Generate(), TradeSide.SellBase, Fixtures.Eur(5.00m), EurUsd, Fixtures.Monday)));

        // Assert
        Assert.Equal(SettlementRefusalReason.DuplicateTrade, refusal.Reason);
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void A_pair_the_sheet_cannot_price_is_refused_with_its_reason() {
        // Arrange: the sheet quotes neither CAD nor GBP.
        SettlementBatch batch       = OpenBatch();
        ExchangeRate    gbpCad      = Fixtures.Rate(Currency.Gbp, Currency.Cad, 1.80000m);
        SpotTrade       unpriceable = new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), TradeSide.BuyBase, new Money(1000m, Currency.Gbp), gbpCad, Fixtures.Monday);

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Book(unpriceable));

        // Assert
        Assert.Equal(SettlementRefusalReason.NoReferenceRate, refusal.Reason);
        Assert.Empty(batch.Trades);
    }

    [Fact]
    public void A_pair_with_one_currency_the_sheet_does_not_quote_is_refused() {
        // Arrange: EUR is quoted, GBP is not.
        SettlementBatch batch  = OpenBatch();
        ExchangeRate    eurGbp = Fixtures.Rate(Currency.Eur, Currency.Gbp, 0.85000m);
        SpotTrade       trade  = new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), TradeSide.BuyBase, Fixtures.Eur(1000m), eurGbp, Fixtures.Monday);

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Book(trade));

        // Assert
        Assert.Equal(SettlementRefusalReason.NoReferenceRate, refusal.Reason);
    }

    [Theory]
    [InlineData("1.01000", true)]
    [InlineData("0.99000", true)]
    [InlineData("1.00000", true)]
    [InlineData("1.01001", false)]
    [InlineData("0.98999", false)]
    [InlineData("1.10000", false)]
    [InlineData("0.50000", false)]
    public void A_dealt_rate_is_accepted_up_to_the_tolerance_around_the_reference_rate_inclusive(string dealtRate, bool accepted) {
        // Arrange: the reference is exactly 1, the tolerance exactly one percent.
        SettlementBatch batch = new SettlementBatch(Fixtures.Wednesday, ParitySheet(), 0.01m);
        SpotTrade       trade = EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m, Fixtures.Rate(Currency.Eur, Currency.Usd, decimal.Parse(dealtRate, System.Globalization.CultureInfo.InvariantCulture)));

        // Act
        Action book = () => batch.Book(trade);

        // Assert
        if (accepted) {
            book();
            Assert.Single(batch.Trades);
        } else {
            Assert.Equal(SettlementRefusalReason.RateOffMarket, Refusal(book).Reason);
            Assert.Empty(batch.Trades);
        }
    }

    [Fact]
    public void An_off_market_refusal_leaves_the_postings_untouched() {
        // Arrange
        SettlementBatch batch = new SettlementBatch(Fixtures.Wednesday, ParitySheet(), 0.01m);
        SpotTrade       offMarket = EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m, Fixtures.Rate(Currency.Eur, Currency.Usd, 1.5m));

        // Act
        Refusal(() => batch.Book(offMarket));

        // Assert
        Assert.Empty(batch.NetPositions());
        Assert.True(batch.BankNetPosition(Currency.Eur).IsZero);
    }

    [Fact]
    public void The_tolerance_the_batch_was_built_with_is_the_one_applied() {
        // Arrange: a 3 % gap is within a 5 % tolerance and beyond a 1 % one.
        SpotTrade tradeAt103 = EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m, Fixtures.Rate(Currency.Eur, Currency.Usd, 1.03000m));
        SettlementBatch loose  = new SettlementBatch(Fixtures.Wednesday, ParitySheet(), 0.05m);
        SettlementBatch strict = new SettlementBatch(Fixtures.Wednesday, ParitySheet(), 0.01m);

        // Act
        loose.Book(tradeAt103);

        // Assert
        Assert.Equal(SettlementRefusalReason.RateOffMarket, Refusal(() => strict.Book(tradeAt103)).Reason);
    }

    [Fact]
    public void The_reference_of_the_inverse_pair_is_the_quoted_rate_inverted() {
        // Arrange: a trade on USD/EUR at the inverse of 1.08425, which the sheet only quotes the other way round.
        SettlementBatch batch = new SettlementBatch(Fixtures.Wednesday, StandardSheet(), 0.001m);
        ExchangeRate    usdEur = Fixtures.Rate(Currency.Usd, Currency.Eur, 0.92230m);

        // Act
        batch.Book(new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), TradeSide.BuyBase, Fixtures.Usd(1000.00m), usdEur, Fixtures.Monday));

        // Assert
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void The_reference_of_a_cross_pair_is_triangulated_through_the_pivot() {
        // Arrange: EUR/JPY is not quoted; the sheet gives 163.979 by triangulation. A tight tolerance proves the reference is that one.
        SettlementBatch batch   = new SettlementBatch(Fixtures.Wednesday, StandardSheet(), 0.0001m);
        ExchangeRate    eurJpy  = Fixtures.Rate(Currency.Eur, Currency.Jpy, 163.979m);
        ExchangeRate    farOff  = Fixtures.Rate(Currency.Eur, Currency.Jpy, 164.100m);

        // Act
        batch.Book(new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), TradeSide.BuyBase, Fixtures.Eur(1000.00m), eurJpy, Fixtures.Monday));
        SettlementRefusedException refusal = Refusal(() => batch.Book(new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), TradeSide.BuyBase, Fixtures.Eur(1000.00m), farOff, Fixtures.Monday)));

        // Assert
        Assert.Equal(SettlementRefusalReason.RateOffMarket, refusal.Reason);
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void A_refusal_is_a_settlement_refusal_and_also_an_invalid_operation() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.CutOff();

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Book(EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m)));

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
        Assert.False(string.IsNullOrWhiteSpace(refusal.Message));
    }

    // ---- the order in which a booking is checked -----------------------------------------------------------

    [Fact]
    public void A_cut_off_batch_refuses_before_looking_at_the_trade() {
        // Arrange: the trade would also settle on the wrong day.
        SettlementBatch batch = OpenBatch();
        batch.CutOff();
        SpotTrade wrongDay = EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m, tradeDate: Fixtures.Tuesday);

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Book(wrongDay));

        // Assert
        Assert.Equal(SettlementRefusalReason.BatchCutOff, refusal.Reason);
    }

    [Fact]
    public void The_value_date_is_checked_before_the_rate() {
        // Arrange: the trade settles on the wrong day and is also far off market.
        SettlementBatch batch = new SettlementBatch(Fixtures.Wednesday, ParitySheet(), 0.01m);
        SpotTrade       wrongDayOffMarket = EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m, Fixtures.Rate(Currency.Eur, Currency.Usd, 2m), Fixtures.Tuesday);

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Book(wrongDayOffMarket));

        // Assert
        Assert.Equal(SettlementRefusalReason.ValueDateMismatch, refusal.Reason);
    }

    [Fact]
    public void The_value_date_is_checked_before_the_duplicate() {
        // Arrange: a booked id comes back for the wrong day.
        SettlementBatch batch = OpenBatch();
        TradeId         anyId = Any.TradeId().Generate();
        Lei             anyCounterparty = Any.Lei().Generate();
        batch.Book(new SpotTrade(anyId, anyCounterparty, TradeSide.BuyBase, Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday));

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Book(new SpotTrade(anyId, anyCounterparty, TradeSide.BuyBase, Fixtures.Eur(100.00m), EurUsd, Fixtures.Tuesday)));

        // Assert
        Assert.Equal(SettlementRefusalReason.ValueDateMismatch, refusal.Reason);
    }

    [Fact]
    public void The_duplicate_is_checked_before_the_reference_rate() {
        // Arrange: a booked id comes back on a pair the sheet cannot price.
        SettlementBatch batch = OpenBatch();
        TradeId         anyId = Any.TradeId().Generate();
        Lei             anyCounterparty = Any.Lei().Generate();
        batch.Book(new SpotTrade(anyId, anyCounterparty, TradeSide.BuyBase, Fixtures.Eur(100.00m), EurUsd, Fixtures.Monday));
        ExchangeRate gbpCad = Fixtures.Rate(Currency.Gbp, Currency.Cad, 1.80000m);

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Book(new SpotTrade(anyId, anyCounterparty, TradeSide.BuyBase, new Money(1000m, Currency.Gbp), gbpCad, Fixtures.Monday)));

        // Assert
        Assert.Equal(SettlementRefusalReason.DuplicateTrade, refusal.Reason);
    }

    [Fact]
    public void A_missing_trade_is_refused() {
        Assert.Throws<ArgumentNullException>(() => OpenBatch().Book(null!));
    }

    // ---- cancelling -----------------------------------------------------------------------------------------

    [Fact]
    public void Cancelling_a_trade_reverses_its_postings() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        SpotTrade       trade = EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m);
        batch.Book(trade);

        // Act
        batch.Cancel(trade.Id);

        // Assert
        Assert.Empty(batch.Trades);
        Assert.Empty(batch.NetPositions());
        Assert.True(batch.BankNetPosition(Currency.Eur).IsZero);
        Assert.True(batch.BankNetPosition(Currency.Usd).IsZero);
    }

    [Fact]
    public void Cancelling_one_trade_leaves_the_others_netted() {
        // Arrange
        SettlementBatch batch     = OpenBatch();
        SpotTrade       kept      = EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m);
        SpotTrade       cancelled = EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 40.00m);
        batch.Book(kept);
        batch.Book(cancelled);

        // Act
        batch.Cancel(cancelled.Id);

        // Assert
        Assert.Same(kept, Assert.Single(batch.Trades));
        Assert.Equal(
            [
                new NetPosition(Fixtures.LeiFirst, Fixtures.Eur(100.00m)),
                new NetPosition(Fixtures.LeiFirst, Fixtures.Usd(-108.43m)),
            ],
            batch.NetPositions());
    }

    [Fact]
    public void A_cancelled_trade_id_can_be_booked_again() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        SpotTrade       trade = EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m);
        batch.Book(trade);
        batch.Cancel(trade.Id);

        // Act
        batch.Book(trade);

        // Assert
        Assert.Same(trade, Assert.Single(batch.Trades));
    }

    [Fact]
    public void Cancelling_an_unknown_trade_is_refused_with_its_reason() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        TradeId         anyUnknownId = Any.TradeId().Generate();

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Cancel(anyUnknownId));

        // Assert
        Assert.Equal(SettlementRefusalReason.UnknownTrade, refusal.Reason);
    }

    [Fact]
    public void A_trade_cannot_be_cancelled_twice() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        SpotTrade       trade = EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m);
        batch.Book(trade);
        batch.Cancel(trade.Id);

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Cancel(trade.Id));

        // Assert
        Assert.Equal(SettlementRefusalReason.UnknownTrade, refusal.Reason);
    }

    [Fact]
    public void Nothing_is_cancelled_after_cut_off_and_the_cut_off_is_what_is_reported() {
        // Arrange: the id is unknown too, but the cut-off comes first.
        SettlementBatch batch = OpenBatch();
        batch.CutOff();
        TradeId anyUnknownId = Any.TradeId().Generate();

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Cancel(anyUnknownId));

        // Assert
        Assert.Equal(SettlementRefusalReason.BatchCutOff, refusal.Reason);
    }

    [Fact]
    public void A_booked_trade_cannot_be_cancelled_after_cut_off() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        SpotTrade       trade = EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m);
        batch.Book(trade);
        batch.CutOff();

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Cancel(trade.Id));

        // Assert
        Assert.Equal(SettlementRefusalReason.BatchCutOff, refusal.Reason);
        Assert.Single(batch.Trades);
    }

    // ---- cut-off --------------------------------------------------------------------------------------------

    [Fact]
    public void Cutting_off_closes_the_batch() {
        // Arrange
        SettlementBatch batch = OpenBatch();

        // Act
        batch.CutOff();

        // Assert
        Assert.Equal(BatchStatus.CutOff, batch.Status);
    }

    [Fact]
    public void A_batch_is_cut_off_only_once() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.CutOff();

        // Act
        SettlementRefusedException refusal = Refusal(batch.CutOff);

        // Assert
        Assert.Equal(SettlementRefusalReason.AlreadyCutOff, refusal.Reason);
        Assert.Equal(BatchStatus.CutOff, batch.Status);
    }

    [Fact]
    public void Booking_is_closed_after_cut_off() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.CutOff();

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Book(EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m)));

        // Assert
        Assert.Equal(SettlementRefusalReason.BatchCutOff, refusal.Reason);
        Assert.Empty(batch.Trades);
    }

    [Fact]
    public void The_net_positions_are_still_readable_after_cut_off_and_final() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));

        // Act
        batch.CutOff();

        // Assert
        Assert.Equal(2, batch.NetPositions().Count);
        Assert.Equal(Fixtures.Eur(100.00m), batch.BankNetPosition(Currency.Eur));
    }

    // ---- valuation ------------------------------------------------------------------------------------------

    [Fact]
    public void The_net_value_converts_each_currency_with_banker_s_rounding() {
        // Arrange: +100.00 EUR is worth 108.425 USD, which banker's rounding makes 108.42, against -108.43 USD paid.
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m));

        // Act
        Money netValue = batch.NetValueIn(Currency.Usd);

        // Assert: the client was charged away from zero, the desk values to even, hence a cent apart.
        Assert.Equal(Fixtures.Usd(-0.01m), netValue);
    }

    [Fact]
    public void The_net_value_in_the_base_currency_uses_the_inverse_rate_rounded_once() {
        // Arrange: USD -> EUR through 0.92230 (the inverse of 1.08425 rounded to five places): -108.43 -> -100.00.
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m));

        // Act
        Money netValue = batch.NetValueIn(Currency.Eur);

        // Assert
        Assert.Equal(Fixtures.Eur(0.00m), netValue);
    }

    [Fact]
    public void The_net_value_in_a_cross_currency_goes_through_the_triangulated_rate() {
        // Arrange: EUR/JPY 163.979 turns +100.00 EUR into 16 398 JPY; USD/JPY 151.237 turns -108.43 USD into -16 399 JPY.
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m));

        // Act
        Money netValue = batch.NetValueIn(Currency.Jpy);

        // Assert
        Assert.Equal(Fixtures.Jpy(-1m), netValue);
    }

    [Fact]
    public void The_net_value_of_an_empty_batch_is_zero_in_the_reporting_currency() {
        Money netValue = OpenBatch().NetValueIn(Currency.Jpy);

        Assert.True(netValue.IsZero);
        Assert.Equal(Currency.Jpy, netValue.Currency);
    }

    [Fact]
    public void The_net_value_cannot_be_given_in_a_currency_the_sheet_does_not_cover() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        Currency anyUncovered = Any.Currency().WithCode(c => c.Except("USD", "EUR", "JPY", "AUD")).Generate();

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.NetValueIn(anyUncovered));

        // Assert
        Assert.Equal(SettlementRefusalReason.NoReferenceRate, refusal.Reason);
    }

    [Fact]
    public void Exposure_to_a_counterparty_values_only_that_counterpartys_nets() {
        // Arrange: the first counterparty trades 100.00 EUR, the second sells 50.00 EUR.
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));
        batch.Book(EurUsdTrade(Fixtures.LeiSecond, TradeSide.SellBase, 50.00m));

        // Act
        Money first  = batch.ExposureTo(Fixtures.LeiFirst, Currency.Usd);
        Money second = batch.ExposureTo(Fixtures.LeiSecond, Currency.Usd);

        // Assert: +100.00 EUR -> 108.42, -108.43 USD: -0.01. Second: -50.00 EUR -> -54.21 (54.2125 to even), +54.21 USD (54.2125 away from zero).
        Assert.Equal(Fixtures.Usd(-0.01m), first);
        Assert.Equal(Fixtures.Usd(0.00m), second);
    }

    [Fact]
    public void Exposure_to_a_counterparty_never_traded_is_zero() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));
        Lei anyOtherCounterparty = Any.Lei().Generate();

        // Act
        Money exposure = batch.ExposureTo(anyOtherCounterparty, Currency.Usd);

        // Assert
        Assert.True(exposure.IsZero);
        Assert.Equal(Currency.Usd, exposure.Currency);
    }

    [Fact]
    public void Exposure_cannot_be_given_in_a_currency_the_sheet_does_not_cover() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        Lei      anyCounterparty = Any.Lei().Generate();
        Currency anyUncovered    = Any.Currency().WithCode(c => c.Except("USD", "EUR", "JPY", "AUD")).Generate();

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.ExposureTo(anyCounterparty, anyUncovered));

        // Assert
        Assert.Equal(SettlementRefusalReason.NoReferenceRate, refusal.Reason);
    }

    // ---- instructions ---------------------------------------------------------------------------------------

    [Fact]
    public void There_are_no_instructions_while_the_batch_is_open() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m));

        // Act
        SettlementRefusedException refusal = Refusal(() => batch.Instructions());

        // Assert
        Assert.Equal(SettlementRefusalReason.NotCutOff, refusal.Reason);
    }

    [Fact]
    public void After_cut_off_each_non_flat_position_becomes_an_instruction_on_the_batch_value_date() {
        // Arrange: the bank buys 100.00 EUR (receives them) and pays 108.43 USD.
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));
        batch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = batch.Instructions();

        // Assert
        Assert.Equal(
            [
                new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Receive, Fixtures.Eur(100.00m), Fixtures.Wednesday),
                new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Pay, Fixtures.Usd(108.43m), Fixtures.Wednesday),
            ],
            instructions);
    }

    [Fact]
    public void A_negative_net_becomes_a_payment_of_its_absolute_amount() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Fixtures.LeiSecond, TradeSide.SellBase, 100.00m));
        batch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = batch.Instructions();

        // Assert
        Assert.Equal(
            [
                new SettlementInstruction(Fixtures.LeiSecond, SettlementDirection.Pay, Fixtures.Eur(100.00m), Fixtures.Wednesday),
                new SettlementInstruction(Fixtures.LeiSecond, SettlementDirection.Receive, Fixtures.Usd(108.43m), Fixtures.Wednesday),
            ],
            instructions);
    }

    [Fact]
    public void Instructions_follow_the_order_of_the_net_positions() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Fixtures.LeiSecond, TradeSide.BuyBase, 100.00m));
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));
        batch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = batch.Instructions();

        // Assert
        Assert.Equal(
            [Fixtures.LeiFirst, Fixtures.LeiFirst, Fixtures.LeiSecond, Fixtures.LeiSecond],
            instructions.Select(instruction => instruction.Counterparty));
        Assert.Equal(
            ["EUR", "USD", "EUR", "USD"],
            instructions.Select(instruction => instruction.Amount.Currency.Code));
    }

    [Fact]
    public void A_flat_position_gives_no_instruction() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.BuyBase, 100.00m));
        batch.Book(EurUsdTrade(Fixtures.LeiFirst, TradeSide.SellBase, 100.00m));
        batch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = batch.Instructions();

        // Assert
        Assert.Empty(instructions);
    }

    [Fact]
    public void An_empty_batch_gives_no_instruction() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        batch.CutOff();

        // Assert
        Assert.Empty(batch.Instructions());
    }

    [Fact]
    public void A_cancelled_trade_gives_no_instruction() {
        // Arrange
        SettlementBatch batch = OpenBatch();
        SpotTrade       trade = EurUsdTrade(Any.Lei().Generate(), TradeSide.BuyBase, 100.00m);
        batch.Book(trade);
        batch.Cancel(trade.Id);
        batch.CutOff();

        // Assert
        Assert.Empty(batch.Instructions());
    }

    [Fact]
    public void A_trade_of_a_currency_with_no_minor_units_settles_in_whole_units() {
        // Arrange: the bank buys 10.00 USD with yen at 151.237: it receives 10.00 USD and pays 1 512 JPY (1 512.37 rounded).
        SettlementBatch batch = OpenBatch();
        ExchangeRate    usdJpy = Fixtures.Rate(Currency.Usd, Currency.Jpy, 151.237m);
        batch.Book(new SpotTrade(Any.TradeId().Generate(), Fixtures.LeiFirst, TradeSide.BuyBase, Fixtures.Usd(10.00m), usdJpy, Fixtures.Monday));
        batch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = batch.Instructions();

        // Assert
        Assert.Equal(
            [
                new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Pay, Fixtures.Jpy(1512m), Fixtures.Wednesday),
                new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Receive, Fixtures.Usd(10.00m), Fixtures.Wednesday),
            ],
            instructions);
    }

    [Fact]
    public void A_USD_CAD_trade_dealt_the_day_before_settles_with_the_batch() {
        // Arrange: USD/CAD settles one business day after the trade date, so a Tuesday trade lands on Wednesday.
        RateSheet sheet = new RateSheet(Currency.Usd, Fixtures.Wednesday, [Fixtures.Rate(Currency.Usd, Currency.Cad, 1.43210m)]);
        SettlementBatch batch = new SettlementBatch(Fixtures.Wednesday, sheet, 0.01m);
        SpotTrade       trade = new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), TradeSide.BuyBase, Fixtures.Usd(100.00m), Fixtures.Rate(Currency.Usd, Currency.Cad, 1.43210m), Fixtures.Tuesday);

        // Act
        batch.Book(trade);

        // Assert
        Assert.Same(trade, Assert.Single(batch.Trades));
    }

    // ---- drawn batches --------------------------------------------------------------------------------------

    [Fact]
    public void A_trade_at_a_quoted_reference_rate_settling_on_the_value_date_can_be_booked() {
        // Arrange: whatever the sheet and the amounts, a trade dealt at a rate the sheet quotes is exactly on market.
        RateSheet       anySheet  = Any.RateSheet().WithAsOf(Fixtures.Monday).Generate();
        SettlementBatch batch     = new SettlementBatch(Fixtures.Wednesday, anySheet, 0.005m);
        ExchangeRate    anyQuoted = anySheet.Rates.First();
        SpotTrade       anyTrade  = Any.SpotTrade().WithRate(anyQuoted).SettlingOn(Fixtures.Wednesday).Generate();

        // Act
        batch.Book(anyTrade);

        // Assert
        Assert.Same(anyTrade, Assert.Single(batch.Trades));
    }

}
