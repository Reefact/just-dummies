using JustDummies;
using PulmonaryNodules.Domain;
using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class VolumeChangeTests {

    [Fact]
    public void Uncertainties_combine_in_quadrature() {
        // Arrange
        int          anyInterval = Any.Int32().Between(1, 3_650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(100.0, 3.0), new MeasuredVolume(110.0, 4.0), anyInterval);

        // Act
        double combined = change.CombinedUncertaintyMm3;

        // Assert
        Assert.Equal(5.0, combined, 10);
    }

    [Fact]
    public void A_large_relative_change_within_segmentation_uncertainty_is_stable() {
        // Arrange
        int          anyInterval = Any.Int32().Between(1, 3_650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(100.0, 30.0), new MeasuredVolume(140.0, 30.0), anyInterval);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_measurable_change_below_the_clinical_threshold_is_stable() {
        // Arrange
        int          anyInterval = Any.Int32().Between(1, 3_650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(1_000.0, 5.0), new MeasuredVolume(1_200.0, 5.0), anyInterval);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
        Assert.Null(change.VolumeDoublingTimeDays);
    }

    [Fact]
    public void A_doubling_over_ninety_days_is_growth_with_a_ninety_day_doubling_time() {
        // Arrange
        VolumeChange change = new VolumeChange(new MeasuredVolume(100.0, 5.0), new MeasuredVolume(200.0, 5.0), 90);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Growth, trend);
        Assert.Equal(90.0, change.VolumeDoublingTimeDays!.Value, 9);
    }

    [Fact]
    public void A_halving_is_regression_and_has_no_doubling_time() {
        // Arrange
        int          anyInterval = Any.Int32().Between(1, 3_650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(400.0, 10.0), new MeasuredVolume(200.0, 10.0), anyInterval);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Regression, trend);
        Assert.Null(change.VolumeDoublingTimeDays);
    }

    [Fact]
    public void Two_volumes_from_the_same_day_cannot_be_compared() {
        // Arrange
        MeasuredVolume anyBefore = Arbitrary.MeasuredVolume().Generate();
        MeasuredVolume anyAfter  = Arbitrary.MeasuredVolume().Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(() => new VolumeChange(anyBefore, anyAfter, 0));

        // Assert
        Assert.Equal("intervalDays", thrown.ParamName);
    }

}
