namespace LiquidityGap.Domain.Tests.Expert.Tests;

public sealed class ScenarioTests {

    [Fact]
    public void The_baseline_leaves_everything_as_agreed() {
        // Assert
        Assert.Equal("Baseline", Scenario.Baseline.Name);
        Assert.Equal(0m, Scenario.Baseline.InflowHaircut);
        Assert.Equal(1m, Scenario.Baseline.OutflowMultiplier);
        Assert.Equal(0m, Scenario.Baseline.DepositStableShareHaircut);
        Assert.False(Scenario.Baseline.IsStressed);
    }

    [Fact]
    public void A_scenario_keeps_its_name_and_its_three_levers() {
        // Act
        Scenario scenario = new Scenario("Severe", 0.25m, 1.5m, 0.4m);

        // Assert
        Assert.Equal("Severe", scenario.Name);
        Assert.Equal(0.25m, scenario.InflowHaircut);
        Assert.Equal(1.5m, scenario.OutflowMultiplier);
        Assert.Equal(0.4m, scenario.DepositStableShareHaircut);
    }

    [Fact]
    public void A_scenario_with_no_lever_pulled_is_not_stressed() {
        // Arrange
        Scenario anyUnstressedScenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0m).Generate();

        // Assert
        Assert.False(anyUnstressedScenario.IsStressed);
    }

    [Fact]
    public void Cutting_inflows_alone_stresses_a_scenario() {
        // Arrange
        Scenario anyScenario = Any.Scenario().WithInflowHaircut(0.01m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0m).Generate();

        // Assert
        Assert.True(anyScenario.IsStressed);
    }

    [Fact]
    public void Scaling_outflows_alone_stresses_a_scenario() {
        // Arrange
        Scenario anyScenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1.01m).WithDepositStableShareHaircut(0m).Generate();

        // Assert
        Assert.True(anyScenario.IsStressed);
    }

    [Fact]
    public void Eroding_the_stable_share_alone_stresses_a_scenario() {
        // Arrange
        Scenario anyScenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0.01m).Generate();

        // Assert
        Assert.True(anyScenario.IsStressed);
    }

    [Fact]
    public void A_name_of_64_characters_is_accepted() {
        // Arrange
        string name = new string('N', 64);

        // Act
        Scenario scenario = Any.Scenario().WithName(name).Generate();

        // Assert
        Assert.Equal(name, scenario.Name);
    }

    [Fact]
    public void A_name_of_65_characters_is_refused() {
        // Arrange
        string tooLong = new string('N', 65);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Scenario().WithName(tooLong).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_name_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Scenario().WithName(blank).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.9999")]
    public void An_inflow_haircut_below_one_is_accepted(string haircut) {
        // Act
        Scenario scenario = Any.Scenario().WithInflowHaircut(decimal.Parse(haircut)).Generate();

        // Assert
        Assert.Equal(decimal.Parse(haircut), scenario.InflowHaircut);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("1")]
    public void An_inflow_haircut_outside_zero_included_to_one_excluded_is_refused(string haircut) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Scenario().WithInflowHaircut(decimal.Parse(haircut)).Generate());

        // Assert
        Assert.Equal("inflowHaircut", refusal.ParamName);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("3")]
    public void An_outflow_multiplier_from_one_to_three_is_accepted(string multiplier) {
        // Act
        Scenario scenario = Any.Scenario().WithOutflowMultiplier(decimal.Parse(multiplier)).Generate();

        // Assert
        Assert.Equal(decimal.Parse(multiplier), scenario.OutflowMultiplier);
    }

    [Theory]
    [InlineData("0.9999")]
    [InlineData("3.0001")]
    public void An_outflow_multiplier_outside_one_to_three_is_refused(string multiplier) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Scenario().WithOutflowMultiplier(decimal.Parse(multiplier)).Generate());

        // Assert
        Assert.Equal("outflowMultiplier", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("1.0001")]
    public void A_stable_share_haircut_outside_zero_to_one_is_refused(string haircut) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Scenario().WithDepositStableShareHaircut(decimal.Parse(haircut)).Generate());

        // Assert
        Assert.Equal("depositStableShareHaircut", refusal.ParamName);
    }

    [Fact]
    public void A_stable_share_haircut_of_one_is_accepted() {
        // Act
        Scenario scenario = Any.Scenario().WithDepositStableShareHaircut(1m).Generate();

        // Assert
        Assert.Equal(1m, scenario.DepositStableShareHaircut);
    }

}
