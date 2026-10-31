using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Expert;

/// <summary>The ladder the bucketing tests are about: its boundaries are literals because the assertions read them.</summary>
internal static class Ladders {

    public static IReadOnlyList<TimeBucket> UpTo30Then90Days() {
        return [
            new TimeBucket("0-30d", 0, 30),
            new TimeBucket("30-90d", 30, 90),
            new TimeBucket(">90d", 90, null)
        ];
    }

    public static IReadOnlyList<TimeBucket> UpTo30DaysThenAYear() {
        return [
            new TimeBucket("0-30d", 0, 30),
            new TimeBucket("30d-1y", 30, 365),
            new TimeBucket(">1y", 365, null)
        ];
    }

}
