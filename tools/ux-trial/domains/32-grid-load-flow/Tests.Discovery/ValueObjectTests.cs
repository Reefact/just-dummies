using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies.Xunit;

using Xunit;

namespace GridLoadFlow.Tests.Discovery;

[Reproducible]
public sealed class ValueObjectTests {

    [Fact]
    public void A_30_MW_and_40_Mvar_injection_has_50_MVA_apparent_power_and_a_0_6_power_factor() {
        // Arrange
        PowerInjection injection = new PowerInjection(30.0, 40.0);

        // Act
        double apparent    = injection.ApparentMva;
        double powerFactor = injection.PowerFactor;

        // Assert
        Assert.Equal(50.0, apparent, 9);
        Assert.Equal(0.6, powerFactor, 9);
    }

    [Fact]
    public void A_zero_injection_has_a_unity_power_factor() {
        // Arrange
        PowerInjection injection = PowerInjection.Zero;

        // Act
        double powerFactor = injection.PowerFactor;

        // Assert
        Assert.Equal(1.0, powerFactor);
    }

    [Fact]
    public void An_injection_converts_to_per_unit_on_the_system_base() {
        // Arrange
        PowerInjection injection = new PowerInjection(50.0, -20.0);

        // Act
        Complex perUnit = injection.ToPerUnit(100.0);

        // Assert
        Assert.Equal(new Complex(0.5, -0.2), perUnit);
    }

    [Fact]
    public void A_purely_reactive_impedance_has_a_purely_susceptive_admittance_and_an_infinite_X_over_R() {
        // Arrange
        Impedance impedance = new Impedance(0.0, 0.1);

        // Act
        Complex admittance = impedance.Admittance;
        double  xOverR     = impedance.XOverR;

        // Assert
        Assert.Equal(0.0, admittance.Real, 9);
        Assert.Equal(-10.0, admittance.Imaginary, 9);
        Assert.Equal(double.PositiveInfinity, xOverR);
    }

    [Theory]
    [InlineData(0.95, true)]
    [InlineData(1.05, true)]
    [InlineData(0.9499, false)]
    [InlineData(1.0501, false)]
    public void The_standard_band_holds_its_own_edges_and_nothing_beyond(double magnitudePu, bool expected) {
        // Arrange
        VoltageLimits limits = VoltageLimits.Standard;

        // Act
        bool contains = limits.Contains(magnitudePu);

        // Assert
        Assert.Equal(expected, contains);
    }

    [Fact]
    public void A_band_that_does_not_hold_the_nominal_voltage_is_refused() {
        // Arrange
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new VoltageLimits(1.02, 1.10));

        // Assert
        Assert.Equal("minimumPu", error.ParamName);
    }

}
