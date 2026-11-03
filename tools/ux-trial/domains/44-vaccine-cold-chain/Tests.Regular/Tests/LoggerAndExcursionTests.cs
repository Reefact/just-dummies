using AgriTech.Domain;

using ColdChain.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests.Regular.Tests;

public sealed class DataLoggerTests {

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(60)]
    public void A_logger_records_every_1_to_60_minutes(int interval) {
        // Act
        DataLogger logger = new AnyDataLogger().WithRecordingIntervalMinutes(interval).Generate();

        // Assert
        Assert.Equal(interval, logger.RecordingIntervalMinutes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(61)]
    public void A_recording_interval_outside_1_to_60_minutes_is_refused(int interval) {
        // Act
        Action act = () => new AnyDataLogger().WithRecordingIntervalMinutes(interval).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(5, 10)]
    [InlineData(15, 30)]
    [InlineData(60, 120)]
    public void The_longest_silence_that_still_counts_as_a_record_is_two_intervals(int interval, int expectedSilenceMinutes) {
        // Arrange
        DataLogger logger = new AnyDataLogger().WithRecordingIntervalMinutes(interval).Generate();

        // Act
        TimeSpan silence = logger.MaxSilence;

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(expectedSilenceMinutes), silence);
    }

    [Fact]
    public void A_calibration_falls_due_after_the_day_it_was_done() {
        // Arrange
        DateOnly anyDay = Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2040, 12, 31)).Generate();

        // Act
        ArgumentException sameDay   = Assert.Throws<ArgumentException>(() => new DataLogger(new AnySensorId().Generate(), 10, anyDay, anyDay));
        ArgumentException dayBefore = Assert.Throws<ArgumentException>(() => new DataLogger(new AnySensorId().Generate(), 10, anyDay, anyDay.AddDays(-1)));

        // Assert
        Assert.Equal("calibrationDueOn", sameDay.ParamName);
        Assert.Equal("calibrationDueOn", dayBefore.ParamName);
    }

    [Fact]
    public void A_calibration_may_fall_due_the_day_after() {
        // Arrange
        DateOnly anyDay = Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2040, 12, 31)).Generate();

        // Act
        DataLogger logger = new AnyDataLogger().WithCalibratedOn(anyDay).WithCalibrationDueOn(anyDay.AddDays(1)).Generate();

        // Assert
        Assert.Equal(anyDay.AddDays(1), logger.CalibrationDueOn);
    }

    [Fact]
    public void A_calibration_certificate_is_valid_for_at_most_one_year() {
        // Arrange
        DateOnly anyDay = Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2040, 12, 31)).Generate();

        // Act
        DataLogger oneYear = new AnyDataLogger().WithCalibratedOn(anyDay).WithCalibrationDueOn(anyDay.AddYears(1)).Generate();
        ArgumentException beyond = Assert.Throws<ArgumentException>(() => new AnyDataLogger().WithCalibratedOn(anyDay).WithCalibrationDueOn(anyDay.AddYears(1).AddDays(1)).Generate());

        // Assert
        Assert.Equal(anyDay.AddYears(1), oneYear.CalibrationDueOn);
        Assert.Equal("calibrationDueOn", beyond.ParamName);
    }

    [Fact]
    public void A_calibration_done_on_the_leap_day_is_valid_until_the_end_of_february_of_the_next_year() {
        // Arrange
        DateOnly leapDay = new DateOnly(2028, 2, 29);

        // Act
        DataLogger logger = new AnyDataLogger().WithCalibratedOn(leapDay).WithCalibrationDueOn(new DateOnly(2029, 2, 28)).Generate();
        Action nextDay = () => new AnyDataLogger().WithCalibratedOn(leapDay).WithCalibrationDueOn(new DateOnly(2029, 3, 1)).Generate();

        // Assert
        Assert.True(logger.IsCalibratedOn(new DateOnly(2029, 2, 28)));
        Assert.Throws<ArgumentException>(nextDay);
    }

    [Fact]
    public void A_logger_is_calibrated_from_the_day_it_was_done_to_the_day_it_falls_due_both_included() {
        // Arrange
        DateOnly done = new DateOnly(2030, 3, 10);
        DateOnly due  = new DateOnly(2031, 3, 9);
        DataLogger logger = new AnyDataLogger().WithCalibratedOn(done).WithCalibrationDueOn(due).Generate();

        // Assert
        Assert.False(logger.IsCalibratedOn(done.AddDays(-1)));
        Assert.True(logger.IsCalibratedOn(done));
        Assert.True(logger.IsCalibratedOn(done.AddDays(100)));
        Assert.True(logger.IsCalibratedOn(due));
        Assert.False(logger.IsCalibratedOn(due.AddDays(1)));
    }

    [Fact]
    public void Loggers_with_the_same_sensor_are_the_same_logger() {
        // Arrange
        SensorId sensor = new AnySensorId().Generate();

        // Act
        DataLogger first  = new AnyDataLogger().WithId(sensor).Generate();
        DataLogger second = new AnyDataLogger().WithId(sensor).Generate();

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(sensor.ToString(), first.ToString());
    }

    [Fact]
    public void Loggers_with_another_sensor_are_other_loggers() {
        // Arrange
        List<SensorId> sensors = Any.ListOf(new AnySensorId()).WithCount(2).Distinct().Generate();

        // Act
        DataLogger first  = new AnyDataLogger().WithId(sensors[0]).Generate();
        DataLogger second = new AnyDataLogger().WithId(sensors[1]).Generate();

        // Assert
        Assert.NotEqual(first, second);
        Assert.False(first.Equals(null));
    }

}

