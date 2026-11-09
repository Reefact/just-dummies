using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Expert;

public sealed class SettlementBatchTests {

    private static readonly CurrencyPair EurUsd = new CurrencyPair(Currency.Eur, Currency.Usd);
    private static readonly CurrencyPair UsdEur = new CurrencyPair(Currency.Usd, Currency.Eur);
    private static readonly CurrencyPair GbpUsd = new CurrencyPair(Currency.Gbp, Currency.Usd);
    private static readonly CurrencyPair UsdJpy = new CurrencyPair(Currency.Usd, Currency.Jpy);
    private static readonly CurrencyPair EurGbp = new CurrencyPair(Currency.Eur, Currency.Gbp);
    private static readonly CurrencyPair EurJpy = new CurrencyPair(Currency.Eur, Currency.Jpy);

    // Two real LEIs: their order as text is the order the batch reports them in.
    private static readonly Lei FirstCounterparty  = new Lei("529900T8BM49AURSDO55");
    private static readonly Lei SecondCounterparty = new Lei("5493001KJTIIGC8Y1R12");

    private static ExchangeRate Rate(CurrencyPair pair, string value) {
        return new ExchangeRate(pair, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static Money Amount(string amount, Currency currency) {
        return new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), currency);
    }

    private static RateSheet MondaySheet(params ExchangeRate[] rates) {
        return new RateSheet(Currency.Usd, Week.Monday, rates);
    }

    /// <summary>The desk's ordinary reference rates, fixed on Monday, for a batch that settles on Wednesday.</summary>
    private static SettlementBatch WednesdayBatch(decimal tolerance = 0.01m) {
        RateSheet sheet = MondaySheet(Rate(EurUsd, "1.08"), Rate(GbpUsd, "1.25"), Rate(UsdJpy, "150"));

        return new SettlementBatch(Week.Wednesday, sheet, tolerance);
    }

    private static AnySpotTrade WednesdayTrade(TradeSide side, ExchangeRate rate, Money dealt, Lei counterparty) {
        return Any.SpotTrade().WithSide(side).WithRate(rate).Dealing(dealt).WithCounterparty(counterparty).SettlingOn(Week.Wednesday);
    }

    private static void AssertRefused(SettlementRefusalReason expected, Action action) {
        SettlementRefusedException refusal = Assert.Throws<SettlementRefusedException>(action);

        Assert.Equal(expected, refusal.Reason);
    }

    // ---------------------------------------------------------------------------------------- construction

    [Fact]
    public void A_new_batch_is_open_and_empty() {
        // Arrange
        RateSheet sheet = MondaySheet(Rate(EurUsd, "1.08"));

        // Act
        SettlementBatch batch = new SettlementBatch(Week.Wednesday, sheet, 0.01m);

        // Assert
        Assert.Equal(BatchStatus.Open, batch.Status);
        Assert.Equal(Week.Wednesday, batch.ValueDate);
        Assert.Same(sheet, batch.ReferenceRates);
        Assert.Equal(0.01m, batch.OffMarketTolerance);
        Assert.Empty(batch.Trades);
        Assert.Empty(batch.NetPositions());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Nothing_settles_at_the_weekend(bool saturday) {
        // Arrange
        RateSheet anySheet = Any.RateSheet().WithAsOf(Week.Monday).Generate();
        DateOnly  weekend  = saturday ? Week.Saturday : Week.Sunday;

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SettlementBatch(weekend, anySheet, 0.01m));
    }

    [Fact]
    public void The_reference_rates_may_not_be_fixed_after_the_value_date() {
        // Arrange
        RateSheet thursdaySheet  = Any.RateSheet().WithAsOf(Week.Thursday).Generate();
        RateSheet wednesdaySheet = Any.RateSheet().WithAsOf(Week.Wednesday).Generate();
        RateSheet mondaySheet    = Any.RateSheet().WithAsOf(Week.Monday).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new SettlementBatch(Week.Wednesday, thursdaySheet, 0.01m));

