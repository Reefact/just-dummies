using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Regular;

public sealed class ScenarioAndBucketTests {

    [Fact]
    public void The_baseline_is_not_stressed() {
        // Act
        bool stressed = Scenario.Baseline.IsStressed;

        // Assert
        Assert.False(stressed);
    }

    [Fact]
    public void A_scenario_amplifying_outflows_is_stressed() {
        // Arrange
        Scenario anyAmplifyingScenario = Arbitrary.Scenario().WithOutflowMultiplier(m => m.GreaterThan(1m)).Generate();

        // Act
        bool stressed = anyAmplifyingScenario.IsStressed;

        // Assert
        Assert.True(stressed);
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(29, true)]
    [InlineData(30, false)]
    public void A_bucket_contains_its_start_day_but_not_its_end_day(int day, bool expected) {
        // Arrange
        TimeBucket anyWeekToMonth = Arbitrary.TimeBucket().WithStartDay(7).WithEndDay(30).Generate();

        // Act
        bool contains = anyWeekToMonth.Contains(day);

        // Assert
        Assert.Equal(expected, contains);
    }

    [Fact]
    public void A_profile_whose_cumulative_gap_does_not_add_up_is_refused() {
        // Arrange
        Currency anyCurrency = Arbitrary.Currency().Generate();
        BucketGap[] inconsistentGaps = [
            new BucketGap(new TimeBucket("0-7d", 0, 7), inflows: 100m, outflows: 0m, cumulativeGap: 100m),
            new BucketGap(new TimeBucket("7d+", 7, null), inflows: 0m, outflows: 50m, cumulativeGap: -50m),
        ];

        // Act
        Exception? refusal = Record.Exception(() => new LiquidityProfile("Baseline", anyCurrency, inconsistentGaps));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
