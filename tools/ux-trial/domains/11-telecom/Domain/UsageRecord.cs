namespace Telecom.Domain;

public enum UsageKind {
    Data,
    Voice,
    Sms
}

/// <summary>One metered event: megabytes for data, seconds for voice, a count of one for an SMS.</summary>
public sealed class UsageRecord {

    public UsageRecord(DateTime occurredAt, UsageKind kind, long quantity) {
        if (occurredAt.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("Usage is metered in UTC.", nameof(occurredAt));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (kind == UsageKind.Sms && quantity != 1) {
            throw new ArgumentException("An SMS record counts exactly one message.", nameof(quantity));
        }

        OccurredAt = occurredAt;
        Kind       = kind;
        Quantity   = quantity;
    }

    public DateTime  OccurredAt { get; }
    public UsageKind Kind       { get; }
    public long      Quantity   { get; }

}
