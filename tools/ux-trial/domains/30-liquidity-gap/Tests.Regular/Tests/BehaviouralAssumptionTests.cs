using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class BehaviouralAssumptionTests {

    [Fact]
    public void An_assumption_keeps_the_three_parameters_it_was_given() {
        // Act
        BehaviouralAssumption assumption = new BehaviouralAssumption(0.65m, 30, 24);

        // Assert
        Assert.Equal(0.65m, assumption.StableShare);
        Assert.Equal(30, assumption.VolatileRunOffDays);
        Assert.Equal(24, assumption.CoreMaturityMonths);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("0.5")]
    public void A_stable_share_between_zero_and_one_inclusive_is_accepted(string share) {
        // Arrange
        decimal stableShare = decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        BehaviouralAssumption assumption = new BehaviouralAssumption(stableShare, 10, 12);

        // Assert
        Assert.Equal(stableShare, assumption.StableShare);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("1.0001")]
    [InlineData("2")]
    public void A_stable_share_outside_zero_to_one_is_refused(string share) {
        // Arrange
        decimal stableShare = decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BehaviouralAssumption(stableShare, 10, 12));

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
    [InlineData(-4)]
    [InlineData(121)]
    public void A_core_maturity_outside_one_to_120_months_is_refused(int months) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BehaviouralAssumption(0.5m, 10, months));

        // Assert
        Assert.Equal("coreMaturityMonths", refusal.ParamName);
    }

    [Fact]
    public void The_extreme_valid_horizon_and_maturity_are_accepted() {
        // Act
        BehaviouralAssumption shortest = new BehaviouralAssumption(0m, 1, 1);
        BehaviouralAssumption longest  = new BehaviouralAssumption(1m, 365, 120);

        // Assert
        Assert.Equal(1, shortest.VolatileRunOffDays);
        Assert.Equal(120, longest.CoreMaturityMonths);
    }

    [Theory]
    [InlineData(30, 1)]  // exactly one month of thirty days
    [InlineData(31, 1)]
    [InlineData(60, 2)]
    [InlineData(365, 12)] // 360 days < 365
    public void A_volatile_horizon_that_does_not_stay_shorter_than_the_core_maturity_is_refused(int days, int months) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BehaviouralAssumption(0.5m, days, months));

        // Assert
        Assert.Equal("volatileRunOffDays", refusal.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(29, 1)]
    [InlineData(59, 2)]
    [InlineData(365, 13)]
    public void A_volatile_horizon_just_below_the_core_maturity_is_accepted(int days, int months) {
        // Act
        BehaviouralAssumption assumption = new BehaviouralAssumption(0.5m, days, months);

        // Assert
        Assert.Equal(days, assumption.VolatileRunOffDays);
        Assert.Equal(months, assumption.CoreMaturityMonths);
    }

}
