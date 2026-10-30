namespace Trading.Domain;

/// <summary>Orders submitted together for one ticker, under a client batch reference ("B-20261027-0001").</summary>
public sealed class OrderBatch {

    public const int MaximumSize = 50;

    public OrderBatch(string reference, IReadOnlyList<Order> orders) {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        if (!System.Text.RegularExpressions.Regex.IsMatch(reference, @"^B-\d{8}-\d{4}$")) {
            throw new ArgumentException($"'{reference}' is not a batch reference.", nameof(reference));
        }
        ArgumentNullException.ThrowIfNull(orders);
        if (orders.Count == 0 || orders.Count > MaximumSize) {
            throw new ArgumentOutOfRangeException(nameof(orders), orders.Count, $"A batch holds 1 to {MaximumSize} orders.");
        }
        if (orders.Select(order => order.Id).Distinct().Count() != orders.Count) {
            throw new ArgumentException("A batch cannot hold the same order twice.", nameof(orders));
        }
        if (orders.Select(order => order.Ticker).Distinct().Count() != 1) {
            throw new ArgumentException("A batch is for a single ticker.", nameof(orders));
        }

        Reference = reference;
        Orders    = orders;
    }

    public string               Reference { get; }
    public IReadOnlyList<Order> Orders    { get; }

    public decimal TotalNotional() {
        return Orders.Sum(order => order.Notional());
    }

    /// <summary>Buy orders by price-time priority: highest limit first, then earliest.</summary>
    public IReadOnlyList<Order> BidsByPriority() {
        return Orders.Where(order => order.Side == Side.Buy)
                     .OrderByDescending(order => order.Limit.Amount)
                     .ThenBy(order => order.PlacedAt)
                     .ToList();
    }

}
