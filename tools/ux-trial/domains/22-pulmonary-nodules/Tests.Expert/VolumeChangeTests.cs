using JustDummies;

using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Arbitrary;

using Xunit;

namespace PulmonaryNodules.Tests.Expert;

public sealed class VolumeChangeTests {

    [Fact]
    public void A_nodule_that_doubles_beyond_its_uncertainty_is_growing_and_its_doubling_time_is_the_interval() {
        // Arrange
        VolumeChange change = new VolumeChange(new MeasuredVolume(400.0, 20.0), new MeasuredVolume(800.0, 30.0), intervalDays: 90);

        // Act
        VolumeTrend trend        = change.Trend;
        double?     doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.Equal(VolumeTrend.Growth, trend);
        Assert.Equal(90.0, doublingTime!.Value, precision: 6);
    }

    [Fact]
    public void A_nodule_that_halves_beyond_its_uncertainty_is_regressing_and_has_no_doubling_time() {
        // Arrange
        VolumeChange change = new VolumeChange(new MeasuredVolume(800.0, 30.0), new MeasuredVolume(400.0, 20.0), intervalDays: 90);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Regression, trend);
        Assert.Null(change.VolumeDoublingTimeDays);
    }

    [Fact]
    public void A_40_percent_increase_inside_the_segmentation_uncertainty_is_not_mistaken_for_growth() {
        // Arrange — 100 ± 40 then 140 ± 40: the combined uncertainty (56.6 mm³) covers the 40 mm³ delta.
        VolumeChange change = new VolumeChange(new MeasuredVolume(100.0, 40.0), new MeasuredVolume(140.0, 40.0), intervalDays: 90);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
        Assert.Null(change.VolumeDoublingTimeDays);
    }

    [Fact]
    public void A_measurable_increase_below_the_25_percent_clinical_threshold_is_stable() {
        // Arrange — 1000 ± 10 then 1200 ± 10: well beyond the uncertainty, but only 20 %.
        VolumeChange change = new VolumeChange(new MeasuredVolume(1000.0, 10.0), new MeasuredVolume(1200.0, 10.0), intervalDays: 90);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void The_uncertainty_of_a_difference_adds_both_uncertainties_in_quadrature() {
        // Arrange
        MeasuredVolume anyBefore = Any.MeasuredVolume().WithUncertaintyMm3(30.0).WithVolumeMm3(v => v.GreaterThan(30.0)).Generate();
        MeasuredVolume anyAfter  = Any.MeasuredVolume().WithUncertaintyMm3(40.0).WithVolumeMm3(v => v.GreaterThan(40.0)).Generate();
        int            anyDays   = Any.Int32().Between(1, 3650).Generate();

        // Act
        VolumeChange change = new VolumeChange(anyBefore, anyAfter, anyDays);

        // Assert
        Assert.Equal(50.0, change.CombinedUncertaintyMm3, precision: 9);
    }

    [Fact]
    public void Two_measurements_of_the_same_day_cannot_be_compared() {
        // Arrange
        MeasuredVolume anyBefore = Any.MeasuredVolume().Generate();
        MeasuredVolume anyAfter  = Any.MeasuredVolume().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new VolumeChange(anyBefore, anyAfter, intervalDays: 0));

        // Assert
        Assert.Equal("intervalDays", error.ParamName);
    }

}
