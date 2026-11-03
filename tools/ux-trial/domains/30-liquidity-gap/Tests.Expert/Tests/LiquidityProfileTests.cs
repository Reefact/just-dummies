namespace LiquidityGap.Domain.Tests.Expert.Tests;

public sealed class LiquidityProfileTests {

    private static BucketGap Gap(TimeBucket bucket, decimal inflows, decimal outflows, decimal cumulativeGap) {
        return Any.BucketGap().WithBucket(bucket).WithInflows(inflows).WithOutflows(outflows).WithCumulativeGap(cumulativeGap).Generate();
    }

    /// <summary>
    /// Three buckets: [0, 30) +100, [30, 90) -150 (cumulative -50), [90, open) +80 (cumulative +30).
    /// The profile dips below zero in its second bucket and recovers in the third.
    /// </summary>
    private static LiquidityProfile DippingProfile() {
        TimeBucket week = Any.TimeBucket().WithLabel("1M").WithStartDay(0).WithEndDay(30).Generate();
        TimeBucket mid  = Any.TimeBucket().WithLabel("3M").WithStartDay(30).WithEndDay(90).Generate();
        TimeBucket far  = Any.TimeBucket().WithLabel("3M+").WithStartDay(90).WithoutEndDay().Generate();

        return new LiquidityProfile("Baseline", Any.Currency().Generate(), [Gap(week, 100m, 0m, 100m), Gap(mid, 0m, 150m, -50m), Gap(far, 80m, 0m, 30m)]);
    }

    // ----- construction -----

    [Fact]
    public void A_profile_keeps_its_scenario_name_currency_and_gaps() {
        // Arrange
        Currency yen = new Currency("JPY");
        BucketGap onlyGap = Gap(TestBuckets.Bucket(0, null), 10m, 4m, 6m);

        // Act
        LiquidityProfile profile = new LiquidityProfile("Severe", yen, [onlyGap]);

        // Assert
        Assert.Equal("Severe", profile.ScenarioName);
        Assert.Equal(yen, profile.Currency);
        Assert.Same(onlyGap, Assert.Single(profile.Gaps));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_scenario_name_is_refused(string blank) {
        // Arrange
        BucketGap onlyGap = Gap(TestBuckets.Bucket(0, null), 0m, 0m, 0m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LiquidityProfile(blank, Any.Currency().Generate(), [onlyGap]));

        // Assert
        Assert.Equal("scenarioName", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Arrange
        BucketGap onlyGap = Gap(TestBuckets.Bucket(0, null), 0m, 0m, 0m);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LiquidityProfile("Baseline", null!, [onlyGap]));

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void Missing_gaps_are_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LiquidityProfile("Baseline", Any.Currency().Generate(), null!));

        // Assert
        Assert.Equal("gaps", refusal.ParamName);
    }

