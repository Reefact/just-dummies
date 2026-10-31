using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Discovery;

public sealed class BehaviouralFlowTests {

    private static readonly DateOnly AsOf = new DateOnly(2026, 6, 30);

    [Fact]
    public void A_volatile_horizon_not_shorter_than_the_core_maturity_is_refused() {
        // Arrange
        decimal anyStableShare = Any.Decimal().Between(0m, 1m).Generate();

        // Act
        Exception? error = Record.Exception(() => new BehaviouralAssumption(anyStableShare, volatileRunOffDays: 90, coreMaturityMonths: 3));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void The_volatile_part_leaves_in_one_outflow_at_the_run_off_horizon() {
        // Arrange
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit()
                                           .WithBalance(1_000m)
                                           .WithAssumption(new BehaviouralAssumption(0.6m, volatileRunOffDays: 30, coreMaturityMonths: 12))
                                           .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        ContractualFlow volatileFlow = Assert.Single(flows, f => f.FlowId.EndsWith("-volatile", StringComparison.Ordinal));
        Assert.Equal(400m, volatileFlow.Amount);
        Assert.Equal(new DateOnly(2026, 7, 30), volatileFlow.ValueDate);
    }

    [Fact]
    public void The_stable_part_amortises_monthly_and_the_last_instalment_absorbs_rounding() {
        // Arrange
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit()
                                           .WithBalance(100m)
                                           .WithAssumption(a => a.WithStableShare(1m).WithCoreMaturityMonths(3).WithVolatileRunOffDays(v => v.LessThan(90)))
                                           .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal([33.33m, 33.33m, 33.34m], flows.Select(f => f.Amount));
        Assert.Equal([new DateOnly(2026, 7, 30), new DateOnly(2026, 8, 30), new DateOnly(2026, 9, 30)], flows.Select(f => f.ValueDate));
    }

    [Fact]
    public void Behavioural_outflows_add_up_to_the_balance_to_the_cent() {
        // Arrange
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit()
                                           .WithBalance(1_000_000.01m)
                                           .WithAssumption(new BehaviouralAssumption(0.73m, volatileRunOffDays: 7, coreMaturityMonths: 7))
                                           .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(1_000_000.01m, flows.Sum(f => f.Amount));
        Assert.All(flows, f => Assert.Equal(FlowDirection.Outflow, f.Direction));
    }

    [Fact]
    public void A_stress_haircut_moves_part_of_the_stable_share_into_the_volatile_outflow() {
        // Arrange
        Scenario anyDepositRun = Any.Scenario().WithDepositStableShareHaircut(0.5m).Generate();
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit()
                                           .WithBalance(1_000m)
                                           .WithAssumption(a => a.WithStableShare(0.8m))
                                           .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(AsOf, anyDepositRun);

        // Assert
        ContractualFlow volatileFlow = Assert.Single(flows, f => f.FlowId.EndsWith("-volatile", StringComparison.Ordinal));
        Assert.Equal(600m, volatileFlow.Amount);
    }

    [Fact]
    public void A_fully_stable_deposit_in_the_baseline_has_no_volatile_outflow() {
        // Arrange
        NonMaturityDeposit anyCoreDeposit = Any.NonMaturityDeposit().WithAssumption(a => a.WithStableShare(1m)).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyCoreDeposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.DoesNotContain(flows, f => f.FlowId.EndsWith("-volatile", StringComparison.Ordinal));
    }

    [Fact]
    public void The_baseline_scenario_is_not_stressed() {
        // Arrange
        Scenario baseline = Scenario.Baseline;

        // Act
        bool stressed = baseline.IsStressed;

        // Assert
        Assert.False(stressed);
    }

    [Fact]
    public void A_scenario_that_scales_outflows_up_is_stressed() {
        // Arrange
        Scenario anyScenario = Any.Scenario().WithOutflowMultiplier(1.5m).Generate();

        // Act
        bool stressed = anyScenario.IsStressed;

        // Assert
        Assert.True(stressed);
    }

}
