namespace Trading.Domain;

/// <summary>One trading day of an exchange: its date and continuous-trading hours.</summary>
public sealed class TradingSession {

    public TradingSession(DateOnly date, TimeOnly opensAt, TimeOnly closesAt) {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) {
            throw new ArgumentException("Exchanges do not trade at the weekend.", nameof(date));
        }
        if (opensAt >= closesAt) {
            throw new ArgumentException("A session opens before it closes.", nameof(closesAt));
        }

        Date     = date;
        OpensAt  = opensAt;
        ClosesAt = closesAt;
    }

    public DateOnly Date     { get; }
    public TimeOnly OpensAt  { get; }
    public TimeOnly ClosesAt { get; }

    /// <summary>A day order expires at the session close; others live on.</summary>
    public bool Expires(Order order, TimeOnly now) {
        return order.TimeInForce == TimeInForce.Day && now >= ClosesAt;
    }

    public bool IsOpenAt(TimeOnly time) {
        return time >= OpensAt && time < ClosesAt;
    }

}
