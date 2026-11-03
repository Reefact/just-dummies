using JustDummies.Xunit;

using LiquidityGap.Domain;

[assembly: Reproducible]

namespace LiquidityGap.Tests.Regular;

internal static class Fixtures {

    /// <summary>The ladder most treasury reports use: up to a month, to a quarter, to a year, beyond.</summary>
    public static IReadOnlyList<TimeBucket> StandardLadder() {
        return [
            new TimeBucket("0-30d", 0, 30),
            new TimeBucket("30-90d", 30, 90),
            new TimeBucket("90-365d", 90, 365),
            new TimeBucket("1y+", 365, null),
        ];
    }

}
