namespace LiquidityGap.Domain.Tests.Expert.Tests;

/// <summary>
/// Projecting a portfolio into a ladder. The portfolio's as-of date and currency are arbitrary: flows are
/// placed a stated number of days after the as-of date, and that offset is what these tests are about.
/// </summary>
public sealed class PortfolioProjectionTests {

    /// <summary>An open portfolio and the currency it reports in, drawn when a test starts rather than when the class is built.</summary>
    private sealed class Book {

        public Book() {
            Currency  = Any.Currency().Generate();
            Portfolio = Any.Portfolio().WithReportingCurrency(Currency).Generate();
        }

        public Currency Currency { get; }

        public Portfolio Portfolio { get; }

        public void AddFlow(string flowId, int daysAfterAsOf, decimal amount, FlowDirection direction) {
            Portfolio.AddFlow(Any.ContractualFlow()
                                 .WithCurrency(Currency)
                                 .WithFlowId(flowId)
                                 .WithValueDate(Portfolio.AsOfDate.AddDays(daysAfterAsOf))
                                 .WithAmount(amount)
                                 .WithDirection(direction)
                                 .Generate());
        }

        public void AddDeposit(decimal balance, BehaviouralAssumption assumption) {
            Portfolio.AddDeposit(Any.NonMaturityDeposit().WithCurrency(Currency).WithBalance(balance).WithAssumption(assumption).Generate());
        }

    }

    private static Scenario Stress(decimal inflowHaircut, decimal outflowMultiplier) {
        return Any.Scenario().WithInflowHaircut(inflowHaircut).WithOutflowMultiplier(outflowMultiplier).WithDepositStableShareHaircut(0m).Generate();
    }

    // ----- under the baseline -----

    [Fact]
    public void Each_flow_lands_in_the_bucket_containing_its_distance_in_days_from_the_as_of_date() {
        // Arrange
        Book book = new Book();
        book.AddFlow("IN", 10, 100.00m, FlowDirection.Inflow);
        book.AddFlow("OUT-1", 45, 40.00m, FlowDirection.Outflow);
        book.AddFlow("OUT-2", 120, 90.00m, FlowDirection.Outflow);

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Scenario.Baseline, TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal([100.00m, 0m, 0m], profile.Gaps.Select(g => g.Inflows));
        Assert.Equal([0m, 40.00m, 90.00m], profile.Gaps.Select(g => g.Outflows));
        Assert.Equal([100.00m, 60.00m, -30.00m], profile.Gaps.Select(g => g.CumulativeGap));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(29, 0)]
    [InlineData(30, 1)]
    [InlineData(89, 1)]
    [InlineData(90, 2)]
    [InlineData(3650, 2)]
    public void A_flow_on_the_edge_of_a_bucket_belongs_to_the_bucket_that_starts_there(int daysAfterAsOf, int expectedBucket) {
        // Arrange
        Book book = new Book();
        book.AddFlow("F", daysAfterAsOf, 10.00m, FlowDirection.Inflow);

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Scenario.Baseline, TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal(10.00m, profile.Gaps[expectedBucket].Inflows);
        Assert.Equal(10.00m, profile.TotalInflows);
    }

    [Fact]
    public void Flows_of_the_same_bucket_and_direction_add_up() {
        // Arrange
        Book book = new Book();
        book.AddFlow("A", 5, 10.25m, FlowDirection.Outflow);
        book.AddFlow("B", 6, 20.50m, FlowDirection.Outflow);
        book.AddFlow("C", 7, 5.00m, FlowDirection.Inflow);

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Scenario.Baseline, TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal(30.75m, profile.Gaps[0].Outflows);
        Assert.Equal(5.00m, profile.Gaps[0].Inflows);
        Assert.Equal(-25.75m, profile.NetPosition);
    }

