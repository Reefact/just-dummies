using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Regular;

public sealed class NonMaturityDepositTests {

    private static readonly Scenario NoDepositStress = new("No deposit stress", 0m, 1m, 0m);

    [Fact]
    public void The_volatile_part_leaves_in_one_go_at_the_run_off_horizon() {
        // Arrange
        DateOnly anyAsOf = Arbitrary.Portfolio().Generate().AsOfDate;
        NonMaturityDeposit anyDeposit = Arbitrary.NonMaturityDeposit()
                                                 .WithBalance(1_000m)
                                                 .WithAssumption(a => a.WithStableShare(0.6m).WithVolatileRunOffDays(10))
                                                 .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(anyAsOf, NoDepositStress);

        // Assert
        ContractualFlow volatileFlow = Assert.Single(flows, f => f.FlowId.EndsWith("-volatile", StringComparison.Ordinal));
        Assert.Equal(400m, volatileFlow.Amount);
        Assert.Equal(anyAsOf.AddDays(10), volatileFlow.ValueDate);
    }

    [Fact]
    public void The_stable_part_amortises_monthly_and_the_last_instalment_absorbs_rounding() {
        // Arrange
        DateOnly asOf = new DateOnly(2026, 3, 31);
        NonMaturityDeposit anyFullyStableDeposit = Arbitrary.NonMaturityDeposit()
                                                            .WithBalance(100m)
                                                            .WithAssumption(a => a.WithStableShare(1m).WithCoreMaturityMonths(3))
                                                            .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyFullyStableDeposit.ToBehaviouralFlows(asOf, NoDepositStress);

        // Assert
        Assert.Equal([33.33m, 33.33m, 33.34m], flows.Select(f => f.Amount));
        Assert.Equal([new DateOnly(2026, 4, 30), new DateOnly(2026, 5, 31), new DateOnly(2026, 6, 30)], flows.Select(f => f.ValueDate));
    }

    [Fact]
    public void A_stressed_scenario_moves_part_of_the_stable_share_into_the_volatile_share() {
        // Arrange
        DateOnly anyAsOf      = Arbitrary.Portfolio().Generate().AsOfDate;
        Scenario anyRunOnBank = Arbitrary.Scenario().WithDepositStableShareHaircut(0.5m).Generate();
        NonMaturityDeposit anyDeposit = Arbitrary.NonMaturityDeposit()
                                                 .WithBalance(1_000m)
                                                 .WithAssumption(a => a.WithStableShare(0.8m))
                                                 .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(anyAsOf, anyRunOnBank);

        // Assert
        ContractualFlow volatileFlow = Assert.Single(flows, f => f.FlowId.EndsWith("-volatile", StringComparison.Ordinal));
        Assert.Equal(600m, volatileFlow.Amount);
    }

    [Fact]
    public void Behavioural_flows_are_outflows_that_sum_to_the_balance_exactly() {
        // Arrange
        DateOnly           anyAsOf     = Arbitrary.Portfolio().Generate().AsOfDate;
        Scenario           anyScenario = Arbitrary.Scenario().Generate();
        NonMaturityDeposit anyDeposit  = Arbitrary.NonMaturityDeposit().WithBalance(12_345.67m).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(anyAsOf, anyScenario);

        // Assert
        Assert.All(flows, f => Assert.Equal(FlowDirection.Outflow, f.Direction));
        Assert.Equal(12_345.67m, flows.Sum(f => f.Amount));
    }

    [Fact]
    public void A_balance_with_more_than_two_decimals_is_refused() {
        // Arrange
        AnyNonMaturityDeposit anyDepositOfFractionalCents = Arbitrary.NonMaturityDeposit().WithBalance(10.005m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyDepositOfFractionalCents.Generate());

        // Assert
        Assert.Equal("balance", refusal.ParamName);
    }

    [Fact]
    public void A_volatile_horizon_reaching_the_core_maturity_is_refused() {
        // Arrange
        AnyBehaviouralAssumption anyInconsistentAssumption = Arbitrary.BehaviouralAssumption()
                                                                      .WithCoreMaturityMonths(1)
                                                                      .WithVolatileRunOffDays(30);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyInconsistentAssumption.Generate());

        // Assert
        Assert.Equal("volatileRunOffDays", refusal.ParamName);
    }

}
