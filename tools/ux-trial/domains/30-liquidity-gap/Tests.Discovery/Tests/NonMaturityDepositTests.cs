using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class NonMaturityDepositTests {

    private static readonly DateOnly AsOf = new DateOnly(2026, 1, 31);

    private static NonMaturityDeposit Deposit(string id, decimal balance, decimal stableShare, int volatileDays, int coreMonths) {
        return Any.NonMaturityDeposit().WithDepositId(id)
                                       .WithBalance(balance)
                                       .WithAssumption(new BehaviouralAssumption(stableShare, volatileDays, coreMonths))
                                       .Generate();
    }

    [Fact]
    public void The_volatile_part_leaves_in_one_outflow_and_the_stable_part_in_monthly_instalments() {
        // Arrange
        NonMaturityDeposit deposit = Deposit("CA", 1000m, 0.6m, 30, 3);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(new[] { "CA-volatile", "CA-core-1", "CA-core-2", "CA-core-3" }, flows.Select(f => f.FlowId).ToArray());
        Assert.Equal(new[] { 400m, 200m, 200m, 200m }, flows.Select(f => f.Amount).ToArray());
        Assert.Equal(new[] { new DateOnly(2026, 3, 2), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 30) },
                     flows.Select(f => f.ValueDate).ToArray());
        Assert.All(flows, flow => Assert.Equal(FlowDirection.Outflow, flow.Direction));
    }

    [Fact]
    public void The_behavioural_flows_are_in_the_currency_of_the_deposit() {
        // Arrange
        NonMaturityDeposit deposit = Any.NonMaturityDeposit().WithCurrency(Ladders.Usd).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.NotEmpty(flows);
        Assert.All(flows, flow => Assert.Equal(Ladders.Usd, flow.Currency));
    }

    [Theory]
    [InlineData("1000", "0.6", 30, 3)]
    [InlineData("100", "1", 30, 3)]
    [InlineData("100", "0.5", 10, 7)]
    [InlineData("0.01", "1", 29, 12)]
    [InlineData("0.01", "0.5", 29, 12)]
    [InlineData("12345.67", "0.8", 90, 120)]
    [InlineData("1000000", "0.333", 200, 100)]
    [InlineData("99.99", "0", 1, 1)]
    public void The_outflows_always_sum_to_the_balance_exactly(string balance, string stableShare, int volatileDays, int coreMonths) {
        // Arrange
        NonMaturityDeposit deposit = Deposit("CA", Parse(balance), Parse(stableShare), volatileDays, coreMonths);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(Parse(balance), flows.Sum(f => f.Amount));
        Assert.All(flows, flow => Assert.True(flow.Amount > 0m));
    }

    [Fact]
    public void The_last_instalment_absorbs_the_rounding() {
        // Arrange: 100.00 over 3 months = 33.33, 33.33 and 33.34
        NonMaturityDeposit deposit = Deposit("CA", 100m, 1m, 10, 3);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, flows.Select(f => f.Amount).ToArray());
    }

    [Fact]
    public void A_fully_stable_deposit_has_no_volatile_outflow() {
        // Arrange
        NonMaturityDeposit deposit = Deposit("CA", 600m, 1m, 10, 6);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.DoesNotContain(flows, f => f.FlowId.EndsWith("-volatile", StringComparison.Ordinal));
        Assert.Equal(6, flows.Count);
    }

    [Fact]
    public void A_fully_volatile_deposit_leaves_in_a_single_outflow_at_the_horizon() {
        // Arrange
        NonMaturityDeposit deposit = Deposit("CA", 600m, 0m, 15, 6);

        // Act
        ContractualFlow flow = Assert.Single(deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline));

        // Assert
        Assert.Equal("CA-volatile", flow.FlowId);
        Assert.Equal(600m, flow.Amount);
        Assert.Equal(AsOf.AddDays(15), flow.ValueDate);
    }

    [Fact]
    public void A_stress_moves_part_of_the_stable_share_into_the_volatile_share() {
        // Arrange: effective stable share = 0.8 * (1 - 0.5) = 0.4
        NonMaturityDeposit deposit  = Deposit("CA", 1000m, 0.8m, 30, 4);
        Scenario           stressed = Any.Scenario().WithDepositStableShareHaircut(0.5m).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, stressed);

        // Assert
        Assert.Equal(600m, flows.Single(f => f.FlowId == "CA-volatile").Amount);
        Assert.Equal(400m, flows.Where(f => f.FlowId.Contains("-core-", StringComparison.Ordinal)).Sum(f => f.Amount));
    }

    [Fact]
    public void A_total_stable_share_haircut_makes_the_whole_balance_volatile() {
        // Arrange
        NonMaturityDeposit deposit  = Deposit("CA", 1000m, 1m, 30, 4);
        Scenario           stressed = Any.Scenario().WithDepositStableShareHaircut(1m).Generate();

        // Act
        ContractualFlow flow = Assert.Single(deposit.ToBehaviouralFlows(AsOf, stressed));

        // Assert
        Assert.Equal("CA-volatile", flow.FlowId);
        Assert.Equal(1000m, flow.Amount);
    }

    [Fact]
    public void The_stress_on_other_scenario_dimensions_does_not_touch_the_deposit() {
        // Arrange
        NonMaturityDeposit deposit = Deposit("CA", 1000m, 0.6m, 30, 3);
        Scenario inflowStress      = Any.Scenario().WithInflowHaircut(0.5m).WithOutflowMultiplier(3m).WithDepositStableShareHaircut(0m).Generate();

        // Act
        IReadOnlyList<decimal> baseline = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline).Select(f => f.Amount).ToList();
        IReadOnlyList<decimal> stressed = deposit.ToBehaviouralFlows(AsOf, inflowStress).Select(f => f.Amount).ToList();

        // Assert
        Assert.Equal(baseline, stressed);
    }

    [Fact]
    public void A_balance_of_one_cent_over_twelve_months_is_a_single_last_instalment() {
        // Arrange
        NonMaturityDeposit deposit = Deposit("CA", 0.01m, 1m, 10, 12);

        // Act
        ContractualFlow flow = Assert.Single(deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline));

        // Assert
        Assert.Equal("CA-core-12", flow.FlowId);
        Assert.Equal(0.01m, flow.Amount);
    }

    [Fact]
    public void Computing_the_flows_without_a_scenario_is_refused() {
        // Arrange
        NonMaturityDeposit deposit = Any.NonMaturityDeposit().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => deposit.ToBehaviouralFlows(AsOf, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_deposit_id_is_refused(string depositId) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.NonMaturityDeposit().WithDepositId(depositId).Generate());

        // Assert
        Assert.Equal("depositId", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-10")]
    public void A_balance_that_is_not_strictly_positive_is_refused(string balance) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.NonMaturityDeposit().WithBalance(Parse(balance)).Generate());

        // Assert
        Assert.Equal("balance", refusal.ParamName);
    }

    [Theory]
    [InlineData("10.001")]
    [InlineData("0.005")]
    [InlineData("1234.5678")]
    public void A_balance_with_more_than_two_decimals_is_refused(string balance) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.NonMaturityDeposit().WithBalance(Parse(balance)).Generate());

        // Assert
        Assert.Equal("balance", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Any.NonMaturityDeposit().WithCurrency((Currency)null!).Generate());

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void A_missing_assumption_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Any.NonMaturityDeposit().WithAssumption((BehaviouralAssumption)null!).Generate());

        // Assert
        Assert.Equal("assumption", refusal.ParamName);
    }

    private static decimal Parse(string value) {
        return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

}
