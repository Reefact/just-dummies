using JustDummies;

namespace Shop.Domain.Tests;

/// <summary>Test data builders: they pin only what a test is about, everything else stays arbitrary.</summary>
internal static class OrderBuilders {

    public static OrderLine LineOf(decimal unitPrice, int units, string currency) {
        return Any.OrderLine()
                  .WithUnitPrice(new Money(unitPrice, currency))
                  .WithQuantity(new Quantity(units))
                  .Generate();
    }

    /// <summary>An order whose single line brings the given subtotal, in the given currency.</summary>
    public static AnyOrder OrderWithSubtotal(decimal subtotal, string currency) {
        return Any.Order().WithLines(new List<OrderLine> { LineOf(subtotal, 1, currency) });
    }

    public static Order OrderIn(OrderStatus status) {
        Order order = Any.Order().Generate();
        switch (status) {
            case OrderStatus.Placed:
                break;
            case OrderStatus.Paid:
                order.MarkPaid();
                break;
            case OrderStatus.Shipped:
                order.MarkPaid();
                order.Ship();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return order;
    }

}
