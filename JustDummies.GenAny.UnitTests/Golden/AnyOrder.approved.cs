// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins it; to draw from an isolated
///     <c>Any.WithSeed(...)</c> context, pass that context's generators through the
///     <c>With…</c> overloads.
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
        : this(reference: ReferenceFactory(),
               customer:  CustomerFactory(),
               quantity:  QuantityFactory(),
               status:    StatusFactory(),
               tags:      TagsFactory(),
               placedAt:  PlacedAtFactory()) { }

    private static IAny<OrderReference> ReferenceFactory() {
        return Any.String().NonEmpty().As(OrderReference.Create);
    }

    private static IAny<Customer> CustomerFactory() {
        return new AnyCustomer();
    }

    private static IAny<int> QuantityFactory() {
        return Any.Int32().Positive();
    }

    private static IAny<OrderStatus> StatusFactory() {
        return Any.Enum<OrderStatus>();
    }

    private static IAny<IReadOnlyList<string>> TagsFactory() {
        return Any.ListOf(Any.String().NonEmpty());
    }

    private static IAny<DateTime> PlacedAtFactory() {
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
        return WithReference(new FixedValue<OrderReference>(value));
    }

    /// <summary>Draws <c>reference</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithReference(IAny<OrderReference> generator) {
        return new AnyOrder(generator, _customer, _quantity, _status, _tags, _placedAt);
    }

    /// <summary>Pins <c>customer</c> to a fixed value.</summary>
    public AnyOrder WithCustomer(Customer value) {
        return WithCustomer(new FixedValue<Customer>(value));
    }

    /// <summary>Draws <c>customer</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithCustomer(IAny<Customer> generator) {
        return new AnyOrder(_reference, generator, _quantity, _status, _tags, _placedAt);
    }

    /// <summary>Pins <c>quantity</c> to a fixed value.</summary>
    public AnyOrder WithQuantity(int value) {
        return WithQuantity(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>quantity</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithQuantity(IAny<int> generator) {
        return new AnyOrder(_reference, _customer, generator, _status, _tags, _placedAt);
    }

    /// <summary>Pins <c>status</c> to a fixed value.</summary>
    public AnyOrder WithStatus(OrderStatus value) {
        return WithStatus(new FixedValue<OrderStatus>(value));
    }

    /// <summary>Draws <c>status</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithStatus(IAny<OrderStatus> generator) {
        return new AnyOrder(_reference, _customer, _quantity, generator, _tags, _placedAt);
    }

    /// <summary>Pins <c>tags</c> to a fixed value.</summary>
    public AnyOrder WithTags(IReadOnlyList<string> value) {
        return WithTags(new FixedValue<IReadOnlyList<string>>(value));
    }

    /// <summary>Draws <c>tags</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithTags(IAny<IReadOnlyList<string>> generator) {
        return new AnyOrder(_reference, _customer, _quantity, _status, generator, _placedAt);
    }

    /// <summary>Pins <c>placedAt</c> to a fixed value.</summary>
    public AnyOrder WithPlacedAt(DateTime value) {
        return WithPlacedAt(new FixedValue<DateTime>(value));
    }

    /// <summary>Draws <c>placedAt</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithPlacedAt(IAny<DateTime> generator) {
        return new AnyOrder(_reference, _customer, _quantity, _status, _tags, generator);
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
