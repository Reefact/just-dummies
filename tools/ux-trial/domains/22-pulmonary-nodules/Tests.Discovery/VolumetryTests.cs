using JustDummies;

using PulmonaryNodules.Domain;

using Xunit;

namespace PulmonaryNodules.Tests.Discovery;

public sealed class VolumetryTests {

    private const double Precision = 1e-9;

    [Fact]
    public void A_measured_volume_spans_its_uncertainty_on_either_side() {
        // Arrange
        MeasuredVolume volume = new MeasuredVolume(500.0, 40.0);

        // Act
        (double lower, double upper) = (volume.LowerBoundMm3, volume.UpperBoundMm3);

        // Assert
        Assert.Equal(460.0, lower);
        Assert.Equal(540.0, upper);
    }

    [Fact]
    public void The_equivalent_diameter_is_that_of_the_sphere_of_same_volume() {
        // Arrange
        MeasuredVolume anyTenMillimetreNodule = Any.MeasuredVolume().WithVolumeMm3(Math.PI * 1000.0 / 6.0).Generate();

        // Act
        double diameter = anyTenMillimetreNodule.EquivalentDiameterMm;

        // Assert
        Assert.Equal(10.0, diameter, Precision);
    }

    [Fact]
    public void A_segmentation_measures_every_voxel_and_half_of_its_boundary_as_uncertainty() {
        // Arrange
        Segmentation anySegmentation = Any.Segmentation()
                                          .WithVoxelCount(400)
                                          .WithBoundaryVoxelCount(80)
                                          .WithSpacing(new VoxelSpacing(0.5, 0.5, 1.0))
                                          .Generate();

        // Act
        MeasuredVolume measured = anySegmentation.Measure();

        // Assert
        Assert.Equal(100.0, measured.VolumeMm3, Precision);
        Assert.Equal(10.0, measured.UncertaintyMm3, Precision);
    }

    [Fact]
    public void A_volume_gain_beyond_the_uncertainty_and_the_25_percent_threshold_is_growth() {
        // Arrange
        VolumeChange change = Any.VolumeChange()
                                 .WithBefore(new MeasuredVolume(400.0, 20.0))
                                 .WithAfter(new MeasuredVolume(600.0, 30.0))
                                 .Generate();

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Growth, trend);
    }

    [Fact]
    public void A_volume_loss_beyond_the_uncertainty_and_the_25_percent_threshold_is_regression() {
        // Arrange
        VolumeChange change = Any.VolumeChange()
                                 .WithBefore(new MeasuredVolume(600.0, 30.0))
                                 .WithAfter(new MeasuredVolume(300.0, 20.0))
                                 .Generate();

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Regression, trend);
    }

    [Fact]
    public void A_large_relative_change_hidden_by_segmentation_uncertainty_is_stable() {
        // Arrange — +50 % but the combined uncertainty is 50 mm3 for a 50 mm3 difference.
        VolumeChange change = Any.VolumeChange()
                                 .WithBefore(new MeasuredVolume(100.0, 30.0))
                                 .WithAfter(new MeasuredVolume(150.0, 40.0))
                                 .Generate();

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_change_beyond_the_uncertainty_but_below_25_percent_is_stable() {
        // Arrange
        VolumeChange change = Any.VolumeChange()
                                 .WithBefore(new MeasuredVolume(1000.0, 10.0))
                                 .WithAfter(new MeasuredVolume(1200.0, 10.0))
                                 .Generate();

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_nodule_that_doubles_in_90_days_has_a_doubling_time_of_90_days() {
        // Arrange
        VolumeChange change = new VolumeChange(new MeasuredVolume(400.0, 10.0), new MeasuredVolume(800.0, 10.0), 90);

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.NotNull(doublingTime);
        Assert.Equal(90.0, doublingTime.Value, Precision);
    }

    [Fact]
    public void A_stable_nodule_has_no_doubling_time() {
        // Arrange
        VolumeChange change = Any.VolumeChange()
                                 .WithBefore(new MeasuredVolume(1000.0, 10.0))
                                 .WithAfter(new MeasuredVolume(1050.0, 10.0))
                                 .Generate();

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.Null(doublingTime);
    }

    [Fact]
    public void Two_volumes_measured_the_same_day_cannot_be_compared() {
        // Arrange
        MeasuredVolume anyBefore = Any.MeasuredVolume().Generate();
        MeasuredVolume anyAfter  = Any.MeasuredVolume().Generate();

        // Act
        ArgumentOutOfRangeException failure = Assert.Throws<ArgumentOutOfRangeException>(() => new VolumeChange(anyBefore, anyAfter, 0));

        // Assert
        Assert.Equal("intervalDays", failure.ParamName);
    }

}
