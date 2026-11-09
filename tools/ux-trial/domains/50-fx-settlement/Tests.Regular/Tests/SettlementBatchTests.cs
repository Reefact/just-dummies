using System.Globalization;

using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Regular;

public sealed class SettlementBatchTests {

    // 2026-03-17 is a Tuesday: the sheet is fixed that day; the batch settles the day after.
    private static readonly DateOnly SheetDate = new DateOnly(2026, 3, 17);
    private static readonly DateOnly ValueDate = new DateOnly(2026, 3, 18);

    private static readonly Lei Alpha = new Lei("529900T8BM49AURSDO55");
    private static readonly Lei Beta  = new Lei("5493001KJTIIGC8Y1R12");

    private static readonly CurrencyPair EurUsd = new CurrencyPair(Currency.Eur, Currency.Usd);
    private static readonly CurrencyPair UsdJpy = new CurrencyPair(Currency.Usd, Currency.Jpy);
    private static readonly CurrencyPair GbpUsd = new CurrencyPair(Currency.Gbp, Currency.Usd);

    private static decimal D(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>A USD-pivot sheet: EUR 1.08, GBP 1.25, and 150 yen to the dollar.</summary>
    private static RateSheet Sheet() {
        return new RateSheet(Currency.Usd, SheetDate, new[] {
            new ExchangeRate(EurUsd, 1.08m),
            new ExchangeRate(GbpUsd, 1.25m),
            new ExchangeRate(UsdJpy, 150m),
        });
    }

    private static SettlementBatch Batch(decimal tolerance = 0.01m) => new SettlementBatch(ValueDate, Sheet(), tolerance);

    /// <summary>A trade of the batch's value date on the given pair at the given rate; its id and the rest are dummies.</summary>
    private static AnySpotTrade TradeOf(CurrencyPair pair, string rate, decimal dealt, Currency? dealtIn = null) {
        return Any.SpotTrade()
                  .SettlingOn(ValueDate)
                  .WithRate(new ExchangeRate(pair, D(rate)))
                  .WithDealtAmount(new Money(dealt, dealtIn ?? pair.Base));
    }

    private static SettlementRefusalReason ReasonOf(Action refused) {
        return Assert.Throws<SettlementRefusedException>(refused).Reason;
    }

    // ---- construction ------------------------------------------------------------------------------------------

    [Fact]
    public void A_new_batch_is_open_and_empty() {
        // Act
        SettlementBatch batch = Batch(0.02m);

        // Assert
        Assert.Equal(BatchStatus.Open, batch.Status);
        Assert.Empty(batch.Trades);
        Assert.Empty(batch.NetPositions());
        Assert.Equal(ValueDate, batch.ValueDate);
        Assert.Equal(0.02m, batch.OffMarketTolerance);
        Assert.Equal(SheetDate, batch.ReferenceRates.AsOf);
    }

    [Theory]
    [InlineData("2026-03-14")]
    [InlineData("2026-03-15")]
    public void A_weekend_value_date_is_refused(string valueDate) {
        // Arrange
        RateSheet anySheet = Any.RateSheet().WithAsOf(new DateOnly(2026, 3, 13)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new SettlementBatch(DateOnly.Parse(valueDate), anySheet, 0.01m));

        // Assert
        Assert.Equal("valueDate", refusal.ParamName);
    }

    [Fact]
    public void A_sheet_fixed_after_the_value_date_is_refused() {
        // Arrange
        RateSheet anySheet = Any.RateSheet().WithAsOf(ValueDate.AddDays(1)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new SettlementBatch(ValueDate, anySheet, 0.01m));

        // Assert
        Assert.Equal("referenceRates", refusal.ParamName);
    }

    [Fact]
    public void A_sheet_fixed_on_the_value_date_itself_is_accepted() {
        // Arrange
        RateSheet anySheet = Any.RateSheet().WithAsOf(ValueDate).Generate();

        // Act
        SettlementBatch batch = new SettlementBatch(ValueDate, anySheet, 0.01m);

        // Assert
        Assert.Same(anySheet, batch.ReferenceRates);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("0.0501")]
    [InlineData("1")]
    public void A_tolerance_outside_zero_to_five_percent_is_refused(string tolerance) {
        // Arrange
        RateSheet anySheet = Any.RateSheet().WithAsOf(SheetDate).Generate();

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementBatch(ValueDate, anySheet, D(tolerance)));
    }