    [Fact]
    public void A_profile_without_a_single_gap_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LiquidityProfile("Baseline", Any.Currency().Generate(), []));

        // Assert
        Assert.Equal("buckets", refusal.ParamName);
    }

    [Fact]
    public void Gaps_whose_buckets_do_not_form_a_ladder_are_refused() {
        // Arrange
        BucketGap first  = Gap(TestBuckets.Bucket(0, 30), 10m, 0m, 10m);
        BucketGap second = Gap(TestBuckets.Bucket(40, null), 10m, 0m, 20m);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new LiquidityProfile("Baseline", Any.Currency().Generate(), [first, second]));
    }

    [Fact]
    public void A_cumulative_gap_that_is_not_the_running_sum_of_the_gaps_is_refused_naming_its_bucket() {
        // Arrange: the second cumulative gap should be 100 + (0 - 150) = -50
        TimeBucket first  = Any.TimeBucket().WithLabel("1M").WithStartDay(0).WithEndDay(30).Generate();
        TimeBucket second = Any.TimeBucket().WithLabel("3M").WithStartDay(30).WithoutEndDay().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new LiquidityProfile("Baseline", Any.Currency().Generate(), [Gap(first, 100m, 0m, 100m), Gap(second, 0m, 150m, -49m)]));

        // Assert
        Assert.Equal("gaps", refusal.ParamName);
        Assert.Contains("'3M'", refusal.Message);
        Assert.Contains("-49", refusal.Message);
        Assert.Contains("-50", refusal.Message);
    }

    [Fact]
    public void The_cumulative_gap_of_the_first_bucket_is_its_own_gap() {
        // Arrange
        BucketGap lying = Gap(TestBuckets.Bucket(0, null), 10m, 4m, 7m);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new LiquidityProfile("Baseline", Any.Currency().Generate(), [lying]));
    }

    [Fact]
    public void A_profile_is_not_changed_by_a_later_change_to_the_list_it_was_built_from() {
        // Arrange
        List<BucketGap> gaps = [Gap(TestBuckets.Bucket(0, null), 10m, 4m, 6m)];
        LiquidityProfile profile = new LiquidityProfile("Baseline", Any.Currency().Generate(), gaps);

        // Act
        gaps.Clear();

        // Assert
        Assert.Single(profile.Gaps);
    }

    // ----- totals and net position -----

    [Fact]
    public void The_totals_add_up_the_buckets_and_the_net_position_is_the_last_cumulative_gap() {
        // Arrange
        LiquidityProfile profile = DippingProfile();

        // Assert
        Assert.Equal(180m, profile.TotalInflows);
        Assert.Equal(150m, profile.TotalOutflows);
        Assert.Equal(30m, profile.NetPosition);
    }

    // ----- first shortfall -----

    [Fact]
    public void The_first_shortfall_is_the_first_bucket_whose_cumulative_gap_is_negative() {
        // Arrange
        LiquidityProfile profile = DippingProfile();

        // Act
        BucketGap? shortfall = profile.FirstShortfall;

        // Assert
        Assert.NotNull(shortfall);
        Assert.Equal("3M", shortfall.Bucket.Label);
        Assert.Equal(30, profile.SurvivalDays);
    }

    [Fact]
    public void A_bucket_that_loses_money_while_the_cumulative_position_stays_positive_is_not_a_shortfall() {
        // Arrange: the second bucket has a negative gap (-40), the cumulative stays at +60
        TimeBucket first  = TestBuckets.Bucket(0, 30);
        TimeBucket second = TestBuckets.Bucket(30, null);
        LiquidityProfile profile = new LiquidityProfile("Baseline", Any.Currency().Generate(), [Gap(first, 100m, 0m, 100m), Gap(second, 0m, 40m, 60m)]);

        // Assert
        Assert.Null(profile.FirstShortfall);
        Assert.Null(profile.SurvivalDays);
    }

    [Fact]
    public void A_cumulative_gap_of_exactly_zero_is_not_a_shortfall() {
        // Arrange
        BucketGap balanced = Gap(TestBuckets.Bucket(0, null), 50m, 50m, 0m);
        LiquidityProfile profile = new LiquidityProfile("Baseline", Any.Currency().Generate(), [balanced]);

        // Assert
        Assert.Null(profile.FirstShortfall);
        Assert.Null(profile.SurvivalDays);
    }

    [Fact]
    public void A_portfolio_short_from_the_first_bucket_survives_zero_days() {
        // Arrange
        BucketGap deficit = Gap(TestBuckets.Bucket(0, null), 0m, 10m, -10m);
        LiquidityProfile profile = new LiquidityProfile("Baseline", Any.Currency().Generate(), [deficit]);

        // Assert
        Assert.Equal(0, profile.SurvivalDays);
    }

    [Fact]
    public void The_first_shortfall_is_the_earliest_even_when_a_later_bucket_is_deeper() {
        // Arrange
        TimeBucket early = Any.TimeBucket().WithLabel("early").WithStartDay(0).WithEndDay(10).Generate();
        TimeBucket late  = Any.TimeBucket().WithLabel("late").WithStartDay(10).WithoutEndDay().Generate();
        LiquidityProfile profile = new LiquidityProfile("Baseline", Any.Currency().Generate(), [Gap(early, 0m, 1m, -1m), Gap(late, 0m, 99m, -100m)]);

        // Assert
        Assert.Equal("early", profile.FirstShortfall!.Bucket.Label);
        Assert.Equal(0, profile.SurvivalDays);
    }

    // ----- EnsureWithinLimit -----

    [Fact]
    public void A_profile_within_the_limit_over_the_horizon_is_accepted() {
        // Arrange: the cumulative gap bottoms at -50 in the bucket starting on day 30
        LiquidityProfile profile = DippingProfile();

        // Act
        Exception? refusal = Record.Exception(() => profile.EnsureWithinLimit(60, -50m));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_cumulative_gap_just_below_the_limit_is_refused_naming_scenario_bucket_gap_and_limit() {
        // Arrange
        LiquidityProfile profile = DippingProfile();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => profile.EnsureWithinLimit(60, -49.99m));

        // Assert
        Assert.Contains("Scenario", refusal.Message);
        Assert.Contains("'3M'", refusal.Message);
        Assert.Contains("-50", refusal.Message);
        Assert.Contains("-49.99", refusal.Message);
    }

    [Fact]
    public void The_scenario_name_is_part_of_the_refusal() {
        // Arrange
        TimeBucket only = Any.TimeBucket().WithStartDay(0).WithoutEndDay().Generate();
        LiquidityProfile profile = new LiquidityProfile("Severe", Any.Currency().Generate(), [Gap(only, 0m, 10m, -10m)]);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => profile.EnsureWithinLimit(30, 0m));

        // Assert
        Assert.Contains("'Severe'", refusal.Message);
    }

    [Fact]
    public void A_breach_in_a_bucket_starting_at_the_horizon_is_out_of_scope() {
        // Arrange: the deficit sits in the bucket starting on day 30, and the horizon is day 30
        LiquidityProfile profile = DippingProfile();

        // Act
        Exception? refusal = Record.Exception(() => profile.EnsureWithinLimit(30, 0m));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_breach_in_a_bucket_starting_the_day_before_the_horizon_is_in_scope() {
        // Arrange
        LiquidityProfile profile = DippingProfile();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => profile.EnsureWithinLimit(31, 0m));
    }

    [Fact]
    public void A_bucket_that_straddles_the_horizon_is_judged_on_its_cumulative_gap_at_its_end() {
        // Arrange: [30, 90) starts before a horizon of 60 and ends after it
        LiquidityProfile profile = DippingProfile();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => profile.EnsureWithinLimit(60, -10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_horizon_shorter_than_one_day_is_refused(int horizonDays) {
        // Arrange
        LiquidityProfile profile = DippingProfile();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => profile.EnsureWithinLimit(horizonDays, -1000m));

        // Assert
        Assert.Equal("horizonDays", refusal.ParamName);
    }

}
