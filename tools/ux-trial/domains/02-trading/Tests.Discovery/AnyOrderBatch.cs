// Scaffolded by `any`, then rewritten by hand: the orders of a batch share one ticker, which the
// scaffold's per-parameter list cannot say.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Trading.Domain;

public sealed partial class AnyOrderBatch : IAny<OrderBatch> {

    private IAny<string>                _reference = Any.StringMatching(@"B-\d{8}-\d{4}");
    private IReadOnlyList<Order>?       _orders;
    private int                         _minCount = 1;
    private int                         _maxCount = OrderBatch.MaximumSize;

    public AnyOrderBatch() { }

    private AnyOrderBatch(AnyOrderBatch from) {
        _reference = from._reference;
        _orders    = from._orders;
        _minCount  = from._minCount;
        _maxCount  = from._maxCount;
    }

    public AnyOrderBatch WithReference(string value) {
        AnyOrderBatch copy = new AnyOrderBatch(this);
        copy._reference = Any.Fixed(value);
        return copy;
    }

    public AnyOrderBatch WithOrders(IReadOnlyList<Order> value) {
        AnyOrderBatch copy = new AnyOrderBatch(this);
        copy._orders = value;
        return copy;
    }

    public AnyOrderBatch WithOrderCount(int count) {
        AnyOrderBatch copy = new AnyOrderBatch(this);
        copy._minCount = count;
        copy._maxCount = count;
        return copy;
    }

    public OrderBatch Generate() {
        IReadOnlyList<Order> orders = _orders ?? DrawOrders();

        return new OrderBatch(_reference.Generate(), orders);
    }

    private IReadOnlyList<Order> DrawOrders() {
        Ticker ticker = new AnyTicker().Generate();

        return Any.ListOf(new AnyOrder().WithTicker(ticker))
                  .WithCountBetween(_minCount, _maxCount)
                  .DistinctBy(order => order.Id)
                  .Generate();
    }

}