public sealed class LoggerReadingTests {

    [Fact]
    public void A_reading_keeps_what_the_logger_measured() {
        // Arrange
        SensorId       logger = new AnySensorId().Generate();
        DateTimeOffset takenAt = new DateTimeOffset(2030, 6, 1, 8, 30, 0, TimeSpan.Zero);

        // Act
        LoggerReading reading = new AnyLoggerReading().WithLogger(logger).WithTakenAt(takenAt).WithCelsius(4.7m).Generate();

        // Assert
        Assert.Equal(logger, reading.Logger);
        Assert.Equal(takenAt, reading.TakenAt);
        Assert.Equal(4.7m, reading.Celsius);
    }

    [Fact]
    public void A_reading_is_time_stamped_in_utc() {
        // Arrange
        DateTimeOffset anyLocalInstant = Any.DateTimeOffset()
                                            .Between(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2034, 12, 31, 0, 0, 0, TimeSpan.Zero))
                                            .WithOffsetBetween(TimeSpan.FromMinutes(1), TimeSpan.FromHours(14))
                                            .Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AnyLoggerReading().WithTakenAt(anyLocalInstant).Generate());

        // Assert
        Assert.Equal("takenAt", refusal.ParamName);
    }

    [Fact]
    public void A_reading_stamped_west_of_greenwich_is_refused_too() {
        // Arrange
        DateTimeOffset takenAt = new DateTimeOffset(2030, 6, 1, 8, 30, 0, TimeSpan.FromHours(-5));

        // Act
        Action act = () => new AnyLoggerReading().WithTakenAt(takenAt).Generate();

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(-40.0)]
    [InlineData(85.0)]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(25.1)]
    public void A_logger_measures_from_minus_40_to_85_degrees(double celsius) {
        // Act
        LoggerReading reading = new AnyLoggerReading().WithCelsius((decimal)celsius).Generate();

        // Assert
        Assert.Equal((decimal)celsius, reading.Celsius);
    }

    [Theory]
    [InlineData(-40.1)]
    [InlineData(85.1)]
    [InlineData(-273.0)]
    [InlineData(1000.0)]
    public void A_temperature_outside_the_range_of_the_logger_is_refused(double celsius) {
        // Act
        Action act = () => new AnyLoggerReading().WithCelsius((decimal)celsius).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_logger_reads_to_a_tenth_of_a_degree() {
        // Act
        Action twoDecimals   = () => new AnyLoggerReading().WithCelsius(4.15m).Generate();
        Action manyDecimals  = () => new AnyLoggerReading().WithCelsius(4.123456m).Generate();
        LoggerReading padded = new AnyLoggerReading().WithCelsius(4.10m).Generate();

        // Assert
        Assert.Throws<ArgumentException>(twoDecimals);
        Assert.Throws<ArgumentException>(manyDecimals);
        Assert.Equal(4.1m, padded.Celsius);
    }

    [Theory]
    [InlineData(-0.1, true)]
    [InlineData(0.0, false)]
    [InlineData(5.0, false)]
    [InlineData(25.0, false)]
    [InlineData(25.1, true)]
    [InlineData(-40.0, true)]
    [InlineData(85.0, true)]
    public void A_reading_breaches_the_hard_limit_below_zero_or_above_twenty_five(double celsius, bool breaches) {
        // Arrange
        LoggerReading reading = new AnyLoggerReading().WithCelsius((decimal)celsius).Generate();

        // Assert
        Assert.Equal(breaches, reading.BreachesHardLimit);
    }

    [Fact]
    public void Readings_that_agree_on_everything_are_equal() {
        // Arrange
        LoggerReading anyReading = new AnyLoggerReading().Generate();

        // Act
        LoggerReading sameReading = new LoggerReading(anyReading.Logger, anyReading.TakenAt, anyReading.Celsius, anyReading.Battery);

        // Assert
        Assert.Equal(anyReading, sameReading);
        Assert.Equal(anyReading.GetHashCode(), sameReading.GetHashCode());
    }

    [Fact]
    public void Readings_taken_at_another_instant_or_temperature_are_not_equal() {
        // Arrange
        DateTimeOffset takenAt = new DateTimeOffset(2030, 6, 1, 8, 30, 0, TimeSpan.Zero);
        SensorId       logger  = new AnySensorId().Generate();
        LoggerReading  reading = new AnyLoggerReading().WithLogger(logger).WithTakenAt(takenAt).WithCelsius(5m).Generate();

        // Assert
        Assert.NotEqual(reading, new AnyLoggerReading().WithLogger(logger).WithTakenAt(takenAt.AddSeconds(1)).WithCelsius(5m).WithBattery(reading.Battery).Generate());
        Assert.NotEqual(reading, new AnyLoggerReading().WithLogger(logger).WithTakenAt(takenAt).WithCelsius(5.1m).WithBattery(reading.Battery).Generate());
        Assert.False(reading.Equals(null));
    }

}