    [Fact]
    public void An_empty_portfolio_projects_a_flat_profile_that_never_runs_short() {
        // Arrange
        Book book = new Book();
        // Act
        LiquidityProfile profile = book.Portfolio.Project(Scenario.Baseline, TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.All(profile.Gaps, gap => Assert.Equal(0m, gap.CumulativeGap));
        Assert.Equal(3, profile.Gaps.Count);
        Assert.Null(profile.SurvivalDays);
    }

    [Fact]
    public void The_profile_carries_the_scenario_name_the_reporting_currency_and_the_ladders_buckets() {
        // Arrange
        Book book = new Book();
        Scenario severe = Any.Scenario().WithName("Severe").Generate();
        IReadOnlyList<TimeBucket> ladder = TestBuckets.ThreeBucketLadder();

        // Act
        LiquidityProfile profile = book.Portfolio.Project(severe, ladder);

        // Assert
        Assert.Equal("Severe", profile.ScenarioName);
        Assert.Equal(book.Currency, profile.Currency);
        Assert.Equal(ladder, profile.Gaps.Select(g => g.Bucket));
    }

    [Fact]
    public void A_frozen_portfolio_can_still_be_projected() {
        // Arrange
        Book book = new Book();
        book.AddFlow("IN", 10, 100.00m, FlowDirection.Inflow);
        book.Portfolio.Freeze();

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Scenario.Baseline, TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal(100.00m, profile.TotalInflows);
    }

    [Fact]
    public void Projecting_does_not_change_the_portfolio() {
        // Arrange
        Book book = new Book();
        book.AddFlow("IN", 10, 100.00m, FlowDirection.Inflow);
        LiquidityProfile first = book.Portfolio.Project(Stress(0.5m, 2m), TestBuckets.ThreeBucketLadder());

        // Act
        LiquidityProfile second = book.Portfolio.Project(Stress(0.5m, 2m), TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal(first.TotalInflows, second.TotalInflows);
        Assert.Single(book.Portfolio.ContractualFlows);
    }

    // ----- under stress -----

    [Fact]
    public void An_inflow_haircut_cuts_the_inflows_and_leaves_the_outflows() {
        // Arrange
        Book book = new Book();
        book.AddFlow("IN", 10, 100.00m, FlowDirection.Inflow);
        book.AddFlow("OUT", 10, 40.00m, FlowDirection.Outflow);

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Stress(0.25m, 1m), TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal(75.00m, profile.TotalInflows);
        Assert.Equal(40.00m, profile.TotalOutflows);
    }

    [Fact]
    public void An_outflow_multiplier_scales_the_contractual_outflows_and_leaves_the_inflows() {
        // Arrange
        Book book = new Book();
        book.AddFlow("IN", 10, 100.00m, FlowDirection.Inflow);
        book.AddFlow("OUT", 10, 40.00m, FlowDirection.Outflow);

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Stress(0m, 1.5m), TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal(100.00m, profile.TotalInflows);
        Assert.Equal(60.00m, profile.TotalOutflows);
    }

    [Fact]
    public void A_stressed_amount_is_rounded_to_the_cent() {
        // Arrange: 33.33 * 0.99 = 32.9967 and 33.33 * 1.01 = 33.6633
        Book book = new Book();
        book.AddFlow("IN", 10, 33.33m, FlowDirection.Inflow);
        book.AddFlow("OUT", 10, 33.33m, FlowDirection.Outflow);

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Stress(0.01m, 1.01m), TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal(33.00m, profile.TotalInflows);
        Assert.Equal(33.66m, profile.TotalOutflows);
    }

    [Fact]
    public void A_deposit_is_spread_into_behavioural_outflows_over_the_ladder() {
        // Arrange: 400 leaves after 10 days; 600 amortises in 200 a month over three months
        Book book = new Book();
        book.AddDeposit(1000.00m, new BehaviouralAssumption(0.6m, 10, 3));

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Scenario.Baseline, TestBuckets.MonthlyLadder());

        // Assert
        Assert.Equal([400.00m, 200.00m, 400.00m], profile.Gaps.Select(g => g.Outflows));
        Assert.Equal(0m, profile.TotalInflows);
        Assert.Equal(-1000.00m, profile.NetPosition);
    }

    [Fact]
    public void The_outflow_multiplier_does_not_scale_the_behavioural_outflows_of_a_deposit() {
        // Arrange
        Book book = new Book();
        book.AddDeposit(1000.00m, new BehaviouralAssumption(0.6m, 10, 3));

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Stress(0m, 3m), TestBuckets.MonthlyLadder());

        // Assert
        Assert.Equal(1000.00m, profile.TotalOutflows);
    }

    [Fact]
    public void Eroding_the_stable_share_of_deposits_pulls_their_outflows_into_the_first_bucket() {
        // Arrange
        Book book = new Book();
        book.AddDeposit(1000.00m, new BehaviouralAssumption(0.6m, 10, 3));
        Scenario run = Any.Scenario().WithDepositStableShareHaircut(1m).Generate();

        // Act
        LiquidityProfile profile = book.Portfolio.Project(run, TestBuckets.MonthlyLadder());

        // Assert
        Assert.Equal([1000.00m, 0m, 0m], profile.Gaps.Select(g => g.Outflows));
    }

