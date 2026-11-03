namespace PulmonaryNodules.Tests.Discovery;

public sealed class VolumeChangeTests {

    private static MeasuredVolume Volume(double volumeMm3, double uncertaintyMm3) {
        return new MeasuredVolume(volumeMm3, uncertaintyMm3);
    }

    [Fact]
    public void A_volume_change_keeps_its_two_volumes_and_its_interval() {
        // Arrange
        MeasuredVolume before = Volume(100, 5);
        MeasuredVolume after  = Volume(150, 5);

        // Act
        VolumeChange change = new VolumeChange(before, after, 90);

        // Assert
        Assert.Same(before, change.Before);
        Assert.Same(after, change.After);
        Assert.Equal(90, change.IntervalDays);
    }

    [Fact]
    public void The_delta_and_the_relative_change_are_measured_against_the_earlier_volume() {
        // Arrange
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(200, 1), Volume(150, 1), anyIntervalDays);

        // Act / Assert
        Assert.Equal(-50.0, change.DeltaMm3, 9);
        Assert.Equal(-0.25, change.RelativeChange, 9);
    }

    [Fact]
    public void The_combined_uncertainty_adds_the_two_uncertainties_in_quadrature() {
        // Arrange
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(1000, 3), Volume(1000, 4), anyIntervalDays);

        // Act
        double combined = change.CombinedUncertaintyMm3;

        // Assert
        Assert.Equal(5.0, combined, 9);
    }

    [Fact]
    public void A_difference_within_the_combined_uncertainty_is_stable_however_large_it_is_relatively() {
        // Arrange: a 50 % relative growth, but 50 mm3 against a combined uncertainty of 50 mm3 (30 and 40 in quadrature).
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(100, 30), Volume(150, 40), anyIntervalDays);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_difference_above_the_uncertainty_but_under_twenty_five_percent_is_stable() {
        // Arrange: +24 % with no uncertainty at all.
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(1000, 0), Volume(1240, 0), anyIntervalDays);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_growth_of_exactly_twenty_five_percent_beyond_the_uncertainty_is_a_growth() {
        // Arrange
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(100, 0), Volume(125, 0), anyIntervalDays);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Growth, trend);
    }

    [Fact]
    public void A_difference_exactly_equal_to_the_combined_uncertainty_is_not_yet_a_change() {
        // Arrange: 500 mm3 of difference, 300 and 400 in quadrature make 500.
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(1000, 300), Volume(1500, 400), anyIntervalDays);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_clear_loss_of_volume_is_a_regression() {
        // Arrange
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(1000, 10), Volume(600, 10), anyIntervalDays);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Regression, trend);
    }

    [Fact]
    public void The_same_volume_twice_is_stable() {
        // Arrange
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(400, 0), Volume(400, 0), anyIntervalDays);

        // Act
        VolumeTrend trend = change.Trend;

        // Assert
        Assert.Equal(VolumeTrend.Stable, trend);
    }

    [Fact]
    public void A_volume_that_doubles_in_thirty_days_doubles_in_thirty_days() {
        // Arrange
        VolumeChange change = new VolumeChange(Volume(100, 1), Volume(200, 1), 30);

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.NotNull(doublingTime);
        Assert.Equal(30.0, doublingTime.Value, 9);
    }

    [Fact]
    public void A_volume_that_quadruples_in_sixty_days_doubles_in_thirty() {
        // Arrange
        VolumeChange change = new VolumeChange(Volume(100, 1), Volume(400, 1), 60);

        // Act
        double? doublingTime = change.VolumeDoublingTimeDays;

        // Assert
        Assert.Equal(30.0, doublingTime!.Value, 9);
    }

    [Fact]
    public void A_stable_nodule_has_no_doubling_time() {
        // Arrange
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(100, 0), Volume(110, 0), anyIntervalDays);

        // Act / Assert
        Assert.Null(change.VolumeDoublingTimeDays);
    }

    [Fact]
    public void A_shrinking_nodule_has_no_doubling_time() {
        // Arrange
        int anyIntervalDays = Any.Int32().Between(1, 3650).Generate();
        VolumeChange change = new VolumeChange(Volume(1000, 0), Volume(500, 0), anyIntervalDays);

        // Act / Assert
        Assert.Null(change.VolumeDoublingTimeDays);
    }

    [Fact]
    public void Two_exams_less_than_one_day_apart_cannot_be_compared() {
        // Arrange
        MeasuredVolume anyBefore = Any.MeasuredVolume().Generate();
        MeasuredVolume anyAfter  = Any.MeasuredVolume().Generate();

        // Act
        ArgumentOutOfRangeException zero     = Assert.Throws<ArgumentOutOfRangeException>(() => new VolumeChange(anyBefore, anyAfter, 0));
        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(() => new VolumeChange(anyBefore, anyAfter, -3));

        // Assert
        Assert.Equal("intervalDays", zero.ParamName);
        Assert.Equal("intervalDays", negative.ParamName);
    }

    [Fact]
    public void A_one_day_interval_is_accepted() {
        // Arrange
        MeasuredVolume anyBefore = Any.MeasuredVolume().Generate();
        MeasuredVolume anyAfter  = Any.MeasuredVolume().Generate();

        // Act
        Exception? exception = Record.Exception(() => new VolumeChange(anyBefore, anyAfter, 1));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void A_comparison_needs_both_volumes() {
        // Arrange
        MeasuredVolume anyVolume = Any.MeasuredVolume().Generate();

        // Act
        ArgumentNullException noBefore = Assert.Throws<ArgumentNullException>(() => new VolumeChange(null!, anyVolume, 10));
        ArgumentNullException noAfter  = Assert.Throws<ArgumentNullException>(() => new VolumeChange(anyVolume, null!, 10));

        // Assert
        Assert.Equal("before", noBefore.ParamName);
        Assert.Equal("after", noAfter.ParamName);
    }

}
