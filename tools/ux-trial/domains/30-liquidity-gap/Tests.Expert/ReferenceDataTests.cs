using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Expert;

public sealed class ReferenceDataTests {

    [Fact]
    public void A_volatile_horizon_as_long_as_the_core_maturity_is_refused() {
        // Arrange
        decimal anyStableShare = Any.Decimal().Between(0m, 1m).WithScale(2).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new BehaviouralAssumption(anyStableShare, volatileRunOffDays: 90, coreMaturityMonths: 3));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_volatile_horizon_one_day_shorter_than_the_core_maturity_is_accepted() {
        // Arrange
        decimal anyStableShare = Any.Decimal().Between(0m, 1m).WithScale(2).Generate();

        // Act
        BehaviouralAssumption assumption = new(anyStableShare, volatileRunOffDays: 89, coreMaturityMonths: 3);

        // Assert
        Assert.Equal(89, assumption.VolatileRunOffDays);
    }

    [Fact]
    public void A_scenario_stressing_only_outflows_is_stressed() {
        // Arrange
        Scenario anyOutflowStress = Any.Scenario()
                                       .WithInflowHaircut(0m)
                                       .WithDepositStableShareHaircut(0m)
                                       .WithOutflowMultiplier(1.25m)
                                       .Generate();

        // Act
        bool stressed = anyOutflowStress.IsStressed;

        // Assert
        Assert.True(stressed);
    }

    [Fact]
    public void The_baseline_scenario_is_not_stressed() {
        // Act
        bool stressed = Scenario.Baseline.IsStressed;

        // Assert
        Assert.False(stressed);
    }

    [Fact]
    public void A_bucket_contains_its_start_day_but_not_its_end_day() {
        // Arrange
        TimeBucket anyMonthTwoAndThree = Any.TimeBucket().WithStartDay(30).WithEndDay(90).Generate();

        // Act
        bool containsStart = anyMonthTwoAndThree.Contains(30);
        bool containsEnd   = anyMonthTwoAndThree.Contains(90);

        // Assert
        Assert.True(containsStart);
        Assert.False(containsEnd);
    }

    [Fact]
    public void A_ladder_with_a_hole_between_two_buckets_is_refused() {
        // Arrange
        AnyTimeBucket anyBucket = Any.TimeBucket();
        IReadOnlyList<TimeBucket> ladderWithAHole = [
            anyBucket.WithStartDay(0).WithEndDay(30).Generate(),
            anyBucket.WithStartDay(31).WithEndDay((int?)null).Generate()
        ];

        // Act
        Exception? refusal = Record.Exception(() => TimeBucket.EnsureLadder(ladderWithAHole));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("not contiguous", refusal.Message);
    }

    [Fact]
    public void A_ladder_whose_last_bucket_is_closed_is_refused() {
        // Arrange
        AnyTimeBucket anyBucket = Any.TimeBucket();
        IReadOnlyList<TimeBucket> closedLadder = [
            anyBucket.WithStartDay(0).WithEndDay(30).Generate(),
            anyBucket.WithStartDay(30).WithEndDay(90).Generate()
        ];

        // Act
        Exception? refusal = Record.Exception(() => TimeBucket.EnsureLadder(closedLadder));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("open-ended", refusal.Message);
    }

}
