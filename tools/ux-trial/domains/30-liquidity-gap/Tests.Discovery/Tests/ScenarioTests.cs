using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class ScenarioTests {

    [Fact]
    public void The_baseline_is_business_as_usual() {
        // Act
        Scenario baseline = Scenario.Baseline;

        // Assert
        Assert.Equal("Baseline", baseline.Name);
        Assert.Equal(0m, baseline.InflowHaircut);
        Assert.Equal(1m, baseline.OutflowMultiplier);
        Assert.Equal(0m, baseline.DepositStableShareHaircut);
        Assert.False(baseline.IsStressed);
    }

    [Fact]
    public void A_scenario_keeps_what_it_was_built_with() {
        // Act
        Scenario scenario = new Scenario("Severe", 0.25m, 1.5m, 0.4m);

        // Assert
        Assert.Equal("Severe", scenario.Name);
        Assert.Equal(0.25m, scenario.InflowHaircut);
        Assert.Equal(1.5m, scenario.OutflowMultiplier);
        Assert.Equal(0.4m, scenario.DepositStableShareHaircut);
    }

    [Fact]
    public void A_scenario_with_no_haircut_and_no_multiplier_is_not_stressed() {
        // Arrange
        Scenario scenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0m).Generate();

        // Assert
        Assert.False(scenario.IsStressed);
    }

    [Theory]
    [InlineData("0.01", "1", "0")]
    [InlineData("0", "1.01", "0")]
    [InlineData("0", "1", "0.01")]
    public void Any_single_stress_makes_a_scenario_stressed(string inflowHaircut, string outflowMultiplier, string stableHaircut) {
        // Arrange
        Scenario scenario = Any.Scenario().WithInflowHaircut(Parse(inflowHaircut))
                                          .WithOutflowMultiplier(Parse(outflowMultiplier))
                                          .WithDepositStableShareHaircut(Parse(stableHaircut))
                                          .Generate();

        // Assert
        Assert.True(scenario.IsStressed);
    }

    [Fact]
    public void A_name_of_64_characters_is_accepted() {
        // Arrange
        string name64 = new string('S', 64);

        // Act
        Scenario scenario = Any.Scenario().WithName(name64).Generate();

        // Assert
        Assert.Equal(name64, scenario.Name);
    }

    [Fact]
    public void A_name_of_65_characters_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Scenario().WithName(new string('S', 65)).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\r\n")]
    public void A_blank_name_is_refused(string name) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Scenario().WithName(name).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.9999")]
    public void An_inflow_haircut_below_one_is_accepted(string haircut) {
        // Act
        Scenario scenario = Any.Scenario().WithInflowHaircut(Parse(haircut)).Generate();

        // Assert
        Assert.Equal(Parse(haircut), scenario.InflowHaircut);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("1")]
    [InlineData("1.5")]
    public void An_inflow_haircut_outside_zero_inclusive_to_one_exclusive_is_refused(string haircut) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Scenario().WithInflowHaircut(Parse(haircut)).Generate());

        // Assert
        Assert.Equal("inflowHaircut", refusal.ParamName);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("3")]
    [InlineData("2.25")]
    public void An_outflow_multiplier_between_one_and_three_is_accepted(string multiplier) {
        // Act
        Scenario scenario = Any.Scenario().WithOutflowMultiplier(Parse(multiplier)).Generate();

        // Assert
        Assert.Equal(Parse(multiplier), scenario.OutflowMultiplier);
    }

    [Theory]
    [InlineData("0.9999")]
    [InlineData("0")]
    [InlineData("3.0001")]
    [InlineData("10")]
    public void An_outflow_multiplier_outside_one_to_three_is_refused(string multiplier) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Scenario().WithOutflowMultiplier(Parse(multiplier)).Generate());

        // Assert
        Assert.Equal("outflowMultiplier", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void The_limits_of_the_stable_share_haircut_are_accepted(string haircut) {
        // Act
        Scenario scenario = Any.Scenario().WithDepositStableShareHaircut(Parse(haircut)).Generate();

        // Assert
        Assert.Equal(Parse(haircut), scenario.DepositStableShareHaircut);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("1.0001")]
    public void A_stable_share_haircut_outside_zero_to_one_is_refused(string haircut) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Scenario().WithDepositStableShareHaircut(Parse(haircut)).Generate());

        // Assert
        Assert.Equal("depositStableShareHaircut", refusal.ParamName);
    }

    private static decimal Parse(string value) {
        return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

}