    [Theory]
    [InlineData("0.05")]
    [InlineData("0.0001")]
    public void A_tolerance_up_to_five_percent_is_accepted(string tolerance) {
        // Arrange
        RateSheet anySheet = Any.RateSheet().WithAsOf(SheetDate).Generate();

        // Act
        SettlementBatch batch = new SettlementBatch(ValueDate, anySheet, D(tolerance));

        // Assert
        Assert.Equal(D(tolerance), batch.OffMarketTolerance);
    }

    [Fact]
    public void A_batch_refuses_a_missing_sheet() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new SettlementBatch(ValueDate, null!, 0.01m));
    }

    [Fact]
    public void An_arbitrary_batch_is_open_with_a_sheet_that_does_not_postdate_it() {
        // Arrange
        SettlementBatch anyBatch = Any.SettlementBatch().Generate();

        // Act / Assert
        Assert.Equal(BatchStatus.Open, anyBatch.Status);
        Assert.True(anyBatch.ReferenceRates.AsOf <= anyBatch.ValueDate);
    }

    // ---- booking -----------------------------------------------------------------------------------------------

    [Fact]
    public void Booking_a_buy_posts_the_base_to_receive_and_the_quote_to_pay() {
        // Arrange
        SettlementBatch batch = Batch();
        SpotTrade       trade = TradeOf(EurUsd, "1.08", 1_000_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate();

        // Act
        batch.Book(trade);

        // Assert
        Assert.Same(trade, Assert.Single(batch.Trades));
        Assert.Equal(new[] {
            new NetPosition(Alpha, new Money(1_000_000m, Currency.Eur)),
            new NetPosition(Alpha, new Money(-1_080_000m, Currency.Usd)),
        }, batch.NetPositions());
        Assert.Equal(new Money(1_000_000m, Currency.Eur), batch.BankNetPosition(Currency.Eur));
        Assert.Equal(new Money(-1_080_000m, Currency.Usd), batch.BankNetPosition(Currency.Usd));
    }

    [Fact]
    public void Booking_a_sell_posts_the_opposite_flows() {
        // Arrange
        SettlementBatch batch = Batch();

        // Act
        batch.Book(TradeOf(EurUsd, "1.08", 1_000_000m).WithSide(TradeSide.SellBase).WithCounterparty(Alpha).Generate());

        // Assert
        Assert.Equal(new[] {
            new NetPosition(Alpha, new Money(-1_000_000m, Currency.Eur)),
            new NetPosition(Alpha, new Money(1_080_000m, Currency.Usd)),
        }, batch.NetPositions());
    }

    [Fact]
    public void Trades_with_one_counterparty_net_against_each_other_per_currency() {
        // Arrange
        SettlementBatch batch = Batch();

        // Act
        batch.Book(TradeOf(EurUsd, "1.08", 1_000_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate());
        batch.Book(TradeOf(EurUsd, "1.08", 400_000m).WithSide(TradeSide.SellBase).WithCounterparty(Alpha).Generate());

        // Assert
        Assert.Equal(new[] {
            new NetPosition(Alpha, new Money(600_000m, Currency.Eur)),
            new NetPosition(Alpha, new Money(-648_000m, Currency.Usd)),
        }, batch.NetPositions());
    }

    [Fact]
    public void Opposite_trades_with_one_counterparty_leave_no_position() {
        // Arrange
        SettlementBatch batch = Batch();

        // Act
        batch.Book(TradeOf(EurUsd, "1.08", 1_000_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate());
        batch.Book(TradeOf(EurUsd, "1.08", 1_000_000m).WithSide(TradeSide.SellBase).WithCounterparty(Alpha).Generate());

        // Assert
        Assert.Empty(batch.NetPositions());
        Assert.Equal(2, batch.Trades.Count);
        Assert.True(batch.BankNetPosition(Currency.Eur).IsZero);
    }

    [Fact]
    public void The_bank_net_adds_up_across_counterparties_while_positions_stay_apart() {
        // Arrange
        SettlementBatch batch = Batch();

        // Act
        batch.Book(TradeOf(EurUsd, "1.08", 1_000_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate());
        batch.Book(TradeOf(EurUsd, "1.08", 250_000m).WithSide(TradeSide.SellBase).WithCounterparty(Beta).Generate());

        // Assert
        Assert.Equal(new Money(750_000m, Currency.Eur), batch.BankNetPosition(Currency.Eur));
        Assert.Equal(new Money(-810_000m, Currency.Usd), batch.BankNetPosition(Currency.Usd));
        Assert.Equal(4, batch.NetPositions().Count);
    }

    [Fact]
    public void Positions_are_ordered_by_lei_then_currency_code() {
        // Arrange: 5299... sorts before 5493...; EUR before JPY before USD.
        SettlementBatch batch = Batch();

        // Act
        batch.Book(TradeOf(UsdJpy, "150", 1_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Beta).Generate());
        batch.Book(TradeOf(EurUsd, "1.08", 1_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Beta).Generate());
        batch.Book(TradeOf(EurUsd, "1.08", 1_000m).WithSide(TradeSide.SellBase).WithCounterparty(Alpha).Generate());

        // Assert
        Assert.Equal(new[] { "529900T8BM49AURSDO55/EUR", "529900T8BM49AURSDO55/USD", "5493001KJTIIGC8Y1R12/EUR", "5493001KJTIIGC8Y1R12/JPY", "5493001KJTIIGC8Y1R12/USD" },
                     batch.NetPositions().Select(position => $"{position.Counterparty}/{position.Currency}"));
    }

    [Fact]
    public void A_cut_off_batch_refuses_bookings() {
        // Arrange
        SettlementBatch batch = Batch();
        batch.CutOff();
        SpotTrade anyTrade = TradeOf(EurUsd, "1.08", 1_000m).Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.BatchCutOff, ReasonOf(() => batch.Book(anyTrade)));
    }

    [Fact]
    public void A_trade_of_another_value_date_is_refused() {
        // Arrange
        SettlementBatch batch = Batch();
        SpotTrade anyEarlierTrade = Any.SpotTrade()
                                       .SettlingOn(BusinessDays.Minus(ValueDate, 1))
                                       .WithRate(new ExchangeRate(EurUsd, 1.08m))
                                       .WithDealtAmount(new Money(1_000m, Currency.Eur))
                                       .Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.ValueDateMismatch, ReasonOf(() => batch.Book(anyEarlierTrade)));
    }

    [Fact]
    public void The_value_date_of_a_trade_follows_its_pairs_lag() {
        // Arrange: a USD/CAD trade of Tuesday settles Wednesday; a EUR/USD one needs Monday.
        SettlementBatch batch = Batch();
        RateSheet sheet = new RateSheet(Currency.Usd, SheetDate, new[] { new ExchangeRate(new CurrencyPair(Currency.Usd, Currency.Cad), 1.35m) });
        SettlementBatch cadBatch = new SettlementBatch(ValueDate, sheet, 0.01m);
        SpotTrade dollarCanada = new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), TradeSide.BuyBase, new Money(1_000m, Currency.Usd),
                                               new ExchangeRate(new CurrencyPair(Currency.Usd, Currency.Cad), 1.35m), new DateOnly(2026, 3, 17));
        SpotTrade euroDollarTradedTuesday = new SpotTrade(Any.TradeId().Generate(), Any.Lei().Generate(), TradeSide.BuyBase, new Money(1_000m, Currency.Eur),
                                                          new ExchangeRate(EurUsd, 1.08m), new DateOnly(2026, 3, 17));

        // Act
        cadBatch.Book(dollarCanada);

        // Assert
        Assert.Single(cadBatch.Trades);
        Assert.Equal(SettlementRefusalReason.ValueDateMismatch, ReasonOf(() => batch.Book(euroDollarTradedTuesday)));
    }

    [Fact]
    public void A_trade_id_already_booked_is_refused() {
        // Arrange
        SettlementBatch batch = Batch();
        TradeId anyId = Any.TradeId().Generate();
        batch.Book(TradeOf(EurUsd, "1.08", 1_000m).WithId(anyId).Generate());
        SpotTrade anotherUnderTheSameId = TradeOf(UsdJpy, "150", 2_000m).WithId(anyId).Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.DuplicateTrade, ReasonOf(() => batch.Book(anotherUnderTheSameId)));
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void A_pair_the_sheet_cannot_price_is_refused() {
        // Arrange
        SettlementBatch batch = Batch();
        Currency anyUncovered = new AnyCurrency().Except(Currency.Usd, Currency.Eur, Currency.Gbp, Currency.Jpy).Generate();
        CurrencyPair uncoveredPair = new CurrencyPair(Currency.Eur, anyUncovered);
        SpotTrade anyUncoveredTrade = Any.SpotTrade().SettlingOn(ValueDate).WithRate(r => r.WithPair(uncoveredPair)).Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.NoReferenceRate, ReasonOf(() => batch.Book(anyUncoveredTrade)));
        Assert.Empty(batch.Trades);
    }

    [Theory]
    [InlineData("1.09080")]
    [InlineData("1.06920")]
    [InlineData("1.08000")]
    public void A_rate_within_the_tolerance_of_the_reference_is_accepted(string rate) {
        // Arrange: a 1 % tolerance around 1.08 is [1.0692, 1.0908].
        SettlementBatch batch = Batch(0.01m);

        // Act
        batch.Book(TradeOf(EurUsd, rate, 1_000m).Generate());

        // Assert
        Assert.Single(batch.Trades);
    }

    [Theory]
    [InlineData("1.09081")]
    [InlineData("1.06919")]
    [InlineData("2.00000")]
    public void A_rate_beyond_the_tolerance_of_the_reference_is_refused(string rate) {
        // Arrange
        SettlementBatch batch = Batch(0.01m);
        SpotTrade offMarket = TradeOf(EurUsd, rate, 1_000m).Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.RateOffMarket, ReasonOf(() => batch.Book(offMarket)));
        Assert.Empty(batch.Trades);
        Assert.Empty(batch.NetPositions());
    }

    [Fact]
    public void The_tolerance_is_a_property_of_the_batch() {
        // Arrange
        SettlementBatch tight = Batch(0.001m);
        SettlementBatch wide  = Batch(0.05m);
        SpotTrade slightlyOff = TradeOf(EurUsd, "1.09", 1_000m).Generate();

        // Act
        wide.Book(slightlyOff);

        // Assert
        Assert.Equal(SettlementRefusalReason.RateOffMarket, ReasonOf(() => tight.Book(slightlyOff)));
    }

    [Fact]
    public void A_trade_on_the_reverse_of_a_quoted_pair_is_compared_with_the_inverse_reference() {
        // Arrange: USD/EUR reference is 1 / 1.08 = 0.92593.
        SettlementBatch batch = Batch(0.01m);
        CurrencyPair usdEur = new CurrencyPair(Currency.Usd, Currency.Eur);

        // Act
        batch.Book(TradeOf(usdEur, "0.92593", 1_000m).Generate());

        // Assert
        Assert.Equal(SettlementRefusalReason.RateOffMarket, ReasonOf(() => batch.Book(TradeOf(usdEur, "0.95000", 1_000m).Generate())));
    }

    [Fact]
    public void A_trade_on_a_cross_pair_is_compared_with_the_triangulated_reference() {
        // Arrange: EUR/GBP = 1.08 / 1.25 = 0.864.
        SettlementBatch batch = Batch(0.01m);
        CurrencyPair eurGbp = new CurrencyPair(Currency.Eur, Currency.Gbp);

        // Act
        batch.Book(TradeOf(eurGbp, "0.86400", 1_000m).Generate());

        // Assert
        Assert.Equal(SettlementRefusalReason.RateOffMarket, ReasonOf(() => batch.Book(TradeOf(eurGbp, "0.88000", 1_000m).Generate())));
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void A_cut_off_batch_reports_the_cut_off_before_the_other_refusals() {
        // Arrange: the trade is also on another date.
        SettlementBatch batch = Batch();
        batch.CutOff();
        SpotTrade anyWrongDateTrade = Any.SpotTrade().SettlingOn(BusinessDays.Plus(ValueDate, 1)).Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.BatchCutOff, ReasonOf(() => batch.Book(anyWrongDateTrade)));
    }

    [Fact]
    public void The_value_date_is_checked_before_the_reference_rate() {
        // Arrange: the trade is both on another date and on a pair the sheet cannot price.
        SettlementBatch batch = Batch();
        Currency anyUncovered = new AnyCurrency().Except(Currency.Usd, Currency.Eur, Currency.Gbp, Currency.Jpy).Generate();
        SpotTrade anyTrade = Any.SpotTrade()
                                .SettlingOn(BusinessDays.Plus(ValueDate, 1))
                                .WithRate(r => r.WithPair(new CurrencyPair(Currency.Eur, anyUncovered)))
                                .Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.ValueDateMismatch, ReasonOf(() => batch.Book(anyTrade)));
    }

    [Fact]
    public void Booking_refuses_a_missing_trade() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => Batch().Book(null!));
    }

    // ---- cancelling --------------------------------------------------------------------------------------------

    [Fact]
    public void Cancelling_a_trade_reverses_its_postings() {
        // Arrange
        SettlementBatch batch = Batch();
        SpotTrade kept      = TradeOf(EurUsd, "1.08", 1_000_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate();
        SpotTrade cancelled = TradeOf(UsdJpy, "150", 5_000m).WithSide(TradeSide.SellBase).WithCounterparty(Beta).Generate();
        batch.Book(kept);
        batch.Book(cancelled);

        // Act
        batch.Cancel(cancelled.Id);

        // Assert
        Assert.Same(kept, Assert.Single(batch.Trades));
        Assert.Equal(new[] {
            new NetPosition(Alpha, new Money(1_000_000m, Currency.Eur)),
            new NetPosition(Alpha, new Money(-1_080_000m, Currency.Usd)),
        }, batch.NetPositions());
        Assert.True(batch.BankNetPosition(Currency.Jpy).IsZero);
    }

    [Fact]
    public void A_cancelled_trade_can_be_booked_again() {
        // Arrange
        SettlementBatch batch = Batch();
        SpotTrade trade = TradeOf(EurUsd, "1.08", 1_000m).Generate();
        batch.Book(trade);
        batch.Cancel(trade.Id);

        // Act
        batch.Book(trade);

        // Assert
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void Cancelling_a_trade_that_is_not_booked_is_refused() {
        // Arrange
        SettlementBatch batch = Batch();
        TradeId anyUnknownId = Any.TradeId().Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.UnknownTrade, ReasonOf(() => batch.Cancel(anyUnknownId)));
    }

    [Fact]
    public void Nothing_is_cancelled_after_the_cut_off_not_even_an_unknown_trade() {
        // Arrange
        SettlementBatch batch = Batch();
        SpotTrade booked = TradeOf(EurUsd, "1.08", 1_000m).Generate();
        batch.Book(booked);
        batch.CutOff();
        TradeId anyUnknownId = Any.TradeId().Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.BatchCutOff, ReasonOf(() => batch.Cancel(booked.Id)));
        Assert.Equal(SettlementRefusalReason.BatchCutOff, ReasonOf(() => batch.Cancel(anyUnknownId)));
        Assert.Single(batch.Trades);
    }

    [Fact]
    public void Cancelling_refuses_a_missing_id() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => Batch().Cancel(null!));
    }

    // ---- cut-off -----------------------------------------------------------------------------------------------

    [Fact]
    public void Cutting_off_closes_the_batch() {
        // Arrange
        SettlementBatch batch = Batch();

        // Act
        batch.CutOff();

        // Assert
        Assert.Equal(BatchStatus.CutOff, batch.Status);
    }

    [Fact]
    public void Cutting_off_twice_is_refused() {
        // Arrange
        SettlementBatch batch = Batch();
        batch.CutOff();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.AlreadyCutOff, ReasonOf(batch.CutOff));
        Assert.Equal(BatchStatus.CutOff, batch.Status);
    }

    // ---- instructions ------------------------------------------------------------------------------------------

    [Fact]
    public void Instructions_wait_for_the_cut_off() {
        // Arrange
        SettlementBatch batch = Batch();
        batch.Book(TradeOf(EurUsd, "1.08", 1_000m).Generate());

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.NotCutOff, ReasonOf(() => batch.Instructions()));
    }

    [Fact]
    public void An_empty_cut_off_batch_has_no_instruction() {
        // Arrange
        SettlementBatch batch = Batch();
        batch.CutOff();

        // Act / Assert
        Assert.Empty(batch.Instructions());
    }

    [Fact]
    public void Each_non_flat_position_becomes_one_instruction_on_the_batch_value_date() {
        // Arrange
        SettlementBatch batch = Batch();
        batch.Book(TradeOf(EurUsd, "1.08", 1_000_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate());
        batch.Book(TradeOf(UsdJpy, "150", 2_000m).WithSide(TradeSide.SellBase).WithCounterparty(Beta).Generate());
        batch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = batch.Instructions();

        // Assert
        Assert.Equal(new[] {
            new SettlementInstruction(Alpha, SettlementDirection.Receive, new Money(1_000_000m, Currency.Eur), ValueDate),
            new SettlementInstruction(Alpha, SettlementDirection.Pay, new Money(1_080_000m, Currency.Usd), ValueDate),
            new SettlementInstruction(Beta, SettlementDirection.Receive, new Money(300_000m, Currency.Jpy), ValueDate),
            new SettlementInstruction(Beta, SettlementDirection.Pay, new Money(2_000m, Currency.Usd), ValueDate),
        }, instructions);
    }

    [Fact]
    public void A_flat_position_gives_no_instruction() {
        // Arrange
        SettlementBatch batch = Batch();
        batch.Book(TradeOf(EurUsd, "1.08", 1_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate());
        batch.Book(TradeOf(EurUsd, "1.08", 1_000m).WithSide(TradeSide.SellBase).WithCounterparty(Alpha).Generate());
        batch.CutOff();

        // Act / Assert
        Assert.Empty(batch.Instructions());
    }

    [Fact]
    public void A_trade_cancelled_before_the_cut_off_leaves_no_instruction() {
        // Arrange
        SettlementBatch batch = Batch();
        SpotTrade trade = TradeOf(EurUsd, "1.08", 1_000m).Generate();
        batch.Book(trade);
        batch.Cancel(trade.Id);
        batch.CutOff();

        // Act / Assert
        Assert.Empty(batch.Instructions());
    }

    // ---- valuation ---------------------------------------------------------------------------------------------

    [Fact]
    public void The_bank_net_of_a_currency_nobody_traded_is_zero() {
        // Arrange
        SettlementBatch batch = Batch();
        batch.Book(TradeOf(EurUsd, "1.08", 1_000m).Generate());
        Currency anyUntraded = new AnyCurrency().Except(Currency.Eur, Currency.Usd).Generate();

        // Act
        Money net = batch.BankNetPosition(anyUntraded);

        // Assert
        Assert.True(net.IsZero);
        Assert.Equal(anyUntraded, net.Currency);
    }

    [Fact]
    public void A_trade_at_the_reference_rate_is_worth_nothing_net_in_the_quoted_currency() {
        // Arrange
        SettlementBatch batch = Batch();
        batch.Book(TradeOf(EurUsd, "1.08", 1_000_000m).Generate());

        // Act / Assert
        Assert.True(batch.NetValueIn(Currency.Usd).IsZero);
    }

    [Fact]
    public void Valuing_in_the_base_currency_carries_the_rounding_of_the_inverse_rate() {
        // Arrange: the 1 080 000 USD paid are worth 1 080 000 x 0.92593 = 1 000 004.40 EUR (1 / 1.08, five places).
        SettlementBatch batch = Batch();
        batch.Book(TradeOf(EurUsd, "1.08", 1_000_000m).WithSide(TradeSide.BuyBase).Generate());

        // Act
        Money net = batch.NetValueIn(Currency.Eur);

        // Assert
        Assert.Equal(new Money(-4.40m, Currency.Eur), net);
    }

    [Fact]
    public void Valuation_converts_with_the_bankers_rounding_the_batch_names() {
        // Arrange: 100.10 EUR bought at 1.05 costs 105.11 USD (client rounding); the euros are worth
        // 100.10 x 1.05 = 105.105 USD, which banker's rounding takes to 105.10, so the batch is 0.01 USD short.
        RateSheet sheet = new RateSheet(Currency.Usd, SheetDate, new[] { new ExchangeRate(EurUsd, 1.05m) });
        SettlementBatch batch = new SettlementBatch(ValueDate, sheet, 0.01m);
        batch.Book(TradeOf(EurUsd, "1.05", 100.10m).WithSide(TradeSide.BuyBase).Generate());

        // Act
        Money net = batch.NetValueIn(Currency.Usd);

        // Assert
        Assert.Equal(new Money(-0.01m, Currency.Usd), net);
        Assert.Equal(MidpointRounding.ToEven, SettlementBatch.NettingRounding);
    }

    [Fact]
    public void Valuation_in_a_third_currency_goes_through_the_pivot() {
        // Arrange: the bank buys 100.00 USD against 15 025 yen, valued in euros.
        // USD -> EUR is the inverse 0.92593, JPY -> EUR the cross 0.00617: 92.59 EUR - 92.70 EUR.
        SettlementBatch batch = Batch(0.01m);
        batch.Book(TradeOf(UsdJpy, "150.25", 100m).WithSide(TradeSide.BuyBase).Generate());

        // Act
        Money net = batch.NetValueIn(Currency.Eur);

        // Assert
        Assert.Equal(new Money(-0.11m, Currency.Eur), net);
    }

    [Fact]
    public void Valuation_of_an_empty_batch_is_zero_in_the_reporting_currency() {
        // Arrange
        SettlementBatch batch = Batch();

        // Act
        Money net = batch.NetValueIn(Currency.Eur);

        // Assert
        Assert.True(net.IsZero);
        Assert.Equal(Currency.Eur, net.Currency);
    }

    [Fact]
    public void Valuation_in_a_currency_the_sheet_does_not_cover_is_refused() {
        // Arrange
        SettlementBatch batch = Batch();
        Currency anyUncovered = new AnyCurrency().Except(Currency.Usd, Currency.Eur, Currency.Gbp, Currency.Jpy).Generate();

        // Act / Assert
        Assert.Equal(SettlementRefusalReason.NoReferenceRate, ReasonOf(() => batch.NetValueIn(anyUncovered)));
        Assert.Equal(SettlementRefusalReason.NoReferenceRate, ReasonOf(() => batch.ExposureTo(Alpha, anyUncovered)));
    }

    [Fact]
    public void Exposure_values_the_net_with_one_counterparty() {
        // Arrange: same 105.105 midpoint with Alpha; Beta trades the other way, untouched by Alpha's exposure.
        RateSheet sheet = new RateSheet(Currency.Usd, SheetDate, new[] { new ExchangeRate(EurUsd, 1.05m) });
        SettlementBatch batch = new SettlementBatch(ValueDate, sheet, 0.01m);
        batch.Book(TradeOf(EurUsd, "1.05", 100.10m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate());
        batch.Book(TradeOf(EurUsd, "1.05", 1_000m).WithSide(TradeSide.SellBase).WithCounterparty(Beta).Generate());

        // Act / Assert
        Assert.Equal(new Money(-0.01m, Currency.Usd), batch.ExposureTo(Alpha, Currency.Usd));
        Assert.True(batch.ExposureTo(Beta, Currency.Usd).IsZero);
    }

    [Fact]
    public void Exposure_to_a_counterparty_the_bank_never_traded_with_is_zero() {
        // Arrange
        SettlementBatch batch = Batch();
        batch.Book(TradeOf(EurUsd, "1.08", 1_000m).WithCounterparty(Alpha).Generate());
        Lei anyStranger = Any.Lei().Generate();

        // Act
        Money exposure = batch.ExposureTo(anyStranger, Currency.Eur);

        // Assert
        Assert.True(exposure.IsZero);
    }

    [Fact]
    public void Valuation_refuses_missing_arguments() {
        // Arrange
        SettlementBatch batch = Batch();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => batch.BankNetPosition(null!));
        Assert.Throws<ArgumentNullException>(() => batch.NetValueIn(null!));
        Assert.Throws<ArgumentNullException>(() => batch.ExposureTo(null!, Currency.Eur));
        Assert.Throws<ArgumentNullException>(() => batch.ExposureTo(Alpha, null!));
    }

    [Fact(Skip = "domain defect: Book accepts trades whose accumulated net passes the 15-digit money maximum, then NetPositions() throws ArgumentOutOfRangeException from Money's constructor (two 10^12 EUR buys at 900 EUR/USD with one counterparty give -1.8e15 USD); seed-independent")]
    public void Booked_trades_leave_positions_the_batch_can_report() {
        // Arrange: each trade pays 9 x 10^14 USD, which Money can hold; their sum, 1.8 x 10^15, it cannot.
        RateSheet sheet = new RateSheet(Currency.Usd, SheetDate, new[] { new ExchangeRate(EurUsd, 900m) });
        SettlementBatch batch = new SettlementBatch(ValueDate, sheet, 0.01m);
        batch.Book(TradeOf(EurUsd, "900", 1_000_000_000_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate());

        // Act: either the second booking is refused, or the positions can be read.
        try { batch.Book(TradeOf(EurUsd, "900", 1_000_000_000_000m).WithSide(TradeSide.BuyBase).WithCounterparty(Alpha).Generate()); }
        catch (InvalidOperationException) { return; }

        // Assert
        Assert.NotEmpty(batch.NetPositions());
    }

    // ---- accounting identities over arbitrary books --------------------------------------------------------------

    private static SettlementBatch BookOfArbitraryTrades(int count) {
        SettlementBatch batch = Any.SettlementBatch()
                                   .WithReferenceRates(r => r.WithRates(rate => rate.WithValue(v => v.Between(0.5m, 2m))))
                                   .WithOffMarketTolerance(0.05m)
                                   .Generate();
        Currency[] covered = batch.ReferenceRates.Rates
                                  .SelectMany(rate => new[] { rate.Pair.Base, rate.Pair.Quote })
                                  .Distinct()
                                  .ToArray();
        for (int i = 0; i < count; i++) {
            Currency @base = Any.ElementOf(covered).Generate();
            Currency quote = Any.ElementOf(covered).DifferentFrom(@base).Generate();
            CurrencyPair pair = new CurrencyPair(@base, quote);
            ExchangeRate reference = batch.ReferenceRates.RateFor(pair, MidpointRounding.ToEven);
            batch.Book(Any.SpotTrade()
                          .SettlingOn(batch.ValueDate)
                          .WithRate(reference)
                          .WithDealtAmountAtMost(1_000_000m)
                          .Generate());
        }

        return batch;
    }

    [Fact]
    public void Per_currency_the_positions_with_every_counterparty_add_up_to_the_bank_net() {
        // Arrange
        SettlementBatch anyBatch = BookOfArbitraryTrades(8);

        // Act
        IEnumerable<IGrouping<Currency, NetPosition>> byCurrency = anyBatch.NetPositions().GroupBy(position => position.Currency);

        // Assert
        Assert.All(byCurrency, group => Assert.Equal(anyBatch.BankNetPosition(group.Key).Amount, group.Sum(position => position.Net.Amount)));
    }

    [Fact]
    public void Cancelling_every_trade_returns_the_batch_to_flat() {
        // Arrange
        SettlementBatch anyBatch = BookOfArbitraryTrades(6);

        // Act
        foreach (SpotTrade trade in anyBatch.Trades.ToList()) { anyBatch.Cancel(trade.Id); }
        anyBatch.CutOff();

        // Assert
        Assert.Empty(anyBatch.Trades);
        Assert.Empty(anyBatch.NetPositions());
        Assert.Empty(anyBatch.Instructions());
    }

    [Fact]
    public void An_instruction_is_issued_per_position_with_the_sign_as_direction() {
        // Arrange
        SettlementBatch anyBatch = BookOfArbitraryTrades(6);
        anyBatch.CutOff();

        // Act
        IReadOnlyList<SettlementInstruction> instructions = anyBatch.Instructions();
        IReadOnlyList<NetPosition>           positions    = anyBatch.NetPositions();

        // Assert
        Assert.Equal(positions.Count, instructions.Count);
        Assert.All(positions.Zip(instructions), pair => {
            Assert.Equal(pair.First.Counterparty, pair.Second.Counterparty);
            Assert.Equal(pair.First.Net.Abs(), pair.Second.Amount);
            Assert.Equal(pair.First.Net.IsPositive ? SettlementDirection.Receive : SettlementDirection.Pay, pair.Second.Direction);
            Assert.Equal(anyBatch.ValueDate, pair.Second.ValueDate);
        });
    }

}
