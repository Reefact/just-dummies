using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Expert;

public sealed class NonMaturityDepositTests {

    private static readonly DateOnly AsOf = new(2026, 3, 31);

    [Fact]
    public void A_balance_with_fractions_of_a_cent_is_refused() {
        // Arrange
        string                anyDepositId  = Any.String().Printable().NotBlank().WithMaxLength(32).Generate();
        Currency              anyCurrency   = Any.Currency().Generate();
        BehaviouralAssumption anyAssumption = Any.BehaviouralAssumption().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new NonMaturityDeposit(anyDepositId, 1_000.005m, anyCurrency, anyAssumption));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void The_volatile_share_leaves_in_one_go_at_the_run_off_horizon() {
        // Arrange
        NonMaturityDeposit anyCurrentAccount = Any.NonMaturityDeposit()
                                                  .WithDepositId("CA-1")
                                                  .WithBalance(1_000.00m)
                                                  .WithAssumption(a => a.WithStableShare(0.60m).WithVolatileRunOffDays(30))
                                                  .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyCurrentAccount.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        ContractualFlow volatileFlow = Assert.Single(flows, f => f.FlowId == "CA-1-volatile");
        Assert.Equal(400.00m, volatileFlow.Amount);
        Assert.Equal(new DateOnly(2026, 4, 30), volatileFlow.ValueDate);
        Assert.Equal(FlowDirection.Outflow, volatileFlow.Direction);
    }

    [Fact]
    public void The_stable_share_amortises_monthly_over_the_core_maturity() {
        // Arrange
        NonMaturityDeposit anySavingsAccount = Any.NonMaturityDeposit()
                                                  .WithDepositId("SA-1")
                                                  .WithBalance(1_000.00m)
                                                  .WithAssumption(a => a.WithStableShare(0.60m).WithVolatileRunOffDays(30).WithCoreMaturityMonths(4))
                                                  .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anySavingsAccount.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        ContractualFlow[] core = flows.Where(f => f.FlowId.StartsWith("SA-1-core-", StringComparison.Ordinal)).ToArray();
        Assert.Equal([150.00m, 150.00m, 150.00m, 150.00m], core.Select(f => f.Amount));
        Assert.Equal([new DateOnly(2026, 4, 30), new DateOnly(2026, 5, 31), new DateOnly(2026, 6, 30), new DateOnly(2026, 7, 31)],
                     core.Select(f => f.ValueDate));
    }

    [Fact]
    public void The_last_core_instalment_absorbs_the_rounding_so_the_outflows_sum_to_the_balance() {
        // Arrange
        // 1,000.01 x 0.33 = 330.0033 -> 330.00 stable, 670.01 volatile; 330.00 / 7 = 47.14 a month, the 7th takes 47.16.
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit()
                                           .WithBalance(1_000.01m)
                                           .WithAssumption(a => a.WithStableShare(0.33m).WithCoreMaturityMonths(7).WithVolatileRunOffDays(v => v.Between(1, 209)))
                                           .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(1_000.01m, flows.Sum(f => f.Amount));
        Assert.Equal(47.16m, flows[^1].Amount);
    }

    [Fact]
    public void A_stressed_scenario_moves_part_of_the_stable_share_into_the_volatile_run_off() {
        // Arrange
        // Only the deposit haircut acts on a deposit: the scenario's other parameters are dummies.
        Scenario anyDepositRun = Any.Scenario().WithDepositStableShareHaircut(0.50m).Generate();
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit()
                                           .WithDepositId("CA-7")
                                           .WithBalance(1_000.00m)
                                           .WithAssumption(a => a.WithStableShare(0.80m))
                                           .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(AsOf, anyDepositRun);

        // Assert
        Assert.Equal(600.00m, Assert.Single(flows, f => f.FlowId == "CA-7-volatile").Amount);
    }

    [Fact]
    public void A_full_haircut_turns_the_whole_balance_into_a_single_volatile_outflow() {
        // Arrange
        Scenario anyBankRun = Any.Scenario().WithDepositStableShareHaircut(1m).Generate();
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit()
                                           .WithBalance(2_500.00m)
                                           .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyDeposit.ToBehaviouralFlows(AsOf, anyBankRun);

        // Assert
        Assert.Equal(2_500.00m, Assert.Single(flows).Amount);
    }

}
