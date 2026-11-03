using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class ScenarioTests {

    [Fact]
    public void The_baseline_leaves_everything_as_agreed() {
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
    public void The_baseline_is_one_shared_instance() {
        // Act & Assert
        Assert.Same(Scenario.Baseline, Scenario.Baseline);
    }

    [Fact]
    public void A_scenario_keeps_the_parameters_it_was_given() {
        // Act
        Scenario scenario = new Scenario("Severe", 0.2m, 1.5m, 0.4m);

        // Assert
        Assert.Equal("Severe", scenario.Name);
        Assert.Equal(0.2m, scenario.InflowHaircut);
        Assert.Equal(1.5m, scenario.OutflowMultiplier);
        Assert.Equal(0.4m, scenario.DepositStableShareHaircut);
    }

    [Fact]
    public void A_scenario_with_an_inflow_haircut_is_stressed() {
        // Arrange
        Scenario scenario = Any.Scenario().WithInflowHaircut(0.01m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0m).Generate();

        // Act & Assert
        Assert.True(scenario.IsStressed);
    }

    [Fact]
    public void A_scenario_with_an_outflow_multiplier_above_one_is_stressed() {
        // Arrange
        Scenario scenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1.01m).WithDepositStableShareHaircut(0m).Generate();

        // Act & Assert
        Assert.True(scenario.IsStressed);
    }

    [Fact]
    public void A_scenario_with_a_stable_share_haircut_is_stressed() {
        // Arrange
        Scenario scenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0.01m).Generate();

        // Act & Assert
        Assert.True(scenario.IsStressed);
    }

    [Fact]
    public void A_scenario_with_no_haircut_no_multiplier_and_no_stable_share_haircut_is_not_stressed_whatever_its_name() {
        // Arrange
        Scenario scenario = Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1m).WithDepositStableShareHaircut(0m).Generate();

        // Act & Assert
        Assert.False(scenario.IsStressed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_blank_name_is_refused(string? blankName) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Scenario(blankName!, 0m, 1m, 0m));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_name_of_sixty_four_characters_is_accepted_and_sixty_five_is_refused() {
        // Arrange
        string longestName = new string('n', 64);
        string tooLong     = new string('n', 65);

        // Act
        Scenario scenario = new Scenario(longestName, 0m, 1m, 0m);
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Scenario(tooLong, 0m, 1m, 0m));

        // Assert
        Assert.Equal(longestName, scenario.Name);
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.9999")]
    public void An_inflow_haircut_in_zero_inclusive_to_one_exclusive_is_accepted(string haircut) {
        // Arrange
        decimal inflowHaircut = decimal.Parse(haircut, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        Scenario scenario = Any.Scenario().WithInflowHaircut(inflowHaircut).Generate();

        // Assert
        Assert.Equal(inflowHaircut, scenario.InflowHaircut);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1")]
    [InlineData("1.5")]
    public void An_inflow_haircut_outside_zero_to_one_exclusive_is_refused(string haircut) {
        // Arrange
        string  anyName       = Any.String().NotBlank().WithMaxLength(64).Generate();
        decimal inflowHaircut = decimal.Parse(haircut, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Scenario(anyName, inflowHaircut, 1m, 0m));

        // Assert
        Assert.Equal("inflowHaircut", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.99")]
    [InlineData("3.01")]
    [InlineData("0")]
    [InlineData("-1")]
    public void An_outflow_multiplier_outside_one_to_three_is_refused(string multiplier) {
        // Arrange
        string  anyName            = Any.String().NotBlank().WithMaxLength(64).Generate();
        decimal outflowMultiplier = decimal.Parse(multiplier, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Scenario(anyName, 0m, outflowMultiplier, 0m));

        // Assert
        Assert.Equal("outflowMultiplier", refusal.ParamName);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("3")]
    public void The_outflow_multiplier_bounds_one_and_three_are_accepted(string multiplier) {
        // Arrange
        decimal outflowMultiplier = decimal.Parse(multiplier, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        Scenario scenario = Any.Scenario().WithOutflowMultiplier(outflowMultiplier).Generate();

        // Assert
        Assert.Equal(outflowMultiplier, scenario.OutflowMultiplier);
    }

    [Theory]
    [InlineData("-0.1")]
    [InlineData("1.01")]
    public void A_stable_share_haircut_outside_zero_to_one_is_refused(string haircut) {
        // Arrange
        string  anyName = Any.String().NotBlank().WithMaxLength(64).Generate();
        decimal stableShareHaircut = decimal.Parse(haircut, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Scenario(anyName, 0m, 1m, stableShareHaircut));

        // Assert
        Assert.Equal("depositStableShareHaircut", refusal.ParamName);
    }

    [Fact]
    public void A_stable_share_haircut_of_exactly_one_is_accepted() {
        // Act
        Scenario scenario = Any.Scenario().WithDepositStableShareHaircut(1m).Generate();

        // Assert
        Assert.Equal(1m, scenario.DepositStableShareHaircut);
    }

}
