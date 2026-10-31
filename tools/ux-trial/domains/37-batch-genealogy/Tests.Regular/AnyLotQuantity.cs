// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LotQuantity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LotQuantity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLotQuantity : IAny<LotQuantity> {

    private readonly IAny<LotNumber> _lot;
    private readonly IAny<Quantity>  _quantity;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLotQuantity()
        : this(lot:      AnyValidLot(),
               quantity: AnyValidQuantity()) { }

    private static AnyLotNumber AnyValidLot() {
        return new AnyLotNumber();
    }

    private static AnyQuantity AnyValidQuantity() {
        return new AnyQuantity().WithAmount(a => a.Positive());
    }

    private AnyLotQuantity(IAny<LotNumber> lot,
                           IAny<Quantity>  quantity) {
        _lot      = lot;
        _quantity = quantity;
    }

    /// <summary>Pins <c>lot</c> to a fixed value.</summary>
    public AnyLotQuantity WithLot(LotNumber value) {
        return new AnyLotQuantity(new FixedValue<LotNumber>(value), _quantity);
    }

    /// <summary>Draws <c>lot</c> from <c>AnyValidLot()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLotQuantity WithLot(Func<AnyLotNumber, AnyLotNumber> configure) {
        return new AnyLotQuantity(configure(AnyValidLot()), _quantity);
    }

    /// <summary>Pins <c>quantity</c> to a fixed value.</summary>
    public AnyLotQuantity WithQuantity(Quantity value) {
        return new AnyLotQuantity(_lot, new FixedValue<Quantity>(value));
    }

    /// <summary>Draws <c>quantity</c> from <c>AnyValidQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLotQuantity WithQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        return new AnyLotQuantity(_lot, configure(AnyValidQuantity()));
    }

    /// <summary>Produces one arbitrary <see cref="LotQuantity" />.</summary>
    public LotQuantity Generate() {
        return new LotQuantity(_lot.Generate(),
                               _quantity.Generate());
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