public sealed class ExcursionTests {

    private static readonly TemperatureRange Label = new TemperatureRange(2m, 8m);

    private static readonly DateTimeOffset Start = new DateTimeOffset(2030, 6, 1, 8, 0, 0, TimeSpan.Zero);

    private static Excursion Cold(decimal extreme, TimeSpan duration, bool isOpen = false) {
        return new AnyExcursion().WithKind(ExcursionKind.Cold).WithLabelledRange(Label).WithStartedAt(Start).WithEndedAt(Start + duration).WithExtremeCelsius(extreme).WithIsOpen(isOpen).Generate();
    }

    private static Excursion Warm(decimal extreme, TimeSpan duration, bool isOpen = false) {
        return new AnyExcursion().WithKind(ExcursionKind.Warm).WithLabelledRange(Label).WithStartedAt(Start).WithEndedAt(Start + duration).WithExtremeCelsius(extreme).WithIsOpen(isOpen).Generate();
    }

    [Fact]
    public void A_cold_excursion_goes_strictly_below_the_minimum_of_the_label() {
        // Act
        Excursion excursion = Cold(1.9m, TimeSpan.FromMinutes(10));
        Action atTheMinimum = () => Cold(2m, TimeSpan.FromMinutes(10));
        Action insideTheLabel = () => Cold(5m, TimeSpan.FromMinutes(10));
        Action warmSide = () => Cold(9m, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(ExcursionKind.Cold, excursion.Kind);
        Assert.Equal(1.9m, excursion.ExtremeCelsius);
        Assert.Throws<ArgumentException>(atTheMinimum);
        Assert.Throws<ArgumentException>(insideTheLabel);
        Assert.Throws<ArgumentException>(warmSide);
    }

    [Fact]
    public void A_warm_excursion_goes_strictly_above_the_maximum_of_the_label() {
        // Act
        Excursion excursion = Warm(8.1m, TimeSpan.FromMinutes(10));
        Action atTheMaximum = () => Warm(8m, TimeSpan.FromMinutes(10));
        Action insideTheLabel = () => Warm(5m, TimeSpan.FromMinutes(10));
        Action coldSide = () => Warm(1m, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(ExcursionKind.Warm, excursion.Kind);
        Assert.Equal(8.1m, excursion.ExtremeCelsius);
        Assert.Throws<ArgumentException>(atTheMaximum);
        Assert.Throws<ArgumentException>(insideTheLabel);
        Assert.Throws<ArgumentException>(coldSide);
    }

    [Fact]
    public void An_excursion_cannot_end_before_it_starts() {
        // Act
        Action act = () => new Excursion(ExcursionKind.Warm, Label, Start, Start.AddSeconds(-1), 9m, false);
        Excursion instant = Warm(9m, TimeSpan.Zero);

        // Assert
        Assert.Throws<ArgumentException>(act);
        Assert.Equal(Start, instant.EndedAt);
    }

    [Fact]
    public void An_excursion_of_an_unknown_kind_is_refused() {
        // Act
        Action act = () => new AnyExcursion().WithKind((ExcursionKind)2).WithLabelledRange(Label).WithExtremeCelsius(9m).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(59, 1)]
    [InlineData(60, 1)]
    [InlineData(61, 2)]
    [InlineData(120, 2)]
    [InlineData(121, 3)]
    [InlineData(3_600, 60)]
    public void A_started_minute_is_charged_to_the_budget(int seconds, int expectedMinutes) {
        // Arrange
        Excursion excursion = Warm(9m, TimeSpan.FromSeconds(seconds));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(seconds), excursion.Duration);
        Assert.Equal(expectedMinutes, excursion.DurationMinutes);
    }

    [Theory]
    [InlineData(-0.1, true)]
    [InlineData(-5.0, true)]
    [InlineData(0.0, false)]
    [InlineData(1.9, false)]
    public void A_cold_excursion_breaches_the_hard_limit_when_it_goes_below_zero(double extreme, bool breaches) {
        // Arrange
        Excursion excursion = Cold((decimal)extreme, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(breaches, excursion.BreachesHardLimit);
    }

    [Theory]
    [InlineData(25.1, true)]
    [InlineData(30.0, true)]
    [InlineData(25.0, false)]
    [InlineData(8.1, false)]
    public void A_warm_excursion_breaches_the_hard_limit_when_it_goes_above_twenty_five(double extreme, bool breaches) {
        // Arrange
        Excursion excursion = Warm((decimal)extreme, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(breaches, excursion.BreachesHardLimit);
    }

    [Fact]
    public void An_excursion_remembers_whether_the_record_ended_in_it() {
        // Act
        Excursion open   = Warm(9m, TimeSpan.FromMinutes(10), isOpen: true);
        Excursion closed = Warm(9m, TimeSpan.FromMinutes(10), isOpen: false);

        // Assert
        Assert.True(open.IsOpen);
        Assert.False(closed.IsOpen);
        Assert.Contains("(open)", open.ToString());
        Assert.DoesNotContain("(open)", closed.ToString());
    }

    [Fact]
    public void Excursions_that_agree_on_everything_are_equal() {
        // Arrange
        Excursion anyExcursion = new AnyExcursion().Generate();

        // Act
        Excursion same = new Excursion(anyExcursion.Kind, anyExcursion.LabelledRange, anyExcursion.StartedAt, anyExcursion.EndedAt, anyExcursion.ExtremeCelsius, anyExcursion.IsOpen);

        // Assert
        Assert.Equal(anyExcursion, same);
        Assert.Equal(anyExcursion.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Excursions_that_differ_by_one_field_are_not_equal() {
        // Arrange
        Excursion excursion = Warm(9m, TimeSpan.FromMinutes(10));

        // Assert
        Assert.NotEqual(excursion, Warm(9.1m, TimeSpan.FromMinutes(10)));
        Assert.NotEqual(excursion, Warm(9m, TimeSpan.FromMinutes(11)));
        Assert.NotEqual(excursion, Warm(9m, TimeSpan.FromMinutes(10), isOpen: true));
        Assert.False(excursion.Equals(null));
    }

}
