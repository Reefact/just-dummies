// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrder : IAny<Order> {

    private readonly IAny<OrderReference>        _reference;
    private readonly IAny<Customer>              _customer;
    private readonly IAny<int>                   _quantity;
    private readonly IAny<OrderStatus>           _status;
    private readonly IAny<IReadOnlyList<string>> _tags;
    private readonly IAny<DateTime>              _placedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(reference: AnyValidReference(),
               customer:  AnyValidCustomer(),
               quantity:  AnyValidQuantity(),
               status:    AnyValidStatus(),
               tags:      AnyValidTags(),
               placedAt:  AnyValidPlacedAt()) { }

    private static AnyOrderReference AnyValidReference() {
        return new AnyOrderReference();
    }

    private static AnyCustomer AnyValidCustomer() {
        return new AnyCustomer();
    }

    private static AnyInt32 AnyValidQuantity() {
        return Any.Int32().Positive();
    }

    private static AnyEnum<OrderStatus> AnyValidStatus() {
        return Any.Enum<OrderStatus>();
    }

    private static AnyList<string> AnyValidTags() {
        return Any.ListOf(Any.String().NonEmpty());
    }

    private static AnyDateTime AnyValidPlacedAt() {
        return Any.DateTime();
    }

    private AnyOrder(IAny<OrderReference>        reference,
                     IAny<Customer>              customer,
                     IAny<int>                   quantity,
                     IAny<OrderStatus>           status,
                     IAny<IReadOnlyList<string>> tags,
                     IAny<DateTime>              placedAt) {
        _reference = reference;
        _customer  = customer;
        _quantity  = quantity;
        _status    = status;
        _tags      = tags;
        _placedAt  = placedAt;
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public AnyOrder WithReference(OrderReference value) {
        return new AnyOrder(new FixedValue<OrderReference>(value), _customer, _quantity, _status, _tags, _placedAt);
    }

    /// <summary>Draws <c>reference</c> from <c>AnyValidReference()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithReference(Func<AnyOrderReference, AnyOrderReference> configure) {
        return new AnyOrder(configure(AnyValidReference()), _customer, _quantity, _status, _tags, _placedAt);
    }

    /// <summary>Pins <c>customer</c> to a fixed value.</summary>
    public AnyOrder WithCustomer(Customer value) {
        return new AnyOrder(_reference, new FixedValue<Customer>(value), _quantity, _status, _tags, _placedAt);
    }

    /// <summary>Draws <c>customer</c> from <c>AnyValidCustomer()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithCustomer(Func<AnyCustomer, AnyCustomer> configure) {
        return new AnyOrder(_reference, configure(AnyValidCustomer()), _quantity, _status, _tags, _placedAt);
    }

    /// <summary>Pins <c>quantity</c> to a fixed value.</summary>
    public AnyOrder WithQuantity(int value) {
        return new AnyOrder(_reference, _customer, new FixedValue<int>(value), _status, _tags, _placedAt);
    }

    /// <summary>Draws <c>quantity</c> from <c>AnyValidQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithQuantity(Func<AnyInt32, AnyInt32> configure) {
        return new AnyOrder(_reference, _customer, configure(AnyValidQuantity()), _status, _tags, _placedAt);
    }

    /// <summary>Pins <c>status</c> to a fixed value.</summary>
    public AnyOrder WithStatus(OrderStatus value) {
        return new AnyOrder(_reference, _customer, _quantity, new FixedValue<OrderStatus>(value), _tags, _placedAt);
    }

    /// <summary>Draws <c>status</c> from <c>AnyValidStatus()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithStatus(Func<AnyEnum<OrderStatus>, AnyEnum<OrderStatus>> configure) {
        return new AnyOrder(_reference, _customer, _quantity, configure(AnyValidStatus()), _tags, _placedAt);
    }

    /// <summary>Pins <c>tags</c> to a fixed value.</summary>
    public AnyOrder WithTags(IReadOnlyList<string> value) {
        return new AnyOrder(_reference, _customer, _quantity, _status, new FixedValue<IReadOnlyList<string>>(value), _placedAt);
    }

    /// <summary>Draws <c>tags</c> from <c>AnyValidTags()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithTags(Func<AnyList<string>, AnyList<string>> configure) {
        return new AnyOrder(_reference, _customer, _quantity, _status, configure(AnyValidTags()), _placedAt);
    }

    /// <summary>Pins <c>placedAt</c> to a fixed value.</summary>
    public AnyOrder WithPlacedAt(DateTime value) {
        return new AnyOrder(_reference, _customer, _quantity, _status, _tags, new FixedValue<DateTime>(value));
    }

    /// <summary>Draws <c>placedAt</c> from <c>AnyValidPlacedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithPlacedAt(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyOrder(_reference, _customer, _quantity, _status, _tags, configure(AnyValidPlacedAt()));
    }

    /// <summary>Produces one arbitrary <see cref="Order" />.</summary>
    public Order Generate() {
        return new Order(_reference.Generate(),
                         _customer.Generate(),
                         _quantity.Generate(),
                         _status.Generate(),
                         _tags.Generate(),
                         _placedAt.Generate());
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
