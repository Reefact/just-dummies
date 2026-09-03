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
///     <c>Dummy.WithSeed(...)</c> context, pass that context's generators through the
///     <c>With…</c> overloads.
/// </summary>
public sealed partial class DummyOrder : IDummy<Order> {

    private readonly IDummy<OrderReference>        _reference;
    private readonly IDummy<Customer>              _customer;
    private readonly IDummy<int>                   _quantity;
    private readonly IDummy<OrderStatus>           _status;
    private readonly IDummy<IReadOnlyList<string>> _tags;
    private readonly IDummy<DateTime>              _placedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public DummyOrder()
        : this(reference: new DummyOrderReference(),
               customer:  DummyValidCustomer(),
               quantity:  DummyValidQuantity(),
               status:    Dummy.Enum<OrderStatus>(),
               tags:      Dummy.ListOf(Dummy.String().NonEmpty()),
               placedAt:  Dummy.DateTime()) { }

    private static IDummy<Customer> DummyValidCustomer() {
        // TODO(dum): 'Customer customer' may be guarded by something dum could not read (§9).
        //   This is dum's best generator for the type; verify it honours the real invariant,
        //   or replace it, then delete the line below.
        _ = TODO_verify_the_generator_for_customer;

        return new DummyCustomer();
    }

    private static IDummy<int> DummyValidQuantity() {
        return Dummy.Int32().Positive();
    }

    private DummyOrder(IDummy<OrderReference>        reference,
                       IDummy<Customer>              customer,
                       IDummy<int>                   quantity,
                       IDummy<OrderStatus>           status,
                       IDummy<IReadOnlyList<string>> tags,
                       IDummy<DateTime>              placedAt) {
        _reference = reference;
        _customer  = customer;
        _quantity  = quantity;
        _status    = status;
        _tags      = tags;
        _placedAt  = placedAt;
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public DummyOrder WithReference(OrderReference value) {
        return WithReference(new FixedValue<OrderReference>(value));
    }

    /// <summary>Draws <c>reference</c> from <paramref name="generator" />.</summary>
    public DummyOrder WithReference(IDummy<OrderReference> generator) {
        return new DummyOrder(generator, _customer, _quantity, _status, _tags, _placedAt);
    }

    /// <summary>Pins <c>customer</c> to a fixed value.</summary>
    public DummyOrder WithCustomer(Customer value) {
        return WithCustomer(new FixedValue<Customer>(value));
    }

    /// <summary>Draws <c>customer</c> from <paramref name="generator" />.</summary>
    public DummyOrder WithCustomer(IDummy<Customer> generator) {
        return new DummyOrder(_reference, generator, _quantity, _status, _tags, _placedAt);
    }

    /// <summary>Pins <c>quantity</c> to a fixed value.</summary>
    public DummyOrder WithQuantity(int value) {
        return WithQuantity(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>quantity</c> from <paramref name="generator" />.</summary>
    public DummyOrder WithQuantity(IDummy<int> generator) {
        return new DummyOrder(_reference, _customer, generator, _status, _tags, _placedAt);
    }

    /// <summary>Pins <c>status</c> to a fixed value.</summary>
    public DummyOrder WithStatus(OrderStatus value) {
        return WithStatus(new FixedValue<OrderStatus>(value));
    }

    /// <summary>Draws <c>status</c> from <paramref name="generator" />.</summary>
    public DummyOrder WithStatus(IDummy<OrderStatus> generator) {
        return new DummyOrder(_reference, _customer, _quantity, generator, _tags, _placedAt);
    }

    /// <summary>Pins <c>tags</c> to a fixed value.</summary>
    public DummyOrder WithTags(IReadOnlyList<string> value) {
        return WithTags(new FixedValue<IReadOnlyList<string>>(value));
    }

    /// <summary>Draws <c>tags</c> from <paramref name="generator" />.</summary>
    public DummyOrder WithTags(IDummy<IReadOnlyList<string>> generator) {
        return new DummyOrder(_reference, _customer, _quantity, _status, generator, _placedAt);
    }

    /// <summary>Pins <c>placedAt</c> to a fixed value.</summary>
    public DummyOrder WithPlacedAt(DateTime value) {
        return WithPlacedAt(new FixedValue<DateTime>(value));
    }

    /// <summary>Draws <c>placedAt</c> from <paramref name="generator" />.</summary>
    public DummyOrder WithPlacedAt(IDummy<DateTime> generator) {
        return new DummyOrder(_reference, _customer, _quantity, _status, _tags, generator);
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

    private sealed class FixedValue<TValue> : IDummy<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
