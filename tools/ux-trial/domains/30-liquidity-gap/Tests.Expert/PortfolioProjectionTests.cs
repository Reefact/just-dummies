using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Expert;

public sealed class PortfolioProjectionTests {

    private static readonly DateOnly AsOf = new(2026, 3, 2);

    [Fact]
    public void Inflows_are_cut_by_the_haircut_and_outflows_scaled_by_the_multiplier_in_their_bucket() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().WithAsOfDate(AsOf).Generate();
        AnyContractualFlow anyFlow = Any.ContractualFlow().BookableIn(anyPortfolio);
        anyPortfolio.AddFlow(anyFlow.WithFlowId("IN-1").WithDirection(FlowDirection.Inflow).WithAmount(1_000.00m).WithValueDate(AsOf.AddDays(10)).Generate());
        anyPortfolio.AddFlow(anyFlow.WithFlowId("OUT-1").WithDirection(FlowDirection.Outflow).WithAmount(400.00m).WithValueDate(AsOf.AddDays(45)).Generate());
        // No deposit in this book, so the deposit haircut is a dummy.
        Scenario anyStress = Any.Scenario().WithInflowHaircut(0.20m).WithOutflowMultiplier(1.50m).Generate();

        // Act
        LiquidityProfile profile = anyPortfolio.Project(anyStress, Ladders.UpTo30Then90Days());

        // Assert
        Assert.Equal([800.00m, 0m, 0m], profile.Gaps.Select(g => g.Inflows));
        Assert.Equal([0m, 600.00m, 0m], profile.Gaps.Select(g => g.Outflows));
        Assert.Equal([800.00m, 200.00m, 200.00m], profile.Gaps.Select(g => g.CumulativeGap));
    }

    [Fact]
    public void A_deposit_is_spread_into_behavioural_outflows_across_the_buckets() {
        // Arrange
        // As of 1 March: volatile part on day 10, core instalments on 1 April, 1 May and 1 June (days 31, 61, 92).
        DateOnly  firstOfMarch = new(2026, 3, 1);
        Portfolio anyPortfolio = Any.Portfolio().WithAsOfDate(firstOfMarch).Generate();
        anyPortfolio.AddDeposit(Any.NonMaturityDeposit()
                                   .BookableIn(anyPortfolio)
                                   .WithBalance(1_200.00m)
                                   .WithAssumption(new BehaviouralAssumption(stableShare: 0.50m, volatileRunOffDays: 10, coreMaturityMonths: 3))
                                   .Generate());
        // A deposit-only book: the contractual haircut and multiplier are dummies.
        Scenario anyNoDepositStress = Any.Scenario().WithDepositStableShareHaircut(0m).Generate();

        // Act
        LiquidityProfile profile = anyPortfolio.Project(anyNoDepositStress, Ladders.UpTo30DaysThenAYear());

        // Assert
        Assert.Equal([600.00m, 600.00m, 0m], profile.Gaps.Select(g => g.Outflows));
        Assert.Equal(-1_200.00m, profile.NetPosition);
    }

    [Fact]
    public void Survival_days_is_the_start_of_the_first_bucket_running_short() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().WithAsOfDate(AsOf).Generate();
        AnyContractualFlow anyFlow = Any.ContractualFlow().BookableIn(anyPortfolio);
        anyPortfolio.AddFlow(anyFlow.WithFlowId("IN-1").WithDirection(FlowDirection.Inflow).WithAmount(100.00m).WithValueDate(AsOf.AddDays(5)).Generate());
        anyPortfolio.AddFlow(anyFlow.WithFlowId("OUT-1").WithDirection(FlowDirection.Outflow).WithAmount(300.00m).WithValueDate(AsOf.AddDays(40)).Generate());

        // Act
        LiquidityProfile profile = anyPortfolio.Project(Scenario.Baseline, Ladders.UpTo30Then90Days());

        // Assert
        Assert.Equal(30, profile.SurvivalDays);
        Assert.Equal("30-90d", profile.FirstShortfall?.Bucket.Label);
    }

    [Fact]
    public void A_book_of_inflows_alone_has_no_shortfall() {
        // Arrange
        // Which inflows, which scenario and which ladder: none of it can make inflows produce a shortfall.
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        List<ContractualFlow> anyInflows = Any.ListOf(Any.ContractualFlow().BookableIn(anyPortfolio).WithDirection(FlowDirection.Inflow))
                                              .WithCountBetween(1, 6)
                                              .Distinct(PositionIdComparer.Instance)
                                              .Generate();
        anyInflows.ForEach(anyPortfolio.AddFlow);

        Scenario                  anyScenario = Any.Scenario().Generate();
        IReadOnlyList<TimeBucket> anyLadder   = Any.BucketLadder().Generate();

        // Act
        LiquidityProfile profile = anyPortfolio.Project(anyScenario, anyLadder);

        // Assert
        Assert.Null(profile.SurvivalDays);
    }

    [Fact]
    public void Projecting_reports_in_the_portfolio_currency_under_the_scenario_name() {
        // Arrange
        Portfolio anyDollarPortfolio = Any.Portfolio().WithReportingCurrency(new Currency("USD")).Generate();
        Scenario  anyMarketStress    = Any.Scenario().WithName("Market-wide 3M").Generate();

        // Act
        LiquidityProfile profile = anyDollarPortfolio.Project(anyMarketStress, Any.BucketLadder().Generate());

        // Assert
        Assert.Equal(new Currency("USD"), profile.Currency);
        Assert.Equal("Market-wide 3M", profile.ScenarioName);
    }

    [Fact]
    public void One_portfolio_yields_one_profile_per_scenario_in_the_order_given() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        IReadOnlyList<Scenario> anyScenarios = [
            Any.Scenario().WithName("Baseline").Generate(),
            Any.Scenario().WithName("Idiosyncratic").Generate(),
            Any.Scenario().WithName("Combined").Generate()
        ];

        // Act
        IReadOnlyList<LiquidityProfile> profiles = anyPortfolio.ProjectAll(anyScenarios, Any.BucketLadder().Generate());

        // Assert
        Assert.Equal(["Baseline", "Idiosyncratic", "Combined"], profiles.Select(p => p.ScenarioName));
    }

    [Fact]
    public void Two_scenarios_sharing_a_name_are_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        AnyScenario anyStress = Any.Scenario().WithName("Stress");
        IReadOnlyList<Scenario> sameNameTwice = [anyStress.Generate(), anyStress.Generate()];

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.ProjectAll(sameNameTwice, Any.BucketLadder().Generate()));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    private sealed class PositionIdComparer : IEqualityComparer<ContractualFlow> {

        public static readonly PositionIdComparer Instance = new();

        public bool Equals(ContractualFlow? x, ContractualFlow? y) => string.Equals(x?.FlowId, y?.FlowId, StringComparison.Ordinal);

        public int GetHashCode(ContractualFlow flow) => StringComparer.Ordinal.GetHashCode(flow.FlowId);

    }

}
