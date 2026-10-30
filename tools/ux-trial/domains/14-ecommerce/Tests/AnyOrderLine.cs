// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OrderLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Shop.Domain;

namespace Shop.Tests;

/// <summary>
///     A generator of arbitrary <see cref="OrderLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrderLine : IAny<OrderLine> {

    private readonly IAny<Sku>      _sku;
    private readonly IAny<Quantity> _quantity;
    private readonly IAny<Money>    _unitPrice;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrderLine()
        : this(sku:       AnyValidSku(),
               quantity:  AnyValidQuantity(),
               unitPrice: AnyValidUnitPrice()) { }

    private static AnySku AnyValidSku() {
        return new AnySku();
    }

    private static AnyQuantity AnyValidQuantity() {
        return new AnyQuantity();
    }

    private static AnyMoney AnyValidUnitPrice() {
        return new AnyMoney();
    }

    private AnyOrderLine(IAny<Sku>      sku,
                         IAny<Quantity> quantity,
                         IAny<Money>    unitPrice) {
        _sku       = sku;
        _quantity  = quantity;
        _unitPrice = unitPrice;
    }

    /// <summary>Pins <c>sku</c> to a fixed value.</summary>
    public AnyOrderLine WithSku(Sku value) {
        return new AnyOrderLine(new FixedValue<Sku>(value), _quantity, _unitPrice);
    }

    /// <summary>Draws <c>sku</c> from <c>AnyValidSku()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrderLine WithSku(Func<AnySku, AnySku> configure) {
        return new AnyOrderLine(configure(AnyValidSku()), _quantity, _unitPrice);
    }

    /// <summary>Pins <c>quantity</c> to a fixed value.</summary>
    public AnyOrderLine WithQuantity(Quantity value) {
        return new AnyOrderLine(_sku, new FixedValue<Quantity>(value), _unitPrice);
    }

    /// <summary>Draws <c>quantity</c> from <c>AnyValidQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrderLine WithQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        return new AnyOrderLine(_sku, configure(AnyValidQuantity()), _unitPrice);
    }

    /// <summary>Pins <c>unitPrice</c> to a fixed value.</summary>
    public AnyOrderLine WithUnitPrice(Money value) {
        return new AnyOrderLine(_sku, _quantity, new FixedValue<Money>(value));
    }

    /// <summary>Draws <c>unitPrice</c> from <c>AnyValidUnitPrice()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrderLine WithUnitPrice(Func<AnyMoney, AnyMoney> configure) {
        return new AnyOrderLine(_sku, _quantity, configure(AnyValidUnitPrice()));
    }

    /// <summary>Produces one arbitrary <see cref="OrderLine" />.</summary>
    public OrderLine Generate() {
        return new OrderLine(_sku.Generate(),
                             _quantity.Generate(),
                             _unitPrice.Generate());
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
