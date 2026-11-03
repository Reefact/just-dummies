// Scaffolded by `any` (JustDummies), then rewritten by hand: the sheet's rates must each involve the pivot,
// quote no currency twice, and cannot be an arbitrary sequence of arbitrary rates.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>
///     A generator of arbitrary <see cref="RateSheet" /> values: a pivot, a business day, and one to five other
///     currencies quoted against it, either way round.
/// </summary>
public sealed class AnyRateSheet : IAny<RateSheet> {

    private readonly IAny<Currency>                 _pivot;
    private readonly IAny<DateOnly>                 _asOf;
    private readonly IReadOnlyList<Currency>        _covering;
    private readonly IReadOnlyCollection<ExchangeRate>? _rates;

    public AnyRateSheet()
        : this(new AnyCurrency(), Days.Business(), [], null) { }

    private AnyRateSheet(IAny<Currency> pivot, IAny<DateOnly> asOf, IReadOnlyList<Currency> covering, IReadOnlyCollection<ExchangeRate>? rates) {
        _pivot    = pivot;
        _asOf     = asOf;
        _covering = covering;
        _rates    = rates;
    }

    public AnyRateSheet WithPivot(Currency value) {
        return new AnyRateSheet(Any.Fixed(value), _asOf, _covering, _rates);
    }

    public AnyRateSheet WithAsOf(DateOnly value) {
        return new AnyRateSheet(_pivot, Any.Fixed(value), _covering, _rates);
    }

    public AnyRateSheet WithAsOf(IAny<DateOnly> generator) {
        return new AnyRateSheet(_pivot, generator, _covering, _rates);
    }

    /// <summary>Guarantees these currencies are quoted (the pivot, if named, always is); others may be quoted too.</summary>
    public AnyRateSheet Covering(params Currency[] currencies) {
        return new AnyRateSheet(_pivot, _asOf, _covering.Concat(currencies).ToList(), _rates);
    }

    /// <summary>Pins the rates; <see cref="RateSheet" /> checks them on every draw.</summary>
    public AnyRateSheet WithRates(params ExchangeRate[] rates) {
        return new AnyRateSheet(_pivot, _asOf, _covering, rates);
    }

    public RateSheet Generate() {
        Currency pivot = _pivot.Generate();
        DateOnly asOf  = _asOf.Generate();

        if (_rates is not null) { return new RateSheet(pivot, asOf, _rates); }

        List<Currency> quoted = _covering.Where(currency => !currency.Equals(pivot)).Distinct().ToList();
        int            extra  = Any.Int32().Between(quoted.Count == 0 ? 1 : 0, 4).Generate();

        List<Currency> candidates = new AnyCurrency().Except([pivot, .. quoted]).Pool.ToList();
        quoted.AddRange(Any.PermutationOf(candidates).Generate().Take(extra));

        List<ExchangeRate> rates = new List<ExchangeRate>();
        foreach (Currency currency in quoted) {
            CurrencyPair pair = Any.Boolean().Generate() ? new CurrencyPair(currency, pivot) : new CurrencyPair(pivot, currency);

            rates.Add(new AnyExchangeRate().WithPair(pair).Generate());
        }

        return new RateSheet(pivot, asOf, rates);
    }

}
