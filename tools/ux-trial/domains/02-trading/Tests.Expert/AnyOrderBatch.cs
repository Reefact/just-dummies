// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OrderBatch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="OrderBatch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrderBatch : IAny<OrderBatch> {

    private IAny<string>               _reference;
    private IAny<IReadOnlyList<Order>> _orders;

    private AnyPattern     _referenceRecipe;
    private AnyList<Order> _ordersRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrderBatch()
        : this(reference: AnyValidReference(),
               orders:    AnyValidOrders()) { }

    private static AnyPattern AnyValidReference() {
        return Any.StringMatching(@"B-\d{8}-\d{4}");
    }

    private static AnyList<Order> AnyValidOrders() {
        // A batch is for one ticker and holds each order once: the ticker is drawn here, shared by the orders, and the ids
        // are told apart. The list is drawn when this generator is created, so every Generate() of it shares that ticker.
        Ticker ticker = new AnyTicker().Generate();

        return Any.ListOf(new AnyOrder().WithTicker(ticker)).NonEmpty().WithMaxCount(OrderBatch.MaximumSize).DistinctBy(order => order.Id);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyOrderBatch(AnyPattern     reference,
                          AnyList<Order> orders) {
        _reference       = reference;
        _referenceRecipe = reference;
        _orders          = orders;
        _ordersRecipe    = orders;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyOrderBatch(AnyOrderBatch from) {
        _reference       = from._reference;
        _referenceRecipe = from._referenceRecipe;
        _orders          = from._orders;
        _ordersRecipe    = from._ordersRecipe;
    }

    /// <summary>Pins <c>reference</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrderBatch WithReference(string value) {
        AnyOrderBatch copy = new AnyOrderBatch(this);

        copy._reference = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reference</c> is drawn from — <c>AnyValidReference()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrderBatch WithReference(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrderBatch copy = new AnyOrderBatch(this);

        copy._referenceRecipe = configure(_referenceRecipe);
        copy._reference       = copy._referenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>orders</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrderBatch WithOrders(IReadOnlyList<Order> value) {
        AnyOrderBatch copy = new AnyOrderBatch(this);

        copy._orders = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>orders</c> is drawn from — <c>AnyValidOrders()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;Order&gt;, AnyList&lt;Order&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrderBatch WithOrders(Func<AnyList<Order>, AnyList<Order>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrderBatch copy = new AnyOrderBatch(this);

        copy._ordersRecipe = configure(_ordersRecipe);
        copy._orders       = copy._ordersRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="OrderBatch" />.</summary>
    public OrderBatch Generate() {
        return new OrderBatch(_reference.Generate(),
                              _orders.Generate());
    }

}
