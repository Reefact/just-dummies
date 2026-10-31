using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Expert;

public sealed class LiquidityProfileTests {

    private static readonly AnyTimeBucket AnyBucket = Any.TimeBucket();

    // 0-30d: +100 (cumulative +100), 30-90d: -300 (cumulative -200), >90d: nothing.
    private static LiquidityProfile ShortFromDay30() {
        return new LiquidityProfile(Any.Scenario().Generate().Name,
                                    Any.Currency().Generate(),
                                    [
                                        new BucketGap(AnyBucket.WithStartDay(0).WithEndDay(30).Generate(), 100m, 0m, 100m),
                                        new BucketGap(AnyBucket.WithStartDay(30).WithEndDay(90).Generate(), 0m, 300m, -200m),
                                        new BucketGap(AnyBucket.WithStartDay(90).WithEndDay((int?)null).Generate(), 0m, 0m, -200m)
                                    ]);
    }

    [Fact]
    public void A_shortfall_starting_at_the_horizon_is_outside_the_limit_check() {
        // Arrange
        LiquidityProfile profile = ShortFromDay30();

        // Act
        Exception? refusal = Record.Exception(() => profile.EnsureWithinLimit(horizonDays: 30, minimumCumulativeGap: 0m));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_shortfall_within_the_horizon_breaches_the_limit() {
        // Arrange
        LiquidityProfile profile = ShortFromDay30();

        // Act
        Exception? refusal = Record.Exception(() => profile.EnsureWithinLimit(horizonDays: 31, minimumCumulativeGap: 0m));

        // Assert
        InvalidOperationException breach = Assert.IsType<InvalidOperationException>(refusal);
        Assert.Contains("-200", breach.Message);
    }

    [Fact]
    public void A_shortfall_above_a_negative_limit_is_tolerated() {
        // Arrange
        LiquidityProfile profile = ShortFromDay30();

        // Act
        Exception? refusal = Record.Exception(() => profile.EnsureWithinLimit(horizonDays: 365, minimumCumulativeGap: -250m));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_profile_whose_cumulative_gap_does_not_add_up_is_refused() {
        // Arrange
        string   anyScenarioName = Any.Scenario().Generate().Name;
        Currency anyCurrency     = Any.Currency().Generate();
        IReadOnlyList<BucketGap> inconsistent = [
            new BucketGap(AnyBucket.WithStartDay(0).WithEndDay(30).Generate(), 100m, 0m, 100m),
            new BucketGap(AnyBucket.WithStartDay(30).WithEndDay((int?)null).Generate(), 0m, 300m, -150m)
        ];

        // Act
        Exception? refusal = Record.Exception(() => new LiquidityProfile(anyScenarioName, anyCurrency, inconsistent));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
