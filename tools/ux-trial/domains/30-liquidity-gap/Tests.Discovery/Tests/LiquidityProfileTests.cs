using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class LiquidityProfileTests {

    /// <summary>Gaps over the three-bucket ladder, with a consistent running sum, for the given per-bucket (inflows, outflows).</summary>
    private static IReadOnlyList<BucketGap> Gaps(params (decimal Inflows, decimal Outflows)[] amounts) {
        IReadOnlyList<TimeBucket> ladder     = Ladders.ThreeBuckets();
        List<BucketGap>           gaps       = new();
        decimal                   cumulative = 0m;
        for (int i = 0; i < amounts.Length; i++) {
            cumulative += amounts[i].Inflows - amounts[i].Outflows;
            gaps.Add(Any.BucketGap().WithBucket(ladder[i]).WithInflows(amounts[i].Inflows).WithOutflows(amounts[i].Outflows).WithCumulativeGap(cumulative).Generate());
        }

        return gaps;
    }

    private static LiquidityProfile Profile(params (decimal Inflows, decimal Outflows)[] amounts) {
        return Any.LiquidityProfile().WithGaps(Gaps(amounts)).Generate();
    }

    // ---- construction -------------------------------------------------------------------------

    [Fact]
    public void An_arbitrary_profile_is_a_valid_one() {
        // Act
        LiquidityProfile profile = Any.LiquidityProfile().Generate();

        // Assert
        Assert.NotEmpty(profile.Gaps);
    }

    [Fact]
    public void A_profile_keeps_its_scenario_name_currency_and_gaps() {
        // Arrange
        IReadOnlyList<BucketGap> gaps = Gaps((100m, 40m), (0m, 10m), (5m, 0m));

        // Act
        LiquidityProfile profile = Any.LiquidityProfile().WithScenarioName("Severe").WithCurrency(Ladders.Usd).WithGaps(gaps).Generate();

        // Assert
        Assert.Equal("Severe", profile.ScenarioName);
        Assert.Equal(Ladders.Usd, profile.Currency);
        Assert.Equal(gaps, profile.Gaps);
    }

    [Fact]
    public void A_cumulative_gap_that_is_not_the_running_sum_is_refused_naming_the_bucket() {
        // Arrange: the second bucket should accumulate to 90, not 95
        TimeBucket second = Any.TimeBucket().WithLabel("30-90").WithStartDay(30).WithEndDay(90).Generate();
        List<BucketGap> gaps = new() {
            Any.BucketGap().WithBucket(Ladders.Bucket(0, 30)).WithInflows(100m).WithOutflows(40m).WithCumulativeGap(60m).Generate(),
            Any.BucketGap().WithBucket(second).WithInflows(50m).WithOutflows(20m).WithCumulativeGap(95m).Generate(),
            Any.BucketGap().WithBucket(Ladders.Bucket(90, null)).WithInflows(0m).WithOutflows(0m).WithCumulativeGap(90m).Generate(),
        };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.LiquidityProfile().WithGaps(gaps).Generate());

        // Assert
        Assert.Contains("30-90", refusal.Message);
        Assert.Equal("gaps", refusal.ParamName);
    }

    [Fact]
    public void Gaps_that_do_not_form_a_ladder_are_refused() {
        // Arrange
        List<BucketGap> gaps = new() {
            Any.BucketGap().WithBucket(Ladders.Bucket(0, 30)).WithInflows(0m).WithOutflows(0m).WithCumulativeGap(0m).Generate(),
            Any.BucketGap().WithBucket(Ladders.Bucket(60, null)).WithInflows(0m).WithOutflows(0m).WithCumulativeGap(0m).Generate(),
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.LiquidityProfile().WithGaps(gaps).Generate());
    }

    [Fact]
    public void Gaps_that_are_empty_are_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.LiquidityProfile().WithGaps(new List<BucketGap>()).Generate());
    }

    [Fact]
    public void Missing_gaps_are_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Any.LiquidityProfile().WithGaps((IReadOnlyList<BucketGap>)null!).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_scenario_name_is_refused(string name) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.LiquidityProfile().WithScenarioName(name).Generate());

        // Assert
        Assert.Equal("scenarioName", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Any.LiquidityProfile().WithCurrency((Currency)null!).Generate());

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    // ---- totals -------------------------------------------------------------------------------

    [Fact]
    public void The_totals_add_up_the_buckets_and_the_net_position_is_the_last_cumulative_gap() {
        // Arrange
        LiquidityProfile profile = Profile((100m, 40m), (20m, 80m), (5m, 30m));

        // Assert
        Assert.Equal(125m, profile.TotalInflows);
        Assert.Equal(150m, profile.TotalOutflows);
        Assert.Equal(-25m, profile.NetPosition);
        Assert.Equal(profile.TotalInflows - profile.TotalOutflows, profile.NetPosition);
    }

    // ---- shortfall ----------------------------------------------------------------------------

    [Fact]
    public void The_first_shortfall_is_the_first_bucket_whose_cumulative_gap_is_negative() {
        // Arrange: cumulative 60, -10, -5 -> the shortfall starts in the second bucket
        LiquidityProfile profile = Profile((100m, 40m), (10m, 80m), (5m, 0m));

        // Act
        BucketGap? shortfall = profile.FirstShortfall;

        // Assert
        Assert.NotNull(shortfall);
        Assert.Equal(30, shortfall.Bucket.StartDay);
        Assert.Equal(-10m, shortfall.CumulativeGap);
        Assert.Equal(30, profile.SurvivalDays);
    }

    [Fact]
    public void A_shortfall_in_the_first_bucket_means_zero_days_of_survival() {
        // Arrange
        LiquidityProfile profile = Profile((0m, 1m), (100m, 0m), (0m, 0m));

        // Assert
        Assert.Equal(0, profile.SurvivalDays);
    }

    [Fact]
    public void A_cumulative_gap_of_exactly_zero_is_not_a_shortfall() {
        // Arrange
        LiquidityProfile profile = Profile((100m, 100m), (0m, 0m), (50m, 50m));

        // Assert
        Assert.Null(profile.FirstShortfall);
        Assert.Null(profile.SurvivalDays);
    }

    [Fact]
    public void A_shortfall_that_recovers_is_still_the_first_one() {
        // Arrange: cumulative -10, 90, 90
        LiquidityProfile profile = Profile((0m, 10m), (100m, 0m), (0m, 0m));

        // Assert
        Assert.Equal(0, profile.FirstShortfall!.Bucket.StartDay);
        Assert.Equal(-10m, profile.FirstShortfall.CumulativeGap);
    }

    [Fact]
    public void A_portfolio_that_never_runs_short_survives_indefinitely() {
        // Arrange
        LiquidityProfile profile = Profile((100m, 10m), (0m, 20m), (0m, 30m));

        // Assert
        Assert.Null(profile.FirstShortfall);
        Assert.Null(profile.SurvivalDays);
    }

    // ---- limit --------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void A_limit_horizon_of_less_than_one_day_is_refused(int horizon) {
        // Arrange
        LiquidityProfile profile = Profile((100m, 0m), (0m, 0m), (0m, 0m));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => profile.EnsureWithinLimit(horizon, -100m));

        // Assert
        Assert.Equal("horizonDays", refusal.ParamName);
    }

    [Fact]
    public void A_profile_within_the_limit_over_the_horizon_is_accepted() {
        // Arrange: cumulative 60, 40, -100
        LiquidityProfile profile = Profile((100m, 40m), (0m, 20m), (0m, 140m));

        // Act & Assert
        profile.EnsureWithinLimit(90, 0m);
    }

    [Fact]
    public void A_cumulative_gap_exactly_on_the_limit_is_accepted() {
        // Arrange
        LiquidityProfile profile = Profile((0m, 50m), (0m, 0m), (0m, 0m));

        // Act & Assert
        profile.EnsureWithinLimit(90, -50m);
    }

    [Fact]
    public void A_cumulative_gap_below_the_limit_is_refused_naming_scenario_bucket_and_values() {
        // Arrange
        LiquidityProfile profile = Any.LiquidityProfile().WithScenarioName("Severe").WithGaps(Gaps((0m, 50.01m), (0m, 0m), (0m, 0m))).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => profile.EnsureWithinLimit(90, -50m));

        // Assert
        Assert.Contains("Severe", refusal.Message);
        Assert.Contains("-50.01", refusal.Message);
        Assert.Contains("-50", refusal.Message);
    }

    [Fact]
    public void A_breach_in_a_bucket_starting_on_or_after_the_horizon_is_not_looked_at() {
        // Arrange: only the third bucket (from day 90) is below the limit
        LiquidityProfile profile = Profile((100m, 0m), (0m, 0m), (0m, 500m));

        // Act & Assert
        profile.EnsureWithinLimit(90, 0m);
    }

    [Fact]
    public void A_breach_in_a_bucket_starting_one_day_before_the_horizon_is_refused() {
        // Arrange: the second bucket starts on day 30 and breaches
        LiquidityProfile profile = Profile((100m, 0m), (0m, 500m), (0m, 0m));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => profile.EnsureWithinLimit(31, 0m));
        profile.EnsureWithinLimit(30, 0m);
    }

}
