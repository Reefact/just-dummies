using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Discovery;

public sealed class ProjectionTests {

    private static ContractualFlow FlowIn(Portfolio portfolio, int day, decimal amount, FlowDirection direction) {
        return Any.ContractualFlow()
                  .WithCurrency(portfolio.ReportingCurrency)
                  .WithValueDate(portfolio.AsOfDate.AddDays(day))
                  .WithAmount(amount)
                  .WithDirection(direction)
                  .Generate();
    }

    [Fact]
    public void Each_flow_lands_in_the_bucket_containing_its_distance_from_the_as_of_date() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 6, 100m, FlowDirection.Inflow));
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 7, 200m, FlowDirection.Inflow));
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 400, 300m, FlowDirection.Outflow));

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladders.Regulatory());

        // Assert
        Assert.Equal([100m, 200m, 0m, 0m, 0m], profile.Gaps.Select(g => g.Inflows));
        Assert.Equal([0m, 0m, 0m, 0m, 300m], profile.Gaps.Select(g => g.Outflows));
    }

    [Fact]
    public void A_stress_scenario_cuts_inflows_by_its_haircut_and_scales_outflows_by_its_multiplier() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 10, 1_000m, FlowDirection.Inflow));
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 10, 333.33m, FlowDirection.Outflow));
        Scenario anyStress = Any.Scenario().WithInflowHaircut(0.25m).WithOutflowMultiplier(1.5m).Generate();

        // Act
        LiquidityProfile profile = anyPortfolio.Project(anyStress, Ladders.Regulatory());

        // Assert
        BucketGap oneWeekToOneMonth = profile.Gaps[1];
        Assert.Equal(750m, oneWeekToOneMonth.Inflows);
        Assert.Equal(500.00m, oneWeekToOneMonth.Outflows); // 333.33 x 1.5 = 499.995, rounded to the cent
    }

    [Fact]
    public void Non_maturity_deposits_are_projected_as_behavioural_outflows() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().WithAsOfDate(new DateOnly(2026, 6, 30)).Generate();
        anyPortfolio.AddDeposit(Any.NonMaturityDeposit()
                                   .WithCurrency(anyPortfolio.ReportingCurrency)
                                   .WithBalance(1_000m)
                                   .WithAssumption(new BehaviouralAssumption(0.6m, volatileRunOffDays: 5, coreMaturityMonths: 24))
                                   .Generate());

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladders.Regulatory());

        // Assert
        Assert.Equal(400m, profile.Gaps[0].Outflows);   // the volatile part, at day 5
        Assert.Equal(1_000m, profile.TotalOutflows);
    }

    [Fact]
    public void The_cumulative_gap_carries_each_bucket_into_the_next_and_ends_on_the_net_position() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 1, 500m, FlowDirection.Inflow));
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 20, 800m, FlowDirection.Outflow));
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 100, 400m, FlowDirection.Inflow));

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladders.Regulatory());

        // Assert
        Assert.Equal([500m, -300m, -300m, 100m, 100m], profile.Gaps.Select(g => g.CumulativeGap));
        Assert.Equal(100m, profile.NetPosition);
    }

    [Fact]
    public void The_survival_horizon_is_the_start_of_the_first_bucket_running_short() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 1, 500m, FlowDirection.Inflow));
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 45, 800m, FlowDirection.Outflow));

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladders.Regulatory());

        // Assert
        Assert.Equal(30, profile.SurvivalDays);
    }

    [Fact]
    public void A_portfolio_that_never_runs_short_has_no_survival_horizon() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 1, 500m, FlowDirection.Inflow));
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 45, 500m, FlowDirection.Outflow));

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladders.Regulatory());

        // Assert
        Assert.Null(profile.SurvivalDays);
    }

    [Fact]
    public void One_portfolio_yields_one_profile_per_scenario() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 3, 1_000m, FlowDirection.Inflow));
        Scenario[] scenarios = [
            Scenario.Baseline,
            Any.Scenario().WithName("Idiosyncratic").WithInflowHaircut(0.5m).Generate(),
        ];

        // Act
        IReadOnlyList<LiquidityProfile> profiles = anyPortfolio.ProjectAll(scenarios, Ladders.Regulatory());

        // Assert
        Assert.Equal(["Baseline", "Idiosyncratic"], profiles.Select(p => p.ScenarioName));
        Assert.Equal([1_000m, 500m], profiles.Select(p => p.TotalInflows));
    }

    [Fact]
    public void Two_scenarios_sharing_a_name_are_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        Scenario[] anySameNamedScenarios = [
            Any.Scenario().WithName("Market-wide").Generate(),
            Any.Scenario().WithName("Market-wide").Generate(),
        ];

        // Act
        Exception? error = Record.Exception(() => anyPortfolio.ProjectAll(anySameNamedScenarios, Ladders.Regulatory()));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_breach_inside_the_limit_horizon_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 10, 1_000m, FlowDirection.Outflow));
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladders.Regulatory());

        // Act
        Exception? error = Record.Exception(() => profile.EnsureWithinLimit(horizonDays: 30, minimumCumulativeGap: -500m));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_breach_beyond_the_limit_horizon_is_tolerated() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(FlowIn(anyPortfolio, day: 200, 1_000m, FlowDirection.Outflow));
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladders.Regulatory());

        // Act
        Exception? error = Record.Exception(() => profile.EnsureWithinLimit(horizonDays: 90, minimumCumulativeGap: -500m));

        // Assert
        Assert.Null(error);
    }

    [Fact]
    public void A_ladder_with_a_hole_between_two_buckets_is_refused() {
        // Arrange
        TimeBucket[] ladderWithAHole = [
            new TimeBucket("0-7", 0, 7),
            new TimeBucket("10+", 10, null),
        ];

        // Act
        Exception? error = Record.Exception(() => TimeBucket.EnsureLadder(ladderWithAHole));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_ladder_whose_last_bucket_is_closed_is_refused() {
        // Arrange
        TimeBucket[] closedLadder = [
            new TimeBucket("0-7", 0, 7),
            new TimeBucket("7-30", 7, 30),
        ];

        // Act
        Exception? error = Record.Exception(() => TimeBucket.EnsureLadder(closedLadder));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_bucket_excludes_its_end_day() {
        // Arrange
        TimeBucket anyBucket = Any.TimeBucket().WithStartDay(7).WithEndDay(30).Generate();

        // Act
        bool containsEnd = anyBucket.Contains(30);

        // Assert
        Assert.False(containsEnd);
    }

}
