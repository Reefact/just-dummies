using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class SolverSettingsTests {

    [Theory]
    [InlineData(1e-12, 1, 1.0)]
    [InlineData(1e-2, 10_000, 1.999)]
    public void Accepts_the_extreme_values_of_each_setting(double tolerancePu, int maximumIterations, double accelerationFactor) {
        // Act
        SolverSettings settings = new SolverSettings(tolerancePu, maximumIterations, accelerationFactor);

        // Assert
        Assert.Equal(tolerancePu, settings.TolerancePu);
        Assert.Equal(maximumIterations, settings.MaximumIterations);
        Assert.Equal(accelerationFactor, settings.AccelerationFactor);
    }

    [Theory]
    [InlineData(9.9e-13)]
    [InlineData(0.0101)]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_tolerance_outside_the_supported_range(double tolerancePu) {
        // Arrange
        SolverSettings anySettings = Any.SolverSettings().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SolverSettings(tolerancePu, anySettings.MaximumIterations, anySettings.AccelerationFactor));

        // Assert
        Assert.Equal("tolerancePu", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void Refuses_an_iteration_budget_outside_one_to_ten_thousand(int maximumIterations) {
        // Arrange
        SolverSettings anySettings = Any.SolverSettings().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SolverSettings(anySettings.TolerancePu, maximumIterations, anySettings.AccelerationFactor));

        // Assert
        Assert.Equal("maximumIterations", exception.ParamName);
    }

    [Theory]
    [InlineData(0.999)]
    [InlineData(2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_an_acceleration_factor_outside_one_inclusive_to_two_exclusive(double accelerationFactor) {
        // Arrange
        SolverSettings anySettings = Any.SolverSettings().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SolverSettings(anySettings.TolerancePu, anySettings.MaximumIterations, accelerationFactor));

        // Assert
        Assert.Equal("accelerationFactor", exception.ParamName);
    }

    [Fact]
    public void The_default_settings_are_a_millionth_of_a_per_unit_in_five_hundred_iterations_at_one_point_four() {
        Assert.Equal(1e-6, SolverSettings.Default.TolerancePu);
        Assert.Equal(500, SolverSettings.Default.MaximumIterations);
        Assert.Equal(1.4, SolverSettings.Default.AccelerationFactor);
    }

}
