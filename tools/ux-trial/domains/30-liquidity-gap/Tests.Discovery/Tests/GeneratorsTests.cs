using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

/// <summary>The hand-completed generators draw valid objects on every run: building one is the assertion.</summary>
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Currencies_are_drawn_valid() {
        List<Currency> currencies = Any.ListOf(Any.Currency()).WithCount(Draws).Generate();

        Assert.Equal(Draws, currencies.Count);
    }

    [Fact]
    public void Behavioural_assumptions_are_drawn_valid_whatever_the_core_maturity() {
        List<BehaviouralAssumption> assumptions = Any.ListOf(Any.BehaviouralAssumption()).WithCount(Draws).Generate();

        Assert.All(assumptions, a => Assert.True(a.VolatileRunOffDays < a.CoreMaturityMonths * 30));
    }

    [Fact]
    public void Time_buckets_are_drawn_valid_bounded_or_open() {
        List<TimeBucket> buckets = Any.ListOf(Any.TimeBucket()).WithCount(Draws).Generate();

        Assert.Contains(buckets, b => b.IsOpenEnded);
        Assert.Contains(buckets, b => !b.IsOpenEnded);
    }

    [Fact]
    public void Schedules_flows_and_deposits_are_drawn_valid() {
        List<CashFlowSchedule>    schedules = Any.ListOf(Any.CashFlowSchedule()).WithCount(Draws).Generate();
        List<ContractualFlow>     flows     = Any.ListOf(Any.ContractualFlow()).WithCount(Draws).Generate();
        List<NonMaturityDeposit>  deposits  = Any.ListOf(Any.NonMaturityDeposit()).WithCount(Draws).Generate();

        Assert.All(schedules, s => Assert.Equal(s.NumberOfPayments, s.Flows().Count));
        Assert.All(flows, f => Assert.True(f.Amount > 0m));
        Assert.All(deposits, d => Assert.Equal(d.Balance, d.ToBehaviouralFlows(new DateOnly(2026, 1, 1), Scenario.Baseline).Sum(f => f.Amount)));
    }

    [Fact]
    public void Scenarios_portfolios_and_profiles_are_drawn_valid() {
        List<Scenario>         scenarios  = Any.ListOf(Any.Scenario()).WithCount(Draws).Generate();
        List<Portfolio>        portfolios = Any.ListOf(Any.Portfolio()).WithCount(Draws).Generate();
        List<LiquidityProfile> profiles   = Any.ListOf(Any.LiquidityProfile()).WithCount(Draws).Generate();

        Assert.Equal(Draws, scenarios.Count);
        Assert.Equal(Draws, portfolios.Count);
        Assert.Equal(Draws, profiles.Count);
    }

}
