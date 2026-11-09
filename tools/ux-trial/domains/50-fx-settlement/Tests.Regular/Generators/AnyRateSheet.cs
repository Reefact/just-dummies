// Scaffolded by `any`, then rewritten by hand: a sheet quotes each of its currencies against the pivot, once, in
// whichever direction, which a per-parameter recipe cannot say. The generator draws a pivot, then the rates.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace FxSettlement.Domain.Tests.Regular;

public sealed partial class AnyRateSheet : IAny<RateSheet> {

    private IAny<Currency>     _pivot;
    private IAny<DateOnly>     _asOf;
    private AnyExchangeRate    _rateRecipe;
    private IReadOnlyList<ExchangeRate>? _rates;
    private IReadOnlyList<Currency>?     _quoted;
    private int                _minQuoted;
    private int                _maxQuoted;

    public AnyRateSheet() {
        _pivot      = new AnyCurrency();
        _asOf       = BusinessDays.Any();
        _rateRecipe = new AnyExchangeRate();
        _minQuoted  = 1;
        _maxQuoted  = 6;
    }

    private AnyRateSheet(AnyRateSheet from) {
        _pivot      = from._pivot;
        _asOf       = from._asOf;
        _rateRecipe = from._rateRecipe;
        _rates      = from._rates;
        _quoted     = from._quoted;
        _minQuoted  = from._minQuoted;
        _maxQuoted  = from._maxQuoted;
    }

    public AnyRateSheet WithPivot(Currency value) {
        AnyRateSheet copy = new AnyRateSheet(this);
        copy._pivot = Any.Fixed(value);
        return copy;
    }

    public AnyRateSheet WithPivot(Func<AnyCurrency, AnyCurrency> configure) {
        AnyRateSheet copy = new AnyRateSheet(this);
        copy._pivot = configure(new AnyCurrency());
        return copy;
    }

    public AnyRateSheet WithAsOf(DateOnly value) {
        AnyRateSheet copy = new AnyRateSheet(this);
        copy._asOf = Any.Fixed(value);
        return copy;
    }

    /// <summary>Pins the whole list of quotes; the pivot must be the one they are quoted against.</summary>
    public AnyRateSheet WithRates(IEnumerable<ExchangeRate> value) {
        AnyRateSheet copy = new AnyRateSheet(this);
        copy._rates = value.ToList();
        return copy;
    }

    /// <summary>Refines every quote the sheet draws; the pair is chosen by the sheet and stays its own.</summary>
    public AnyRateSheet WithRates(Func<AnyExchangeRate, AnyExchangeRate> configure) {
        AnyRateSheet copy = new AnyRateSheet(this);
        copy._rateRecipe = configure(_rateRecipe);
        return copy;
    }

    /// <summary>The sheet quotes exactly these currencies against its pivot.</summary>
    public AnyRateSheet Quoting(params Currency[] currencies) {
        AnyRateSheet copy = new AnyRateSheet(this);
        copy._quoted = currencies;
        return copy;
    }

    public AnyRateSheet WithQuotedCount(int min, int max) {
        AnyRateSheet copy = new AnyRateSheet(this);
        copy._minQuoted = min;
        copy._maxQuoted = max;
        return copy;
    }

    public RateSheet Generate() {
        Currency pivot = _pivot.Generate();
        DateOnly asOf  = _asOf.Generate();
        if (_rates is not null) {
            return new RateSheet(pivot, asOf, _rates);
        }

        List<Currency> candidates = new AnyCurrency().Pool.Where(currency => !currency.Equals(pivot)).ToList();
        List<Currency> quoted     = new List<Currency>();
        if (_quoted is not null) {
            quoted.AddRange(_quoted);
        } else {
            int count = Any.Int32().Between(_minQuoted, Math.Min(_maxQuoted, candidates.Count)).Generate();
            for (int i = 0; i < count; i++) {
                Currency drawn = Any.ElementOf(candidates).Generate();
                quoted.Add(drawn);
                candidates.Remove(drawn);
            }
        }

        List<ExchangeRate> rates = new List<ExchangeRate>();
        foreach (Currency other in quoted) {
            CurrencyPair pair = Any.Boolean().Generate() ? new CurrencyPair(pivot, other) : new CurrencyPair(other, pivot);
            rates.Add(_rateRecipe.WithPair(pair).Generate());
        }

        return new RateSheet(pivot, asOf, rates);
    }

}
