// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OrderBatch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

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

    private readonly IAny<string>               _reference;
    private readonly IAny<IReadOnlyList<Order>> _orders;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrderBatch()
        : this(reference: AnyValidReference(),
               orders:    AnyValidOrders()) { }

    private static AnyPattern AnyValidReference() {
        // OrderBatch: ^B-\d{8}-\d{4}$.
        return Any.StringMatching(@"^B-\d{8}-\d{4}$");
    }

    private static IAny<IReadOnlyList<Order>> AnyValidOrders() {
        // OrderBatch: 1 to 50 orders, distinct ids, one ticker shared by all. The shared ticker is
        // a relation between elements, so it is drawn first and every order is built on it.
        return new AnyTicker().As(ticker => (IReadOnlyList<Order>)Any.ListOf(new AnyOrder().WithTicker(ticker))
                                                                     .WithCountBetween(1, OrderBatch.MaximumSize)
                                                                     .Generate());
    }

    private AnyOrderBatch(IAny<string>               reference,
                          IAny<IReadOnlyList<Order>> orders) {
        _reference = reference;
        _orders    = orders;
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public AnyOrderBatch WithReference(string value) {
        return new AnyOrderBatch(new FixedValue<string>(value), _orders);
    }

    /// <summary>Draws <c>reference</c> from <c>AnyValidReference()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrderBatch WithReference(Func<AnyPattern, AnyPattern> configure) {
        return new AnyOrderBatch(configure(AnyValidReference()), _orders);
    }

    /// <summary>Pins <c>orders</c> to a fixed value.</summary>
    public AnyOrderBatch WithOrders(IReadOnlyList<Order> value) {
        return new AnyOrderBatch(_reference, new FixedValue<IReadOnlyList<Order>>(value));
    }

    /// <summary>Produces one arbitrary <see cref="OrderBatch" />.</summary>
    public OrderBatch Generate() {
        return new OrderBatch(_reference.Generate(),
                              _orders.Generate());
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
