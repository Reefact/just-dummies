using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Regular;

public sealed class ProjectionTests {

    // The ladder is part of what every projection test asserts on, so it is written out.
    private static readonly IReadOnlyList<TimeBucket> Ladder = [
        new TimeBucket("0-7d", 0, 7),
        new TimeBucket("7d-1m", 7, 30),
        new TimeBucket("1m+", 30, null),
    ];

    private static ContractualFlow Flow(Portfolio portfolio, int day, decimal amount, FlowDirection direction) {
        return Arbitrary.ContractualFlow()
                        .WithCurrency(portfolio.ReportingCurrency)
                        .WithValueDate(portfolio.AsOfDate.AddDays(day))
                        .WithAmount(amount)
                        .WithDirection(direction)
                        .Generate();
    }

    [Fact]
    public void Each_flow_lands_in_the_bucket_containing_its_distance_from_the_as_of_date() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 7, amount: 1_000m, FlowDirection.Inflow));

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladder);

        // Assert
        Assert.Equal([0m, 1_000m, 0m], profile.Gaps.Select(g => g.Inflows));
    }

    [Fact]
    public void A_stressed_scenario_haircuts_inflows_and_scales_outflows() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 3, amount: 1_000m, FlowDirection.Inflow));
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 3, amount: 400m, FlowDirection.Outflow));
        Scenario anyStress = Arbitrary.Scenario().WithInflowHaircut(0.25m).WithOutflowMultiplier(1.5m).Generate();

        // Act
        LiquidityProfile profile = anyPortfolio.Project(anyStress, Ladder);

        // Assert
        Assert.Equal(750m, profile.Gaps[0].Inflows);
        Assert.Equal(600m, profile.Gaps[0].Outflows);
    }

    [Fact]
    public void The_cumulative_gap_runs_across_buckets_and_the_first_shortfall_gives_the_survival_days() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 2, amount: 100m, FlowDirection.Inflow));
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 12, amount: 300m, FlowDirection.Outflow));
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 45, amount: 500m, FlowDirection.Inflow));

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladder);

        // Assert
        Assert.Equal([100m, -200m, 300m], profile.Gaps.Select(g => g.CumulativeGap));
        Assert.Equal(7, profile.SurvivalDays);
        Assert.Equal(300m, profile.NetPosition);
    }

    [Fact]
    public void A_portfolio_that_never_runs_short_has_no_survival_horizon() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 1, amount: 500m, FlowDirection.Inflow));
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 40, amount: 200m, FlowDirection.Outflow));

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladder);

        // Assert
        Assert.Null(profile.FirstShortfall);
        Assert.Null(profile.SurvivalDays);
    }

    [Fact]
    public void A_deposit_contributes_its_behavioural_outflows_to_the_profile() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddDeposit(Arbitrary.NonMaturityDeposit()
                                         .WithCurrency(anyPortfolio.ReportingCurrency)
                                         .WithBalance(1_000m)
                                         .WithAssumption(a => a.WithStableShare(0m).WithVolatileRunOffDays(5))
                                         .Generate());

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladder);

        // Assert
        Assert.Equal([1_000m, 0m, 0m], profile.Gaps.Select(g => g.Outflows));
    }

    [Fact]
    public void Projecting_under_several_scenarios_yields_one_profile_per_scenario() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 10, amount: 1_000m, FlowDirection.Inflow));
        Scenario anyMildStress   = Arbitrary.Scenario().WithName("Mild").WithInflowHaircut(0.1m).Generate();
        Scenario anySevereStress = Arbitrary.Scenario().WithName("Severe").WithInflowHaircut(0.4m).Generate();

        // Act
        IReadOnlyList<LiquidityProfile> profiles = anyPortfolio.ProjectAll([Scenario.Baseline, anyMildStress, anySevereStress], Ladder);

        // Assert
        Assert.Equal(["Baseline", "Mild", "Severe"], profiles.Select(p => p.ScenarioName));
        Assert.Equal([1_000m, 900m, 600m], profiles.Select(p => p.TotalInflows));
    }

    [Fact]
    public void Two_scenarios_with_the_same_name_are_refused() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        Scenario  anyScenario  = Arbitrary.Scenario().Generate();
        Scenario  anyNamesake  = Arbitrary.Scenario().WithName(anyScenario.Name).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.ProjectAll([anyScenario, anyNamesake], Ladder));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_ladder_with_a_hole_between_two_buckets_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        TimeBucket[] ladderWithHole = [new TimeBucket("0-7d", 0, 7), new TimeBucket("1m+", 30, null)];

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.Project(Scenario.Baseline, ladderWithHole));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_limit_breached_before_the_horizon_refuses_the_profile() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 10, amount: 300m, FlowDirection.Outflow));
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladder);

        // Act
        Exception? refusal = Record.Exception(() => profile.EnsureWithinLimit(horizonDays: 30, minimumCumulativeGap: -100m));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_breach_in_a_bucket_starting_at_or_after_the_horizon_is_tolerated() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddFlow(Flow(anyPortfolio, day: 60, amount: 300m, FlowDirection.Outflow));
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladder);

        // Act
        Exception? refusal = Record.Exception(() => profile.EnsureWithinLimit(horizonDays: 30, minimumCumulativeGap: -100m));

        // Assert
        Assert.Null(refusal);
    }

}
