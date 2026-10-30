// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Tariff --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GuardMiss.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Tariff" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTariff : IAny<Tariff> {

    private readonly IAny<decimal> _rate;
    private readonly IAny<string>  _currency;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTariff()
        : this(rate:     AnyValidRate(),
               currency: AnyValidCurrency()) { }

    private static AnyDecimal AnyValidRate() {
        return Any.Decimal();
    }

    private static AnyString AnyValidCurrency() {
        return Any.String().NonEmpty();
    }

    private AnyTariff(IAny<decimal> rate,
                      IAny<string>  currency) {
        _rate     = rate;
        _currency = currency;
    }

    /// <summary>Pins <c>rate</c> to a fixed value.</summary>
    public AnyTariff WithRate(decimal value) {
        return new AnyTariff(new FixedValue<decimal>(value), _currency);
    }

    /// <summary>Draws <c>rate</c> from <c>AnyValidRate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTariff WithRate(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyTariff(configure(AnyValidRate()), _currency);
    }

    /// <summary>Pins <c>currency</c> to a fixed value.</summary>
    public AnyTariff WithCurrency(string value) {
        return new AnyTariff(_rate, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>currency</c> from <c>AnyValidCurrency()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTariff WithCurrency(Func<AnyString, AnyString> configure) {
        return new AnyTariff(_rate, configure(AnyValidCurrency()));
    }

    /// <summary>Produces one arbitrary <see cref="Tariff" />.</summary>
    public Tariff Generate() {
        return new Tariff(_rate.Generate(),
                          _currency.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