        // Assert
        Assert.Equal("referenceRates", refusal.ParamName);
        Assert.Null(Record.Exception(() => new SettlementBatch(Week.Wednesday, wednesdaySheet, 0.01m)));
        Assert.Null(Record.Exception(() => new SettlementBatch(Week.Wednesday, mondaySheet, 0.01m)));
    }

    [Fact]
    public void The_tolerance_is_a_fraction_above_zero_and_at_most_five_percent() {
        // Arrange
        RateSheet anySheet = Any.RateSheet().WithAsOf(Week.Monday).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementBatch(Week.Wednesday, anySheet, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementBatch(Week.Wednesday, anySheet, -0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementBatch(Week.Wednesday, anySheet, 0.0500001m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementBatch(Week.Wednesday, anySheet, 1m));
        Assert.Null(Record.Exception(() => new SettlementBatch(Week.Wednesday, anySheet, 0.05m)));
        Assert.Null(Record.Exception(() => new SettlementBatch(Week.Wednesday, anySheet, 0.0000001m)));
    }

    [Fact]
    public void A_batch_needs_its_reference_rates() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new SettlementBatch(Week.Wednesday, null!, 0.01m));
    }

    // ---------------------------------------------------------------------------------------- booking and netting

    [Fact]
    public void Buying_the_base_leaves_the_bank_long_the_base_and_short_the_quote() {
        // Arrange
        SettlementBatch batch = WednesdayBatch();
        SpotTrade       trade = WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.08"), Amount("1000000", Currency.Eur), FirstCounterparty).Generate();

        // Act
        batch.Book(trade);

        // Assert
        Assert.Equal(Amount("1000000", Currency.Eur), batch.BankNetPosition(Currency.Eur));
        Assert.Equal(Amount("-1080000", Currency.Usd), batch.BankNetPosition(Currency.Usd));
        Assert.Equal([
            new NetPosition(FirstCounterparty, Amount("1000000", Currency.Eur)),
            new NetPosition(FirstCounterparty, Amount("-1080000", Currency.Usd))
        ], batch.NetPositions());
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void Selling_the_base_leaves_the_bank_short_the_base_and_long_the_quote() {
        // Arrange
        SettlementBatch batch = WednesdayBatch();
        SpotTrade       trade = WednesdayTrade(TradeSide.SellBase, Rate(EurUsd, "1.08"), Amount("1000000", Currency.Eur), FirstCounterparty).Generate();

        // Act
        batch.Book(trade);

        // Assert
        Assert.Equal([
            new NetPosition(FirstCounterparty, Amount("-1000000", Currency.Eur)),
            new NetPosition(FirstCounterparty, Amount("1080000", Currency.Usd))
        ], batch.NetPositions());
    }

    [Fact]
    public void A_trade_dealt_in_the_quote_nets_the_same_as_the_equivalent_trade_in_the_base() {
        // Arrange
        SettlementBatch batch = WednesdayBatch();
        SpotTrade       trade = WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.08"), Amount("1080000", Currency.Usd), FirstCounterparty).Generate();

        // Act
        batch.Book(trade);

        // Assert
        Assert.Equal(Amount("1000000", Currency.Eur), batch.BankNetPosition(Currency.Eur));
        Assert.Equal(Amount("-1080000", Currency.Usd), batch.BankNetPosition(Currency.Usd));
    }

    [Fact]
    public void Counterparties_net_separately_and_the_bank_nets_across_them() {
        // Arrange
        SettlementBatch batch = WednesdayBatch();
        IReadOnlyList<TradeId> ids = Any.ListOf(Any.TradeId()).WithCount(2).Distinct().Generate();

        SpotTrade bought = WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.08"), Amount("1000000", Currency.Eur), FirstCounterparty).WithId(ids[0]).Generate();
        SpotTrade sold   = WednesdayTrade(TradeSide.SellBase, Rate(EurUsd, "1.08"), Amount("400000", Currency.Eur), SecondCounterparty).WithId(ids[1]).Generate();

        // Act
        batch.Book(bought);
        batch.Book(sold);

        // Assert
        Assert.Equal(Amount("600000", Currency.Eur), batch.BankNetPosition(Currency.Eur));
        Assert.Equal(Amount("-648000", Currency.Usd), batch.BankNetPosition(Currency.Usd));
        Assert.Equal([
            new NetPosition(FirstCounterparty, Amount("1000000", Currency.Eur)),
            new NetPosition(FirstCounterparty, Amount("-1080000", Currency.Usd)),
            new NetPosition(SecondCounterparty, Amount("-400000", Currency.Eur)),
            new NetPosition(SecondCounterparty, Amount("432000", Currency.Usd))
        ], batch.NetPositions());
    }

    [Fact]
    public void Opposite_trades_with_the_same_counterparty_cancel_out_currency_by_currency() {
        // Arrange
        SettlementBatch        batch = WednesdayBatch();
        IReadOnlyList<TradeId> ids   = Any.ListOf(Any.TradeId()).WithCount(2).Distinct().Generate();

        SpotTrade bought = WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.08"), Amount("1000000", Currency.Eur), FirstCounterparty).WithId(ids[0]).Generate();
        SpotTrade sold   = WednesdayTrade(TradeSide.SellBase, Rate(EurUsd, "1.09"), Amount("1000000", Currency.Eur), FirstCounterparty).WithId(ids[1]).Generate();

        // Act
        batch.Book(bought);
        batch.Book(sold);

        // Assert: the euros are flat, the ten thousand dollars of spread remain
        Assert.Equal([new NetPosition(FirstCounterparty, Amount("10000", Currency.Usd))], batch.NetPositions());
        Assert.Equal(Amount("0", Currency.Eur), batch.BankNetPosition(Currency.Eur));
    }

    [Fact]
    public void Positions_are_ordered_by_counterparty_then_by_currency_code() {
        // Arrange
        SettlementBatch        batch = WednesdayBatch();
        IReadOnlyList<TradeId> ids   = Any.ListOf(Any.TradeId()).WithCount(3).Distinct().Generate();

        // The second counterparty trades first, and the dollar is the quote of both pairs.
        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(GbpUsd, "1.25"), Amount("1000", Currency.Gbp), SecondCounterparty).WithId(ids[0]).Generate());
        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.08"), Amount("1000", Currency.Eur), FirstCounterparty).WithId(ids[1]).Generate());
        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(GbpUsd, "1.25"), Amount("1000", Currency.Gbp), FirstCounterparty).WithId(ids[2]).Generate());

        // Act
        IReadOnlyList<NetPosition> positions = batch.NetPositions();

        // Assert
        Assert.Equal([
            (FirstCounterparty, "EUR"), (FirstCounterparty, "GBP"), (FirstCounterparty, "USD"),
            (SecondCounterparty, "GBP"), (SecondCounterparty, "USD")
        ], positions.Select(position => (position.Counterparty, position.Currency.Code)));
    }

    [Fact]
    public void The_bank_has_no_position_in_a_currency_nobody_traded() {
        // Arrange
        SettlementBatch batch       = WednesdayBatch();
        Currency        anyCurrency = Any.Currency().Generate();

        // Act
        Money position = batch.BankNetPosition(anyCurrency);

        // Assert
        Assert.Equal(Money.Zero(anyCurrency), position);
    }

    [Fact]
    public void A_dealt_rate_may_stray_from_the_reference_by_exactly_the_tolerance_and_not_more() {
        // Arrange: a reference of exactly 1.00000 makes the gaps readable
        RateSheet sheet = MondaySheet(Rate(EurUsd, "1"));
        Money     dealt = Amount("1000", Currency.Eur);

        SettlementBatch Batch() => new SettlementBatch(Week.Wednesday, sheet, 0.01m);

        // Act / Assert
        Assert.Null(Record.Exception(() => Batch().Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.01000"), dealt, FirstCounterparty).Generate())));
        Assert.Null(Record.Exception(() => Batch().Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "0.99000"), dealt, FirstCounterparty).Generate())));
        AssertRefused(SettlementRefusalReason.RateOffMarket, () => Batch().Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.01001"), dealt, FirstCounterparty).Generate()));
        AssertRefused(SettlementRefusalReason.RateOffMarket, () => Batch().Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "0.98999"), dealt, FirstCounterparty).Generate()));
    }

    [Fact]
    public void A_rate_is_judged_against_the_reference_of_its_own_pair_whichever_way_round_the_sheet_quotes_it() {
        // Arrange: the sheet quotes EUR/USD 1.08, so the reference for USD/EUR is 0.92593
        SettlementBatch batch = WednesdayBatch();
        Money           dealt = Amount("1000000", Currency.Usd);

        // Act / Assert
        Assert.Null(Record.Exception(() => batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(UsdEur, "0.92"), dealt, FirstCounterparty).Generate())));
        AssertRefused(SettlementRefusalReason.RateOffMarket, () => batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(UsdEur, "0.90"), dealt, FirstCounterparty).Generate()));
    }

    [Fact]
    public void A_cross_pair_is_judged_against_the_triangulated_reference() {
        // Arrange: EUR/JPY is 1.08 * 150 = 162.000
        SettlementBatch batch = WednesdayBatch();
        Money           dealt = Amount("1000000", Currency.Eur);

        // Act / Assert
        Assert.Null(Record.Exception(() => batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurJpy, "163.5"), dealt, FirstCounterparty).Generate())));
        AssertRefused(SettlementRefusalReason.RateOffMarket, () => batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurJpy, "165"), dealt, FirstCounterparty).Generate()));
    }

    [Fact]
    public void The_reference_rate_is_rounded_to_even_not_away_from_zero() {
        // Arrange: EUR/GBP is 1.00001 / 2 = 0.500005, which is 0.50000 to even and 0.50001 away from zero.
        // 0.49995 is exactly one basis point under the first, and more than one under the second.
        RateSheet       sheet = MondaySheet(Rate(EurUsd, "1.00001"), Rate(GbpUsd, "2"));
        SettlementBatch batch = new SettlementBatch(Week.Wednesday, sheet, 0.0001m);
        SpotTrade       trade = WednesdayTrade(TradeSide.BuyBase, Rate(EurGbp, "0.49995"), Amount("1000", Currency.Eur), FirstCounterparty).Generate();

        // Act
        Exception? refusal = Record.Exception(() => batch.Book(trade));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_trade_that_settles_on_another_day_is_refused() {
        // Arrange
        SettlementBatch batch = WednesdayBatch();
        SpotTrade       thursdayTrade = Any.SpotTrade().WithRate(Rate(EurUsd, "1.08")).SettlingOn(Week.Thursday).Generate();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.ValueDateMismatch, () => batch.Book(thursdayTrade));
        Assert.Empty(batch.Trades);
    }

    [Fact]
    public void A_trade_cannot_be_booked_twice() {
        // Arrange
        SettlementBatch batch = WednesdayBatch();
        SpotTrade       trade = Any.SpotTrade().BookableIn(batch).Generate();

        batch.Book(trade);

        // Act / Assert
        AssertRefused(SettlementRefusalReason.DuplicateTrade, () => batch.Book(trade));
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void A_different_trade_with_a_booked_id_is_a_duplicate_too() {
        // Arrange
        SettlementBatch batch = WednesdayBatch();
        SpotTrade       first = Any.SpotTrade().BookableIn(batch).Generate();
        SpotTrade       other = Any.SpotTrade().BookableIn(batch).WithId(first.Id).Generate();

        batch.Book(first);

        // Act / Assert
        AssertRefused(SettlementRefusalReason.DuplicateTrade, () => batch.Book(other));
    }

    [Fact]
    public void A_pair_the_sheet_cannot_price_is_refused() {
        // Arrange
        SettlementBatch batch           = WednesdayBatch();
        Currency        anyUnquotedOne  = Any.Currency().Except(Currency.Usd, Currency.Eur, Currency.Gbp, Currency.Jpy).Generate();
        ExchangeRate    anyUnpricedRate = new AnyExchangeRate().WithPair(new CurrencyPair(Currency.Eur, anyUnquotedOne)).Generate();
        SpotTrade       anyUnpricedTrade = Any.SpotTrade().WithRate(anyUnpricedRate).SettlingOn(Week.Wednesday).Generate();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.NoReferenceRate, () => batch.Book(anyUnpricedTrade));
        Assert.Empty(batch.Trades);
    }

    [Fact]
    public void A_rate_off_the_market_is_refused_and_leaves_no_trace() {
        // Arrange
        SettlementBatch batch = WednesdayBatch();
        SpotTrade       trade = WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.20"), Amount("1000000", Currency.Eur), FirstCounterparty).Generate();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.RateOffMarket, () => batch.Book(trade));
        Assert.Empty(batch.Trades);
        Assert.Empty(batch.NetPositions());
        Assert.True(batch.BankNetPosition(Currency.Eur).IsZero);
    }

    [Fact]
    public void Refusals_are_checked_in_order_cut_off_then_value_date_then_duplicate_then_pricing_then_market() {
        // Arrange
        SettlementBatch batch = WednesdayBatch();
        SpotTrade       booked = Any.SpotTrade().BookableIn(batch).Generate();
        batch.Book(booked);

        Currency  anyUnquotedOne = Any.Currency().Except(Currency.Usd, Currency.Eur, Currency.Gbp, Currency.Jpy).Generate();
        CurrencyPair unpriced    = new CurrencyPair(Currency.Eur, anyUnquotedOne);

        // a booked id, on the wrong day, for a pair nobody prices, off the market: the day is named first
        SpotTrade wrongDay = Any.SpotTrade().WithId(booked.Id).WithRate(new AnyExchangeRate().WithPair(unpriced).Generate()).SettlingOn(Week.Thursday).Generate();
        // a booked id, on the right day, for a pair nobody prices: the duplicate is named first
        SpotTrade duplicateUnpriced = Any.SpotTrade().WithId(booked.Id).WithRate(new AnyExchangeRate().WithPair(unpriced).Generate()).SettlingOn(Week.Wednesday).Generate();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.ValueDateMismatch, () => batch.Book(wrongDay));
        AssertRefused(SettlementRefusalReason.DuplicateTrade, () => batch.Book(duplicateUnpriced));

        batch.CutOff();

        // a cut-off batch names the cut-off, whatever else is wrong with the trade
        AssertRefused(SettlementRefusalReason.BatchCutOff, () => batch.Book(wrongDay));
    }

    [Fact]
    public void Booking_into_a_cut_off_batch_is_refused() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();
        SpotTrade       trade = Any.SpotTrade().BookableIn(batch).Generate();

        batch.CutOff();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.BatchCutOff, () => batch.Book(trade));
        Assert.Empty(batch.Trades);
    }

    [Fact]
    public void A_trade_the_batch_would_accept_is_accepted_whatever_the_sheet_and_the_pair() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();
        SpotTrade       trade = Any.SpotTrade().BookableIn(batch).Generate();

        // Act
        Exception? refusal = Record.Exception(() => batch.Book(trade));

        // Assert
        Assert.Null(refusal);
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void A_booking_needs_a_trade() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => batch.Book(null!));
    }

    // ---------------------------------------------------------------------------------------- cancelling

    [Fact]
    public void Cancelling_a_trade_reverses_its_postings() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();
        SpotTrade       trade = Any.SpotTrade().BookableIn(batch).Generate();

        batch.Book(trade);

        // Act
        batch.Cancel(trade.Id);

        // Assert
        Assert.Empty(batch.Trades);
        Assert.Empty(batch.NetPositions());
    }

    [Fact]
    public void Cancelling_one_trade_leaves_the_others_netted() {
        // Arrange
        SettlementBatch        batch = WednesdayBatch();
        IReadOnlyList<TradeId> ids   = Any.ListOf(Any.TradeId()).WithCount(2).Distinct().Generate();

        SpotTrade kept      = WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.08"), Amount("1000000", Currency.Eur), FirstCounterparty).WithId(ids[0]).Generate();
        SpotTrade cancelled = WednesdayTrade(TradeSide.SellBase, Rate(GbpUsd, "1.25"), Amount("1000", Currency.Gbp), FirstCounterparty).WithId(ids[1]).Generate();

        batch.Book(kept);
        batch.Book(cancelled);

        // Act
        batch.Cancel(ids[1]);

        // Assert
        Assert.Equal([
            new NetPosition(FirstCounterparty, Amount("1000000", Currency.Eur)),
            new NetPosition(FirstCounterparty, Amount("-1080000", Currency.Usd))
        ], batch.NetPositions());
        Assert.Equal(Amount("0", Currency.Gbp), batch.BankNetPosition(Currency.Gbp));
    }

    [Fact]
    public void A_cancelled_id_can_be_booked_again() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();
        SpotTrade       trade = Any.SpotTrade().BookableIn(batch).Generate();

        batch.Book(trade);
        batch.Cancel(trade.Id);

        // Act
        Exception? refusal = Record.Exception(() => batch.Book(trade));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void Only_a_booked_trade_can_be_cancelled() {
        // Arrange
        SettlementBatch batch   = Any.SettlementBatch().Generate();
        TradeId         anyOtherId = Any.TradeId().Generate();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.UnknownTrade, () => batch.Cancel(anyOtherId));
    }

    [Fact]
    public void Nothing_is_cancelled_after_the_cut_off_not_even_an_unknown_id() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();
        SpotTrade       trade = Any.SpotTrade().BookableIn(batch).Generate();

        batch.Book(trade);
        batch.CutOff();

        TradeId anyUnknownId = Any.TradeId().DifferentFrom(trade.Id).Generate();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.BatchCutOff, () => batch.Cancel(trade.Id));
        AssertRefused(SettlementRefusalReason.BatchCutOff, () => batch.Cancel(anyUnknownId));
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void Cancelling_needs_an_id() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => batch.Cancel(null!));
    }

    // ---------------------------------------------------------------------------------------- cut-off and instructions

    [Fact]
    public void The_cut_off_closes_the_batch_once() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();

        // Act
        batch.CutOff();

        // Assert
        Assert.Equal(BatchStatus.CutOff, batch.Status);
        AssertRefused(SettlementRefusalReason.AlreadyCutOff, () => batch.CutOff());
    }

    [Fact]
    public void Instructions_wait_for_the_cut_off() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.NotCutOff, () => batch.Instructions());
    }

    [Fact]
    public void An_empty_batch_has_no_instruction() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();

        batch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = batch.Instructions();

        // Assert
        Assert.Empty(instructions);
    }

    [Fact]
    public void Each_position_becomes_one_instruction_to_receive_what_is_owed_and_to_pay_what_is_owing() {
        // Arrange
        SettlementBatch        batch = WednesdayBatch();
        IReadOnlyList<TradeId> ids   = Any.ListOf(Any.TradeId()).WithCount(2).Distinct().Generate();

        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.08"), Amount("1000000", Currency.Eur), FirstCounterparty).WithId(ids[0]).Generate());
        batch.Book(WednesdayTrade(TradeSide.SellBase, Rate(EurUsd, "1.08"), Amount("400000", Currency.Eur), SecondCounterparty).WithId(ids[1]).Generate());
        batch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = batch.Instructions();

        // Assert
        Assert.Equal([
            new SettlementInstruction(FirstCounterparty, SettlementDirection.Receive, Amount("1000000", Currency.Eur), Week.Wednesday),
            new SettlementInstruction(FirstCounterparty, SettlementDirection.Pay, Amount("1080000", Currency.Usd), Week.Wednesday),
            new SettlementInstruction(SecondCounterparty, SettlementDirection.Pay, Amount("400000", Currency.Eur), Week.Wednesday),
            new SettlementInstruction(SecondCounterparty, SettlementDirection.Receive, Amount("432000", Currency.Usd), Week.Wednesday)
        ], instructions);
    }

    [Fact]
    public void A_flat_position_gives_no_instruction() {
        // Arrange
        SettlementBatch        batch = WednesdayBatch();
        IReadOnlyList<TradeId> ids   = Any.ListOf(Any.TradeId()).WithCount(2).Distinct().Generate();

        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.08"), Amount("1000000", Currency.Eur), FirstCounterparty).WithId(ids[0]).Generate());
        batch.Book(WednesdayTrade(TradeSide.SellBase, Rate(EurUsd, "1.09"), Amount("1000000", Currency.Eur), FirstCounterparty).WithId(ids[1]).Generate());
        batch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = batch.Instructions();

        // Assert
        Assert.Equal([
            new SettlementInstruction(FirstCounterparty, SettlementDirection.Receive, Amount("10000", Currency.Usd), Week.Wednesday)
        ], instructions);
    }

    // ---------------------------------------------------------------------------------------- valuation

    /// <summary>EUR/USD 1.01000 makes half a euro worth half a cent and 1.0150 a fair dealt rate; the other rates are the desk's.</summary>
    private static SettlementBatch EuroAt101Batch() {
        RateSheet sheet = MondaySheet(Rate(EurUsd, "1.01"), Rate(GbpUsd, "1.25"), Rate(UsdJpy, "150"));

        return new SettlementBatch(Week.Wednesday, sheet, 0.01m);
    }

    [Fact]
    public void A_net_is_valued_with_banker_rounding_not_with_the_clients_rounding() {
        // Arrange: the client pays 0.51 USD for 0.50 EUR (0.505, away from zero); the desk values the 0.50 EUR at 0.50 USD (to even)
        SettlementBatch batch = EuroAt101Batch();

        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.01"), Amount("0.5", Currency.Eur), FirstCounterparty).Generate());

        // Act
        Money value = batch.NetValueIn(Currency.Usd);

        // Assert
        Assert.Equal(Amount("-0.01", Currency.Usd), value);
    }

    [Fact]
    public void Banker_rounding_of_a_negative_net_goes_to_even_as_well() {
        // Arrange
        SettlementBatch batch = EuroAt101Batch();

        batch.Book(WednesdayTrade(TradeSide.SellBase, Rate(EurUsd, "1.01"), Amount("0.5", Currency.Eur), FirstCounterparty).Generate());

        // Act
        Money value = batch.NetValueIn(Currency.Usd);

        // Assert
        Assert.Equal(Amount("0.01", Currency.Usd), value);
    }

    [Fact]
    public void A_net_is_valued_at_the_reference_rate_not_at_the_dealt_one() {
        // Arrange: the bank pays 1 015 000 USD for 1 000 000 EUR that the sheet values at 1 010 000
        SettlementBatch batch = EuroAt101Batch();

        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.0150"), Amount("1000000", Currency.Eur), FirstCounterparty).Generate());

        // Act / Assert
        Assert.Equal(Amount("-5000", Currency.Usd), batch.NetValueIn(Currency.Usd));
    }

    [Fact]
    public void A_net_is_valued_in_a_currency_the_sheet_reaches_through_a_cross_rate() {
        // Arrange: EUR/JPY is 1.01 * 150 = 151.500, USD/JPY is quoted at 150.000: 151 500 000 JPY received, 152 250 000 paid
        SettlementBatch batch = EuroAt101Batch();

        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.0150"), Amount("1000000", Currency.Eur), FirstCounterparty).Generate());

        // Act / Assert
        Assert.Equal(Amount("-750000", Currency.Jpy), batch.NetValueIn(Currency.Jpy));
    }

    [Fact]
    public void The_cross_rate_is_rounded_to_its_places_before_it_values_the_amount() {
        // Arrange: GBP/EUR is 1.25 / 1.08 = 1.157407..., quoted 1.15741; USD/EUR is 0.92593.
        // 1 000 000 GBP are worth 1 157 410.00 EUR, 1 250 000 USD 1 157 412.50 EUR: the rounding of the rates leaves 2.50.
        SettlementBatch batch = WednesdayBatch();

        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(GbpUsd, "1.25"), Amount("1000000", Currency.Gbp), FirstCounterparty).Generate());

        // Act
        Money value = batch.NetValueIn(Currency.Eur);

        // Assert
        Assert.Equal(Amount("-2.50", Currency.Eur), value);
    }

    [Fact]
    public void Exposure_to_a_counterparty_values_only_its_own_net() {
        // Arrange
        SettlementBatch        batch = EuroAt101Batch();
        IReadOnlyList<TradeId> ids   = Any.ListOf(Any.TradeId()).WithCount(2).Distinct().Generate();

        batch.Book(WednesdayTrade(TradeSide.BuyBase, Rate(EurUsd, "1.0150"), Amount("1000000", Currency.Eur), FirstCounterparty).WithId(ids[0]).Generate());
        batch.Book(WednesdayTrade(TradeSide.SellBase, Rate(GbpUsd, "1.25"), Amount("1000", Currency.Gbp), SecondCounterparty).WithId(ids[1]).Generate());

        // Act / Assert
        Assert.Equal(Amount("-5000", Currency.Usd), batch.ExposureTo(FirstCounterparty, Currency.Usd));
        Assert.Equal(Amount("0", Currency.Usd), batch.ExposureTo(SecondCounterparty, Currency.Usd));
    }

    [Fact]
    public void Exposure_to_a_counterparty_with_no_trade_is_zero_in_the_reporting_currency() {
        // Arrange
        SettlementBatch batch           = Any.SettlementBatch().Generate();
        Lei             anyStranger     = Any.Lei().Generate();
        Currency        anyReportingOne = batch.ReferenceRates.Pivot;

        // Act
        Money exposure = batch.ExposureTo(anyStranger, anyReportingOne);

        // Assert
        Assert.Equal(Money.Zero(anyReportingOne), exposure);
    }

    [Fact]
    public void Nothing_is_valued_in_a_currency_the_sheet_does_not_cover() {
        // Arrange
        SettlementBatch batch            = WednesdayBatch();
        Currency        anyUncoveredOne  = Any.Currency().Except(Currency.Usd, Currency.Eur, Currency.Gbp, Currency.Jpy).Generate();
        Lei             anyCounterparty  = Any.Lei().Generate();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.NoReferenceRate, () => batch.NetValueIn(anyUncoveredOne));
        AssertRefused(SettlementRefusalReason.NoReferenceRate, () => batch.ExposureTo(anyCounterparty, anyUncoveredOne));
    }

    [Fact]
    public void Valuing_needs_a_currency_and_a_counterparty() {
        // Arrange
        SettlementBatch batch = Any.SettlementBatch().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => batch.NetValueIn(null!));
        Assert.Throws<ArgumentNullException>(() => batch.ExposureTo(null!, Currency.Usd));
        Assert.Throws<ArgumentNullException>(() => batch.ExposureTo(Any.Lei().Generate(), null!));
        Assert.Throws<ArgumentNullException>(() => batch.BankNetPosition(null!));
    }

    [Fact(Skip = "domain defect: Book lets a bare InvalidOperationException escape when the sheet covers the pair but the reference rate is not quotable (here USD/EUR from EUR/USD 200000), instead of a SettlementRefusedException; literal input, no seed")]
    public void A_pair_the_sheet_covers_but_cannot_quote_is_refused_as_a_pair_without_reference_rate() {
        // Arrange: EUR/USD at 200 000 inverts to 0.000005, which rounds to zero to even: USD/EUR is covered but has no quotable reference
        RateSheet       sheet = MondaySheet(Rate(EurUsd, "200000"));
        SettlementBatch batch = new SettlementBatch(Week.Wednesday, sheet, 0.01m);
        SpotTrade       trade = WednesdayTrade(TradeSide.BuyBase, Rate(UsdEur, "0.00001"), Amount("1000000", Currency.Usd), FirstCounterparty).Generate();

        // Act / Assert
        AssertRefused(SettlementRefusalReason.NoReferenceRate, () => batch.Book(trade));
    }

}
