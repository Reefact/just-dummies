// Scaffolded by `any` (JustDummies), then rewritten by hand: the dealt currency belongs to the rate's pair,
// the dealt amount fits its currency and cannot convert to nothing, and the trade date follows from the
// value date a test may pin. Six parameters tied to one another do not survive a per-parameter recipe.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>
///     A generator of arbitrary <see cref="SpotTrade" /> values. By default it is an ordinary trade in a pair of
///     the ISO table; <see cref="BookableIn" /> makes it one the given batch accepts.
/// </summary>
public sealed class AnySpotTrade : IAny<SpotTrade> {

    private readonly IAny<TradeId>   _id;
    private readonly IAny<Lei>       _counterparty;
    private readonly IAny<TradeSide> _side;
    private readonly ExchangeRate?   _rate;
    private readonly Money?          _dealt;
    private readonly DateOnly?       _tradeDate;
    private readonly DateOnly?       _valueDate;
    private readonly SettlementBatch? _batch;

    public AnySpotTrade()
        : this(new AnyTradeId(), new AnyLei(), Any.Enum<TradeSide>(), null, null, null, null, null) { }

    private AnySpotTrade(IAny<TradeId> id, IAny<Lei> counterparty, IAny<TradeSide> side, ExchangeRate? rate, Money? dealt, DateOnly? tradeDate, DateOnly? valueDate, SettlementBatch? batch) {
        _id           = id;
        _counterparty = counterparty;
        _side         = side;
        _rate         = rate;
        _dealt        = dealt;
        _tradeDate    = tradeDate;
        _valueDate    = valueDate;
        _batch        = batch;
    }

    public AnySpotTrade WithId(TradeId value) {
        return new AnySpotTrade(Any.Fixed(value), _counterparty, _side, _rate, _dealt, _tradeDate, _valueDate, _batch);
    }

    public AnySpotTrade WithCounterparty(Lei value) {
        return new AnySpotTrade(_id, Any.Fixed(value), _side, _rate, _dealt, _tradeDate, _valueDate, _batch);
    }

    public AnySpotTrade WithSide(TradeSide value) {
        return new AnySpotTrade(_id, _counterparty, Any.Fixed(value), _rate, _dealt, _tradeDate, _valueDate, _batch);
    }

    public AnySpotTrade WithRate(ExchangeRate value) {
        return new AnySpotTrade(_id, _counterparty, _side, value, _dealt, _tradeDate, _valueDate, _batch);
    }

    /// <summary>Pins the dealt amount; <see cref="SpotTrade" /> checks it against the rate's pair on every draw.</summary>
    public AnySpotTrade Dealing(Money value) {
        return new AnySpotTrade(_id, _counterparty, _side, _rate, value, _tradeDate, _valueDate, _batch);
    }

    /// <summary>Pins the trade date.</summary>
    public AnySpotTrade TradedOn(DateOnly value) {
        return new AnySpotTrade(_id, _counterparty, _side, _rate, _dealt, value, null, _batch);
    }

    /// <summary>Pins the value date: the trade date is the business day that makes the pair's spot lag land on it.</summary>
    public AnySpotTrade SettlingOn(DateOnly value) {
        return new AnySpotTrade(_id, _counterparty, _side, _rate, _dealt, null, value, _batch);
    }

    /// <summary>
    ///     Makes it a trade the batch accepts: a pair the sheet covers, dealt within half the tolerance of the
    ///     reference rate, settling on the batch's value date. A pinned rate or value date wins over it.
    /// </summary>
    public AnySpotTrade BookableIn(SettlementBatch batch) {
        return new AnySpotTrade(_id, _counterparty, _side, _rate, _dealt, _tradeDate, _valueDate, batch);
    }

    public SpotTrade Generate() {
        ExchangeRate rate = _rate ?? (_batch is null ? new AnyExchangeRate().Generate() : DrawRateNearReference(_batch));
        Money        dealt = _dealt ?? DrawDealtAmount(rate.Pair);

        DateOnly tradeDate = _tradeDate
                          ?? TradeDateSettlingOn(rate.Pair, _valueDate ?? _batch?.ValueDate)
                          ?? Days.Business().Generate();

        return new SpotTrade(_id.Generate(), _counterparty.Generate(), _side.Generate(), dealt, rate, tradeDate);
    }

    private static Money DrawDealtAmount(CurrencyPair pair) {
        Currency currency = Any.Boolean().Generate() ? pair.Base : pair.Quote;

        return new AnyMoney().InCurrency(currency).Between(1_000m, 1_000_000m).Generate();
    }

    private static ExchangeRate DrawRateNearReference(SettlementBatch batch) {
        RateSheet      sheet   = batch.ReferenceRates;
        List<Currency> covered = [sheet.Pivot, .. sheet.Rates.Select(rate => rate.Pair.Base.Equals(sheet.Pivot) ? rate.Pair.Quote : rate.Pair.Base)];

        Currency     baseCurrency  = Any.ElementOf(covered).Generate();
        Currency     quoteCurrency = Any.ElementOf(covered).DifferentFrom(baseCurrency).Generate();
        CurrencyPair pair          = new CurrencyPair(baseCurrency, quoteCurrency);

        ExchangeRate reference = sheet.RateFor(pair, SettlementBatch.NettingRounding);
        decimal      gap       = Any.Decimal().Between(-0.5m, 0.5m).WithScale(2).Generate();
        decimal      value     = decimal.Round(reference.Value * (1m + gap * batch.OffMarketTolerance), pair.RateDecimalPlaces);

        // A coarse pair rounds a small reference far from itself: the reference is always acceptable.
        bool stray = value <= 0m || Math.Abs(value - reference.Value) / reference.Value > batch.OffMarketTolerance;

        return new ExchangeRate(pair, stray ? reference.Value : value);
    }

    private static DateOnly? TradeDateSettlingOn(CurrencyPair pair, DateOnly? valueDate) {
        if (valueDate is not DateOnly target) { return null; }

        DateOnly date = target.AddDays(-1);
        for (int attempts = 0; attempts < 10; attempts++, date = date.AddDays(-1)) {
            if (!CurrencyPair.IsWeekend(date) && pair.SpotValueDate(date) == target) { return date; }
        }

        throw new ArgumentException($"No trade date makes {pair} settle on {target:yyyy-MM-dd}.", nameof(valueDate));
    }

}
