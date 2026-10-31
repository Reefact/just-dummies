namespace LiquidityGap.Domain;

/// <summary>One row of a liquidity profile: what comes in and goes out in a bucket, and the running gap.</summary>
public sealed class BucketGap
{
    public BucketGap(TimeBucket bucket, decimal inflows, decimal outflows, decimal cumulativeGap)
    {
        if (inflows < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(inflows), inflows, "Inflows are never negative.");
        }

        if (outflows < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(outflows), outflows, "Outflows are never negative.");
        }

        Bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
        Inflows = inflows;
        Outflows = outflows;
        CumulativeGap = cumulativeGap;
    }

    public TimeBucket Bucket { get; }

    public decimal Inflows { get; }

    public decimal Outflows { get; }

    /// <summary>The net position of this bucket alone.</summary>
    public decimal Gap => Inflows - Outflows;

    /// <summary>The sum of the gaps of this bucket and of every earlier one.</summary>
    public decimal CumulativeGap { get; }
}
