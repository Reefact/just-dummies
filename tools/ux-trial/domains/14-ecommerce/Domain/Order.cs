namespace Shop.Domain;

public enum OrderStatus {

    Placed,
    Paid,
    Shipped,
    Cancelled

}

public enum ShippingMethod {

    Standard,
    Express,
    PickupPoint

}

/// <summary>The order aggregate: non-empty lines in one currency, an address, a discount, and a lifecycle.</summary>
public sealed class Order {

    /// <summary>Above this subtotal (after discount), standard shipping is free.</summary>
    public const decimal FreeShippingThreshold = 50m;

    private readonly List<OrderLine> _lines;

    public Order(Guid id,
                 IReadOnlyList<OrderLine> lines,
                 ShippingAddress shippingAddress,
                 DiscountPercentage discount,
                 ShippingMethod shippingMethod,
                 DateTime placedAt,
                 DateTime deliveryDeadline) {
        if (id == Guid.Empty) {
            throw new ArgumentException("An order needs an identifier.", nameof(id));
        }
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0) {
            throw new ArgumentException("An order has at least one line.", nameof(lines));
        }
        if (lines.Select(line => line.UnitPrice.Currency).Distinct().Count() > 1) {
            throw new ArgumentException("All lines of an order share one currency.", nameof(lines));
        }
        ArgumentNullException.ThrowIfNull(shippingAddress);
        ArgumentNullException.ThrowIfNull(discount);
        if (placedAt >= deliveryDeadline) {
            throw new ArgumentException("The delivery deadline must come after the order is placed.", nameof(deliveryDeadline));
        }

        Id               = id;
        _lines           = lines.ToList();
        ShippingAddress  = shippingAddress;
        Discount         = discount;
        ShippingMethod   = shippingMethod;
        PlacedAt         = placedAt;
        DeliveryDeadline = deliveryDeadline;
        Status           = OrderStatus.Placed;
    }

    public Guid Id { get; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public ShippingAddress ShippingAddress { get; }

    public DiscountPercentage Discount { get; }

    public ShippingMethod ShippingMethod { get; }

    public DateTime PlacedAt { get; }

    public DateTime DeliveryDeadline { get; }

    public OrderStatus Status { get; private set; }

    public string Currency => _lines[0].UnitPrice.Currency;

    public Money Subtotal => _lines.Aggregate(Money.Zero(Currency), (sum, line) => sum.Plus(line.Total));

    public Money DiscountedSubtotal => Subtotal.Less(Discount);

    public Money ShippingFee => ShippingFeePolicy.FeeFor(this);

    public Money Total => DiscountedSubtotal.Plus(ShippingFee);

    public int TotalUnits => _lines.Sum(line => line.Quantity.Units);

    public void MarkPaid() {
        if (Status != OrderStatus.Placed) {
            throw new InvalidOperationException($"Only a placed order can be paid; this one is {Status}.");
        }
        Status = OrderStatus.Paid;
    }

    public void Ship() {
        Status = Status switch {
            OrderStatus.Paid => OrderStatus.Shipped,
            _                => throw new InvalidOperationException($"Only a paid order can ship; this one is {Status}.")
        };
    }

    public void Cancel() {
        if (Status == OrderStatus.Shipped) {
            throw new InvalidOperationException("A shipped order cannot be cancelled.");
        }
        Status = OrderStatus.Cancelled;
    }

}
