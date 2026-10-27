namespace Telecom.Domain;

/// <summary>A subscriber's mobile line: a number, the handset it is paired with, a plan, and its metered usage.</summary>
public sealed class MobileLine {

    private readonly List<UsageRecord> _usage = new List<UsageRecord>();

    public MobileLine(Msisdn number, Imei handset, Plan plan, DateOnly activatedOn) {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(handset);
        ArgumentNullException.ThrowIfNull(plan);
        if (activatedOn.Year < 2000) {
            throw new ArgumentOutOfRangeException(nameof(activatedOn), "No line predates the network.");
        }

        Number      = number;
        Handset     = handset;
        Plan        = plan;
        ActivatedOn = activatedOn;
        Status      = LineStatus.Active;
    }

    public Msisdn     Number      { get; }
    public Imei       Handset     { get; private set; }
    public Plan       Plan        { get; private set; }
    public DateOnly   ActivatedOn { get; }
    public LineStatus Status      { get; private set; }

    public IReadOnlyList<UsageRecord> Usage => _usage;

    public void Record(UsageRecord record) {
        ArgumentNullException.ThrowIfNull(record);
        if (Status != LineStatus.Active) {
            throw new InvalidOperationException($"A {Status} line meters no usage.");
        }
        _usage.Add(record);
    }

    public void Suspend() {
        Status = Status switch {
            LineStatus.Active => LineStatus.Suspended,
            _ => throw new InvalidOperationException($"Cannot suspend a {Status} line.")
        };
    }

    public void Resume() {
        Status = Status switch {
            LineStatus.Suspended => LineStatus.Active,
            _ => throw new InvalidOperationException($"Cannot resume a {Status} line.")
        };
    }

    public void Terminate() {
        if (Status == LineStatus.Terminated) {
            throw new InvalidOperationException("The line is already terminated.");
        }
        Status = LineStatus.Terminated;
    }

    public void SwapHandset(Imei handset) {
        ArgumentNullException.ThrowIfNull(handset);
        if (handset.Equals(Handset)) {
            throw new InvalidOperationException("The line is already paired with that handset.");
        }
        Handset = handset;
    }

    public void ChangePlan(Plan plan) {
        ArgumentNullException.ThrowIfNull(plan);
        Plan = plan;
    }

    /// <summary>The monthly fee, plus the data overage over the period; suspended lines still pay the fee.</summary>
    public decimal BillFor(BillingPeriod period) {
        ArgumentNullException.ThrowIfNull(period);
        long dataMegabytes = _usage.Where(record => record.Kind == UsageKind.Data && period.Contains(record.OccurredAt))
                                   .Sum(record => record.Quantity);
        return Plan.MonthlyFee + Plan.OverageFor(dataMegabytes);
    }

}
