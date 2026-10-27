namespace Telecom.Domain;

/// <summary>A billing period, start inclusive and end exclusive, at most 31 days.</summary>
public sealed class BillingPeriod {

    public BillingPeriod(DateOnly start, DateOnly end) {
        if (start >= end) {
            throw new ArgumentException($"The period must end after it starts ({start} >= {end}).", nameof(end));
        }
        if (end.DayNumber - start.DayNumber > 31) {
            throw new ArgumentException("A billing period lasts at most 31 days.", nameof(end));
        }

        Start = start;
        End   = end;
    }

    public DateOnly Start { get; }
    public DateOnly End   { get; }

    public bool Contains(DateTime instant) {
        DateOnly day = DateOnly.FromDateTime(instant);
        return day >= Start && day < End;
    }

}
