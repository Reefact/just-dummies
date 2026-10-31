namespace CarbonLedger.Domain;

/// <summary>One stretch of a credit's ownership history: who held it, from when, until when (open if still held).</summary>
public sealed class HoldingPeriod
{
    public HoldingPeriod(CreditSerial serial, string accountId, DateTimeOffset from, DateTimeOffset? until)
    {
        ArgumentNullException.ThrowIfNull(serial);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        if (until is { } end && end < from)
        {
            throw new ArgumentException("A holding period cannot end before it starts.", nameof(until));
        }

        Serial = serial;
        AccountId = accountId;
        From = from;
        Until = until;
    }

    public CreditSerial Serial { get; }

    public string AccountId { get; }

    public DateTimeOffset From { get; }

    public DateTimeOffset? Until { get; }

    public bool IsCurrent => Until is null;
}
