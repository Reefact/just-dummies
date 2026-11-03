using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class LiquidityProfileTests {

    private static BucketGap Gap(TimeBucket bucket, decimal inflows, decimal outflows, decimal cumulative) {
        return Any.BucketGap().WithBucket(bucket).WithInflows(inflows).WithOutflows(outflows).WithCumulativeGap(cumulative).Generate();
    }

    /// <summary>Cumulative gaps 100, -50, -200, -150 on the standard ladder.</summary>
    private static LiquidityProfile ProfileRunningShortOnDay30() {
        IReadOnlyList<TimeBucket> ladder = Fixtures.StandardLadder();
        IReadOnlyList<BucketGap> gaps = [
            Gap(ladder[0], 150m, 50m, 100m),
            Gap(ladder[1], 0m, 150m, -50m),
            Gap(ladder[2], 0m, 150m, -200m),
            Gap(ladder[3], 50m, 0m, -150m),
        ];

        return Any.LiquidityProfile().WithGaps(gaps).Generate();
    }

    private static LiquidityProfile ProfileNeverShort() {
        IReadOnlyList<TimeBucket> ladder = Fixtures.StandardLadder();
        IReadOnlyList<BucketGap> gaps = [
            Gap(ladder[0], 100m, 0m, 100m),
            Gap(ladder[1], 0m, 30m, 70m),
            Gap(ladder[2], 0m, 70m, 0m),
            Gap(ladder[3], 5m, 0m, 5m),
        ];

        return Any.LiquidityProfile().WithGaps(gaps).Generate();
    }

    [Fact]
    public void A_profile_keeps_its_scenario_name_currency_and_gaps() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        IReadOnlyList<BucketGap> gaps = new AnyBucketGaps().Generate();

        // Act
        LiquidityProfile profile = new LiquidityProfile("Combined", anyCurrency, gaps);

        // Assert
        Assert.Equal("Combined", profile.ScenarioName);
        Assert.Same(anyCurrency, profile.Currency);
        Assert.Equal(gaps, profile.Gaps);
    }

    [Fact]
    public void The_totals_add_up_the_inflows_and_outflows_of_every_bucket() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act & Assert
        Assert.Equal(200m, profile.TotalInflows);
        Assert.Equal(350m, profile.TotalOutflows);
    }

    [Fact]
    public void The_net_position_is_the_cumulative_gap_of_the_last_bucket() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act & Assert
        Assert.Equal(-150m, profile.NetPosition);
    }

    [Fact]
    public void The_first_shortfall_is_the_first_bucket_with_a_negative_cumulative_gap() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act
        BucketGap? shortfall = profile.FirstShortfall;

        // Assert
        Assert.NotNull(shortfall);
        Assert.Equal("30-90d", shortfall.Bucket.Label);
        Assert.Equal(-50m, shortfall.CumulativeGap);
    }

    [Fact]
    public void The_survival_horizon_is_the_start_day_of_the_first_shortfall_bucket() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act & Assert
        Assert.Equal(30, profile.SurvivalDays);
    }

    [Fact]
    public void A_profile_that_never_runs_short_has_no_first_shortfall_and_no_survival_horizon() {
        // Arrange: a cumulative gap of exactly zero is not a shortfall
        LiquidityProfile profile = ProfileNeverShort();

        // Act & Assert
        Assert.Null(profile.FirstShortfall);
        Assert.Null(profile.SurvivalDays);
    }

    [Fact]
    public void A_shortfall_in_the_very_first_bucket_gives_a_survival_horizon_of_zero_days() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = Fixtures.StandardLadder();
        IReadOnlyList<BucketGap> gaps = [
            Gap(ladder[0], 0m, 1m, -1m),
            Gap(ladder[1], 10m, 0m, 9m),
            Gap(ladder[2], 0m, 0m, 9m),
            Gap(ladder[3], 0m, 0m, 9m),
        ];
        LiquidityProfile profile = Any.LiquidityProfile().WithGaps(gaps).Generate();

        // Act & Assert
        Assert.Equal(0, profile.SurvivalDays);
    }

    [Fact]
    public void A_shortfall_that_recovers_later_still_counts() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = Fixtures.StandardLadder();
        IReadOnlyList<BucketGap> gaps = [
            Gap(ladder[0], 10m, 0m, 10m),
            Gap(ladder[1], 0m, 20m, -10m),
            Gap(ladder[2], 50m, 0m, 40m),
            Gap(ladder[3], 0m, 0m, 40m),
        ];
        LiquidityProfile profile = Any.LiquidityProfile().WithGaps(gaps).Generate();

        // Act & Assert
        Assert.Equal(30, profile.SurvivalDays);
        Assert.Equal(40m, profile.NetPosition);
    }

    [Fact]
    public void A_profile_within_its_limit_over_the_horizon_is_accepted() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act & Assert: only the first bucket (start 0) begins before day 30
        profile.EnsureWithinLimit(30, 0m);
    }

    [Fact]
    public void A_bucket_starting_before_the_horizon_that_breaches_the_limit_is_refused_naming_scenario_bucket_and_figures() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => profile.EnsureWithinLimit(31, -40m));

        // Assert
        Assert.Contains(profile.ScenarioName, refusal.Message);
        Assert.Contains("30-90d", refusal.Message);
        Assert.Contains("-50", refusal.Message);
        Assert.Contains("-40", refusal.Message);
    }

    [Fact]
    public void A_bucket_starting_on_the_horizon_is_not_checked() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act & Assert: the breaching bucket starts on day 30, which is not before the horizon of 30
        profile.EnsureWithinLimit(30, -1_000m);
        profile.EnsureWithinLimit(30, -40m);
    }

    [Fact]
    public void A_cumulative_gap_equal_to_the_limit_is_not_a_breach() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act & Assert
        profile.EnsureWithinLimit(100, -200m);
    }

    [Fact]
    public void A_cumulative_gap_just_below_the_limit_is_a_breach() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => profile.EnsureWithinLimit(100, -199.99m));
    }

    [Fact]
    public void The_first_breach_is_the_one_reported() {
        // Arrange
        LiquidityProfile profile = ProfileRunningShortOnDay30();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => profile.EnsureWithinLimit(10_000, 0m));

        // Assert
        Assert.Contains("30-90d", refusal.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_horizon_shorter_than_one_day_is_refused(int horizon) {
        // Arrange
        LiquidityProfile profile = ProfileNeverShort();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => profile.EnsureWithinLimit(horizon, 0m));

        // Assert
        Assert.Equal("horizonDays", refusal.ParamName);
    }

    [Fact]
    public void A_profile_whose_cumulative_gaps_are_not_the_running_sum_is_refused_naming_the_bucket() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = Fixtures.StandardLadder();
        IReadOnlyList<BucketGap> gaps = [
            Gap(ladder[0], 100m, 0m, 100m),
            Gap(ladder[1], 0m, 30m, 75m), // should be 70
            Gap(ladder[2], 0m, 0m, 75m),
            Gap(ladder[3], 0m, 0m, 75m),
        ];
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LiquidityProfile("S", anyCurrency, gaps));

        // Assert
        Assert.Contains("'30-90d'", refusal.Message);
        Assert.Contains("75", refusal.Message);
        Assert.Contains("70", refusal.Message);
    }

    [Fact]
    public void Gaps_that_do_not_form_a_ladder_are_refused() {
        // Arrange
        TimeBucket[] notALadder = [new TimeBucket("a", 0, 10), new TimeBucket("b", 15, null)];
        IReadOnlyList<BucketGap> gaps = [Gap(notALadder[0], 1m, 0m, 1m), Gap(notALadder[1], 1m, 0m, 2m)];
        Currency anyCurrency = Any.Currency().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LiquidityProfile("S", anyCurrency, gaps));
    }

    [Fact]
    public void An_empty_list_of_gaps_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LiquidityProfile("S", anyCurrency, []));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void A_blank_scenario_name_is_refused(string? blankName) {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        IReadOnlyList<BucketGap> gaps = new AnyBucketGaps().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LiquidityProfile(blankName!, anyCurrency, gaps));

        // Assert
        Assert.Equal("scenarioName", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_or_gap_list_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        IReadOnlyList<BucketGap> gaps = new AnyBucketGaps().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LiquidityProfile("S", null!, gaps));
        Assert.Throws<ArgumentNullException>(() => new LiquidityProfile("S", anyCurrency, null!));
    }

    [Fact]
    public void The_profile_does_not_follow_later_changes_to_the_list_it_was_built_from() {
        // Arrange
        Currency       anyCurrency = Any.Currency().Generate();
        List<BucketGap> gaps       = new AnyBucketGaps().OnLadder(Fixtures.StandardLadder()).Generate().ToList();
        LiquidityProfile profile   = new LiquidityProfile("S", anyCurrency, gaps);

        // Act
        gaps.Clear();

        // Assert
        Assert.Equal(4, profile.Gaps.Count);
    }

}
