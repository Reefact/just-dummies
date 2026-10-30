namespace Trading.Domain;

public enum Side {
    Buy,
    Sell
}

public enum TimeInForce {
    Day,
    GoodTillCancelled,
    ImmediateOrCancel,
    FillOrKill
}

public enum OrderStatus {
    Open,
    PartiallyFilled,
    Filled,
    Cancelled
}
