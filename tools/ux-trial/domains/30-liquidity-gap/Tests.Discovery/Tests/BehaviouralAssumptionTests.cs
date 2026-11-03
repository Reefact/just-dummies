using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class BehaviouralAssumptionTests {

    [Fact]
    public void An_assumption_keeps_what_it_was_built_with() {
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
        // Act
        BehaviouralAssumption assumption = Any.BehaviouralAssumption().WithStableShare(decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture)).Generate();

        // Assert
        Assert.Equal(decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture), assumption.StableShare);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("1.0001")]
    [InlineData("2")]
    public void A_stable_share_outside_zero_to_one_is_refused(string share) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.BehaviouralAssumption().WithStableShare(decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture)).Generate());

        // Assert
        Assert.Equal("stableShare", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(366)]
    public void A_volatile_horizon_outside_1_to_365_days_is_refused(int days) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BehaviouralAssumption(Any.Decimal().Between(0m, 1m).Generate(), days, 120));

        // Assert
        Assert.Equal("volatileRunOffDays", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void A_core_maturity_outside_1_to_120_months_is_refused(int months) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BehaviouralAssumption(Any.Decimal().Between(0m, 1m).Generate(), 1, months));

        // Assert
        Assert.Equal("coreMaturityMonths", refusal.ParamName);
    }

    [Theory]
    [InlineData(30, 1)]
    [InlineData(31, 1)]
    [InlineData(360, 12)]
    [InlineData(365, 12)]
    [InlineData(365, 1)]
    public void A_volatile_horizon_not_shorter_than_the_core_maturity_is_refused(int days, int months) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new BehaviouralAssumption(Any.Decimal().Between(0m, 1m).Generate(), days, months));

        // Assert
        Assert.Equal("volatileRunOffDays", refusal.ParamName);
    }

    [Theory]
    [InlineData(29, 1)]
    [InlineData(359, 12)]
    [InlineData(1, 1)]
    [InlineData(365, 13)]
    [InlineData(365, 120)]
    public void A_volatile_horizon_shorter_than_the_core_maturity_is_accepted(int days, int months) {
        // Act
        BehaviouralAssumption assumption = new BehaviouralAssumption(Any.Decimal().Between(0m, 1m).Generate(), days, months);

        // Assert
        Assert.Equal(days, assumption.VolatileRunOffDays);
        Assert.Equal(months, assumption.CoreMaturityMonths);
    }

}
