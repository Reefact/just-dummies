using JustDummies;

using Trading.Domain;

namespace Trading.Domain.Tests.Expert;

/// <summary>
///     The nine arguments of an <see cref="Order" />, valid as drawn, so a refusal test changes the one argument it is about:
///     <c>OrderDraft.Valid() with { Id = Guid.Empty }</c>.
/// </summary>
public sealed record OrderDraft(Guid Id, Ticker Ticker, Side Side, Quantity Quantity, Price Limit, Price? Stop, TickSize Tick, TimeInForce TimeInForce, DateTime PlacedAt) {

    public static OrderDraft Valid() {
        Order order = Any.Order().Generate();

        return new OrderDraft(order.Id, order.Ticker, order.Side, order.Quantity, order.Limit, order.Stop, order.Tick, order.TimeInForce, order.PlacedAt);
    }

    public Order Place() {
        return new Order(Id, Ticker, Side, Quantity, Limit, Stop, Tick, TimeInForce, PlacedAt);
    }

}
