using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class PortfolioProjectionTests {

    private static readonly DateOnly FixedAsOf = new DateOnly(2025, 6, 30);

    private static Scenario Stress(decimal inflowHaircut, decimal outflowMultiplier, decimal stableShareHaircut) {
        return Any.Scenario()
                  .WithInflowHaircut(inflowHaircut)
                  .WithOutflowMultiplier(outflowMultiplier)
                  .WithDepositStableShareHaircut(stableShareHaircut)
                  .Generate();
    }

    private static void AddFlow(Portfolio portfolio, string id, int dayOffset, decimal amount, FlowDirection direction) {
        portfolio.AddFlow(Any.ContractualFlow()
                             .WithFlowId(id)
                             .WithValueDate(portfolio.AsOfDate.AddDays(dayOffset))
                             .WithAmount(amount)
                             .WithDirection(direction)
                             .WithCurrency(portfolio.ReportingCurrency)
                             .Generate());
    }

    [Fact]
    public void An_empty_portfolio_projects_to_empty_buckets() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Fixtures.StandardLadder());

        // Assert
        Assert.Equal(4, profile.Gaps.Count);
        Assert.All(profile.Gaps, static gap => {
            Assert.Equal(0m, gap.Inflows);
            Assert.Equal(0m, gap.Outflows);
            Assert.Equal(0m, gap.CumulativeGap);
        });
        Assert.Null(profile.FirstShortfall);
    }

    [Fact]
    public void The_profile_carries_the_scenario_name_the_reporting_currency_and_the_ladder() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        Scenario  scenario  = Any.Scenario().WithName("Idiosyncratic").Generate();

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Fixtures.StandardLadder());

        // Assert
        Assert.Equal("Idiosyncratic", profile.ScenarioName);
        Assert.Same(portfolio.ReportingCurrency, profile.Currency);
        Assert.Equal(["0-30d", "30-90d", "90-365d", "1y+"], profile.Gaps.Select(static g => g.Bucket.Label));
    }

    [Fact]
    public void Each_flow_lands_in_the_bucket_containing_its_distance_in_days_from_the_as_of_date() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        AddFlow(portfolio, "in-0", 0, 100m, FlowDirection.Inflow);
        AddFlow(portfolio, "in-29", 29, 200m, FlowDirection.Inflow);
        AddFlow(portfolio, "in-30", 30, 300m, FlowDirection.Inflow);
        AddFlow(portfolio, "out-89", 89, 50m, FlowDirection.Outflow);
        AddFlow(portfolio, "out-90", 90, 70m, FlowDirection.Outflow);
        AddFlow(portfolio, "out-364", 364, 5m, FlowDirection.Outflow);
        AddFlow(portfolio, "out-365", 365, 10m, FlowDirection.Outflow);
        AddFlow(portfolio, "out-9000", 9000, 1m, FlowDirection.Outflow);

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Fixtures.StandardLadder());

        // Assert
        Assert.Equal([300m, 300m, 0m, 0m], profile.Gaps.Select(static g => g.Inflows));
        Assert.Equal([0m, 50m, 75m, 11m], profile.Gaps.Select(static g => g.Outflows));
        Assert.Equal([300m, 550m, 475m, 464m], profile.Gaps.Select(static g => g.CumulativeGap));
    }

    [Fact]
    public void The_bucket_of_a_flow_does_not_depend_on_the_as_of_date_itself() {
        // Arrange
        DateOnly  anyAsOf   = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(anyAsOf).Generate();
        AddFlow(portfolio, "in-30", 30, 300m, FlowDirection.Inflow);

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Fixtures.StandardLadder());

        // Assert
        Assert.Equal([0m, 300m, 0m, 0m], profile.Gaps.Select(static g => g.Inflows));
    }

    [Fact]
    public void An_inflow_haircut_cuts_every_inflow_and_rounds_it_to_the_cent() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        AddFlow(portfolio, "in-1", 1, 100.05m, FlowDirection.Inflow); // * 0.85 = 85.0425
        AddFlow(portfolio, "in-2", 2, 200.00m, FlowDirection.Inflow); // * 0.85 = 170.00
        Scenario scenario = Stress(0.15m, 1m, 0m);

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Fixtures.StandardLadder());

        // Assert
        Assert.Equal(255.04m, profile.Gaps[0].Inflows);
    }

    [Fact]
    public void An_outflow_multiplier_scales_every_contractual_outflow_and_rounds_it_to_the_cent() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        AddFlow(portfolio, "out-1", 1, 33.33m, FlowDirection.Outflow); // * 1.25 = 41.6625
        AddFlow(portfolio, "out-2", 2, 100m, FlowDirection.Outflow);   // * 1.25 = 125
        Scenario scenario = Stress(0m, 1.25m, 0m);

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Fixtures.StandardLadder());

        // Assert
        Assert.Equal(166.66m, profile.Gaps[0].Outflows);
    }

    [Fact]
    public void The_haircut_does_not_touch_outflows_and_the_multiplier_does_not_touch_inflows() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        AddFlow(portfolio, "in-1", 1, 100m, FlowDirection.Inflow);
        AddFlow(portfolio, "out-1", 1, 100m, FlowDirection.Outflow);
        Scenario scenario = Stress(0.5m, 2m, 0m);

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Fixtures.StandardLadder());

        // Assert
        Assert.Equal(50m, profile.Gaps[0].Inflows);
        Assert.Equal(200m, profile.Gaps[0].Outflows);
        Assert.Equal(-150m, profile.Gaps[0].CumulativeGap);
    }

    [Fact]
    public void Deposits_are_spread_into_behavioural_outflows_across_the_ladder() {
        // Arrange: volatile 400 at +30d, core 3 x 200 at 30/07, 30/08, 30/09 (+30, +61, +92 days)
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        portfolio.AddDeposit(Any.NonMaturityDeposit()
                                .WithDepositId("CA-1")
                                .WithBalance(1000.00m)
                                .WithCurrency(portfolio.ReportingCurrency)
                                .WithAssumption(new BehaviouralAssumption(0.6m, 30, 3))
                                .Generate());

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Fixtures.StandardLadder());

        // Assert
        Assert.Equal([0m, 800m, 200m, 0m], profile.Gaps.Select(static g => g.Outflows));
        Assert.Equal(-1000m, profile.NetPosition);
    }

    [Fact]
    public void A_stable_share_haircut_brings_deposit_outflows_forward() {
        // Arrange: effective share 0.6 * (1 - 0.5) = 0.3 -> volatile 700 at +30d, core 3 x 100
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        portfolio.AddDeposit(Any.NonMaturityDeposit()
                                .WithBalance(1000.00m)
                                .WithCurrency(portfolio.ReportingCurrency)
                                .WithAssumption(new BehaviouralAssumption(0.6m, 30, 3))
                                .Generate());
        Scenario scenario = Stress(0m, 1m, 0.5m);

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Fixtures.StandardLadder());

        // Assert
        Assert.Equal([0m, 700m + 100m + 100m, 100m, 0m], profile.Gaps.Select(static g => g.Outflows));
    }

    [Fact]
    public void The_outflow_multiplier_does_not_scale_the_behavioural_outflows_of_deposits() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        portfolio.AddDeposit(Any.NonMaturityDeposit()
                                .WithBalance(1000.00m)
                                .WithCurrency(portfolio.ReportingCurrency)
                                .WithAssumption(new BehaviouralAssumption(0m, 10, 12))
                                .Generate());
        Scenario scenario = Stress(0m, 3m, 0m);

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Fixtures.StandardLadder());

        // Assert
        Assert.Equal(1000m, profile.TotalOutflows);
    }

    [Fact]
    public void Contractual_flows_and_deposits_combine_in_the_same_buckets() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        AddFlow(portfolio, "in-1", 10, 1500m, FlowDirection.Inflow);
        AddFlow(portfolio, "out-1", 10, 100m, FlowDirection.Outflow);
        portfolio.AddDeposit(Any.NonMaturityDeposit()
                                .WithBalance(400m)
                                .WithCurrency(portfolio.ReportingCurrency)
                                .WithAssumption(new BehaviouralAssumption(0m, 10, 1))
                                .Generate());

        // Act: the whole deposit is volatile and leaves at +10d, in the first bucket
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Fixtures.StandardLadder());

        // Assert
        Assert.Equal(1500m, profile.Gaps[0].Inflows);
        Assert.Equal(500m, profile.Gaps[0].Outflows);
        Assert.Equal(1000m, profile.Gaps[0].CumulativeGap);
    }

    [Fact]
    public void A_schedule_projects_its_future_payments_only() {
        // Arrange: monthly 100 inflows on the 30th... as-of 2025-06-30, first payment 2025-05-30 (Fri), 4 payments:
        // 05-30 history; 06-30 (Mon, = as-of, +0); 07-30 (Wed, +30); 08-30 (Sat, Unadjusted, +61)
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        portfolio.AddSchedule(Any.CashFlowSchedule()
                                 .WithCurrency(portfolio.ReportingCurrency)
                                 .WithFirstPaymentDate(new DateOnly(2025, 5, 30))
                                 .WithFrequencyMonths(1)
                                 .WithNumberOfPayments(4)
                                 .WithAmountPerPayment(100m)
                                 .WithDirection(FlowDirection.Inflow)
                                 .WithConvention(BusinessDayConvention.Unadjusted)
                                 .Generate());

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Fixtures.StandardLadder());

        // Assert
        Assert.Equal([100m, 200m, 0m, 0m], profile.Gaps.Select(static g => g.Inflows));
    }

    [Fact]
    public void A_frozen_portfolio_can_still_be_projected() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        AddFlow(portfolio, "in-1", 5, 10m, FlowDirection.Inflow);
        portfolio.Freeze();

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Fixtures.StandardLadder());

        // Assert
        Assert.Equal(10m, profile.TotalInflows);
    }

    [Fact]
    public void Projecting_does_not_change_the_portfolio() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        AddFlow(portfolio, "in-1", 5, 10m, FlowDirection.Inflow);
        Scenario scenario = Stress(0.3m, 2m, 0.5m);
        LiquidityProfile first = portfolio.Project(scenario, Fixtures.StandardLadder());

        // Act
        LiquidityProfile second = portfolio.Project(scenario, Fixtures.StandardLadder());

        // Assert
        Assert.Equal(first.Gaps.Select(static g => g.CumulativeGap), second.Gaps.Select(static g => g.CumulativeGap));
        Assert.Single(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_single_open_ended_bucket_collects_everything() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        AddFlow(portfolio, "in-1", 0, 10m, FlowDirection.Inflow);
        AddFlow(portfolio, "out-1", 4000, 4m, FlowDirection.Outflow);
        TimeBucket[] ladder = [new TimeBucket("all", 0, null)];

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, ladder);

        // Assert
        BucketGap only = Assert.Single(profile.Gaps);
        Assert.Equal(6m, only.CumulativeGap);
    }

    [Fact]
    public void A_ladder_that_is_not_a_ladder_is_refused_before_any_projection() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        TimeBucket[] gappy = [new TimeBucket("a", 0, 10), new TimeBucket("b", 20, null)];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => portfolio.Project(Scenario.Baseline, gappy));
    }

    [Fact]
    public void A_missing_scenario_or_ladder_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPortfolio.Project(null!, Fixtures.StandardLadder()));
        Assert.Throws<ArgumentNullException>(() => anyPortfolio.Project(Scenario.Baseline, null!));
    }

    [Fact]
    public void Projecting_under_several_scenarios_gives_one_profile_each_in_order() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithAsOfDate(FixedAsOf).Generate();
        AddFlow(portfolio, "in-1", 5, 100m, FlowDirection.Inflow);
        Scenario mild   = Any.Scenario().WithName("Mild").WithInflowHaircut(0.1m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0m).Generate();
        Scenario severe = Any.Scenario().WithName("Severe").WithInflowHaircut(0.5m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0m).Generate();

        // Act
        IReadOnlyList<LiquidityProfile> profiles = portfolio.ProjectAll([severe, Scenario.Baseline, mild], Fixtures.StandardLadder());

        // Assert
        Assert.Equal(["Severe", "Baseline", "Mild"], profiles.Select(static p => p.ScenarioName));
        Assert.Equal([50m, 100m, 90m], profiles.Select(static p => p.TotalInflows));
    }

    [Fact]
    public void An_empty_list_of_scenarios_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyPortfolio.ProjectAll([], Fixtures.StandardLadder()));

        // Assert
        Assert.Equal("scenarios", refusal.ParamName);
    }

    [Fact]
    public void Two_scenarios_with_the_same_name_are_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        Scenario  first        = Any.Scenario().WithName("Stress").Generate();
        Scenario  second       = Any.Scenario().WithName("Stress").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyPortfolio.ProjectAll([first, second], Fixtures.StandardLadder()));

        // Assert
        Assert.Equal("scenarios", refusal.ParamName);
    }

    [Fact]
    public void Scenario_names_are_distinct_case_sensitively() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        Scenario  upper        = Any.Scenario().WithName("STRESS").Generate();
        Scenario  lower        = Any.Scenario().WithName("stress").Generate();

        // Act
        IReadOnlyList<LiquidityProfile> profiles = anyPortfolio.ProjectAll([upper, lower], Fixtures.StandardLadder());

        // Assert
        Assert.Equal(2, profiles.Count);
    }

    [Fact]
    public void A_missing_list_of_scenarios_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPortfolio.ProjectAll(null!, Fixtures.StandardLadder()));
    }

}
