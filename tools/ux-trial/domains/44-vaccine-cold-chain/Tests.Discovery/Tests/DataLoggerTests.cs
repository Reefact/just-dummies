using AgriTech.Domain;

using ColdChain.Domain;

using JustDummies;

using Xunit;

namespace ColdChain.Tests;

public sealed class DataLoggerTests {

    [Fact]
    public void A_logger_keeps_everything_it_was_built_with() {
        // Arrange
        SensorId anyId = new AnySensorId().Generate();

        // Act
        DataLogger logger = new DataLogger(anyId, 10, new DateOnly(2026, 1, 15), new DateOnly(2026, 12, 31));

        // Assert
        Assert.Same(anyId, logger.Id);
        Assert.Equal(10, logger.RecordingIntervalMinutes);
        Assert.Equal(new DateOnly(2026, 1, 15), logger.CalibratedOn);
        Assert.Equal(new DateOnly(2026, 12, 31), logger.CalibrationDueOn);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(60)]
    public void An_interval_between_one_and_sixty_minutes_is_accepted(int interval) {
        // Act
        DataLogger logger = new AnyDataLogger().WithRecordingIntervalMinutes(interval).Generate();

        // Assert
        Assert.Equal(interval, logger.RecordingIntervalMinutes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public void An_interval_outside_one_to_sixty_minutes_is_refused(int interval) {
        // Act
        Exception? thrown = Record.Exception(() => new AnyDataLogger().WithRecordingIntervalMinutes(interval).Generate());

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void A_missing_sensor_id_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new DataLogger(null!, 10, new DateOnly(2026, 1, 15), new DateOnly(2026, 6, 1)));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Theory]
    [InlineData("2026-01-15", "2026-01-16")]
    [InlineData("2026-01-15", "2027-01-15")]
    [InlineData("2026-01-15", "2026-07-01")]
    [InlineData("2027-02-28", "2028-02-28")]
    [InlineData("2028-02-29", "2029-02-28")]
    public void A_certificate_falling_due_within_one_year_after_the_calibration_is_accepted(string calibratedOn, string dueOn) {
        // Act
        DataLogger logger = new AnyDataLogger().WithCalibratedOn(DateOnly.Parse(calibratedOn)).WithCalibrationDueOn(DateOnly.Parse(dueOn)).Generate();

        // Assert
        Assert.Equal(DateOnly.Parse(dueOn), logger.CalibrationDueOn);
    }

    [Theory]
    [InlineData("2026-01-15", "2026-01-15")]
    [InlineData("2026-01-15", "2026-01-14")]
    [InlineData("2026-01-15", "2027-01-16")]
    [InlineData("2028-02-29", "2029-03-01")]
    public void A_certificate_not_falling_due_after_the_calibration_or_more_than_a_year_later_is_refused(string calibratedOn, string dueOn) {
        // Act
        Exception? thrown = Record.Exception(() => new AnyDataLogger().WithCalibratedOn(DateOnly.Parse(calibratedOn)).WithCalibrationDueOn(DateOnly.Parse(dueOn)).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("calibrationDueOn", refusal.ParamName);
    }

    [Theory]
    [InlineData("2026-01-14", false)]
    [InlineData("2026-01-15", true)]
    [InlineData("2026-06-01", true)]
    [InlineData("2026-12-31", true)]
    [InlineData("2027-01-01", false)]
    public void A_logger_is_calibrated_from_the_calibration_day_to_the_due_day_both_included(string day, bool calibrated) {
        // Arrange
        DataLogger logger = new AnyDataLogger().WithCalibratedOn(new DateOnly(2026, 1, 15)).WithCalibrationDueOn(new DateOnly(2026, 12, 31)).Generate();

        // Act
        bool actual = logger.IsCalibratedOn(DateOnly.Parse(day));

        // Assert
        Assert.Equal(calibrated, actual);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(10, 20)]
    [InlineData(60, 120)]
    public void The_longest_acceptable_silence_is_two_intervals(int interval, int silenceMinutes) {
        // Arrange
        DataLogger logger = new AnyDataLogger().WithRecordingIntervalMinutes(interval).Generate();

        // Act
        TimeSpan silence = logger.MaxSilence;

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(silenceMinutes), silence);
    }

    [Fact]
    public void Loggers_with_the_same_sensor_are_the_same_logger() {
        // Arrange
        SensorId   anyId        = new AnySensorId().Generate();
        DataLogger logger       = new AnyDataLogger().WithId(anyId).Generate();
        DataLogger sameLogger   = new AnyDataLogger().WithId(anyId).Generate();

        // Assert
        Assert.Equal(logger, sameLogger);
        Assert.Equal(logger.GetHashCode(), sameLogger.GetHashCode());
    }

    [Fact]
    public void Loggers_with_different_sensors_are_different_loggers() {
        // Arrange
        SensorId   anyId          = new AnySensorId().Generate();
        SensorId   anotherId      = new AnySensorId().DifferentFrom(anyId).Generate();
        DataLogger logger         = new AnyDataLogger().WithId(anyId).Generate();
        DataLogger anotherLogger  = new AnyDataLogger().WithId(anotherId).Generate();

        // Assert
        Assert.NotEqual(logger, anotherLogger);
    }

    [Fact]
    public void The_generator_only_draws_loggers_the_constructor_accepts() {
        // Act
        DataLogger anyLogger = new AnyDataLogger().Generate();

        // Assert
        Assert.InRange(anyLogger.RecordingIntervalMinutes, 1, 60);
        Assert.True(anyLogger.CalibrationDueOn > anyLogger.CalibratedOn);
        Assert.True(anyLogger.CalibrationDueOn <= anyLogger.CalibratedOn.AddYears(1));
    }

}
