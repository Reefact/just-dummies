// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;
using Shop.Domain;

namespace Shop.Tests;

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrder : IAny<Order> {

    private readonly IAny<Guid>                     _id;
    private readonly IAny<IReadOnlyList<OrderLine>> _lines;
    private readonly IAny<ShippingAddress>          _shippingAddress;
    private readonly IAny<DiscountPercentage>       _discount;
    private readonly IAny<ShippingMethod>           _shippingMethod;
    private readonly IAny<DateTime>                 _placedAt;
    private readonly IAny<DateTime>                 _deliveryDeadline;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(id:               AnyValidId(),
               lines:            AnyValidLines(),
               shippingAddress:  AnyValidShippingAddress(),
               discount:         AnyValidDiscount(),
               shippingMethod:   AnyValidShippingMethod(),
               placedAt:         AnyValidPlacedAt(),
               deliveryDeadline: AnyValidDeliveryDeadline()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyList<OrderLine> AnyValidLines() {
        // All lines of an order share one currency; the base pins EUR, since an element's draw
        // cannot depend on its siblings'.
        return Any.ListOf(new AnyOrderLine().WithUnitPrice(price => price.WithCurrency("EUR"))).NonEmpty();
    }

    private static AnyShippingAddress AnyValidShippingAddress() {
        return new AnyShippingAddress();
    }

    private static AnyDiscountPercentage AnyValidDiscount() {
        return new AnyDiscountPercentage();
    }

    private static AnyEnum<ShippingMethod> AnyValidShippingMethod() {
        return Any.Enum<ShippingMethod>();
    }

    private static AnyDateTime AnyValidPlacedAt() {
        // placedAt < deliveryDeadline: split into two disjoint windows, as the bases are drawn independently.
        return Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2029, 12, 31));
    }

    private static AnyDateTime AnyValidDeliveryDeadline() {
        // Strictly after any placedAt the base draws (see AnyValidPlacedAt).
        return Any.DateTime().Between(new DateTime(2030, 1, 1), new DateTime(2031, 12, 31));
    }

    private AnyOrder(IAny<Guid>                     id,
                     IAny<IReadOnlyList<OrderLine>> lines,
                     IAny<ShippingAddress>          shippingAddress,
                     IAny<DiscountPercentage>       discount,
                     IAny<ShippingMethod>           shippingMethod,
                     IAny<DateTime>                 placedAt,
                     IAny<DateTime>                 deliveryDeadline) {
        _id               = id;
        _lines            = lines;
        _shippingAddress  = shippingAddress;
        _discount         = discount;
        _shippingMethod   = shippingMethod;
        _placedAt         = placedAt;
        _deliveryDeadline = deliveryDeadline;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyOrder WithId(Guid value) {
        return new AnyOrder(new FixedValue<Guid>(value), _lines, _shippingAddress, _discount, _shippingMethod, _placedAt, _deliveryDeadline);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyOrder(configure(AnyValidId()), _lines, _shippingAddress, _discount, _shippingMethod, _placedAt, _deliveryDeadline);
    }

    /// <summary>Pins <c>lines</c> to a fixed value.</summary>
    public AnyOrder WithLines(IReadOnlyList<OrderLine> value) {
        return new AnyOrder(_id, new FixedValue<IReadOnlyList<OrderLine>>(value), _shippingAddress, _discount, _shippingMethod, _placedAt, _deliveryDeadline);
    }

    /// <summary>Draws <c>lines</c> from <c>AnyValidLines()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithLines(Func<AnyList<OrderLine>, AnyList<OrderLine>> configure) {
        return new AnyOrder(_id, configure(AnyValidLines()), _shippingAddress, _discount, _shippingMethod, _placedAt, _deliveryDeadline);
    }

    /// <summary>Pins <c>shippingAddress</c> to a fixed value.</summary>
    public AnyOrder WithShippingAddress(ShippingAddress value) {
        return new AnyOrder(_id, _lines, new FixedValue<ShippingAddress>(value), _discount, _shippingMethod, _placedAt, _deliveryDeadline);
    }

    /// <summary>Draws <c>shippingAddress</c> from <c>AnyValidShippingAddress()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithShippingAddress(Func<AnyShippingAddress, AnyShippingAddress> configure) {
        return new AnyOrder(_id, _lines, configure(AnyValidShippingAddress()), _discount, _shippingMethod, _placedAt, _deliveryDeadline);
    }

    /// <summary>Pins <c>discount</c> to a fixed value.</summary>
    public AnyOrder WithDiscount(DiscountPercentage value) {
        return new AnyOrder(_id, _lines, _shippingAddress, new FixedValue<DiscountPercentage>(value), _shippingMethod, _placedAt, _deliveryDeadline);
    }

    /// <summary>Draws <c>discount</c> from <c>AnyValidDiscount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithDiscount(Func<AnyDiscountPercentage, AnyDiscountPercentage> configure) {
        return new AnyOrder(_id, _lines, _shippingAddress, configure(AnyValidDiscount()), _shippingMethod, _placedAt, _deliveryDeadline);
    }

    /// <summary>Pins <c>shippingMethod</c> to a fixed value.</summary>
    public AnyOrder WithShippingMethod(ShippingMethod value) {
        return new AnyOrder(_id, _lines, _shippingAddress, _discount, new FixedValue<ShippingMethod>(value), _placedAt, _deliveryDeadline);
    }

    /// <summary>Draws <c>shippingMethod</c> from <c>AnyValidShippingMethod()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithShippingMethod(Func<AnyEnum<ShippingMethod>, AnyEnum<ShippingMethod>> configure) {
        return new AnyOrder(_id, _lines, _shippingAddress, _discount, configure(AnyValidShippingMethod()), _placedAt, _deliveryDeadline);
    }

    /// <summary>Pins <c>placedAt</c> to a fixed value.</summary>
    public AnyOrder WithPlacedAt(DateTime value) {
        return new AnyOrder(_id, _lines, _shippingAddress, _discount, _shippingMethod, new FixedValue<DateTime>(value), _deliveryDeadline);
    }

    /// <summary>Draws <c>placedAt</c> from <c>AnyValidPlacedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithPlacedAt(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyOrder(_id, _lines, _shippingAddress, _discount, _shippingMethod, configure(AnyValidPlacedAt()), _deliveryDeadline);
    }

    /// <summary>Pins <c>deliveryDeadline</c> to a fixed value.</summary>
    public AnyOrder WithDeliveryDeadline(DateTime value) {
        return new AnyOrder(_id, _lines, _shippingAddress, _discount, _shippingMethod, _placedAt, new FixedValue<DateTime>(value));
    }

    /// <summary>Draws <c>deliveryDeadline</c> from <c>AnyValidDeliveryDeadline()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithDeliveryDeadline(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyOrder(_id, _lines, _shippingAddress, _discount, _shippingMethod, _placedAt, configure(AnyValidDeliveryDeadline()));
    }

    /// <summary>Produces one arbitrary <see cref="Order" />.</summary>
    public Order Generate() {
        return new Order(_id.Generate(),
                         _lines.Generate(),
                         _shippingAddress.Generate(),
                         _discount.Generate(),
                         _shippingMethod.Generate(),
                         _placedAt.Generate(),
                         _deliveryDeadline.Generate());
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
