// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReimbursementPrice --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReimbursementPrice" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReimbursementPrice : IAny<ReimbursementPrice> {

    private readonly IAny<decimal> _amount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReimbursementPrice()
        : this(amount: AnyValidAmount()) { }

    private static AnyDecimal AnyValidAmount() {
        // Verified by hand: the constructor also requires a price to the cent.
        return Any.Decimal().Positive().LessThanOrEqualTo(100000m).WithScale(2);
    }

    private AnyReimbursementPrice(IAny<decimal> amount) {
        _amount = amount;
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyReimbursementPrice WithAmount(decimal value) {
        return new AnyReimbursementPrice(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReimbursementPrice WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyReimbursementPrice(configure(AnyValidAmount()));
    }

    /// <summary>Produces one arbitrary <see cref="ReimbursementPrice" />.</summary>
    public ReimbursementPrice Generate() {
        return new ReimbursementPrice(_amount.Generate());
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
