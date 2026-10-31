namespace LiquidityGap.Domain;

/// <summary>
/// How a non-maturity deposit balance is assumed to run off: a volatile part leaves in one go at a
/// short horizon, a stable (core) part amortises in equal monthly instalments over a longer tenor.
/// </summary>
public sealed class BehaviouralAssumption
{
    public BehaviouralAssumption(decimal stableShare, int volatileRunOffDays, int coreMaturityMonths)
    {
        if (stableShare is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(stableShare), stableShare, "The stable share is a fraction between 0 and 1.");
        }

        if (volatileRunOffDays is < 1 or > 365)
        {
            throw new ArgumentOutOfRangeException(nameof(volatileRunOffDays), volatileRunOffDays, "The volatile run-off horizon is between 1 and 365 days.");
        }

        if (coreMaturityMonths is < 1 or > 120)
        {
            throw new ArgumentOutOfRangeException(nameof(coreMaturityMonths), coreMaturityMonths, "The core maturity is between 1 and 120 months.");
        }

        if (volatileRunOffDays >= coreMaturityMonths * 30)
        {
            throw new ArgumentException(
                $"The volatile run-off horizon ({volatileRunOffDays} days) must be shorter than the core maturity ({coreMaturityMonths} months).",
                nameof(volatileRunOffDays));
        }

        StableShare = stableShare;
        VolatileRunOffDays = volatileRunOffDays;
        CoreMaturityMonths = coreMaturityMonths;
    }

    public decimal StableShare { get; }

    public int VolatileRunOffDays { get; }

    public int CoreMaturityMonths { get; }
}
