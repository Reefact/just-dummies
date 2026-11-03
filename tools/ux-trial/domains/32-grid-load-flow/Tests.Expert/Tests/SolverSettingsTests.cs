using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class SolverSettingsTests {

    [Theory]
    [InlineData(1e-12, 1, 1.0)]
    [InlineData(1e-2, 10_000, 1.9999999)]
    public void Settings_on_the_edge_of_their_ranges_are_kept(double tolerance, int iterations, double acceleration) {
        SolverSettings settings = new SolverSettings(tolerance, iterations, acceleration);

        Assert.Equal(tolerance, settings.TolerancePu);
        Assert.Equal(iterations, settings.MaximumIterations);
        Assert.Equal(acceleration, settings.AccelerationFactor);
    }

    [Theory]
    [InlineData(9.99e-13)]
    [InlineData(0.0100001)]
    [InlineData(0d)]
    [InlineData(double.NaN)]
    public void A_tolerance_outside_its_range_is_refused(double tolerance) {
        int    anyIterations   = Any.Int32().Between(1, 10_000).Generate();
        double anyAcceleration = Any.Double().Between(1d, 1.99d).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SolverSettings(tolerance, anyIterations, anyAcceleration));

        Assert.Equal("tolerancePu", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void An_iteration_budget_outside_its_range_is_refused(int iterations) {
        double anyTolerance    = Any.Double().Between(1e-12, 1e-2).Generate();
        double anyAcceleration = Any.Double().Between(1d, 1.99d).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SolverSettings(anyTolerance, iterations, anyAcceleration));

        Assert.Equal("maximumIterations", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.9999)]
    [InlineData(2.0)]        // the ceiling is exclusive
    [InlineData(2.5)]
    [InlineData(double.NaN)]
    public void An_acceleration_factor_outside_one_inclusive_to_two_exclusive_is_refused(double acceleration) {
        double anyTolerance  = Any.Double().Between(1e-12, 1e-2).Generate();
        int    anyIterations = Any.Int32().Between(1, 10_000).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SolverSettings(anyTolerance, anyIterations, acceleration));

        Assert.Equal("accelerationFactor", refusal.ParamName);
    }

    [Fact]
    public void The_default_settings_are_a_micro_pu_tolerance_with_five_hundred_iterations_and_a_1_4_acceleration() {
        Assert.Equal(1e-6, SolverSettings.Default.TolerancePu);
        Assert.Equal(500, SolverSettings.Default.MaximumIterations);
        Assert.Equal(1.4, SolverSettings.Default.AccelerationFactor);
    }

}
