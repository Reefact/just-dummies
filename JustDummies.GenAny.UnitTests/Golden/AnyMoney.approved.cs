// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Money --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Money" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMoney : IAny<Money> {

    private readonly IAny<decimal> _amount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMoney()
        : this(amount: AnyValidAmount()) { }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal();
    }

    private AnyMoney(IAny<decimal> amount) {
        _amount = amount;
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyMoney WithAmount(decimal value) {
        return new AnyMoney(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMoney WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyMoney(configure(AnyValidAmount()));
    }

    /// <summary>Produces one arbitrary <see cref="Money" />.</summary>
    public Money Generate() {
        return new Money(_amount.Generate());
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
