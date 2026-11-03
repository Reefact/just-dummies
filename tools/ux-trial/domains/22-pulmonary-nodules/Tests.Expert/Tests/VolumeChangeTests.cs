using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class VolumeChangeTests {

    private static VolumeChange Change(double beforeMm3, double beforeUncertaintyMm3, double afterMm3, double afterUncertaintyMm3, int intervalDays) {
        return new VolumeChange(new MeasuredVolume(beforeMm3, beforeUncertaintyMm3), new MeasuredVolume(afterMm3, afterUncertaintyMm3), intervalDays);
    }

    /// <summary>A change between the two given measurements over an interval the trend does not depend on.</summary>
    private static VolumeChange ChangeOverAnyInterval(double beforeMm3, double beforeUncertaintyMm3, double afterMm3, double afterUncertaintyMm3) {
        return Any.VolumeChange()
                  .WithBefore(new MeasuredVolume(beforeMm3, beforeUncertaintyMm3))
                  .WithAfter(new MeasuredVolume(afterMm3, afterUncertaintyMm3))
                  .Generate();
    }

    // ---- Arithmetic ----

    [Fact]
    public void The_delta_is_the_volume_after_minus_the_volume_before() {
        // Arrange
        VolumeChange growth = ChangeOverAnyInterval(100.0, 1.0, 150.0, 1.0);
        VolumeChange shrinkage = ChangeOverAnyInterval(150.0, 1.0, 100.0, 1.0);

        // Act / Assert
        Assert.Equal(50.0, growth.DeltaMm3);
        Assert.Equal(-50.0, shrinkage.DeltaMm3);
    }

    [Fact]
    public void The_relative_change_is_measured_against_the_volume_before() {
        // Arrange
        VolumeChange growth = ChangeOverAnyInterval(100.0, 1.0, 150.0, 1.0);
        VolumeChange shrinkage = ChangeOverAnyInterval(200.0, 1.0, 150.0, 1.0);

        // Act / Assert
        Assert.Equal(0.5, growth.RelativeChange);
        Assert.Equal(-0.25, shrinkage.RelativeChange);
    }

    [Fact]
    public void The_two_uncertainties_are_combined_in_quadrature() {
        // Arrange
        VolumeChange change = ChangeOverAnyInterval(100.0, 3.0, 120.0, 4.0);

        // Act
        double combined = change.CombinedUncertaintyMm3;

        // Assert
        Assert.Equal(5.0, combined);
    }

    [Fact]
    public void The_change_keeps_its_two_measurements_and_the_time_between_them() {
        // Arrange
        MeasuredVolume before = new MeasuredVolume(100.0, 2.0);
        MeasuredVolume after = new MeasuredVolume(130.0, 3.0);

        // Act
        VolumeChange change = new VolumeChange(before, after, 45);

        // Assert
        Assert.Same(before, change.Before);
        Assert.Same(after, change.After);
        Assert.Equal(45, change.IntervalDays);
    }

    // ---- Trend ----

    [Fact]
    public void A_clear_increase_beyond_both_the_uncertainty_and_25_percent_is_growth() {
        // Arrange
        VolumeChange change = ChangeOverAnyInterval(100.0, 5.0, 200.0, 5.0);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Growth, trend);
    }

    [Fact]
    public void A_clear_decrease_beyond_both_the_uncertainty_and_25_percent_is_regression() {
        // Arrange
        VolumeChange change = ChangeOverAnyInterval(200.0, 5.0, 100.0, 5.0);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Regression, trend);
    }

    [Fact]
    public void A_large_relative_change_lost_in_the_measurement_noise_is_stable() {
        // Arrange
        // +40 % but only 40 mm3 of difference against 56.6 mm3 of combined uncertainty.
        VolumeChange change = ChangeOverAnyInterval(100.0, 40.0, 140.0, 40.0);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_difference_beyond_the_noise_but_under_25_percent_is_stable() {
        // Arrange
        // +20 %: 200 mm3 of difference against 1.4 mm3 of combined uncertainty.
        VolumeChange change = ChangeOverAnyInterval(1000.0, 1.0, 1200.0, 1.0);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void An_increase_of_exactly_25_percent_beyond_the_noise_is_growth() {
        // Arrange
        VolumeChange change = ChangeOverAnyInterval(100.0, 1.0, 125.0, 1.0);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Growth, trend);
    }

    [Fact]
    public void A_decrease_of_exactly_25_percent_beyond_the_noise_is_regression() {
        // Arrange
        VolumeChange change = ChangeOverAnyInterval(100.0, 1.0, 75.0, 1.0);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Regression, trend);
    }

    [Fact]
    public void An_increase_just_under_25_percent_is_stable() {
        // Arrange
        VolumeChange change = ChangeOverAnyInterval(100.0, 1.0, 124.9, 1.0);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_difference_exactly_equal_to_the_combined_uncertainty_is_still_stable() {
        // Arrange
        // 20 -> 25 mm3 is +25 %, and the 5 mm3 of difference is exactly sqrt(3^2 + 4^2): it does not exceed it.
        VolumeChange change = ChangeOverAnyInterval(20.0, 3.0, 25.0, 4.0);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_difference_just_beyond_the_combined_uncertainty_is_growth() {
        // Arrange
        VolumeChange change = ChangeOverAnyInterval(20.0, 3.0, 25.5, 4.0);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Growth, trend);
    }

    [Fact]
    public void Identical_measurements_are_stable() {
        // Arrange
        MeasuredVolume anyVolume = Any.MeasuredVolume().Generate();
        VolumeChange change = Any.VolumeChange().WithBefore(anyVolume).WithAfter(anyVolume).Generate();

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    // ---- Doubling time ----

    [Fact]
    public void A_nodule_that_doubled_has_a_doubling_time_equal_to_the_interval() {
        // Arrange
        VolumeChange change = Change(100.0, 1.0, 200.0, 1.0, intervalDays: 30);

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.NotNull(doublingTime);
        Assert.Equal(30.0, doublingTime.Value, precision: 9);
    }

    [Fact]
    public void A_nodule_that_quadrupled_doubled_twice_over_the_interval() {
        // Arrange
        VolumeChange change = Change(100.0, 1.0, 400.0, 1.0, intervalDays: 30);

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.NotNull(doublingTime);
        Assert.Equal(15.0, doublingTime.Value, precision: 9);
    }

    [Fact]
    public void A_stable_nodule_has_no_doubling_time() {
        // Arrange
        VolumeChange change = Change(100.0, 1.0, 110.0, 1.0, intervalDays: 30);

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.Null(doublingTime);
    }

    [Fact]
    public void A_regressing_nodule_has_no_doubling_time() {
        // Arrange
        VolumeChange change = Change(200.0, 1.0, 100.0, 1.0, intervalDays: 30);

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.Null(doublingTime);
    }

    // ---- Construction ----

    [Fact]
    public void Two_exams_one_day_apart_can_be_compared() {
        // Act
        VolumeChange change = Any.VolumeChange().WithIntervalDays(1).Generate();

        // Assert
        Assert.Equal(1, change.IntervalDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Two_exams_less_than_a_day_apart_cannot_be_compared(int days) {
        // Arrange
        MeasuredVolume anyBefore = Any.MeasuredVolume().Generate();
        MeasuredVolume anyAfter = Any.MeasuredVolume().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new VolumeChange(anyBefore, anyAfter, days));

        // Assert
        Assert.Equal("intervalDays", refusal.ParamName);
    }

    [Fact]
    public void Both_measurements_are_required() {
        // Arrange
        MeasuredVolume anyVolume = Any.MeasuredVolume().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new VolumeChange(null!, anyVolume, 1));
        Assert.Throws<ArgumentNullException>(() => new VolumeChange(anyVolume, null!, 1));
    }

}
