using System.Numerics;

using GridLoadFlow.Domain;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class ImpedanceTests {

    [Fact]
    public void Admittance_is_the_reciprocal_of_the_impedance() {
        // Arrange
        Impedance impedance = new(0.03, 0.04);

        // Act
        Complex admittance = impedance.Admittance;

        // Assert
        Assert.Equal(12.0, admittance.Real, 9);
        Assert.Equal(-16.0, admittance.Imaginary, 9);
    }

    [Fact]
    public void A_lossless_branch_has_an_infinite_X_over_R() {
        // Arrange
        Impedance impedance = Arbitrary.Impedance().WithResistancePu(0.0).Generate();

        // Act
        double xOverR = impedance.XOverR;

        // Assert
        Assert.Equal(double.PositiveInfinity, xOverR);
    }
}
