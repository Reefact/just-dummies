using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class PortfolioTests {

    private static readonly DateOnly AsOf = new DateOnly(2026, 6, 15);

    private static Portfolio OpenPortfolio() {
        return Any.Portfolio().WithReportingCurrency(Ladders.Eur).WithAsOfDate(AsOf).Generate();
    }

    private static ContractualFlow Flow(string id, int daysFromAsOf, decimal amount, FlowDirection direction) {
        return Any.ContractualFlow().WithFlowId(id)
                                    .WithValueDate(AsOf.AddDays(daysFromAsOf))
                                    .WithAmount(amount)
                                    .WithCurrency(Ladders.Eur)
                                    .WithDirection(direction)
                                    .Generate();
    }

    private static NonMaturityDeposit Deposit(string id, decimal balance, decimal stableShare, int volatileDays, int coreMonths) {
        return Any.NonMaturityDeposit().WithDepositId(id)
                                       .WithBalance(balance)
                                       .WithCurrency(Ladders.Eur)
                                       .WithAssumption(new BehaviouralAssumption(stableShare, volatileDays, coreMonths))
                                       .Generate();
    }

    // ---- construction -------------------------------------------------------------------------

    [Fact]
    public void A_new_portfolio_is_open_and_empty() {
        // Act
        Portfolio portfolio = Any.Portfolio().WithPortfolioId("BANKING-BOOK").WithReportingCurrency(Ladders.Eur).WithAsOfDate(AsOf).Generate();

        // Assert
        Assert.Equal("BANKING-BOOK", portfolio.PortfolioId);
        Assert.Equal(Ladders.Eur, portfolio.ReportingCurrency);
        Assert.Equal(AsOf, portfolio.AsOfDate);
        Assert.False(portfolio.IsFrozen);
        Assert.Empty(portfolio.ContractualFlows);
        Assert.Empty(portfolio.Deposits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_portfolio_id_is_refused(string portfolioId) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Portfolio().WithPortfolioId(portfolioId).Generate());

        // Assert
        Assert.Equal("portfolioId", refusal.ParamName);
    }

    [Fact]
    public void A_missing_reporting_currency_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Any.Portfolio().WithReportingCurrency((Currency)null!).Generate());

        // Assert
        Assert.Equal("reportingCurrency", refusal.ParamName);
    }

    // ---- AddFlow ------------------------------------------------------------------------------

    [Fact]
    public void A_flow_is_added_to_the_portfolio() {
        // Arrange
        Portfolio       portfolio = OpenPortfolio();
        ContractualFlow flow      = Flow("F-1", 10, 100m, FlowDirection.Inflow);

        // Act
        portfolio.AddFlow(flow);

        // Assert
        Assert.Same(flow, Assert.Single(portfolio.ContractualFlows));
    }

    [Fact]
    public void A_flow_settling_on_the_as_of_date_is_accepted() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();

        // Act
        portfolio.AddFlow(Flow("F-1", 0, 100m, FlowDirection.Inflow));

        // Assert
        Assert.Single(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_settling_before_the_as_of_date_is_refused() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow("F-1", -1, 100m, FlowDirection.Inflow)));

        // Assert
        Assert.Contains("F-1", refusal.Message);
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_in_a_foreign_currency_is_refused() {
        // Arrange
        Portfolio       portfolio = OpenPortfolio();
        ContractualFlow dollars   = Any.ContractualFlow().WithValueDate(AsOf.AddDays(5)).WithCurrency(Ladders.Usd).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddFlow(dollars));

        // Assert
        Assert.Contains("USD", refusal.Message);
        Assert.Contains("EUR", refusal.Message);
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_with_an_id_already_in_the_portfolio_is_refused() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddFlow(Flow("F-1", 10, 100m, FlowDirection.Inflow));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow("F-1", 20, 50m, FlowDirection.Outflow)));

        // Assert
        Assert.Contains("F-1", refusal.Message);
        Assert.Single(portfolio.ContractualFlows);
    }

    [Fact]
    public void Position_ids_are_case_sensitive() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddFlow(Flow("f-1", 10, 100m, FlowDirection.Inflow));

        // Act
        portfolio.AddFlow(Flow("F-1", 10, 100m, FlowDirection.Inflow));

        // Assert
        Assert.Equal(2, portfolio.ContractualFlows.Count);
    }

    [Fact]
    public void A_missing_flow_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OpenPortfolio().AddFlow(null!));
    }

    // ---- AddSchedule --------------------------------------------------------------------------

    private static CashFlowSchedule MonthlySchedule(string id, DateOnly first, int payments) {
        return Any.CashFlowSchedule().WithScheduleId(id)
                                     .WithFirstPaymentDate(first)
                                     .WithFrequencyMonths(1)
                                     .WithNumberOfPayments(payments)
                                     .WithCurrency(Ladders.Eur)
                                     .WithConvention(BusinessDayConvention.Unadjusted)
                                     .Generate();
    }

    [Fact]
    public void Only_the_payments_on_or_after_the_as_of_date_of_a_schedule_are_kept() {
        // Arrange: payments on 04-15, 05-15, 06-15, 07-15, 08-15, 09-15 and the as-of date is 06-15
        Portfolio        portfolio = OpenPortfolio();
        CashFlowSchedule schedule  = MonthlySchedule("S", new DateOnly(2026, 4, 15), 6);

        // Act
        portfolio.AddSchedule(schedule);

        // Assert
        Assert.Equal(new[] { "S-3", "S-4", "S-5", "S-6" }, portfolio.ContractualFlows.Select(f => f.FlowId).ToArray());
        Assert.Equal(new DateOnly(2026, 6, 15), portfolio.ContractualFlows[0].ValueDate);
    }

    [Fact]
    public void A_schedule_entirely_in_the_past_adds_nothing() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();

        // Act
        portfolio.AddSchedule(MonthlySchedule("S", new DateOnly(2025, 1, 15), 6));

        // Assert
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_clash_on_one_payment_refuses_the_whole_schedule() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddFlow(Flow("S-5", 90, 10m, FlowDirection.Inflow));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => portfolio.AddSchedule(MonthlySchedule("S", new DateOnly(2026, 6, 15), 6)));

        // Assert
        Assert.Contains("S-5", refusal.Message);
        Assert.Single(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_refused_schedule_leaves_no_trace_in_the_registered_ids() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddFlow(Flow("S-5", 90, 10m, FlowDirection.Inflow));
        Assert.Throws<ArgumentException>(() => portfolio.AddSchedule(MonthlySchedule("S", new DateOnly(2026, 6, 15), 6)));

        // Act: S-2 was part of the refused schedule, so it must still be free
        portfolio.AddFlow(Flow("S-2", 10, 10m, FlowDirection.Inflow));

        // Assert
        Assert.Equal(2, portfolio.ContractualFlows.Count);
    }

    [Fact]
    public void The_same_schedule_cannot_be_added_twice() {
        // Arrange
        Portfolio        portfolio = OpenPortfolio();
        CashFlowSchedule schedule  = MonthlySchedule("S", new DateOnly(2026, 6, 15), 3);
        portfolio.AddSchedule(schedule);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddSchedule(schedule));
        Assert.Equal(3, portfolio.ContractualFlows.Count);
    }

    [Fact]
    public void A_schedule_in_a_foreign_currency_is_refused() {
        // Arrange
        Portfolio        portfolio = OpenPortfolio();
        CashFlowSchedule dollars   = Any.CashFlowSchedule().WithCurrency(Ladders.Usd).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddSchedule(dollars));
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_missing_schedule_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OpenPortfolio().AddSchedule(null!));
    }

    // ---- AddDeposit ---------------------------------------------------------------------------

    [Fact]
    public void A_deposit_is_added_to_the_portfolio() {
        // Arrange
        Portfolio          portfolio = OpenPortfolio();
        NonMaturityDeposit deposit   = Deposit("CA", 1000m, 0.6m, 30, 3);

        // Act
        portfolio.AddDeposit(deposit);

        // Assert
        Assert.Same(deposit, Assert.Single(portfolio.Deposits));
    }

    [Fact]
    public void A_deposit_in_a_foreign_currency_is_refused() {
        // Arrange
        Portfolio          portfolio = OpenPortfolio();
        NonMaturityDeposit dollars   = Any.NonMaturityDeposit().WithCurrency(Ladders.Usd).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddDeposit(dollars));
        Assert.Empty(portfolio.Deposits);
    }

    [Fact]
    public void A_deposit_cannot_reuse_the_id_of_another_deposit() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddDeposit(Deposit("CA", 1000m, 0.6m, 30, 3));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddDeposit(Deposit("CA", 50m, 0.6m, 30, 3)));
        Assert.Single(portfolio.Deposits);
    }

    [Fact]
    public void A_deposit_cannot_reuse_the_id_of_a_flow_and_the_other_way_round() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddFlow(Flow("X", 10, 100m, FlowDirection.Inflow));
        portfolio.AddDeposit(Deposit("Y", 1000m, 0.6m, 30, 3));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddDeposit(Deposit("X", 1000m, 0.6m, 30, 3)));
        Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow("Y", 10, 100m, FlowDirection.Inflow)));
    }

    [Fact]
    public void A_missing_deposit_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OpenPortfolio().AddDeposit(null!));
    }

    // ---- Freeze -------------------------------------------------------------------------------

    [Fact]
    public void Freezing_closes_the_portfolio() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();

        // Act
        portfolio.Freeze();

        // Assert
        Assert.True(portfolio.IsFrozen);
    }

    [Fact]
    public void Freezing_twice_is_refused() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.Freeze();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.Freeze());
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_flow() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.Freeze();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddFlow(Flow("F-1", 10, 100m, FlowDirection.Inflow)));
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_schedule() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.Freeze();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddSchedule(MonthlySchedule("S", AsOf, 3)));
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_deposit() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.Freeze();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddDeposit(Deposit("CA", 1000m, 0.6m, 30, 3)));
        Assert.Empty(portfolio.Deposits);
    }

    [Fact]
    public void A_frozen_portfolio_is_refused_before_its_currency_is_looked_at() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.Freeze();
        ContractualFlow dollars = Any.ContractualFlow().WithValueDate(AsOf.AddDays(5)).WithCurrency(Ladders.Usd).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddFlow(dollars));
    }

    // ---- Project ------------------------------------------------------------------------------

    private static Portfolio ThreeFlowPortfolio() {
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddFlow(Flow("IN-1", 10, 1000m, FlowDirection.Inflow));
        portfolio.AddFlow(Flow("OUT-1", 30, 400m, FlowDirection.Outflow));
        portfolio.AddFlow(Flow("OUT-2", 100, 700m, FlowDirection.Outflow));

        return portfolio;
    }

    [Fact]
    public void Each_flow_lands_in_the_bucket_containing_its_distance_in_days_from_the_as_of_date() {
        // Arrange: OUT-1 is exactly on the boundary between the first two buckets
        Portfolio portfolio = ThreeFlowPortfolio();

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(new[] { 1000m, 0m, 0m }, profile.Gaps.Select(g => g.Inflows).ToArray());
        Assert.Equal(new[] { 0m, 400m, 700m }, profile.Gaps.Select(g => g.Outflows).ToArray());
        Assert.Equal(new[] { 1000m, 600m, -100m }, profile.Gaps.Select(g => g.CumulativeGap).ToArray());
    }

    [Fact]
    public void A_flow_on_the_as_of_date_lands_in_the_first_bucket() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddFlow(Flow("IN-0", 0, 50m, FlowDirection.Inflow));

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(50m, profile.Gaps[0].Inflows);
    }

    [Fact]
    public void The_profile_is_labelled_with_the_scenario_and_the_reporting_currency() {
        // Arrange
        Portfolio portfolio = ThreeFlowPortfolio();
        Scenario  scenario  = Any.Scenario().WithName("Idiosyncratic").Generate();

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal("Idiosyncratic", profile.ScenarioName);
        Assert.Equal(Ladders.Eur, profile.Currency);
    }

    [Fact]
    public void The_inflow_haircut_reduces_inflows_and_leaves_outflows_alone() {
        // Arrange
        Portfolio portfolio = ThreeFlowPortfolio();
        Scenario  scenario  = Any.Scenario().WithInflowHaircut(0.1m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0m).Generate();

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(900m, profile.TotalInflows);
        Assert.Equal(1100m, profile.TotalOutflows);
    }

    [Fact]
    public void The_outflow_multiplier_scales_outflows_and_leaves_inflows_alone() {
        // Arrange
        Portfolio portfolio = ThreeFlowPortfolio();
        Scenario  scenario  = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1.5m).WithDepositStableShareHaircut(0m).Generate();

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(1000m, profile.TotalInflows);
        Assert.Equal(new[] { 0m, 600m, 1050m }, profile.Gaps.Select(g => g.Outflows).ToArray());
    }

    [Fact]
    public void Scaled_flows_are_rounded_to_the_cent() {
        // Arrange: 7.77 * 1.25 = 9.7125 and 3.33 * (1 - 0.15) = 2.8305
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddFlow(Flow("OUT", 5, 7.77m, FlowDirection.Outflow));
        portfolio.AddFlow(Flow("IN", 5, 3.33m, FlowDirection.Inflow));
        Scenario scenario = Any.Scenario().WithInflowHaircut(0.15m).WithOutflowMultiplier(1.25m).WithDepositStableShareHaircut(0m).Generate();

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(9.71m, profile.TotalOutflows);
        Assert.Equal(2.83m, profile.TotalInflows);
    }

    [Fact]
    public void A_deposit_is_spread_into_behavioural_outflows_over_the_buckets() {
        // Arrange: 400 volatile on day 30, then 200 on days 30, 61 and 92 (2026-06-15 + 1, 2, 3 months)
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddDeposit(Deposit("CA", 1000m, 0.6m, 30, 3));

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(1000m, profile.TotalOutflows);
        Assert.Equal(0m, profile.TotalInflows);
        Assert.Equal(-1000m, profile.NetPosition);
        Assert.Equal(0m, profile.Gaps[0].Outflows);
        Assert.Equal(800m, profile.Gaps[1].Outflows);
        Assert.Equal(200m, profile.Gaps[2].Outflows);
    }

    [Fact]
    public void A_deposit_outflow_is_not_scaled_by_the_outflow_multiplier() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddDeposit(Deposit("CA", 1000m, 0m, 30, 3));
        Scenario scenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(3m).WithDepositStableShareHaircut(0m).Generate();

        // Act
        LiquidityProfile profile = portfolio.Project(scenario, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(1000m, profile.TotalOutflows);
    }

    [Fact]
    public void A_schedule_flows_are_projected_like_any_other_flow() {
        // Arrange: three monthly inflows of 100 on 06-15, 07-15, 08-15 -> days 0, 30, 61
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddSchedule(Any.CashFlowSchedule().WithFirstPaymentDate(AsOf)
                                                    .WithFrequencyMonths(1)
                                                    .WithNumberOfPayments(3)
                                                    .WithAmountPerPayment(100m)
                                                    .WithCurrency(Ladders.Eur)
                                                    .WithDirection(FlowDirection.Inflow)
                                                    .WithConvention(BusinessDayConvention.Unadjusted)
                                                    .Generate());

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(new[] { 100m, 200m, 0m }, profile.Gaps.Select(g => g.Inflows).ToArray());
    }

    [Fact]
    public void An_empty_portfolio_projects_to_an_all_zero_profile_that_never_runs_short() {
        // Act
        LiquidityProfile profile = OpenPortfolio().Project(Scenario.Baseline, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(3, profile.Gaps.Count);
        Assert.All(profile.Gaps, gap => Assert.Equal(0m, gap.CumulativeGap));
        Assert.Null(profile.FirstShortfall);
        Assert.Null(profile.SurvivalDays);
    }

    [Fact]
    public void Projecting_does_not_change_the_portfolio() {
        // Arrange
        Portfolio portfolio = ThreeFlowPortfolio();

        // Act
        LiquidityProfile first  = portfolio.Project(Any.Scenario().WithOutflowMultiplier(2m).Generate(), Ladders.ThreeBuckets());
        LiquidityProfile second = portfolio.Project(Scenario.Baseline, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(3, portfolio.ContractualFlows.Count);
        Assert.Equal(1100m, second.TotalOutflows);
        Assert.NotEqual(first.TotalOutflows, second.TotalOutflows);
    }

    [Fact]
    public void A_frozen_portfolio_can_still_be_projected() {
        // Arrange
        Portfolio portfolio = ThreeFlowPortfolio();
        portfolio.Freeze();

        // Act
        LiquidityProfile profile = portfolio.Project(Scenario.Baseline, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(-100m, profile.NetPosition);
    }

    [Fact]
    public void A_ladder_that_is_not_one_is_refused() {
        // Arrange
        List<TimeBucket> notALadder = new() { Ladders.Bucket(0, 30), Ladders.Bucket(60, null) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ThreeFlowPortfolio().Project(Scenario.Baseline, notALadder));
    }

    [Fact]
    public void Projecting_without_a_scenario_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ThreeFlowPortfolio().Project(null!, Ladders.ThreeBuckets()));
    }

    // ---- ProjectAll ---------------------------------------------------------------------------

    [Fact]
    public void One_profile_is_produced_per_scenario_in_the_order_given() {
        // Arrange
        Portfolio portfolio = ThreeFlowPortfolio();
        List<Scenario> scenarios = new() {
            Scenario.Baseline,
            Any.Scenario().WithName("Stress A").WithOutflowMultiplier(2m).Generate(),
            Any.Scenario().WithName("Stress B").WithInflowHaircut(0.5m).Generate(),
        };

        // Act
        IReadOnlyList<LiquidityProfile> profiles = portfolio.ProjectAll(scenarios, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(new[] { "Baseline", "Stress A", "Stress B" }, profiles.Select(p => p.ScenarioName).ToArray());
    }

    [Fact]
    public void Each_profile_is_the_projection_of_its_own_scenario() {
        // Arrange
        Portfolio      portfolio = ThreeFlowPortfolio();
        List<Scenario> scenarios = new() { Scenario.Baseline, Any.Scenario().WithName("Stress").WithInflowHaircut(0.5m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0m).Generate() };

        // Act
        IReadOnlyList<LiquidityProfile> profiles = portfolio.ProjectAll(scenarios, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(1000m, profiles[0].TotalInflows);
        Assert.Equal(500m, profiles[1].TotalInflows);
    }

    [Fact]
    public void Projecting_no_scenario_at_all_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ThreeFlowPortfolio().ProjectAll(new List<Scenario>(), Ladders.ThreeBuckets()));
    }

    [Fact]
    public void Projecting_a_missing_list_of_scenarios_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ThreeFlowPortfolio().ProjectAll(null!, Ladders.ThreeBuckets()));
    }

    [Fact]
    public void Two_scenarios_with_the_same_name_are_refused() {
        // Arrange
        List<Scenario> scenarios = new() {
            Any.Scenario().WithName("Stress").Generate(),
            Any.Scenario().WithName("Stress").Generate(),
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ThreeFlowPortfolio().ProjectAll(scenarios, Ladders.ThreeBuckets()));
    }

    [Fact]
    public void Scenario_names_differing_by_case_are_distinct() {
        // Arrange
        List<Scenario> scenarios = new() {
            Any.Scenario().WithName("Stress").Generate(),
            Any.Scenario().WithName("stress").Generate(),
        };

        // Act
        IReadOnlyList<LiquidityProfile> profiles = ThreeFlowPortfolio().ProjectAll(scenarios, Ladders.ThreeBuckets());

        // Assert
        Assert.Equal(2, profiles.Count);
    }

    // ---- an end-to-end story ------------------------------------------------------------------

    [Fact]
    public void A_book_is_loaded_frozen_and_stress_tested_against_a_limit() {
        // Arrange
        Portfolio portfolio = OpenPortfolio();
        portfolio.AddFlow(Flow("BOND-COUPON", 20, 300m, FlowDirection.Inflow));
        portfolio.AddFlow(Flow("REPO", 45, 900m, FlowDirection.Outflow));
        portfolio.AddDeposit(Deposit("CA", 500m, 0.5m, 30, 3));
        portfolio.Freeze();
        Scenario severe = Any.Scenario().WithName("Severe").WithInflowHaircut(0.5m).WithOutflowMultiplier(2m).WithDepositStableShareHaircut(1m).Generate();

        // Act
        IReadOnlyList<LiquidityProfile> profiles = portfolio.ProjectAll(new List<Scenario> { Scenario.Baseline, severe }, Ladders.ThreeBuckets());

        // Assert: baseline never within 30 days breaches -500, the severe case does
        profiles[0].EnsureWithinLimit(30, -500m);
        Assert.Throws<InvalidOperationException>(() => profiles[1].EnsureWithinLimit(60, -500m));
        Assert.Equal(30, profiles[0].SurvivalDays);
        Assert.Equal(30, profiles[1].SurvivalDays);
    }

}
