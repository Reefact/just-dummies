using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

namespace GridLoadFlow.Tests.Expert;

public sealed class ValueObjectTests {

    [Fact]
    public void Apparent_power_and_power_factor_follow_the_power_triangle() {
        // Arrange
        PowerInjection injection = new(30.0, 40.0);

        // Act
        double apparentMva = injection.ApparentMva;
        double powerFactor = injection.PowerFactor;

        // Assert
        Assert.Equal(50.0, apparentMva, 9);
        Assert.Equal(0.6, powerFactor, 9);
    }

    [Fact]
    public void A_zero_injection_has_unity_power_factor() {
        // Arrange
        PowerInjection nothing = PowerInjection.Zero;

        // Act
        double powerFactor = nothing.PowerFactor;

        // Assert
        Assert.Equal(1.0, powerFactor);
    }

    [Fact]
    public void An_injection_converts_to_per_unit_on_the_system_base() {
        // Arrange
        PowerInjection injection = new(50.0, -20.0);

        // Act
        Complex perUnit = injection.ToPerUnit(100.0);

        // Assert
        Assert.Equal(new Complex(0.5, -0.2), perUnit);
    }

    [Fact]
    public void A_purely_reactive_branch_has_an_infinite_x_over_r_and_a_purely_susceptive_admittance() {
        // Arrange
        Impedance reactance = new(0.0, 0.1);

        // Act
        double  xOverR     = reactance.XOverR;
        Complex admittance = reactance.Admittance;

        // Assert
        Assert.Equal(double.PositiveInfinity, xOverR);
        Assert.Equal(0.0, admittance.Real, 9);
        Assert.Equal(-10.0, admittance.Imaginary, 9);
    }

    [Fact]
    public void A_voltage_band_includes_both_of_its_limits() {
        // Arrange
        VoltageLimits band = new(0.9, 1.1);

        // Act
        bool lowerEdge = band.Contains(0.9);
        bool upperEdge = band.Contains(1.1);
        bool beyond    = band.Contains(1.1001);

        // Assert
        Assert.True(lowerEdge);
        Assert.True(upperEdge);
        Assert.False(beyond);
    }

    [Fact]
    public void A_voltage_band_that_excludes_nominal_voltage_is_refused() {
        // Arrange
        double minimumAboveNominal = 1.02;
        double anyMaximum          = Any.Double().GreaterThan(minimumAboveNominal).LessThanOrEqualTo(VoltageLimits.AbsoluteMaximumPu).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new VoltageLimits(minimumAboveNominal, anyMaximum));

        // Assert
        ArgumentOutOfRangeException outOfRange = Assert.IsType<ArgumentOutOfRangeException>(refusal);
        Assert.Equal("minimumPu", outOfRange.ParamName);
    }

    [Fact]
    public void A_bus_id_must_be_upper_case() {
        // Arrange
        string lowerCaseId = "bus-7";

        // Act
        Exception? refusal = Record.Exception(() => new BusId(lowerCaseId));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

}
