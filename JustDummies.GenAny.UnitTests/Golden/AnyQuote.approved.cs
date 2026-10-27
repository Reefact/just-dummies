// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Quote --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Quote" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyQuote : IAny<Quote> {

    private readonly IAny<decimal>  _amount;
    private readonly IAny<decimal?> _discount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyQuote()
        : this(amount:   AnyValidAmount(),
               discount: AnyValidDiscount().AsNullable()) { }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal().Positive();
    }

    private static AnyDecimal AnyValidDiscount() {
        return Any.Decimal();
    }

    private AnyQuote(IAny<decimal>  amount,
                     IAny<decimal?> discount) {
        _amount   = amount;
        _discount = discount;
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyQuote WithAmount(decimal value) {
        return new AnyQuote(new FixedValue<decimal>(value), _discount);
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyQuote WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyQuote(configure(AnyValidAmount()), _discount);
    }

    /// <summary>Pins <c>discount</c> to a fixed value.</summary>
    public AnyQuote WithDiscount(decimal? value) {
        return new AnyQuote(_amount, new FixedValue<decimal?>(value));
    }

    /// <summary>Draws <c>discount</c> from <c>AnyValidDiscount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyQuote WithDiscount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyQuote(_amount, configure(AnyValidDiscount()).AsNullable());
    }

    /// <summary>Produces one arbitrary <see cref="Quote" />.</summary>
    public Quote Generate() {
        return new Quote(_amount.Generate(),
                         _discount.Generate());
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
