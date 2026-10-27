// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Price --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Price" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPrice : IAny<Price> {

    private readonly IAny<decimal> _amount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPrice()
        : this(amount: AnyValidAmount()) { }

    private static AnyDecimal AnyValidAmount() {
        // Price.Validate: (0, ceiling) and at most four decimal places.
        return Any.Decimal().Positive().LessThan(Price.Ceiling).WithScale(4);
    }

    private AnyPrice(IAny<decimal> amount) {
        _amount = amount;
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyPrice WithAmount(decimal value) {
        return new AnyPrice(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrice WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPrice(configure(AnyValidAmount()));
    }

    /// <summary>Produces one arbitrary <see cref="Price" />.</summary>
    public Price Generate() {
        return new Price(_amount.Generate());
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
