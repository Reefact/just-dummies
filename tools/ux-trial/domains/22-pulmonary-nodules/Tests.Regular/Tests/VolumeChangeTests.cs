using JustDummies;

using PulmonaryNodules.Domain;

using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class VolumeChangeTests {

    [Fact]
    public void The_delta_is_the_later_volume_minus_the_earlier_one() {
        // Arrange
        int          anyInterval = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(200, 5), new MeasuredVolume(150, 5), anyInterval);

        // Act / Assert
        Assert.Equal(-50, change.DeltaMm3, 9);
    }

    [Fact]
    public void The_relative_change_is_expressed_against_the_earlier_volume() {
        // Arrange
        int          anyInterval = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(200, 5), new MeasuredVolume(250, 5), anyInterval);

        // Act / Assert
        Assert.Equal(0.25, change.RelativeChange, 9);
    }

    [Fact]
    public void Uncertainties_are_combined_in_quadrature() {
        // Arrange
        int          anyInterval = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(100, 3), new MeasuredVolume(100, 4), anyInterval);

        // Act / Assert
        Assert.Equal(5.0, change.CombinedUncertaintyMm3, 9);
    }

    [Fact]
    public void A_difference_within_the_combined_uncertainty_is_stable_however_large_it_is_relatively() {
        // Arrange: +60 %, but both measurements are +/- 45 mm3, which combine to about 63.6 mm3
        int          anyInterval = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(100, 45), new MeasuredVolume(160, 45), anyInterval);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
        Assert.Null(change.VolumeDoublingTimeDays);
    }

    [Fact]
    public void A_difference_equal_to_the_combined_uncertainty_is_still_stable() {
        // Arrange: uncertainties 3 and 4 combine to exactly 5; the delta is +5 on 10 mm3 (50 %)
        int          anyInterval = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(10, 3), new MeasuredVolume(15, 4), anyInterval);

        // Act / Assert
        Assert.Equal(VolumeTrend.Stable, change.Trend);
    }

    [Fact]
    public void A_difference_beyond_the_uncertainty_but_below_25_percent_is_stable() {
        // Arrange: +10 %, with no uncertainty at all
        int          anyInterval = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(1000, 0), new MeasuredVolume(1100, 0), anyInterval);

        // Act / Assert
        Assert.Equal(VolumeTrend.Stable, change.Trend);
    }

    [Theory]
    [InlineData(1000, 1250, VolumeTrend.Growth)]
    [InlineData(1000, 1249, VolumeTrend.Stable)]
    [InlineData(1000, 750, VolumeTrend.Regression)]
    [InlineData(1000, 751, VolumeTrend.Stable)]
    public void The_clinical_threshold_is_a_25_percent_change_in_either_direction(double before, double after, VolumeTrend expected) {
        // Arrange
        int          anyInterval = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(before, 0), new MeasuredVolume(after, 0), anyInterval);

        // Act / Assert
        Assert.Equal(expected, change.Trend);
    }

    [Fact]
    public void A_shrinking_nodule_beyond_both_thresholds_is_a_regression_without_a_doubling_time() {
        // Arrange
        int          anyInterval = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change      = new VolumeChange(new MeasuredVolume(1000, 10), new MeasuredVolume(400, 10), anyInterval);

        // Act / Assert
        Assert.Equal(VolumeTrend.Regression, change.Trend);
        Assert.Null(change.VolumeDoublingTimeDays);
    }

    [Fact]
    public void A_nodule_that_doubled_in_ninety_days_has_a_doubling_time_of_ninety_days() {
        // Arrange
        VolumeChange change = new VolumeChange(new MeasuredVolume(100, 1), new MeasuredVolume(200, 1), 90);

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.NotNull(doublingTime);
        Assert.Equal(90.0, doublingTime.Value, 9);
    }

    [Fact]
    public void A_nodule_that_quadrupled_in_ninety_days_doubles_every_forty_five_days() {
        // Arrange
        VolumeChange change = new VolumeChange(new MeasuredVolume(100, 1), new MeasuredVolume(400, 1), 90);

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.Equal(45.0, doublingTime!.Value, 9);
    }

    [Fact]
    public void Two_exams_less_than_a_day_apart_cannot_be_compared() {
        // Arrange
        MeasuredVolume anyBefore   = Any.MeasuredVolume().Generate();
        MeasuredVolume anyAfter    = Any.MeasuredVolume().Generate();
        int            anyInterval = Any.Int32().LessThan(1).Generate();

        // Act
        Action act = () => new VolumeChange(anyBefore, anyAfter, anyInterval);

        // Assert
        Assert.Equal("intervalDays", Assert.Throws<ArgumentOutOfRangeException>(act).ParamName);
    }

    [Fact]
    public void One_day_is_the_shortest_interval_that_can_be_compared() {
        // Arrange
        MeasuredVolume anyBefore = Any.MeasuredVolume().Generate();
        MeasuredVolume anyAfter  = Any.MeasuredVolume().Generate();

        // Act
        VolumeChange change = new VolumeChange(anyBefore, anyAfter, 1);

        // Assert
        Assert.Equal(1, change.IntervalDays);
    }

    [Fact]
    public void A_missing_volume_is_refused() {
        // Arrange
        MeasuredVolume anyVolume = Any.MeasuredVolume().Generate();
        int            anyInterval = Any.Int32().Between(1, 3650).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new VolumeChange(null!, anyVolume, anyInterval));
        Assert.Throws<ArgumentNullException>(() => new VolumeChange(anyVolume, null!, anyInterval));
    }

}
