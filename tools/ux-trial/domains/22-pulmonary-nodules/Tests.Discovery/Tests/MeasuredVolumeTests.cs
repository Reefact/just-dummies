namespace PulmonaryNodules.Tests.Discovery;

public sealed class MeasuredVolumeTests {

    [Fact]
    public void A_measured_volume_exposes_its_bounds() {
        // Arrange
        MeasuredVolume volume = new MeasuredVolume(1000, 150);

        // Act / Assert
        Assert.Equal(1000, volume.VolumeMm3);
        Assert.Equal(150, volume.UncertaintyMm3);
        Assert.Equal(850, volume.LowerBoundMm3);
        Assert.Equal(1150, volume.UpperBoundMm3);
    }

    [Fact]
    public void The_equivalent_diameter_is_the_one_of_the_sphere_of_the_same_volume() {
        // Arrange: a sphere of 10 mm diameter holds pi/6 * 1000 mm3.
        double         sphereVolumeMm3 = Math.PI / 6.0 * 1000.0;
        double         anyUncertaintyMm3 = Any.Double().Positive().LessThan(sphereVolumeMm3).Generate();
        MeasuredVolume volume          = new MeasuredVolume(sphereVolumeMm3, anyUncertaintyMm3);

        // Act
        double diameter = volume.EquivalentDiameterMm;

        // Assert
        Assert.Equal(10.0, diameter, 9);
    }

    [Fact]
    public void A_volume_with_no_uncertainty_is_valid() {
        // Act
        MeasuredVolume volume = new MeasuredVolume(500, 0.0);

        // Assert
        Assert.Equal(500, volume.LowerBoundMm3);
        Assert.Equal(500, volume.UpperBoundMm3);
    }

    [Fact]
    public void The_largest_nodule_volume_is_accepted() {
        // Arrange
        double anyUncertaintyMm3 = Any.Double().Between(0d, 100d).Generate();

        // Act
        Exception? exception = Record.Exception(() => new MeasuredVolume(MeasuredVolume.MaxVolumeMm3, anyUncertaintyMm3));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(20_000.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_volume_outside_of_zero_exclusive_to_twenty_thousand_is_refused(double badVolumeMm3) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MeasuredVolume(badVolumeMm3, 0.0));

        // Assert
        Assert.Equal("volumeMm3", exception.ParamName);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_negative_uncertainty_is_refused(double badUncertaintyMm3) {
        // Arrange
        double anyVolumeMm3 = Any.Double().Positive().LessThanOrEqualTo(MeasuredVolume.MaxVolumeMm3).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MeasuredVolume(anyVolumeMm3, badUncertaintyMm3));

        // Assert
        Assert.Equal("uncertaintyMm3", exception.ParamName);
    }

    [Fact]
    public void An_uncertainty_equal_to_the_volume_is_refused() {
        // Arrange
        double anyVolumeMm3 = Any.Double().Positive().LessThanOrEqualTo(MeasuredVolume.MaxVolumeMm3).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new MeasuredVolume(anyVolumeMm3, anyVolumeMm3));

        // Assert
        Assert.Equal("uncertaintyMm3", exception.ParamName);
    }

    [Fact]
    public void An_uncertainty_greater_than_the_volume_is_refused() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new MeasuredVolume(100, 100.5));

        // Assert
        Assert.Equal("uncertaintyMm3", exception.ParamName);
    }

    [Fact]
    public void An_uncertainty_just_under_the_volume_is_accepted() {
        // Act
        MeasuredVolume volume = new MeasuredVolume(100, 99.999);

        // Assert
        Assert.True(volume.LowerBoundMm3 > 0.0);
    }

}
