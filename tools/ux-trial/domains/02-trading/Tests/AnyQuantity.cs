// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Quantity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Quantity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyQuantity : IAny<Quantity> {

    private readonly IAny<int> _shares;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyQuantity()
        : this(shares: AnyValidShares()) { }

    private static AnyInt32 AnyValidShares() {
        // Quantity: shares is <= 0 or > Maximum throws.
        return Any.Int32().Between(1, Quantity.Maximum);
    }

    private AnyQuantity(IAny<int> shares) {
        _shares = shares;
    }

    /// <summary>Pins <c>shares</c> to a fixed value.</summary>
    public AnyQuantity WithShares(int value) {
        return new AnyQuantity(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>shares</c> from <c>AnyValidShares()</c>, refined by <paramref name="configure" />.</summary>
    public AnyQuantity WithShares(Func<AnyInt32, AnyInt32> configure) {
        return new AnyQuantity(configure(AnyValidShares()));
    }

    /// <summary>Produces one arbitrary <see cref="Quantity" />.</summary>
    public Quantity Generate() {
        return new Quantity(_shares.Generate());
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
