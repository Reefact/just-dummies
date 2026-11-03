// Scaffolded by `any` (JustDummies), then rewritten by hand: the value carries as many decimal places as
// the pair allows, a tie between the two parameters that the scaffold marked `to verify`.

using System;

using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>A generator of arbitrary <see cref="ExchangeRate" /> values, between 0.25 and 4 on the pair's own decimal places. The range is narrow on purpose: a sheet of such rates always triangulates into a quotable cross rate (between 1/16 and 16).</summary>
public sealed class AnyExchangeRate : IAny<ExchangeRate> {

    private readonly IAny<CurrencyPair> _pair;
    private readonly decimal?           _value;

    public AnyExchangeRate()
        : this(new AnyCurrencyPair(), null) { }

    private AnyExchangeRate(IAny<CurrencyPair> pair, decimal? value) {
        _pair  = pair;
        _value = value;
    }

    public AnyExchangeRate WithPair(CurrencyPair value) {
        return new AnyExchangeRate(Any.Fixed(value), _value);
    }

    public AnyExchangeRate WithPair(Func<AnyCurrencyPair, AnyCurrencyPair> configure) {
        return new AnyExchangeRate(configure(new AnyCurrencyPair()), _value);
    }

    /// <summary>Pins the value; <see cref="ExchangeRate" /> checks it against the pair on every draw.</summary>
    public AnyExchangeRate WithValue(decimal value) {
        return new AnyExchangeRate(_pair, value);
    }

    public ExchangeRate Generate() {
        CurrencyPair pair = _pair.Generate();

        decimal value = _value ?? Any.Decimal().Between(0.25m, 4m).WithScale(pair.RateDecimalPlaces).Generate();

        return new ExchangeRate(pair, value);
    }

}
