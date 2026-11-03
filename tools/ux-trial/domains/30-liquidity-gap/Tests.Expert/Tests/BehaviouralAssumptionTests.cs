namespace LiquidityGap.Domain.Tests.Expert.Tests;

public sealed class BehaviouralAssumptionTests {

    [Fact]
    public void An_assumption_keeps_its_three_parameters() {
        // Act
        BehaviouralAssumption assumption = new BehaviouralAssumption(0.6m, 30, 12);

        // Assert
        Assert.Equal(0.6m, assumption.StableShare);
        Assert.Equal(30, assumption.VolatileRunOffDays);
        Assert.Equal(12, assumption.CoreMaturityMonths);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void The_stable_share_may_be_none_or_all_of_the_balance(string share) {
        // Act
        BehaviouralAssumption assumption = new BehaviouralAssumption(decimal.Parse(share), 10, 12);

        // Assert
        Assert.Equal(decimal.Parse(share), assumption.StableShare);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("1.0001")]
    public void A_stable_share_outside_zero_to_one_is_refused(string share) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BehaviouralAssumption(decimal.Parse(share), 10, 12));

        // Assert
        Assert.Equal("stableShare", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    public void A_volatile_horizon_outside_one_to_365_days_is_refused(int days) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BehaviouralAssumption(0.5m, days, 120));

        // Assert
        Assert.Equal("volatileRunOffDays", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void A_core_maturity_outside_one_to_120_months_is_refused(int months) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BehaviouralAssumption(0.5m, 1, months));

        // Assert
        Assert.Equal("coreMaturityMonths", refusal.ParamName);
    }

    [Theory]
    [InlineData(29, 1)]
    [InlineData(359, 12)]
    [InlineData(365, 13)]
    [InlineData(1, 1)]
    public void A_volatile_horizon_shorter_than_the_core_maturity_is_accepted(int days, int months) {
        // Act
        BehaviouralAssumption assumption = new BehaviouralAssumption(0.5m, days, months);

        // Assert
        Assert.Equal(days, assumption.VolatileRunOffDays);
    }

    [Theory]
    [InlineData(30, 1)]
    [InlineData(31, 1)]
    [InlineData(360, 12)]
    [InlineData(365, 12)]
    public void A_volatile_horizon_as_long_as_the_core_maturity_is_refused(int days, int months) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BehaviouralAssumption(0.5m, days, months));

        // Assert
        Assert.Equal("volatileRunOffDays", refusal.ParamName);
    }

    [Fact]
    public void The_widest_assumption_is_accepted() {
        // Act
        BehaviouralAssumption assumption = new BehaviouralAssumption(1m, 365, 120);

        // Assert
        Assert.Equal(120, assumption.CoreMaturityMonths);
    }

}