    [Fact]
    public void A_scheduled_payment_and_a_deposit_are_projected_together() {
        // Arrange: the schedule's single payment is 20 days after the as-of date (a weekday or not, Unadjusted)
        Book book = new Book();
        book.Portfolio.AddSchedule(Any.CashFlowSchedule()
                                  .WithCurrency(book.Currency)
                                  .WithFirstPaymentDate(book.Portfolio.AsOfDate.AddDays(20))
                                  .WithNumberOfPayments(1)
                                  .WithAmountPerPayment(70.00m)
                                  .WithDirection(FlowDirection.Inflow)
                                  .WithConvention(BusinessDayConvention.Unadjusted)
                                  .Generate());
        book.AddDeposit(50.00m, new BehaviouralAssumption(0m, 10, 3));

        // Act
        LiquidityProfile profile = book.Portfolio.Project(Scenario.Baseline, TestBuckets.MonthlyLadder());

        // Assert
        Assert.Equal([-50.00m, 20.00m, 20.00m], profile.Gaps.Select(g => g.CumulativeGap));
    }

    // ----- refusals -----

    [Fact]
    public void A_missing_scenario_is_refused() {
        // Arrange
        Book book = new Book();
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => book.Portfolio.Project(null!, TestBuckets.ThreeBucketLadder()));

        // Assert
        Assert.Equal("scenario", refusal.ParamName);
    }

    [Fact]
    public void A_ladder_that_is_not_one_is_refused_before_anything_is_projected() {
        // Arrange
        Book book = new Book();
        IReadOnlyList<TimeBucket> holey = [TestBuckets.Bucket(0, 30), TestBuckets.Bucket(60, null)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => book.Portfolio.Project(Scenario.Baseline, holey));

        // Assert
        Assert.Equal("buckets", refusal.ParamName);
    }

    // ----- ProjectAll -----

    [Fact]
    public void Projecting_several_scenarios_gives_one_profile_each_in_the_order_given() {
        // Arrange
        Book book = new Book();
        book.AddFlow("IN", 10, 100.00m, FlowDirection.Inflow);
        Scenario mild   = Any.Scenario().WithName("Mild").WithInflowHaircut(0.1m).WithOutflowMultiplier(1m).Generate();
        Scenario severe = Any.Scenario().WithName("Severe").WithInflowHaircut(0.5m).WithOutflowMultiplier(1m).Generate();

        // Act
        IReadOnlyList<LiquidityProfile> profiles = book.Portfolio.ProjectAll([severe, Scenario.Baseline, mild], TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal(["Severe", "Baseline", "Mild"], profiles.Select(p => p.ScenarioName));
        Assert.Equal([50.00m, 100.00m, 90.00m], profiles.Select(p => p.TotalInflows));
    }

    [Fact]
    public void Scenario_names_are_compared_case_sensitively() {
        // Arrange
        Book book = new Book();
        Scenario lower = Any.Scenario().WithName("stress").Generate();
        Scenario upper = Any.Scenario().WithName("STRESS").Generate();

        // Act
        IReadOnlyList<LiquidityProfile> profiles = book.Portfolio.ProjectAll([lower, upper], TestBuckets.ThreeBucketLadder());

        // Assert
        Assert.Equal(2, profiles.Count);
    }

    [Fact]
    public void Two_scenarios_with_the_same_name_are_refused() {
        // Arrange
        Book book = new Book();
        Scenario first  = Any.Scenario().WithName("Stress").Generate();
        Scenario second = Any.Scenario().WithName("Stress").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => book.Portfolio.ProjectAll([first, second], TestBuckets.ThreeBucketLadder()));

        // Assert
        Assert.Equal("scenarios", refusal.ParamName);
    }

    [Fact]
    public void An_empty_list_of_scenarios_is_refused() {
        // Arrange
        Book book = new Book();
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => book.Portfolio.ProjectAll([], TestBuckets.ThreeBucketLadder()));

        // Assert
        Assert.Equal("scenarios", refusal.ParamName);
    }

    [Fact]
    public void A_missing_list_of_scenarios_is_refused() {
        // Arrange
        Book book = new Book();
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => book.Portfolio.ProjectAll(null!, TestBuckets.ThreeBucketLadder()));

        // Assert
        Assert.Equal("scenarios", refusal.ParamName);
    }

}
