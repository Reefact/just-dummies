// Scaffolded by `any` (JustDummies), then rewritten by hand: an amount's scale is its currency's minor
// units, a tie between the two parameters that the scaffold could only mark `to verify`.

using System;

using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>A generator of arbitrary <see cref="Money" />: an amount that fits its currency, up to a billion in magnitude.</summary>
public sealed class AnyMoney : IAny<Money> {

    private const decimal Ceiling = 1_000_000_000m;

    private readonly IAny<Currency> _currency;
    private readonly decimal?       _amount;
    private readonly decimal        _min;
    private readonly decimal        _max;
    private readonly bool           _positive;

    public AnyMoney()
        : this(new AnyCurrency(), null, -Ceiling, Ceiling, false) { }

    private AnyMoney(IAny<Currency> currency, decimal? amount, decimal min, decimal max, bool positive) {
        _currency = currency;
        _amount   = amount;
        _min      = min;
        _max      = max;
        _positive = positive;
    }

    /// <summary>Pins the currency: the amount follows its minor units.</summary>
    public AnyMoney InCurrency(Currency currency) {
        return new AnyMoney(Any.Fixed(currency), _amount, _min, _max, _positive);
    }

    /// <summary>Refines the currency it is drawn from.</summary>
    public AnyMoney InCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        return new AnyMoney(configure(new AnyCurrency()), _amount, _min, _max, _positive);
    }

    /// <summary>Pins the amount; <see cref="Money" /> checks it against the currency on every draw.</summary>
    public AnyMoney WithAmount(decimal amount) {
        return new AnyMoney(_currency, amount, _min, _max, _positive);
    }

    /// <summary>Draws strictly above zero, as small as one minor unit.</summary>
    public AnyMoney Positive() {
        return new AnyMoney(_currency, _amount, _min, _max, true);
    }

    /// <summary>Bounds the amount, both ends included; the draw lands on the currency's minor units.</summary>
    public AnyMoney Between(decimal min, decimal max) {
        return new AnyMoney(_currency, _amount, min, max, _positive);
    }

    public Money Generate() {
        Currency currency = _currency.Generate();

        if (_amount is decimal pinned) { return new Money(pinned, currency); }

        AnyDecimal amount = _positive
            ? Any.Decimal().Positive().LessThanOrEqualTo(_max)
            : Any.Decimal().Between(_min, _max);

        return new Money(amount.WithScale(currency.MinorUnits).Generate(), currency);
    }

}
