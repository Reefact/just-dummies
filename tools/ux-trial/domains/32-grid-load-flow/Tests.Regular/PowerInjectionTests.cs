using System.Numerics;

using GridLoadFlow.Domain;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class PowerInjectionTests {

    [Fact]
    public void Apparent_power_is_the_magnitude_of_P_and_Q() {
        // Arrange
        PowerInjection injection = new(30.0, 40.0);

        // Act
        double apparentMva = injection.ApparentMva;

        // Assert
        Assert.Equal(50.0, apparentMva, 9);
    }

    [Fact]
    public void Power_factor_is_the_active_share_of_the_apparent_power() {
        // Arrange
        PowerInjection injection = new(-30.0, 40.0);

        // Act
        double powerFactor = injection.PowerFactor;

        // Assert
        Assert.Equal(0.6, powerFactor, 9);
    }

    [Fact]
    public void Per_unit_divides_both_components_by_the_base() {
        // Arrange
        PowerInjection injection = new(50.0, -20.0);

        // Act
        Complex perUnit = injection.ToPerUnit(100.0);

        // Assert
        Assert.Equal(new Complex(0.5, -0.2), perUnit);
    }
}
