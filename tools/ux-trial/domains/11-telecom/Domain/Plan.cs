namespace Telecom.Domain;

/// <summary>A tariff: a monthly fee, a data allowance, and a price per started gigabyte beyond it.</summary>
public sealed class Plan {

    public Plan(string name, PlanTier tier, decimal monthlyFee, int dataAllowanceGb, decimal overagePerGb) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, 40);
        ArgumentOutOfRangeException.ThrowIfNegative(monthlyFee);
        if (decimal.Round(monthlyFee, 2) != monthlyFee) {
            throw new ArgumentException("A fee is in whole cents.", nameof(monthlyFee));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dataAllowanceGb);
        ArgumentOutOfRangeException.ThrowIfNegative(overagePerGb);

        Tier = tier switch {
            PlanTier.Prepaid or PlanTier.Postpaid or PlanTier.Unlimited => tier,
            _ => throw new ArgumentOutOfRangeException(nameof(tier))
        };
        Name            = name;
        MonthlyFee      = monthlyFee;
        DataAllowanceGb = dataAllowanceGb;
        OveragePerGb    = overagePerGb;
    }

    public string   Name            { get; }
    public PlanTier Tier            { get; }
    public decimal  MonthlyFee      { get; }
    public int      DataAllowanceGb { get; }
    public decimal  OveragePerGb    { get; }

    /// <summary>The overage charged for a month's data: every started gigabyte beyond the allowance, except on Unlimited.</summary>
    public decimal OverageFor(long usedMegabytes) {
        if (Tier == PlanTier.Unlimited) {
            return 0m;
        }
        long allowanceMb = DataAllowanceGb * 1024L;
        if (usedMegabytes <= allowanceMb) {
            return 0m;
        }
        long startedGb = (usedMegabytes - allowanceMb + 1023) / 1024;
        return startedGb * OveragePerGb;
    }

}
