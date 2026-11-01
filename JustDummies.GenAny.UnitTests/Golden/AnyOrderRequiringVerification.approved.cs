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

    private IAny<OrderReference>        _reference;
    private IAny<Customer>              _customer;
    private IAny<int>                   _quantity;
    private IAny<OrderStatus>           _status;
    private IAny<IReadOnlyList<string>> _tags;
    private IAny<DateTime>              _placedAt;

    private AnyOrderReference    _referenceRecipe;
    private AnyCustomer          _customerRecipe;
    private AnyInt32             _quantityRecipe;
    private AnyEnum<OrderStatus> _statusRecipe;
    private AnyList<string>      _tagsRecipe;
    private AnyDateTime          _placedAtRecipe;

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
        // TODO(any): 'Customer customer' may be guarded by something the scaffolder could not read,
        //   first at Order.cs, line 14: if (!Registry.Knows(customer)) { throw new ArgumentException(nameof(customer)); }
        //   This is the scaffolder's best generator for the type; verify it honours the real invariant,
        //   or replace it, then delete the line below. What the scaffolder reads, and what it cannot:
        //   https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means
        _ = TODO_verify_the_generator_for_customer;

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

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyOrder(AnyOrderReference    reference,
                     AnyCustomer          customer,
                     AnyInt32             quantity,
                     AnyEnum<OrderStatus> status,
                     AnyList<string>      tags,
                     AnyDateTime          placedAt) {
        _reference       = reference;
        _referenceRecipe = reference;
        _customer        = customer;
        _customerRecipe  = customer;
        _quantity        = quantity;
        _quantityRecipe  = quantity;
        _status          = status;
        _statusRecipe    = status;
        _tags            = tags;
        _tagsRecipe      = tags;
        _placedAt        = placedAt;
        _placedAtRecipe  = placedAt;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyOrder(AnyOrder from) {
        _reference       = from._reference;
        _referenceRecipe = from._referenceRecipe;
        _customer        = from._customer;
        _customerRecipe  = from._customerRecipe;
        _quantity        = from._quantity;
        _quantityRecipe  = from._quantityRecipe;
        _status          = from._status;
        _statusRecipe    = from._statusRecipe;
        _tags            = from._tags;
        _tagsRecipe      = from._tagsRecipe;
        _placedAt        = from._placedAt;
        _placedAtRecipe  = from._placedAtRecipe;
    }

    /// <summary>Pins <c>reference</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithReference(OrderReference value) {
        AnyOrder copy = new AnyOrder(this);

        copy._reference = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reference</c> is drawn from — <c>AnyValidReference()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithReference(Func<AnyOrderReference, AnyOrderReference> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._referenceRecipe = configure(_referenceRecipe);
        copy._reference       = copy._referenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>customer</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithCustomer(Customer value) {
        AnyOrder copy = new AnyOrder(this);

        copy._customer = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>customer</c> is drawn from — <c>AnyValidCustomer()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithCustomer(Func<AnyCustomer, AnyCustomer> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._customerRecipe = configure(_customerRecipe);
        copy._customer       = copy._customerRecipe;

        return copy;
    }

    /// <summary>Pins <c>quantity</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithQuantity(int value) {
        AnyOrder copy = new AnyOrder(this);

        copy._quantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quantity</c> is drawn from — <c>AnyValidQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithQuantity(Func<AnyInt32, AnyInt32> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._quantityRecipe = configure(_quantityRecipe);
        copy._quantity       = copy._quantityRecipe;

        return copy;
    }

    /// <summary>Pins <c>status</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithStatus(OrderStatus value) {
        AnyOrder copy = new AnyOrder(this);

        copy._status = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>status</c> is drawn from — <c>AnyValidStatus()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithStatus(Func<AnyEnum<OrderStatus>, AnyEnum<OrderStatus>> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._statusRecipe = configure(_statusRecipe);
        copy._status       = copy._statusRecipe;

        return copy;
    }

    /// <summary>Pins <c>tags</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithTags(IReadOnlyList<string> value) {
        AnyOrder copy = new AnyOrder(this);

        copy._tags = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tags</c> is drawn from — <c>AnyValidTags()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithTags(Func<AnyList<string>, AnyList<string>> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._tagsRecipe = configure(_tagsRecipe);
        copy._tags       = copy._tagsRecipe;

        return copy;
    }

    /// <summary>Pins <c>placedAt</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithPlacedAt(DateTime value) {
        AnyOrder copy = new AnyOrder(this);

        copy._placedAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>placedAt</c> is drawn from — <c>AnyValidPlacedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithPlacedAt(Func<AnyDateTime, AnyDateTime> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._placedAtRecipe = configure(_placedAtRecipe);
        copy._placedAt       = copy._placedAtRecipe;

        return copy;
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

}
