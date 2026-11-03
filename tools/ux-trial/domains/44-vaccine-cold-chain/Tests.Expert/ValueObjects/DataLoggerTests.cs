using AgriTech.Domain;
using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class DataLoggerTests {

    private static readonly DateOnly CalibratedOn = new DateOnly(2026, 1, 15);

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    public void A_logger_records_every_1_to_60_minutes(int interval) {
        // Act
        DataLogger logger = Any.DataLogger().WithRecordingIntervalMinutes(interval).Generate();

        // Assert
        Assert.Equal(interval, logger.RecordingIntervalMinutes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    [InlineData(-5)]
    public void A_recording_interval_outside_1_to_60_minutes_is_refused(int interval) {
        // Arrange
        SensorId anySensor = Any.SensorId().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DataLogger(anySensor, interval, CalibratedOn, CalibratedOn.AddDays(30)));
    }

    [Fact]
    public void A_missing_sensor_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new DataLogger(null!, 5, CalibratedOn, CalibratedOn.AddDays(30)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    [InlineData(365)]
    public void A_calibration_may_fall_due_from_the_next_day_up_to_a_year_later(int daysLater) {
        // Arrange
        AnyDataLogger loggers = Any.DataLogger().WithCalibratedOn(CalibratedOn).WithCalibrationDueOn(CalibratedOn.AddDays(daysLater));

        // Act
        DataLogger logger = loggers.Generate();

        // Assert
        Assert.Equal(CalibratedOn.AddDays(daysLater), logger.CalibrationDueOn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-300)]
    public void A_calibration_that_falls_due_on_or_before_the_day_it_was_done_is_refused(int daysLater) {
        // Arrange
        AnyDataLogger loggers = Any.DataLogger().WithCalibratedOn(CalibratedOn).WithCalibrationDueOn(CalibratedOn.AddDays(daysLater));

        // Assert
        Assert.Throws<ArgumentException>(() => loggers.Generate());
    }

    [Fact]
    public void A_calibration_may_fall_due_exactly_one_calendar_year_later() {
        // Arrange
        AnyDataLogger loggers = Any.DataLogger().WithCalibratedOn(new DateOnly(2026, 1, 15)).WithCalibrationDueOn(new DateOnly(2027, 1, 15));

        // Assert
        Assert.NotNull(loggers.Generate());
    }

    [Fact]
    public void A_calibration_cannot_fall_due_a_day_after_the_calendar_year() {
        // Arrange
        AnyDataLogger loggers = Any.DataLogger().WithCalibratedOn(new DateOnly(2026, 1, 15)).WithCalibrationDueOn(new DateOnly(2027, 1, 16));

        // Assert
        Assert.Throws<ArgumentException>(() => loggers.Generate());
    }

    [Fact]
    public void A_calibration_done_on_a_leap_day_expires_at_the_latest_on_the_28th_of_the_next_february() {
        // Arrange
        DateOnly leapDay = new DateOnly(2028, 2, 29);

        AnyDataLogger lastDay  = Any.DataLogger().WithCalibratedOn(leapDay).WithCalibrationDueOn(new DateOnly(2029, 2, 28));
        AnyDataLogger dayAfter = Any.DataLogger().WithCalibratedOn(leapDay).WithCalibrationDueOn(new DateOnly(2029, 3, 1));

        // Assert
        Assert.NotNull(lastDay.Generate());
        Assert.Throws<ArgumentException>(() => dayAfter.Generate());
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(29, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void A_logger_is_calibrated_from_the_day_it_was_done_to_the_day_it_falls_due_both_included(int daysAfterCalibration, bool expectedCalibrated) {
        // Arrange
        DataLogger logger = Any.DataLogger().WithCalibratedOn(CalibratedOn).WithCalibrationDueOn(CalibratedOn.AddDays(30)).Generate();

        // Assert
        Assert.Equal(expectedCalibrated, logger.IsCalibratedOn(CalibratedOn.AddDays(daysAfterCalibration)));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(5, 10)]
    [InlineData(60, 120)]
    public void The_longest_silence_that_still_counts_as_a_continuous_record_is_two_intervals(int interval, int expectedSilenceMinutes) {
        // Arrange
        DataLogger logger = Any.DataLogger().WithRecordingIntervalMinutes(interval).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(expectedSilenceMinutes), logger.MaxSilence);
    }

    [Fact]
    public void Two_loggers_with_the_same_sensor_are_the_same_logger_whatever_their_calibration() {
        // Arrange
        SensorId sharedSensor = Any.SensorId().Generate();

        DataLogger first  = Any.DataLogger().WithId(sharedSensor).Generate();
        DataLogger second = Any.DataLogger().WithId(sharedSensor).Generate();

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_loggers_with_different_sensors_are_different_loggers() {
        // Arrange
        SensorId anySensor     = Any.SensorId().Generate();
        SensorId anotherSensor = new AnySensorId().DifferentFrom(anySensor).Generate();

        DataLogger first  = Any.DataLogger().WithId(anySensor).WithRecordingIntervalMinutes(5).WithCalibratedOn(CalibratedOn).WithCalibrationDueOn(CalibratedOn.AddDays(30)).Generate();
        DataLogger second = Any.DataLogger().WithId(anotherSensor).WithRecordingIntervalMinutes(5).WithCalibratedOn(CalibratedOn).WithCalibrationDueOn(CalibratedOn.AddDays(30)).Generate();

        // Assert
        Assert.NotEqual(first, second);
    }

}
