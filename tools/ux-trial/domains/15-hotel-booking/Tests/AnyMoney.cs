// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Money --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Money" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMoney : IAny<Money> {

    private readonly IAny<decimal> _amount;
    private readonly IAny<string>  _currency;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMoney()
        : this(amount:   AnyValidAmount(),
               currency: AnyValidCurrency()) { }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2);
    }

    private static AnyPattern AnyValidCurrency() {
        return Any.StringMatching("[A-Z]{3}");
    }

    private AnyMoney(IAny<decimal> amount,
                     IAny<string>  currency) {
        _amount   = amount;
        _currency = currency;
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyMoney WithAmount(decimal value) {
        return new AnyMoney(new FixedValue<decimal>(value), _currency);
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMoney WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyMoney(configure(AnyValidAmount()), _currency);
    }

    /// <summary>Pins <c>currency</c> to a fixed value.</summary>
    public AnyMoney WithCurrency(string value) {
        return new AnyMoney(_amount, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>currency</c> from <c>AnyValidCurrency()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMoney WithCurrency(Func<AnyPattern, AnyPattern> configure) {
        return new AnyMoney(_amount, configure(AnyValidCurrency()));
    }

    /// <summary>Produces one arbitrary <see cref="Money" />.</summary>
    public Money Generate() {
        return new Money(_amount.Generate(),
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
