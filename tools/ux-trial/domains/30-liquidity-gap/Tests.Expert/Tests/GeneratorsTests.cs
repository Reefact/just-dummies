namespace LiquidityGap.Domain.Tests.Expert.Tests;

/// <summary>The hand-completed generators draw what the constructors accept, however often they are asked.</summary>
public sealed class GeneratorsTests {

    private const int Draws = 500;

    [Fact]
    public void Behavioural_assumptions_always_respect_the_tie_between_horizon_and_maturity() {
        // Act / Assert: the constructor refuses a horizon that outlasts the maturity, so drawing is the check
        BehaviouralAssumption[] assumptions = Any.ArrayOf(Any.BehaviouralAssumption()).WithCount(Draws).Generate();

        Assert.All(assumptions, assumption => Assert.True(assumption.VolatileRunOffDays < assumption.CoreMaturityMonths * 30));
    }

    [Fact]
    public void Time_buckets_are_drawn_closed_and_open_ended() {
        // Act
        TimeBucket[] buckets = Any.ArrayOf(Any.TimeBucket()).WithCount(Draws).Generate();

        // Assert: both shapes turn up in 500 draws, and a closed one ends after it starts
        Assert.Contains(buckets, bucket => bucket.IsOpenEnded);
        Assert.Contains(buckets, bucket => !bucket.IsOpenEnded);
        Assert.All(buckets.Where(b => !b.IsOpenEnded), bucket => Assert.True(bucket.EndDay > bucket.StartDay));
    }

    [Fact]
    public void Portfolios_with_positions_of_every_kind_can_be_projected_whatever_is_drawn() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = Any.Portfolio().WithReportingCurrency(anyCurrency).Generate();
        portfolio.AddFlow(Any.ContractualFlow().WithCurrency(anyCurrency).WithValueDate(portfolio.AsOfDate.AddDays(3)).WithFlowId("F").Generate());
        portfolio.AddSchedule(Any.CashFlowSchedule().WithCurrency(anyCurrency).WithScheduleId("S").WithFirstPaymentDate(portfolio.AsOfDate).Generate());
        portfolio.AddDeposit(Any.NonMaturityDeposit().WithCurrency(anyCurrency).WithDepositId("D").Generate());
        Scenario anyScenario = Any.Scenario().Generate();

        // Act
        LiquidityProfile profile = portfolio.Project(anyScenario, TestBuckets.ThreeBucketLadder());

        // Assert: nothing is lost on the way in
        Assert.Equal(3, profile.Gaps.Count);
    }

    [Fact]
    public void Scenarios_currencies_flows_schedules_and_deposits_are_all_constructible() {
        // Act
        Exception? refusal = Record.Exception(() => {
            _ = Any.ArrayOf(Any.Scenario()).WithCount(Draws).Generate();
            _ = Any.ArrayOf(Any.Currency()).WithCount(Draws).Generate();
            _ = Any.ArrayOf(Any.ContractualFlow()).WithCount(Draws).Generate();
            _ = Any.ArrayOf(Any.CashFlowSchedule()).WithCount(Draws).Generate();
            _ = Any.ArrayOf(Any.NonMaturityDeposit()).WithCount(Draws).Generate();
            _ = Any.ArrayOf(Any.BucketGap()).WithCount(Draws).Generate();
        });

        // Assert
        Assert.Null(refusal);
    }

}
