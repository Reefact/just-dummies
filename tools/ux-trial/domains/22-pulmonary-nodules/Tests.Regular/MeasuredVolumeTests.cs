using PulmonaryNodules.Domain;
using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class MeasuredVolumeTests {

    [Fact]
    public void The_bounds_frame_the_volume_by_its_uncertainty() {
        // Arrange
        MeasuredVolume volume = new MeasuredVolume(250.0, 25.0);

        // Act
        (double lower, double upper) = (volume.LowerBoundMm3, volume.UpperBoundMm3);

        // Assert
        Assert.Equal(225.0, lower);
        Assert.Equal(275.0, upper);
    }

    [Fact]
    public void The_equivalent_diameter_is_that_of_the_sphere_of_the_same_volume() {
        // Arrange
        MeasuredVolume tenMillimetreSphere = new MeasuredVolume(4.0 / 3.0 * Math.PI * 125.0, 0.0);

        // Act
        double diameter = tenMillimetreSphere.EquivalentDiameterMm;

        // Assert
        Assert.Equal(10.0, diameter, 9);
    }

    [Fact]
    public void An_uncertainty_as_large_as_the_volume_is_rejected() {
        // Arrange
        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new MeasuredVolume(80.0, 80.0));

        // Assert
        Assert.Equal("uncertaintyMm3", thrown.ParamName);
    }

}
