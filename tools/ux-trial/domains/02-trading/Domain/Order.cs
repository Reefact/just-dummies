namespace Trading.Domain;

/// <summary>A limit or stop-limit order resting in the book.</summary>
public sealed class Order {

    public Order(Guid id, Ticker ticker, Side side, Quantity quantity, Price limit, Price? stop, TickSize tick, TimeInForce timeInForce, DateTime placedAt) {
        if (id == Guid.Empty) {
            throw new ArgumentException("An order needs an identifier.", nameof(id));
        }
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(quantity);
        ArgumentNullException.ThrowIfNull(limit);
        ArgumentNullException.ThrowIfNull(tick);
        if (!tick.Admits(limit.Amount)) {
            throw new ArgumentException($"The limit {limit} is not a multiple of the tick {tick.Increment}.", nameof(limit));
        }
        if (stop is not null) {
            if (!tick.Admits(stop.Amount)) {
                throw new ArgumentException($"The stop {stop} is not a multiple of the tick {tick.Increment}.", nameof(stop));
            }
            if (stop.CompareTo(limit) >= 0) {
                throw new ArgumentException("A stop price is strictly below the limit price.", nameof(stop));
            }
        }
        if (placedAt.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("An order is timestamped in UTC.", nameof(placedAt));
        }

        Id          = id;
        Ticker      = ticker;
        Side        = side;
        Quantity    = quantity;
        Limit       = limit;
        Stop        = stop;
        Tick        = tick;
        TimeInForce = timeInForce;
        PlacedAt    = placedAt;
        Status      = OrderStatus.Open;
    }

    public Guid        Id          { get; }
    public Ticker      Ticker      { get; }
    public Side        Side        { get; }
    public Quantity    Quantity    { get; }
    public Price       Limit       { get; }
    public Price?      Stop        { get; }
    public TickSize    Tick        { get; }
    public TimeInForce TimeInForce { get; }
    public DateTime    PlacedAt    { get; }
    public OrderStatus Status      { get; private set; }
    public int         FilledShares { get; private set; }

    public int RemainingShares => Quantity.Shares - FilledShares;

    public decimal Notional() {
        return Quantity.Shares * Limit.Amount;
    }

    /// <summary>Records an execution. A fill-or-kill order accepts only a complete fill.</summary>
    public void Fill(int shares) {
        if (Status is OrderStatus.Filled or OrderStatus.Cancelled) {
            throw new InvalidOperationException($"A {Status} order cannot be filled.");
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(shares);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(shares, RemainingShares);
        if (TimeInForce == TimeInForce.FillOrKill && shares != Quantity.Shares) {
            throw new InvalidOperationException("A fill-or-kill order fills completely or not at all.");
        }

        FilledShares += shares;
        Status       =  FilledShares == Quantity.Shares ? OrderStatus.Filled : OrderStatus.PartiallyFilled;
    }

    public void Cancel() {
        Status = Status switch {
            OrderStatus.Open or OrderStatus.PartiallyFilled => OrderStatus.Cancelled,
            _ => throw new InvalidOperationException($"A {Status} order cannot be cancelled.")
        };
    }

    /// <summary>Whether a stop order is triggered by a last-traded price.</summary>
    public bool IsTriggeredBy(decimal lastTraded) {
        if (Stop is null) {
            return true;
        }

        return Side == Side.Buy ? lastTraded >= Stop.Amount : lastTraded <= Stop.Amount;
    }

}
