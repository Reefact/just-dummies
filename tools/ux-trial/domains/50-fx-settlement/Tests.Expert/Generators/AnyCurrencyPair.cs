// Scaffolded by `any` (JustDummies), then rewritten by hand: the quote must differ from the base, a rule
// between the two parameters that the scaffold marked `to verify`.

using System;

using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>A generator of arbitrary <see cref="CurrencyPair" /> values: two different currencies of the ISO table.</summary>
public sealed class AnyCurrencyPair : IAny<CurrencyPair> {

    private readonly IAny<Currency> _base;
    private readonly IAny<Currency> _quote;

    public AnyCurrencyPair()
        : this(new AnyCurrency(), new AnyCurrency()) { }

    private AnyCurrencyPair(IAny<Currency> @base, IAny<Currency> quote) {
        _base  = @base;
        _quote = quote;
    }

    public AnyCurrencyPair WithBase(Currency value) {
        return new AnyCurrencyPair(Any.Fixed(value), _quote);
    }

    public AnyCurrencyPair WithBase(Func<AnyCurrency, AnyCurrency> configure) {
        return new AnyCurrencyPair(configure(new AnyCurrency()), _quote);
    }

    public AnyCurrencyPair WithQuote(Currency value) {
        return new AnyCurrencyPair(_base, Any.Fixed(value));
    }

    public AnyCurrencyPair WithQuote(Func<AnyCurrency, AnyCurrency> configure) {
        return new AnyCurrencyPair(_base, configure(new AnyCurrency()));
    }

    public CurrencyPair Generate() {
        Currency baseCurrency  = _base.Generate();
        Currency quoteCurrency = _quote.DifferentFrom(baseCurrency).Generate();

        return new CurrencyPair(baseCurrency, quoteCurrency);
    }

}
