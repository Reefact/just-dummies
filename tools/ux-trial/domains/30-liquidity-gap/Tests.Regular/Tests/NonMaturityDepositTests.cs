using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class NonMaturityDepositTests {

    private static readonly DateOnly AsOf = new DateOnly(2025, 6, 30);

    private static NonMaturityDeposit Deposit(decimal balance, decimal stableShare, int volatileDays, int coreMonths) {
        return Any.NonMaturityDeposit()
                  .WithDepositId("DEP-1")
                  .WithBalance(balance)
                  .WithAssumption(new BehaviouralAssumption(stableShare, volatileDays, coreMonths))
                  .Generate();
    }

    [Fact]
    public void A_deposit_keeps_what_it_was_built_with() {
        // Arrange
        Currency              anyCurrency = Any.Currency().Generate();
        BehaviouralAssumption assumption  = new BehaviouralAssumption(0.5m, 10, 12);

        // Act
        NonMaturityDeposit deposit = new NonMaturityDeposit("SAV-3", 2500.75m, anyCurrency, assumption);

        // Assert
        Assert.Equal("SAV-3", deposit.DepositId);
        Assert.Equal(2500.75m, deposit.Balance);
        Assert.Same(anyCurrency, deposit.Currency);
        Assert.Same(assumption, deposit.Assumption);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void A_blank_identifier_is_refused(string? blankId) {
        // Arrange
        Currency              anyCurrency = Any.Currency().Generate();
        BehaviouralAssumption anyAssumption = Any.BehaviouralAssumption().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NonMaturityDeposit(blankId!, 10m, anyCurrency, anyAssumption));

        // Assert
        Assert.Equal("depositId", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-100")]
    public void A_balance_that_is_not_strictly_positive_is_refused(string balance) {
        // Arrange
        string                anyId         = Any.String().NotBlank().Generate();
        Currency              anyCurrency   = Any.Currency().Generate();
        BehaviouralAssumption anyAssumption = Any.BehaviouralAssumption().Generate();
        decimal               invalid       = decimal.Parse(balance, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new NonMaturityDeposit(anyId, invalid, anyCurrency, anyAssumption));

        // Assert
        Assert.Equal("balance", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("10.005")]
    [InlineData("1234.5678")]
    public void A_balance_with_more_than_two_decimals_is_refused(string balance) {
        // Arrange
        string                anyId         = Any.String().NotBlank().Generate();
        Currency              anyCurrency   = Any.Currency().Generate();
        BehaviouralAssumption anyAssumption = Any.BehaviouralAssumption().Generate();
        decimal               invalid       = decimal.Parse(balance, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NonMaturityDeposit(anyId, invalid, anyCurrency, anyAssumption));

        // Assert
        Assert.Equal("balance", refusal.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("10.10")]
    [InlineData("10.1")]
    [InlineData("79228162514264337593543950335")]
    public void A_balance_with_at_most_two_decimals_is_accepted(string balance) {
        // Arrange
        decimal valid = decimal.Parse(balance, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        NonMaturityDeposit deposit = Any.NonMaturityDeposit().WithBalance(valid).Generate();

        // Assert
        Assert.Equal(valid, deposit.Balance);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Arrange
        string                anyId         = Any.String().NotBlank().Generate();
        BehaviouralAssumption anyAssumption = Any.BehaviouralAssumption().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new NonMaturityDeposit(anyId, 10m, null!, anyAssumption));

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void A_missing_assumption_is_refused() {
        // Arrange
        string   anyId       = Any.String().NotBlank().Generate();
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new NonMaturityDeposit(anyId, 10m, anyCurrency, null!));

        // Assert
        Assert.Equal("assumption", refusal.ParamName);
    }

    [Fact]
    public void Under_the_baseline_the_volatile_part_leaves_at_the_horizon_and_the_stable_part_in_monthly_instalments() {
        // Arrange
        NonMaturityDeposit deposit = Deposit(1000.00m, 0.6m, 30, 3);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(["DEP-1-volatile", "DEP-1-core-1", "DEP-1-core-2", "DEP-1-core-3"], flows.Select(static f => f.FlowId));
        Assert.Equal([400.00m, 200.00m, 200.00m, 200.00m], flows.Select(static f => f.Amount));
        Assert.Equal([new DateOnly(2025, 7, 30), new DateOnly(2025, 7, 30), new DateOnly(2025, 8, 30), new DateOnly(2025, 9, 30)],
                     flows.Select(static f => f.ValueDate));
        Assert.All(flows, static f => Assert.Equal(FlowDirection.Outflow, f.Direction));
    }

    [Fact]
    public void The_behavioural_flows_carry_the_currency_of_the_deposit() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        NonMaturityDeposit deposit = Any.NonMaturityDeposit()
                                        .WithCurrency(anyCurrency)
                                        .WithBalance(100m)
                                        .WithAssumption(new BehaviouralAssumption(0.5m, 10, 4))
                                        .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.All(flows, flow => Assert.Same(anyCurrency, flow.Currency));
    }

    [Fact]
    public void The_last_instalment_absorbs_the_rounding_of_the_stable_part() {
        // Arrange: stable = 100.00 * 0.5 = 50.00 ; 50.00 / 3 = 16.66 truncated, last = 50.00 - 33.32 = 16.68
        NonMaturityDeposit deposit = Deposit(100.00m, 0.5m, 10, 3);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal([50.00m, 16.66m, 16.66m, 16.68m], flows.Select(static f => f.Amount));
    }

    [Fact]
    public void A_stable_share_of_zero_leaves_everything_in_one_volatile_outflow() {
        // Arrange
        NonMaturityDeposit deposit = Deposit(750.00m, 0m, 15, 6);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        ContractualFlow only = Assert.Single(flows);
        Assert.Equal("DEP-1-volatile", only.FlowId);
        Assert.Equal(750.00m, only.Amount);
        Assert.Equal(new DateOnly(2025, 7, 15), only.ValueDate);
    }

    [Fact]
    public void A_stable_share_of_one_produces_no_volatile_outflow() {
        // Arrange
        NonMaturityDeposit deposit = Deposit(600.00m, 1m, 15, 6);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(6, flows.Count);
        Assert.DoesNotContain(flows, static f => f.FlowId.EndsWith("volatile", StringComparison.Ordinal));
        Assert.All(flows, static f => Assert.Equal(100.00m, f.Amount));
    }

    [Fact]
    public void A_stable_share_haircut_moves_part_of_the_stable_share_into_the_volatile_one() {
        // Arrange: effective share = 0.6 * (1 - 0.5) = 0.3 -> stable 300.00, volatile 700.00
        NonMaturityDeposit deposit  = Deposit(1000.00m, 0.6m, 30, 3);
        Scenario           scenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0.5m).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, scenario);

        // Assert
        Assert.Equal([700.00m, 100.00m, 100.00m, 100.00m], flows.Select(static f => f.Amount));
    }

    [Fact]
    public void A_full_stable_share_haircut_leaves_everything_volatile() {
        // Arrange
        NonMaturityDeposit deposit  = Deposit(1000.00m, 0.9m, 30, 3);
        Scenario           scenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(1m).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, scenario);

        // Assert
        ContractualFlow only = Assert.Single(flows);
        Assert.Equal(1000.00m, only.Amount);
    }

    [Fact]
    public void The_inflow_haircut_and_the_outflow_multiplier_do_not_touch_the_behavioural_flows() {
        // Arrange
        NonMaturityDeposit deposit  = Deposit(1000.00m, 0.6m, 30, 3);
        Scenario           scenario = Any.Scenario().WithInflowHaircut(0.5m).WithOutflowMultiplier(3m).WithDepositStableShareHaircut(0m).Generate();

        // Act
        IReadOnlyList<ContractualFlow> stressed = deposit.ToBehaviouralFlows(AsOf, scenario);
        IReadOnlyList<ContractualFlow> baseline = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(baseline.Select(static f => f.Amount), stressed.Select(static f => f.Amount));
    }

    [Fact]
    public void Instalments_below_a_cent_are_skipped_and_the_last_one_collects_the_whole_stable_part() {
        // Arrange: stable = 0.10 * 0.5 -> balance 0.10, share 0.5 -> stable 0.05 ; 0.05 / 10 = 0.00 per instalment
        NonMaturityDeposit deposit = Deposit(0.10m, 0.5m, 5, 10);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(["DEP-1-volatile", "DEP-1-core-10"], flows.Select(static f => f.FlowId));
        Assert.Equal([0.05m, 0.05m], flows.Select(static f => f.Amount));
    }

    [Fact]
    public void A_stable_part_that_rounds_down_to_nothing_leaves_only_the_volatile_outflow() {
        // Arrange: 0.01 * 0.5 = 0.005 -> truncated to 0.00
        NonMaturityDeposit deposit = Deposit(0.01m, 0.5m, 5, 10);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        ContractualFlow only = Assert.Single(flows);
        Assert.Equal(0.01m, only.Amount);
    }

    [Fact]
    public void Monthly_instalments_follow_the_as_of_date_at_month_end() {
        // Arrange
        NonMaturityDeposit deposit = Deposit(300.00m, 1m, 1, 3);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(new DateOnly(2025, 1, 31), Scenario.Baseline);

        // Assert
        Assert.Equal([new DateOnly(2025, 2, 28), new DateOnly(2025, 3, 31), new DateOnly(2025, 4, 30)], flows.Select(static f => f.ValueDate));
    }

    [Fact]
    public void In_an_awkward_split_the_outflows_still_add_up_to_the_balance_to_the_cent() {
        // Arrange: awkward figures on purpose
        NonMaturityDeposit deposit  = Deposit(9999.99m, 0.7333m, 17, 7);
        Scenario           scenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0.37m).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, scenario);

        // Assert
        Assert.Equal(9999.99m, flows.Sum(static f => f.Amount));
        Assert.All(flows, static f => Assert.Equal(decimal.Round(f.Amount, 2), f.Amount));
    }

    [Fact]
    public void A_missing_scenario_is_refused() {
        // Arrange
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyDeposit.ToBehaviouralFlows(AsOf, null!));

        // Assert
        Assert.Equal("scenario", refusal.ParamName);
    }

}
