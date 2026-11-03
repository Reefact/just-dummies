using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class MeasuredVolumeTests {

    [Fact]
    public void The_bounds_are_the_volume_minus_and_plus_its_uncertainty() {
        // Arrange
        MeasuredVolume volume = new MeasuredVolume(100.0, 10.0);

        // Act / Assert
        Assert.Equal(90.0, volume.LowerBoundMm3);
        Assert.Equal(110.0, volume.UpperBoundMm3);
    }

    [Fact]
    public void A_measurement_without_uncertainty_has_equal_bounds() {
        // Arrange
        double anyVolume = Any.Double().Positive().LessThanOrEqualTo(MeasuredVolume.MaxVolumeMm3).Generate();

        // Act
        MeasuredVolume exact = new MeasuredVolume(anyVolume, 0.0);

        // Assert
        Assert.Equal(exact.LowerBoundMm3, exact.UpperBoundMm3);
        Assert.Equal(anyVolume, exact.VolumeMm3);
    }

    [Fact]
    public void The_equivalent_diameter_is_the_one_of_the_sphere_of_the_same_volume() {
        // Arrange
        // A sphere of 10 mm diameter holds pi * 10^3 / 6 mm3.
        double sphereVolume = Math.PI * 1000.0 / 6.0;
        double anyUncertainty = Any.Double().Between(0.0, 50.0).Generate();
        MeasuredVolume volume = new MeasuredVolume(sphereVolume, anyUncertainty);

        // Act
        double diameter = volume.EquivalentDiameterMm;

        // Assert
        Assert.Equal(10.0, diameter, precision: 9);
    }

    [Fact]
    public void A_volume_of_exactly_20000_mm3_is_accepted() {
        // Act
        MeasuredVolume largest = new MeasuredVolume(20_000.0, 0.0);

        // Assert
        Assert.Equal(20_000.0, largest.VolumeMm3);
    }

    [Theory]
    [InlineData(20_000.001)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void A_volume_outside_the_open_to_20000_mm3_range_is_refused(double outsideMm3) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new MeasuredVolume(outsideMm3, 0.0));

        // Assert
        Assert.Equal("volumeMm3", refusal.ParamName);
    }

    [Fact]
    public void The_smallest_positive_volume_is_accepted() {
        // Act
        MeasuredVolume tiny = new MeasuredVolume(double.Epsilon, 0.0);

        // Assert
        Assert.Equal(double.Epsilon, tiny.VolumeMm3);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    public void An_uncertainty_that_is_negative_or_not_a_number_is_refused(double invalidMm3) {
        // Arrange
        double anyVolume = Any.Double().Between(1.0, MeasuredVolume.MaxVolumeMm3).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new MeasuredVolume(anyVolume, invalidMm3));

        // Assert
        Assert.Equal("uncertaintyMm3", refusal.ParamName);
    }

    [Fact]
    public void An_uncertainty_as_large_as_the_volume_is_refused() {
        // Arrange
        double anyVolume = Any.Double().Between(1.0, MeasuredVolume.MaxVolumeMm3).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new MeasuredVolume(anyVolume, anyVolume));

        // Assert
        Assert.Equal("uncertaintyMm3", refusal.ParamName);
    }

    [Fact]
    public void An_uncertainty_larger_than_the_volume_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new MeasuredVolume(50.0, 50.5));

        // Assert
        Assert.Equal("uncertaintyMm3", refusal.ParamName);
    }

    [Fact]
    public void An_uncertainty_just_below_the_volume_is_accepted() {
        // Arrange
        double anyVolume = Any.Double().Between(1.0, MeasuredVolume.MaxVolumeMm3).Generate();
        double justBelow = Math.BitDecrement(anyVolume);

        // Act
        MeasuredVolume wide = new MeasuredVolume(anyVolume, justBelow);

        // Assert
        Assert.Equal(justBelow, wide.UncertaintyMm3);
    }

}
