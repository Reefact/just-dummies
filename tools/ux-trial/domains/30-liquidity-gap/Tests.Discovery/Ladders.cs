using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

/// <summary>The bucket ladder the treasury reports on. Its boundaries are load-bearing in every projection test.</summary>
internal static class Ladders {

    public static IReadOnlyList<TimeBucket> Regulatory() {
        return [
            new TimeBucket("O/N-1W", 0, 7),
            new TimeBucket("1W-1M", 7, 30),
            new TimeBucket("1M-3M", 30, 90),
            new TimeBucket("3M-1Y", 90, 365),
            new TimeBucket(">1Y", 365, null),
        ];
    }

}
