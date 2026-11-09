// Scaffolded by `any`, then rewritten by hand: the dealt currency must belong to the rate's pair, the other leg may
// not round to zero or overflow, and the trade date drives the value date. Four parameters tie to one another, so the
// generator draws the rate first, then an amount that the rate honours.

using System;

using JustDummies;

namespace FxSettlement.Domain.Tests.Regular;

public sealed partial class AnySpotTrade : IAny<SpotTrade> {

    private IAny<TradeId>     _id;
    private IAny<Lei>         _counterparty;
    private IAny<TradeSide>   _side;
    private AnyExchangeRate   _rateRecipe;
    private ExchangeRate?     _rate;
    private Money?            _dealtAmount;
    private bool?             _dealtInBase;
    private decimal           _dealtAtMost;
    private IAny<DateOnly>    _tradeDate;
    private DateOnly?         _settlingOn;

    public AnySpotTrade() {
        _id          = new AnyTradeId();
        _counterparty = new AnyLei();
        _side        = Any.Enum<TradeSide>();
        _rateRecipe  = new AnyExchangeRate();
        _dealtAtMost = SpotTrade.MaxDealtAmount;
        _tradeDate   = BusinessDays.Any();
    }

    private AnySpotTrade(AnySpotTrade from) {
        _id           = from._id;
        _counterparty = from._counterparty;
        _side         = from._side;
        _rateRecipe   = from._rateRecipe;
        _rate         = from._rate;
        _dealtAmount  = from._dealtAmount;
        _dealtInBase  = from._dealtInBase;
        _dealtAtMost  = from._dealtAtMost;
        _tradeDate    = from._tradeDate;
        _settlingOn   = from._settlingOn;
    }

    public AnySpotTrade WithId(TradeId value) {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._id = Any.Fixed(value);
        return copy;
    }

    public AnySpotTrade WithCounterparty(Lei value) {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._counterparty = Any.Fixed(value);
        return copy;
    }

    public AnySpotTrade WithSide(TradeSide value) {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._side = Any.Fixed(value);
        return copy;
    }

    public AnySpotTrade WithRate(ExchangeRate value) {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._rate = value;
        return copy;
    }

    public AnySpotTrade WithRate(Func<AnyExchangeRate, AnyExchangeRate> configure) {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._rateRecipe = configure(_rateRecipe);
        return copy;
    }

    /// <summary>Pins the dealt amount; its currency must belong to the rate's pair, which is then the caller's to arrange.</summary>
    public AnySpotTrade WithDealtAmount(Money value) {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._dealtAmount = value;
        return copy;
    }

    public AnySpotTrade DealtInBase() {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._dealtInBase = true;
        return copy;
    }

    public AnySpotTrade DealtInQuote() {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._dealtInBase = false;
        return copy;
    }

    public AnySpotTrade WithDealtAmountAtMost(decimal value) {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._dealtAtMost = value;
        return copy;
    }

    public AnySpotTrade WithTradeDate(DateOnly value) {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._tradeDate = Any.Fixed(value);
        return copy;
    }

    /// <summary>The trade date is the one that makes the pair's spot value date this day.</summary>
    public AnySpotTrade SettlingOn(DateOnly valueDate) {
        AnySpotTrade copy = new AnySpotTrade(this);
        copy._settlingOn = valueDate;
        return copy;
    }

    public SpotTrade Generate() {
        ExchangeRate rate = _rate ?? _rateRecipe.Generate();
        Money dealt       = _dealtAmount ?? DrawDealt(rate);
        DateOnly tradeDate = _settlingOn is DateOnly valueDate
            ? BusinessDays.Minus(valueDate, rate.Pair.SpotLagDays)
            : _tradeDate.Generate();

        return new SpotTrade(_id.Generate(), _counterparty.Generate(), _side.Generate(), dealt, rate, tradeDate);
    }

    private Money DrawDealt(ExchangeRate rate) {
        bool     inBase = _dealtInBase ?? Any.Boolean().Generate();
        Currency dealt  = inBase ? rate.Pair.Base : rate.Pair.Quote;
        Currency other  = inBase ? rate.Pair.Quote : rate.Pair.Base;

        // The other leg must be worth at least one of its minor units, and stay under the money maximum.
        decimal lower = inBase ? other.SmallestUnit / rate.Value : other.SmallestUnit * rate.Value;
        decimal upper = inBase ? 900_000_000_000_000m / rate.Value : 900_000_000_000_000m * rate.Value;

        decimal scale = (decimal)Math.Pow(10, dealt.MinorUnits);
        decimal low   = Math.Max(dealt.SmallestUnit, Math.Ceiling(lower * scale) / scale);
        decimal high  = Math.Min(_dealtAtMost, Math.Floor(upper * scale) / scale);

        return new Money(Any.Decimal().Between(low, high).WithScale(dealt.MinorUnits).Generate(), dealt);
    }

}
