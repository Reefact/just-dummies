using System.Globalization;

namespace LiquidityGap.Domain.Tests.Expert.Tests;

public sealed class NonMaturityDepositTests {

    private static readonly DateOnly AsOf = new DateOnly(2026, 3, 10);

    private static decimal Money(string amount) {
        return decimal.Parse(amount, CultureInfo.InvariantCulture);
    }

    /// <summary>A savings account: 60 % stable, the rest gone within 30 days, the stable part amortising over three months.</summary>
    private static NonMaturityDeposit SavingsAccount(decimal balance) {
        return Any.NonMaturityDeposit()
                  .WithDepositId("SAV")
                  .WithBalance(balance)
                  .WithAssumption(new BehaviouralAssumption(0.6m, 30, 3))
                  .Generate();
    }

    // ----- construction -----

    [Fact]
    public void A_deposit_with_a_balance_in_cents_is_accepted() {
        // Act
        NonMaturityDeposit deposit = Any.NonMaturityDeposit().WithBalance(1234.56m).Generate();

        // Assert
        Assert.Equal(1234.56m, deposit.Balance);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void A_balance_that_is_not_strictly_positive_is_refused(string balance) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.NonMaturityDeposit().WithBalance(Money(balance)).Generate());

        // Assert
        Assert.Equal("balance", refusal.ParamName);
    }

    [Theory]
    [InlineData("100.001")]
    [InlineData("0.005")]
    public void A_balance_with_more_than_two_decimals_is_refused(string balance) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.NonMaturityDeposit().WithBalance(Money(balance)).Generate());

        // Assert
        Assert.Equal("balance", refusal.ParamName);
    }

    [Fact]
    public void A_balance_of_one_cent_is_accepted() {
        // Act
        NonMaturityDeposit deposit = Any.NonMaturityDeposit().WithBalance(0.01m).Generate();

        // Assert
        Assert.Equal(0.01m, deposit.Balance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\t ")]
    public void A_blank_identifier_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.NonMaturityDeposit().WithDepositId(blank).Generate());

        // Assert
        Assert.Equal("depositId", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.NonMaturityDeposit().WithCurrency((Currency)null!).Generate());

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void A_missing_assumption_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.NonMaturityDeposit().WithAssumption((BehaviouralAssumption)null!).Generate());

        // Assert
        Assert.Equal("assumption", refusal.ParamName);
    }

    // ----- ToBehaviouralFlows -----

    [Fact]
    public void Under_the_baseline_the_volatile_part_leaves_at_the_horizon_and_the_stable_part_in_equal_monthly_instalments() {
        // Arrange
        NonMaturityDeposit deposit = SavingsAccount(1000.00m);

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Collection(flows,
            flow => Assert.Equal(("SAV-volatile", new DateOnly(2026, 4, 9), 400.00m), (flow.FlowId, flow.ValueDate, flow.Amount)),
            flow => Assert.Equal(("SAV-core-1", new DateOnly(2026, 4, 10), 200.00m), (flow.FlowId, flow.ValueDate, flow.Amount)),
            flow => Assert.Equal(("SAV-core-2", new DateOnly(2026, 5, 10), 200.00m), (flow.FlowId, flow.ValueDate, flow.Amount)),
            flow => Assert.Equal(("SAV-core-3", new DateOnly(2026, 6, 10), 200.00m), (flow.FlowId, flow.ValueDate, flow.Amount)));
    }

    [Fact]
    public void Every_behavioural_flow_is_an_outflow_in_the_deposits_currency() {
        // Arrange
        Currency yen = new Currency("JPY");
        NonMaturityDeposit deposit = Any.NonMaturityDeposit()
                                        .WithBalance(1000.00m)
                                        .WithCurrency(yen)
                                        .WithAssumption(new BehaviouralAssumption(0.6m, 30, 3))
                                        .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.All(flows, flow => {
            Assert.Equal(FlowDirection.Outflow, flow.Direction);
            Assert.Equal(yen, flow.Currency);
        });
    }

    [Fact]
    public void A_stressed_scenario_moves_part_of_the_stable_share_into_the_volatile_part() {
        // Arrange: effective stable share = 0.6 * (1 - 0.5) = 0.3
        NonMaturityDeposit deposit = SavingsAccount(1000.00m);
        Scenario halvedStability   = Any.Scenario().WithDepositStableShareHaircut(0.5m).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, halvedStability);

        // Assert
        Assert.Equal(700.00m, flows.Single(f => f.FlowId == "SAV-volatile").Amount);
        Assert.Equal([100.00m, 100.00m, 100.00m], flows.Where(f => f.FlowId.StartsWith("SAV-core-", StringComparison.Ordinal)).Select(f => f.Amount));
    }

    [Fact]
    public void The_other_levers_of_a_scenario_do_not_touch_a_deposit() {
        // Arrange
        NonMaturityDeposit deposit  = SavingsAccount(1000.00m);
        Scenario anyMarketStress    = Any.Scenario().WithInflowHaircut(0.5m).WithOutflowMultiplier(2m).WithDepositStableShareHaircut(0m).Generate();

        // Act
        IReadOnlyList<decimal> stressed = deposit.ToBehaviouralFlows(AsOf, anyMarketStress).Select(f => f.Amount).ToList();
        IReadOnlyList<decimal> baseline = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline).Select(f => f.Amount).ToList();

        // Assert
        Assert.Equal(baseline, stressed);
    }

    [Fact]
    public void When_the_whole_stable_share_is_eroded_everything_leaves_at_the_horizon() {
        // Arrange
        NonMaturityDeposit deposit = SavingsAccount(1000.00m);
        Scenario totalErosion      = Any.Scenario().WithDepositStableShareHaircut(1m).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, totalErosion);

        // Assert
        ContractualFlow only = Assert.Single(flows);
        Assert.Equal(("SAV-volatile", new DateOnly(2026, 4, 9), 1000.00m), (only.FlowId, only.ValueDate, only.Amount));
    }

    [Fact]
    public void A_deposit_with_no_stable_share_leaves_in_one_go() {
        // Arrange
        NonMaturityDeposit deposit = Any.NonMaturityDeposit()
                                        .WithDepositId("SAV")
                                        .WithBalance(250.00m)
                                        .WithAssumption(new BehaviouralAssumption(0m, 5, 24))
                                        .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        ContractualFlow only = Assert.Single(flows);
        Assert.Equal(("SAV-volatile", new DateOnly(2026, 3, 15), 250.00m), (only.FlowId, only.ValueDate, only.Amount));
    }

    [Fact]
    public void A_fully_stable_deposit_has_no_volatile_flow() {
        // Arrange
        NonMaturityDeposit deposit = Any.NonMaturityDeposit()
                                        .WithDepositId("SAV")
                                        .WithBalance(300.00m)
                                        .WithAssumption(new BehaviouralAssumption(1m, 5, 3))
                                        .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal(["SAV-core-1", "SAV-core-2", "SAV-core-3"], flows.Select(f => f.FlowId));
        Assert.Equal([100.00m, 100.00m, 100.00m], flows.Select(f => f.Amount));
    }

    [Fact]
    public void The_last_instalment_absorbs_the_rounding() {
        // Arrange: 100.00 over three months is 33.33, 33.33, 33.34
        NonMaturityDeposit deposit = Any.NonMaturityDeposit()
                                        .WithDepositId("SAV")
                                        .WithBalance(100.00m)
                                        .WithAssumption(new BehaviouralAssumption(1m, 5, 3))
                                        .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        Assert.Equal([33.33m, 33.33m, 33.34m], flows.Select(f => f.Amount));
    }

    [Fact]
    public void An_instalment_that_rounds_to_nothing_is_not_emitted_and_the_last_one_carries_the_whole_stable_part() {
        // Arrange: one cent over twelve months leaves 0.00 per instalment
        NonMaturityDeposit deposit = Any.NonMaturityDeposit()
                                        .WithDepositId("SAV")
                                        .WithBalance(0.01m)
                                        .WithAssumption(new BehaviouralAssumption(1m, 5, 12))
                                        .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(AsOf, Scenario.Baseline);

        // Assert
        ContractualFlow only = Assert.Single(flows);
        Assert.Equal(("SAV-core-12", new DateOnly(2027, 3, 10), 0.01m), (only.FlowId, only.ValueDate, only.Amount));
    }

    [Fact]
    public void Monthly_instalments_are_counted_from_the_as_of_date_so_a_short_month_does_not_drag_the_next_ones() {
        // Arrange
        NonMaturityDeposit deposit = Any.NonMaturityDeposit()
                                        .WithDepositId("SAV")
                                        .WithBalance(300.00m)
                                        .WithAssumption(new BehaviouralAssumption(1m, 5, 3))
                                        .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = deposit.ToBehaviouralFlows(new DateOnly(2026, 1, 31), Scenario.Baseline);

        // Assert
        Assert.Equal([new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 30)], flows.Select(f => f.ValueDate));
    }

    [Theory]
    [InlineData("100.00", "1", "0", 3)]
    [InlineData("0.01", "1", "0", 12)]
    [InlineData("1000.00", "0.333", "0.1", 7)]
    [InlineData("999.99", "0.5", "0.25", 11)]
    [InlineData("12345678.91", "0.87", "0.33", 120)]
    [InlineData("5.00", "0.999", "0", 120)]
    public void The_behavioural_outflows_add_up_to_the_balance_to_the_cent(string balance, string stableShare, string haircut, int months) {
        // Arrange
        NonMaturityDeposit deposit = Any.NonMaturityDeposit()
                                        .WithBalance(Money(balance))
                                        .WithAssumption(new BehaviouralAssumption(Money(stableShare), 10, months))
                                        .Generate();
        Scenario scenario = Any.Scenario().WithDepositStableShareHaircut(Money(haircut)).Generate();

        // Act
        decimal total = deposit.ToBehaviouralFlows(AsOf, scenario).Sum(f => f.Amount);

        // Assert
        Assert.Equal(Money(balance), total);
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
